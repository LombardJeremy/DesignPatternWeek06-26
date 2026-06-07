using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
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
    private static Dictionary<Type, object> m_globalServices = new Dictionary<Type, object>();

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
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); // We have to call it manually the first time
    }
    
    private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (!m_createdGlobalServices)
        {
            // Create/Get global services for the first time
            CreateAndGetGlobalServices();

            GlobalServiceToGlobalServiceInjection();

            m_createdGlobalServices = true;
        }
        
        // Do injection
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            InjectDependenciesInGameObject(root, scene.GetRootGameObjects());
        }
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        
    }

    #region Global Services Creation

    private static void GetGlobalServiceTypes()
    {
        m_globalServiceTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !m_nonUserAssemblyPrefixes.Any(prefix => assembly.GetName().Name.StartsWith(prefix)))
            .SelectMany(assembly => assembly.DefinedTypes)
            .Where(typeInfo => typeInfo.HasAttribute<ServiceAttribute>())
            .Select(typeInfo => typeInfo.AsType())
            .ToList();
    }
    
    private static void CreateAndGetGlobalServices()
    {
        string[] soGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
        Dictionary<Type, List<ScriptableObject>> scriptableObjects = new Dictionary<Type, List<ScriptableObject>>();
        foreach (string guid in soGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            
            if(!scriptableObjects.ContainsKey(so.GetType()))
                scriptableObjects[so.GetType()] = new List<ScriptableObject>();
            
            scriptableObjects[so.GetType()].Add(so);
        }
        
        foreach (Type serviceType in m_globalServiceTypes)
        {
            ServiceAttribute serviceAttribute = serviceType.GetCustomAttribute<ServiceAttribute>();
            if(serviceAttribute == null) continue;
            
            if (typeof(MonoBehaviour).IsAssignableFrom(serviceType))
            {
                GameObject go = new GameObject(serviceType.Name);
        
                Object.DontDestroyOnLoad(go);
            
                MonoBehaviour createdService = go.AddComponent(serviceType) as MonoBehaviour;
        
                m_globalServices.TryAdd(serviceType, createdService);
                continue;
            }
            
            // Custom class global service
            if(!typeof(Object).IsAssignableFrom(serviceType))
            {
                object createdService = Activator.CreateInstance(serviceType);
                
                m_globalServices.TryAdd(serviceType, createdService);
                continue;
            }

            if (typeof(ScriptableObject).IsAssignableFrom(serviceType)
                && scriptableObjects.TryGetValue(serviceType, out List<ScriptableObject> soServicesInstances))
            {

                if (soServicesInstances.Count == 0)
                {
                    Debug.LogError($"ScriptableObject '{serviceType.Name}' is marked as 'Service', but no instance is present in the 'Assets' folder.");
                }
                else
                {
                    m_globalServices.TryAdd(serviceType, soServicesInstances[0]);
                }
                
                if(soServicesInstances.Count > 1)
                    Debug.LogWarning($"Found more than one instance of '{serviceType.Name}' ScriptableObject Service");
                
                continue;
            }
            
            Debug.LogError($"'{serviceType.Name}' is not supported for global service use. Don't use the 'Service' attribute for this type.");
        }
    }
    #endregion

    #region Injection

    private static void GlobalServiceToGlobalServiceInjection()
    {
        foreach (var (receivingServiceType, receivingService) in m_globalServices)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            foreach (FieldInfo fieldInfo in receivingServiceType.GetFields(flags))
            {
                if (!fieldInfo.HasAttribute<DependencyInjectionAttribute>()) continue;
                
                object foundService = ResolveGlobalService(fieldInfo.FieldType);
                
                if(foundService == null)
                {
                    Debug.LogError($"Global service to global service injection failed. Couldn't resolve field: {receivingServiceType.Name}.{fieldInfo.Name}");
                    continue;
                }
                
                fieldInfo.SetValue(receivingService, foundService);
            }
            
            // foreach (MethodInfo methodInfo in receivingServiceType.GetMethods(flags))
            // {
            //     if (!methodInfo.HasAttribute<DependencyInjectionAttribute>()) continue;
            //     
            //     object[] parameters = methodInfo.GetParameters()
            //         .Select(p => ResolveGlobalService(p.ParameterType))
            //         .ToArray();
            //     
            //     if (parameters.Any(p => p == null))
            //     {
            //         Debug.LogError($"Global service to global service injection failed. Couldn't resolve one or more parameter of method: {receivingServiceType.Name}.{methodInfo.Name}");
            //         continue;
            //     }
            //     
            //     methodInfo.Invoke(receivingService, parameters);
            // }
        }
    }
    
    private static void InjectDependenciesInGameObject(GameObject go, GameObject[] roots)
    {
        foreach (MonoBehaviour component in go.GetComponents<MonoBehaviour>())
        {
            Type componentType = component.GetType();
            
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (FieldInfo fieldInfo in componentType.GetFields(flags))
            {
                if (!fieldInfo.HasAttribute<DependencyInjectionAttribute>()) continue;

                object service = ResolveService(go, fieldInfo.FieldType, roots);
                
                if(service == null)
                {
                    Debug.LogError($"Injection failed. Couldn't resolve field: {go.name}.{fieldInfo.Name}");
                    continue;
                }
                
                fieldInfo.SetValue(component, service);
            }
        }
        
        foreach (Transform childTransform in go.transform) InjectDependenciesInGameObject(childTransform.gameObject, roots);
    }

    private static object ResolveService(GameObject inGameObject, Type serviceType, GameObject[]  rootGameObjects)
    {
        // I - Try global service injection first
        object globalService = ResolveGlobalService(serviceType);
        if (globalService != null) return globalService;
                
        // II - Then local service injection
        // Kin scope (same go, parent/child)
        MonoBehaviour kinService = ResolveKinLocalService(inGameObject, serviceType);
        if (kinService != null) return kinService;
        
        // Scene scope
        return ResolveSceneLocalServiceFromRoots(rootGameObjects, serviceType, new List<InjectionScope> { InjectionScope.Scene });
    }

    private static object ResolveGlobalService(Type serviceType)
    {
        m_globalServices.TryGetValue(serviceType, out object globalServiceFound);

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
        return go.GetComponents<MonoBehaviour>()
            .OfType<LocalServiceProvider>()
            .Where(provider => validScopes.Contains(provider.InjectionScope))
            .SelectMany(provider => provider.ServiceComponents)
            .FirstOrDefault(service => service != null && service.GetType() == serviceType);
    }

    private static MonoBehaviour  FindLocalServiceInAncestors(GameObject go, Type serviceType,  List<InjectionScope> validScopes, ref int distance)
    {
        distance ++;

        MonoBehaviour serviceFound = FindLocalServiceInGameObject(go, serviceType, validScopes);
        
        if(serviceFound != null) return serviceFound;

        if (go.transform.parent == null) return null;
        
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
    
    
    private static MonoBehaviour ResolveSceneLocalServiceFromRoots(GameObject[]  roots, Type serviceType, List<InjectionScope> validScopes)
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
