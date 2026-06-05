using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DependencyInjector : MonoBehaviour
{
    private List<Type> m_globalServiceTypes = new List<Type>();
    private Dictionary<Type, Component> m_globalServices = new Dictionary<Type, Component>();
    
    private List<LocalServiceProvider> m_injectorComponents = new List<LocalServiceProvider>();
    
    private enum LocalServiceSearchStrategy
    {
        InParents,
        InChildren,
    }
    
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
        
        foreach (GameObject rootGameObject in rootGameObjects)
        {
            // Get services (global and local) already in scene
            CheckForGlobalServicesInGameObject(rootGameObject);
        }
        
        // Create global services that should be instantiated
        CreateGlobalServicesNotInScene(rootGameObjects);
        
        // Do injection
        foreach (GameObject rootGameObject in rootGameObjects)
        {
            InjectDependenciesFromRoot(rootGameObject);
        }
    }
    #region Services Search And Creation

    private void GetGlobalServiceTypes()
    {
        foreach (TypeInfo typeInfo in GetType().Assembly.DefinedTypes)
        {
            if (typeInfo.HasAttribute<ServiceAttribute>())
            {
                m_globalServiceTypes.Add(typeInfo.AsType());
            }
        }
    }

    private void CheckForGlobalServicesInGameObject(GameObject inGameObject)
    {
        foreach (MonoBehaviour component in inGameObject.GetComponents<MonoBehaviour>())
        {
            // Check if component is a 'local' service with a scope
            if (component is LocalServiceProvider dependencyInjector)
            {
                m_injectorComponents.Add(dependencyInjector);
                continue;
            }
            
            // If not check if the component is a global service
            ServiceAttribute serviceAttribute = component.GetType().GetCustomAttribute<ServiceAttribute>();
            if (serviceAttribute == null) continue;
            
            if ((serviceAttribute.Flags & ServiceFlags.Instantiate) == 0)
            {
                m_globalServices.TryAdd(component.GetType(), component);
            }
        }

        foreach (Transform childTransform in inGameObject.transform) CheckForGlobalServicesInGameObject(childTransform.gameObject);
    }
    
    private void CreateGlobalServicesNotInScene(List<GameObject> rootGameObjects)
    {
        foreach (Type serviceType in m_globalServiceTypes)
        {
            ServiceAttribute serviceAttribute = serviceType.GetCustomAttribute<ServiceAttribute>();
            if(serviceAttribute == null) continue;

            // Check if the service should be instantiated or is already in scene
            if ((serviceAttribute.Flags & ServiceFlags.Instantiate) == 0) continue;
            
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

    private void InjectDependenciesFromRoot(GameObject inGameObject)
    {
        foreach (MonoBehaviour component in inGameObject.GetComponents<MonoBehaviour>())
        {
            Type componentType = component.GetType();
            
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (FieldInfo fieldInfo in componentType.GetFields(flags))
            {
                if (fieldInfo.HasAttribute<DependencyInjectionAttribute>())
                {
                    // Try global service injection first

                    if (m_globalServices.TryGetValue(fieldInfo.FieldType, out Component globalServiceFound))
                    {
                        if (globalServiceFound != null)
                        {
                            fieldInfo.SetValue(component, globalServiceFound);
                        }  
                        
                        continue;
                    }
                    
                    // Then do local injection
                    
                    // I - Search in same game object for injector component with Game object scope 

                    bool foundNeededService = false;
                    foreach (MonoBehaviour otherComponent in component.gameObject.GetComponents<MonoBehaviour>())
                    {
                        if (otherComponent is LocalServiceProvider { InjectionScope: InjectionScope.GameObject } localServiceProvider)
                        {
                            MonoBehaviour serviceFound = localServiceProvider.ServiceComponents.Find((service) => service.GetType() == fieldInfo.FieldType);

                            if (serviceFound == null) continue;
                            
                            fieldInfo.SetValue(component, serviceFound);
                            foundNeededService = true;
                            break;
                        }
                    }
                    
                    if(foundNeededService) break;
                    
                    // II - Else start search in the hierarchy
                    // Start search in parents for injector component with children scope
                    
                    // 0 will be considered default value, meaning if it's still 0 after this step, the game object has no parent.
                    // So there is no local service in parents.
                    int parentDistance = 0; 
                    MonoBehaviour serviceFoundInParent = null;
                    if (inGameObject.transform.parent != null)
                    {
                        serviceFoundInParent = FindFirstLocalServicesInParentGameObjects(inGameObject.transform.parent.gameObject, fieldInfo.FieldType, ref parentDistance);
                    }
                    
                    if (serviceFoundInParent != null)
                    {
                        fieldInfo.SetValue(component, serviceFoundInParent);
                        foundNeededService = true;
                    }
                    
                    // Start search in children for injector component with Parent scope
                    int childDistance = 0; 
                    MonoBehaviour serviceFoundInChildren = null;
                    if (inGameObject.transform.childCount != 0)
                    {
                        serviceFoundInChildren = FindFirstLocalServicesInChildrenGameObjectsFromRoot(inGameObject, fieldInfo.FieldType, ref childDistance);
                    }

                    if (serviceFoundInChildren != null)
                    {
                        fieldInfo.SetValue(component, serviceFoundInChildren);
                        foundNeededService = true;
                    }
                    
                    
                    
                    // If found, check distance and use closest
                    if(foundNeededService)
                    {
                        if(serviceFoundInParent == null) fieldInfo.SetValue(component, serviceFoundInChildren);
                        else if(serviceFoundInChildren == null) fieldInfo.SetValue(component, serviceFoundInParent);
                        else if (parentDistance <= childDistance) fieldInfo.SetValue(component, serviceFoundInParent);
                        else fieldInfo.SetValue(component, serviceFoundInChildren);
                        
                        break;
                    }
                    
                    // III - Else search in from other roots for injector component with scene scope
                    
                    // Start from each root game object and return distance
                    // Keep the one with shortest distance
                    
                    
                    // IV - At this point we didn't find any, we just continue
                }
            }
        }
        
        foreach (Transform childTransform in inGameObject.transform) InjectDependenciesFromRoot(childTransform.gameObject);
    }

    private MonoBehaviour FindFirstLocalServicesInParentGameObjects(GameObject inGameObject, Type serviceTypeToFind, ref int distance)
    {
        distance ++;
        
        foreach (MonoBehaviour component in inGameObject.GetComponents<MonoBehaviour>())
        {
            if (component is not LocalServiceProvider localServiceProvider) continue;
            
            if(!(localServiceProvider.InjectionScope == InjectionScope.Children || localServiceProvider.InjectionScope == InjectionScope.Scene)) continue;
            
            MonoBehaviour foundService = localServiceProvider.ServiceComponents.Find((service) => service.GetType() == serviceTypeToFind);
            
            if(foundService == null) continue;
            
            return foundService;
        }

        if (inGameObject.transform.parent == null)
        {
            return null;
        }
        
        return FindFirstLocalServicesInParentGameObjects(inGameObject.transform.parent.gameObject, serviceTypeToFind, ref distance);
    }

    private MonoBehaviour FindFirstLocalServicesInChildrenGameObjectsFromRoot(GameObject root, Type serviceTypeToFind, ref int distance)
    {
        MonoBehaviour serviceFoundInChildren = null;
        
        int closestDistance = -1;
        foreach (Transform childTransform in root.transform)
        {
            int localDistance = distance;
            
            MonoBehaviour tempFoundService = FindFirstLocalServicesInChildGameObjects(childTransform.gameObject, serviceTypeToFind, ref localDistance);
            
            if(tempFoundService == null || (localDistance >= closestDistance && closestDistance != -1)) continue;
            
            closestDistance = localDistance;
            serviceFoundInChildren = tempFoundService;
        }
        
        if(closestDistance != -1) distance = closestDistance;
        return serviceFoundInChildren;
    }
    
    private MonoBehaviour FindFirstLocalServicesInChildGameObjects(GameObject inGameObject, Type serviceTypeToFind, ref int distance)
    {
        distance ++;
        
        foreach (MonoBehaviour component in inGameObject.GetComponents<MonoBehaviour>())
        {
            if (component is not LocalServiceProvider localServiceProvider) continue;
            
            if(!(localServiceProvider.InjectionScope == InjectionScope.Parents || localServiceProvider.InjectionScope == InjectionScope.Scene)) continue;
            
            MonoBehaviour serviceFound = localServiceProvider.ServiceComponents.Find((service) => service.GetType() == serviceTypeToFind);
            
            if(serviceFound == null) continue;
            
            return serviceFound;
        }

        
        if (inGameObject.transform.childCount == 0)
        {
            // Debug.LogWarning("Didn't find LocalService in children");
            return null;
        }

        MonoBehaviour serviceFoundInChildren = null;
        int closestDistance = -1;
        foreach (Transform childTransform in inGameObject.transform)
        {
            int localDistance = distance;
            
            MonoBehaviour tempFoundService = FindFirstLocalServicesInChildGameObjects(childTransform.gameObject, serviceTypeToFind, ref localDistance);
            
            if(tempFoundService == null || (localDistance >= closestDistance && closestDistance != -1)) continue;
            
            closestDistance = localDistance;
            serviceFoundInChildren = tempFoundService;
        }
        
        if(closestDistance != -1) distance = closestDistance;
        return serviceFoundInChildren;
    }
    #endregion
}
