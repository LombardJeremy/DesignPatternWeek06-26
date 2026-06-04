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
        GetGlobalServiceTypes();
        
        CreateGlobalServices();
        
        List<GameObject> rootGameObjects = new List<GameObject>();
        
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            
            rootGameObjects.AddRange(scene.GetRootGameObjects());
        }
        
        GameObject ddolDummy =  new GameObject("DependencyInjectorDDOLDummy");
        DontDestroyOnLoad(ddolDummy);
        rootGameObjects.AddRange(ddolDummy.scene.GetRootGameObjects());
        Destroy(ddolDummy);
        
        GetServiceComponentsFromRoots(rootGameObjects);
        
        InjectDependenciesFromRoots(rootGameObjects);
    }

    #region Services Search
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

    private void CreateGlobalServices()
    {
        foreach (Type serviceType in m_globalServiceTypes)
        {
            ServiceAttribute serviceAttribute = serviceType.GetCustomAttribute<ServiceAttribute>();
            if(serviceAttribute == null) continue;
            
            Type singleServiceType = serviceType;
            GameObject go = new GameObject(singleServiceType.Name);
            
            DontDestroyOnLoad(go);
            
            Component createdService = go.AddComponent(singleServiceType);
            
            m_globalServices.TryAdd(singleServiceType, createdService);
        }
    }

    private void GetServiceComponentsFromRoots(List<GameObject> rootGameObjects)
    {
        foreach (GameObject rootGameObject in rootGameObjects)
        {
            CheckGameObjectForServiceComponents(rootGameObject);
        }
    }

    private void CheckGameObjectForServiceComponents(GameObject inGameObject)
    {
        foreach (MonoBehaviour component in inGameObject.GetComponents<MonoBehaviour>())
        {
            if (component is DependencyInjectorComponent dependencyInjector)
            {
                m_injectorComponents.Add(dependencyInjector);
            }
        }

        foreach (Transform childTransform in inGameObject.transform) CheckGameObjectForServiceComponents(childTransform.gameObject);
    }
    #endregion

    #region Injection
    private void InjectDependenciesFromRoots(List<GameObject> rootGameObjects)
    {
        foreach (GameObject rootGameObject in rootGameObjects)
        {
            InjectDependencies(rootGameObject);
        }
    }

    private void InjectDependencies(GameObject inGameObject)
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
        
        foreach (Transform childTransform in inGameObject.transform) InjectDependencies(childTransform.gameObject);
    }
    #endregion
}
