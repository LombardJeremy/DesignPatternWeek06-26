using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class DependencyInjector
{
    private static List<string> m_nonUserAssemblyPrefixes = new List<string>()
    {
        "Unity",
        "unity",
        "System",
        "mscorlib", 
        "JetBrains",
        "netstandard",
        "log4net",
        "Mono",
        "I18N"
    };
    
    private static List<Type> m_globalServiceTypes = new List<Type>();
    private static Dictionary<Type, MonoBehaviour> m_globalServices = new Dictionary<Type, MonoBehaviour>();

    private static bool m_initialized = false; 
    private static bool m_createdGlobalServices = false;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if(!m_initialized)
        {
            // Get all services types (used in certain places to simplify some algorithms)
            GetGlobalServiceTypes();

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            
            m_initialized =  true;
        }
        
        m_createdGlobalServices = false;
        m_globalServices.Clear();
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }
    
    private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (!m_createdGlobalServices)
        {
            // Create global services for the first time
            CreateGlobalServicesNotInScene();

            GlobalServiceToGlobalServiceInjection();

            m_createdGlobalServices = true;
        }
        
        // Get all root objects
        List<GameObject> roots = new List<GameObject>();
        
        roots.AddRange(scene.GetRootGameObjects());
        
        // Do injection
        foreach (GameObject root in roots)
        {
            InjectDependenciesInGameObject(root, roots);
        }
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        
    }

    #region Services Search And Creation

    private static void GetGlobalServiceTypes()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly assembly in assemblies)
        {
            bool nonUserAssembly = m_nonUserAssemblyPrefixes.Any(prefix => assembly.GetName().Name.StartsWith(prefix));

            if (nonUserAssembly) continue;
            
            foreach (TypeInfo typeInfo in assembly.DefinedTypes)
            {
                if (typeInfo.HasAttribute<ServiceAttribute>())
                {
                    m_globalServiceTypes.Add(typeInfo.AsType());
                }
            }
        }
    }
    
    private static void CreateGlobalServicesNotInScene()
    {
        foreach (Type serviceType in m_globalServiceTypes)
        {
            if (typeof(MonoBehaviour).IsAssignableFrom(serviceType))
            {
                ServiceAttribute serviceAttribute = serviceType.GetCustomAttribute<ServiceAttribute>();
                if(serviceAttribute == null) continue;

                // For global service that should be instantiated by the injector
                Type singleServiceType = serviceType;
                GameObject go = new GameObject(singleServiceType.Name);
        
                Object.DontDestroyOnLoad(go);
            
                MonoBehaviour createdService = go.AddComponent(singleServiceType) as MonoBehaviour;
        
                m_globalServices.TryAdd(singleServiceType, createdService);
                continue;
            }
            
            Debug.LogError("Type " + serviceType.Name + " is not supported for global service use. Don't use the 'Service' attribute for this type.");
        }
    }
    #endregion

    #region Injection

    private static void GlobalServiceToGlobalServiceInjection()
    {
        foreach (var (serviceType, service) in m_globalServices)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (FieldInfo fieldInfo in serviceType.GetFields(flags))
            {
                if (!fieldInfo.HasAttribute<DependencyInjectionAttribute>()) continue;
                
                MonoBehaviour foundService = ResolveGlobalService(fieldInfo.FieldType);
                
                if(foundService == null)
                {
                    Debug.LogError("Global service to global service injection failed. Couldn't find service for " + fieldInfo.Name + " in " + serviceType.Name);
                    continue;
                }
                
                fieldInfo.SetValue(service, foundService);
            }
        }
    }
    
    private static void InjectDependenciesInGameObject(GameObject go, List<GameObject> roots)
    {
        foreach (MonoBehaviour component in go.GetComponents<MonoBehaviour>())
        {
            Type componentType = component.GetType();
            
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (FieldInfo fieldInfo in componentType.GetFields(flags))
            {
                if (!fieldInfo.HasAttribute<DependencyInjectionAttribute>()) continue;

                MonoBehaviour service = ResolveService(go, fieldInfo.FieldType, roots);
                
                if(service == null)
                {
                    Debug.LogError("Couldn't find service for " + fieldInfo.Name + " in " + go.name);
                    continue;
                }
                
                fieldInfo.SetValue(component, service);
            }
        }
        
        foreach (Transform childTransform in go.transform) InjectDependenciesInGameObject(childTransform.gameObject, roots);
    }

    private static MonoBehaviour ResolveService(GameObject inGameObject, Type serviceType, List<GameObject> rootGameObjects)
    {
        // I - Try global service injection first
        MonoBehaviour globalService = ResolveGlobalService(serviceType);
        if (globalService != null) return globalService;
                
        // II - Then local service injection
        // Kin scope (same go, parent/child)
        MonoBehaviour kinService = ResolveKinLocalService(inGameObject, serviceType);
        if (kinService != null) return kinService;
        
        // Scene scope
        return ResolveSceneLocalServiceFromRoots(rootGameObjects, serviceType, new List<InjectionScope> { InjectionScope.Scene });
    }

    private static MonoBehaviour ResolveGlobalService(Type serviceType)
    {
        m_globalServices.TryGetValue(serviceType, out MonoBehaviour globalServiceFound);

        return globalServiceFound;
    }

    private static MonoBehaviour ResolveKinLocalService(GameObject go, Type serviceType)
    {
        // I - Search in same game object for injector component with Game object scope 
        List<InjectionScope> searchValidScopes = new List<InjectionScope>
        {
            InjectionScope.GameObject, 
            InjectionScope.Children, // I decided that Children scope also provide service to its own game object
            InjectionScope.Parents, // Same as children scope. (Same behavior as InitArgs)
            InjectionScope.Scene
        };

        MonoBehaviour serviceFound = FindLocalServiceInGameObject(go, serviceType, searchValidScopes);
                
        if(serviceFound != null) return serviceFound;
                
        // II - Else start search in kin
                
        int parentDistance = 0, childDistance  = 0; 
        List<InjectionScope> parentScopes = new List<InjectionScope>{ InjectionScope.Children, InjectionScope.Scene };
        List<InjectionScope> childScopes = new List<InjectionScope> { InjectionScope.Parents, InjectionScope.Scene };
        
        MonoBehaviour parentService = null;
        if (go.transform.parent != null)
        {
            parentService = FindLocalServiceInAncestors(go.transform.parent.gameObject, serviceType, parentScopes, ref parentDistance);
        }
                
        // Start search in children for injector component with Parent scope
        MonoBehaviour childService = null;
        if (go.transform.childCount != 0)
        {
            childService = FindLocalServiceInDescendants(go, serviceType, childScopes, ref childDistance);
        }

        return PickClosest(parentService, parentDistance, childService, childDistance);
    }


    private static MonoBehaviour FindLocalServiceInGameObject(GameObject go, Type serviceType,
        List<InjectionScope> validScopes)
    {
        foreach (MonoBehaviour otherComponent in go.GetComponents<MonoBehaviour>())
        {
            if (otherComponent is LocalServiceProvider localServiceProvider && validScopes.Contains(localServiceProvider.InjectionScope))
            {
                MonoBehaviour serviceFound = localServiceProvider.ServiceComponents.Find((service) =>
                {
                    return service != null && service.GetType() == serviceType;
                });

                if (serviceFound != null) return serviceFound;
            }
        }
        
        return null;
    }

    private static MonoBehaviour  FindLocalServiceInAncestors(GameObject go, Type serviceType,  List<InjectionScope> validScopes, ref int distance)
    {
        distance ++;

        MonoBehaviour serviceFound = FindLocalServiceInGameObject(go, serviceType, validScopes);
        
        if(serviceFound != null) return serviceFound;

        if (go.transform.parent == null)
        {
            return null;
        }
        
        return FindLocalServiceInAncestors(go.transform.parent.gameObject, serviceType, validScopes, ref distance);
    }
    
    private static MonoBehaviour FindLocalServiceInDescendants(GameObject root, Type serviceType, List<InjectionScope> validScopes, ref int distance)
    {
        MonoBehaviour closestService = null;
        int closestDistance = -1;
        
        foreach (Transform child in root.transform)
        {
            int localDistance = distance + 1;
            
            MonoBehaviour tempFound = FindLocalServiceInGameObject(child.gameObject, serviceType, validScopes);
            
            if(tempFound == null && child.childCount > 0) 
                tempFound = FindLocalServiceInDescendants(child.gameObject, serviceType, validScopes, ref localDistance);
            
            if(tempFound != null && (closestDistance == -1 || localDistance < closestDistance ))
            {
                closestDistance = localDistance;
                closestService = tempFound;
            }
        }
        
        if(closestDistance != -1) distance = closestDistance;
        return closestService;
    }
    
    
    private static MonoBehaviour ResolveSceneLocalServiceFromRoots(List<GameObject> roots, Type serviceType, List<InjectionScope> validScopes)
    {
        MonoBehaviour closestService = null;
        int closestDistance = -1;
        
        foreach (GameObject root in roots)
        {
            MonoBehaviour tempFoundService = FindLocalServiceInGameObject(root, serviceType, validScopes);
            
            // If we find a service in a root,
            // we return directly since there will be no closer hierarchical distance
            if (tempFoundService != null)
                return tempFoundService;
            
            int localDistance = 0;
            tempFoundService = FindLocalServiceInDescendants(root, serviceType, validScopes, ref localDistance);
            
            if(tempFoundService != null && (closestDistance == -1 || localDistance < closestDistance))
            {
                closestDistance = localDistance;
                closestService = tempFoundService;
            }
        }
        
        return closestService;
    }
   
    #endregion
    
    #region Utilities
    
    private static MonoBehaviour PickClosest(MonoBehaviour a, int distA, MonoBehaviour b,
        int distB)
    {
        if (a == null) return b;
        if(b == null) return a;
        return distA <= distB ? a : b;
    }
    
    #endregion
}
