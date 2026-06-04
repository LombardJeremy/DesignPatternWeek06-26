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
    
    private List<DependencyInjectorComponent> m_injectorComponents = new List<DependencyInjectorComponent>();
    
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
            CheckForServicesInGameObject(rootGameObject);
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

    private void CheckForServicesInGameObject(GameObject inGameObject)
    {
        foreach (MonoBehaviour component in inGameObject.GetComponents<MonoBehaviour>())
        {
            // Check if component is a 'local' service with a scope
            if (component is DependencyInjectorComponent dependencyInjector)
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

        foreach (Transform childTransform in inGameObject.transform) CheckForServicesInGameObject(childTransform.gameObject);
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

                    if (m_globalServices.TryGetValue(fieldInfo.FieldType, out Component foundService))
                    {
                        if (foundService != null)
                        {
                            fieldInfo.SetValue(component, foundService);
                        }  
                        
                        continue;
                    }
                    
                    
                }
            }
        }
        
        foreach (Transform childTransform in inGameObject.transform) InjectDependenciesFromRoot(childTransform.gameObject);
    }
    #endregion
}
