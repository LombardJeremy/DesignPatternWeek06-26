using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DependencyInjector : MonoBehaviour
{
    private List<string> NonUserAssemblyPrefixes = new List<string>()
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
    
    private List<Type> m_globalServiceTypes = new List<Type>();
    private Dictionary<Type, Component> m_globalServices = new Dictionary<Type, Component>();
    
    private List<LocalServiceProvider> m_injectorComponents = new List<LocalServiceProvider>();
    
    private void Awake()
    {
        // Get all services types (used in certain places to simplify some algorithms)
        GetGlobalServiceTypes();
        
        // Get all root objects
        List<GameObject> roots = new List<GameObject>();
        
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            
            roots.AddRange(scene.GetRootGameObjects());
        }
        
        // Dummy object to access game objects already in don't destroy on load scene
        GameObject ddolDummy =  new GameObject("DependencyInjectorDDOLDummy");
        DontDestroyOnLoad(ddolDummy);
        roots.AddRange(ddolDummy.scene.GetRootGameObjects());
        Destroy(ddolDummy);
        
        // Create global services that should be instantiated
        CreateGlobalServicesNotInScene(roots);
        
        // Do injection
        foreach (GameObject rootGameObject in roots)
        {
            InjectDependenciesFromRoot(rootGameObject, roots);
        }
    }
    #region Services Search And Creation

    private void GetGlobalServiceTypes()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly assembly in assemblies)
        {
            bool nonUserAssembly = NonUserAssemblyPrefixes.Any(prefix => assembly.GetName().Name.StartsWith(prefix));

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
    
    private void CreateGlobalServicesNotInScene(List<GameObject> roots)
    {
        foreach (Type serviceType in m_globalServiceTypes)
        {
            ServiceAttribute serviceAttribute = serviceType.GetCustomAttribute<ServiceAttribute>();
            if(serviceAttribute == null) continue;

            // For global service that should be instantiated by the injector
            Type singleServiceType = serviceType;
            GameObject go = new GameObject(singleServiceType.Name);
        
            DontDestroyOnLoad(go);
            roots.Add(go);
            
            Component createdService = go.AddComponent(singleServiceType);
        
            m_globalServices.TryAdd(singleServiceType, createdService);
        }
    }
    #endregion

    #region Injection

    private void InjectDependenciesFromRoot(GameObject go, List<GameObject> roots)
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
        
        foreach (Transform childTransform in go.transform) InjectDependenciesFromRoot(childTransform.gameObject, roots);
    }

    private MonoBehaviour ResolveService(GameObject inGameObject, Type serviceType, List<GameObject> rootGameObjects)
    {
        // I - Try global service injection first

        if (m_globalServices.TryGetValue(serviceType, out Component globalServiceFound) && globalServiceFound != null)
            return globalServiceFound as MonoBehaviour;
                
        // II - Then local service injection
                
        // Kin scope (same go, parent/child)
        MonoBehaviour directParencyService = ResolveKinLocalService(inGameObject, serviceType);
        if (directParencyService != null) return directParencyService;
        
        // Scene scope
        return ResolveSceneLocalServiceFromRoots(rootGameObjects, serviceType, new List<InjectionScope> { InjectionScope.Scene });
    }

    private MonoBehaviour ResolveKinLocalService(GameObject go, Type serviceType)
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


    private MonoBehaviour FindLocalServiceInGameObject(GameObject go, Type serviceType,
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

    private MonoBehaviour FindLocalServiceInAncestors(GameObject go, Type serviceType,  List<InjectionScope> validScopes, ref int distance)
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
    
    private MonoBehaviour FindLocalServiceInDescendants(GameObject root, Type serviceType, List<InjectionScope> validScopes, ref int distance)
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
    
    
    private MonoBehaviour ResolveSceneLocalServiceFromRoots(List<GameObject> roots, Type serviceType, List<InjectionScope> validScopes)
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
    
    private static MonoBehaviour PickClosest(MonoBehaviour a, int distA, MonoBehaviour b,
        int distB)
    {
        if (a == null) return b;
        if(b == null) return a;
        return distA <= distB ? a : b;
    }
}
