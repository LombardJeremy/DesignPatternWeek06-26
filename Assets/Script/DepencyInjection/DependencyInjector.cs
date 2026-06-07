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
        List<GameObject> rootGameObjects = new List<GameObject>();
        
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            
            rootGameObjects.AddRange(scene.GetRootGameObjects());
        }
        
        // Dummy object to access game objects already in don't destroy on load scene
        GameObject ddolDummy =  new GameObject("DependencyInjectorDDOLDummy");
        DontDestroyOnLoad(ddolDummy);
        rootGameObjects.AddRange(ddolDummy.scene.GetRootGameObjects());
        Destroy(ddolDummy);
        
        // Create global services that should be instantiated
        CreateGlobalServicesNotInScene(rootGameObjects);
        
        // Do injection
        foreach (GameObject rootGameObject in rootGameObjects)
        {
            InjectDependenciesFromRoot(rootGameObject, rootGameObjects);
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
    
    private void CreateGlobalServicesNotInScene(List<GameObject> rootGameObjects)
    {
        foreach (Type serviceType in m_globalServiceTypes)
        {
            ServiceAttribute serviceAttribute = serviceType.GetCustomAttribute<ServiceAttribute>();
            if(serviceAttribute == null) continue;

            // For global service that should be instantiated by the injector
            Type singleServiceType = serviceType;
            GameObject go = new GameObject(singleServiceType.Name);
        
            DontDestroyOnLoad(go);
            rootGameObjects.Add(go);
            
            Component createdService = go.AddComponent(singleServiceType);
        
            m_globalServices.TryAdd(singleServiceType, createdService);
        }
    }
    #endregion

    #region Injection

    private void InjectDependenciesFromRoot(GameObject inGameObject, List<GameObject> rootGameObjects)
    {
        foreach (MonoBehaviour component in inGameObject.GetComponents<MonoBehaviour>())
        {
            Type componentType = component.GetType();
            
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (FieldInfo fieldInfo in componentType.GetFields(flags))
            {
                if (!fieldInfo.HasAttribute<DependencyInjectionAttribute>()) continue;

                MonoBehaviour service = ResolveService(inGameObject, fieldInfo.FieldType, rootGameObjects);
                
                if(service == null)
                {
                    Debug.LogError("Couldn't find service for " + fieldInfo.Name);
                    continue;
                }
                
                fieldInfo.SetValue(component, service);
            }
        }
        
        foreach (Transform childTransform in inGameObject.transform) InjectDependenciesFromRoot(childTransform.gameObject, rootGameObjects);
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
        return ResolveSceneLocalService(rootGameObjects, serviceType);
    }

    private MonoBehaviour ResolveKinLocalService(GameObject inGameObject, Type serviceType)
    {
        // I - Search in same game object for injector component with Game object scope 
        List<InjectionScope> searchValidScopes = new List<InjectionScope>
        {
            InjectionScope.GameObject, 
            InjectionScope.Children, // I decided that Children scope also provide service to its own game object
            InjectionScope.Parents, // Same as children scope. (Same behavior as InitArgs)
            InjectionScope.Scene
        };

        MonoBehaviour serviceFound = FindLocalServiceInGameObject(inGameObject, serviceType, searchValidScopes);
                
        if(serviceFound != null) return serviceFound;
                
        // II - Else start search in kin
                
        int parentDistance = 0, childDistance  = 0; 
        List<InjectionScope> parentScopes = new List<InjectionScope>{ InjectionScope.Children, InjectionScope.Scene };
        List<InjectionScope> childScopes = new List<InjectionScope> { InjectionScope.Parents, InjectionScope.Scene };
        
        MonoBehaviour parentService = null;
        if (inGameObject.transform.parent != null)
        {
            parentService = FindFirstLocalServiceInParentGameObjects(inGameObject.transform.parent.gameObject, serviceType, parentScopes, ref parentDistance);
        }
                
        // Start search in children for injector component with Parent scope
        MonoBehaviour childService = null;
        if (inGameObject.transform.childCount != 0)
        {
            childService = FindFirstLocalServiceInChildGameObjectsFromRoot(inGameObject, serviceType, childScopes, ref childDistance);
        }

        return PickClosest(parentService, parentDistance, childService, childDistance);
    }


    private MonoBehaviour FindLocalServiceInGameObject(GameObject inGameObject, Type serviceTypeToFind,
        List<InjectionScope> validScopes)
    {
        foreach (MonoBehaviour otherComponent in inGameObject.GetComponents<MonoBehaviour>())
        {
            if (otherComponent is LocalServiceProvider localServiceProvider && validScopes.Contains(localServiceProvider.InjectionScope))
            {
                MonoBehaviour serviceFound = localServiceProvider.ServiceComponents.Find((service) =>
                {
                    if(service == null) return false;
                    
                    return service.GetType() == serviceTypeToFind;
                });

                if (serviceFound == null) continue;
                            
                return serviceFound;
            }
        }
        
        return null;
    }

    private MonoBehaviour FindFirstLocalServiceInParentGameObjects(GameObject inGameObject, Type serviceTypeToFind,  List<InjectionScope> validScopes, ref int distance)
    {
        distance ++;

        MonoBehaviour serviceFound = FindLocalServiceInGameObject(inGameObject, serviceTypeToFind, validScopes);
        
        if(serviceFound != null) return serviceFound;

        if (inGameObject.transform.parent == null)
        {
            return null;
        }
        
        return FindFirstLocalServiceInParentGameObjects(inGameObject.transform.parent.gameObject, serviceTypeToFind, validScopes, ref distance);
    }

    private MonoBehaviour FindFirstLocalServiceInChildGameObjects(GameObject inGameObject, Type serviceTypeToFind, List<InjectionScope> validScopes, ref int distance)
    {
        distance ++;
        
        MonoBehaviour serviceFound = FindLocalServiceInGameObject(inGameObject, serviceTypeToFind, validScopes);

        if(serviceFound != null) return serviceFound;
        
        if (inGameObject.transform.childCount == 0) return null;
        
        return FindFirstLocalServiceInChildGameObjectsFromRoot(inGameObject, serviceTypeToFind, validScopes, ref distance);
    }
    
    private MonoBehaviour FindFirstLocalServiceInChildGameObjectsFromRoot(GameObject root, Type serviceTypeToFind, List<InjectionScope> validScopes, ref int distance)
    {
        MonoBehaviour serviceFoundInChildren = null;
        int closestDistance = -1;
        
        foreach (Transform childTransform in root.transform)
        {
            int localDistance = distance;
            
            MonoBehaviour tempFoundService = FindFirstLocalServiceInChildGameObjects(childTransform.gameObject, serviceTypeToFind, validScopes, ref localDistance);
            
            if(tempFoundService == null || (localDistance >= closestDistance && closestDistance != -1)) continue;
            
            closestDistance = localDistance;
            serviceFoundInChildren = tempFoundService;
            
            // Since distance can't be less than zero, we stop prematurely because we won't find an object 'closer'.
            if(closestDistance == 0) return serviceFoundInChildren;
        }
        
        if(closestDistance != -1) distance = closestDistance;
        return serviceFoundInChildren;
    }
    
    
    private MonoBehaviour ResolveSceneLocalService(List<GameObject> rootGameObjects, Type serviceTypeToFind)
    {
        MonoBehaviour serviceFound = null;
        List<InjectionScope> validScopes = new List<InjectionScope>{InjectionScope.Scene};
        int closestDistance = -1;
        
        foreach (GameObject rootGameObject in rootGameObjects)
        {
            int localDistance = -1;
            MonoBehaviour tempFoundService = FindFirstLocalServiceInChildGameObjects(rootGameObject, serviceTypeToFind, validScopes, ref localDistance);
            
            if(tempFoundService == null || (localDistance >= closestDistance && closestDistance != -1)) continue;
            
            closestDistance = localDistance;
            serviceFound = tempFoundService;
            
            // Since distance can't be less than zero, we stop prematurely because we won't find an object 'closer'.
            if(closestDistance == 0) return serviceFound;
        }
        
        return serviceFound;
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
