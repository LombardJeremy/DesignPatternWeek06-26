using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DependencyInjector : MonoBehaviour
{
    private Dictionary<Type, Component> m_services = new Dictionary<Type, Component>();
    
    private void Awake()
    {
        CreateSingleServices();
        
        GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        
        //GetServicesFromRoots(rootGameObjects);
        
        InjectDependenciesFromRoots(rootGameObjects);
    }

    #region Services Search
    private void CreateSingleServices()
    {
        foreach (TypeInfo typeInfo in GetType().Assembly.DefinedTypes)
        {
            if (typeInfo.HasAttribute<ServiceAttribute>())
            {
                Type serviceType = typeInfo.AsType();
                GameObject go = new GameObject();
                go.name = typeInfo.Name;
                
                Component createdService = go.AddComponent(serviceType);
                
                m_services.TryAdd(serviceType, createdService);
            }
        }
    }

    private void GetServicesFromRoots(GameObject[] rootGameObjects)
    {
        foreach (GameObject rootGameObject in rootGameObjects)
        {
            CheckGameObjectForServices(rootGameObject);
        }
    }

    private void CheckGameObjectForServices(GameObject inGameObject)
    {
        foreach (MonoBehaviour component in inGameObject.GetComponents<MonoBehaviour>())
        {
            Type componentType = component.GetType();

            if (componentType.HasAttribute<ServiceAttribute>())
            {
                m_services.TryAdd(componentType, component);
            }
        }

        foreach (Transform childTransform in inGameObject.transform) CheckGameObjectForServices(childTransform.gameObject);
    }
    #endregion

    #region Injection
    private void InjectDependenciesFromRoots(GameObject[] rootGameObjects)
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
                    m_services.TryGetValue(fieldInfo.FieldType, out Component foundService);

                    if (foundService != null)
                    {
                        fieldInfo.SetValue(component, foundService);
                    }
                }
            }
        }
        
        foreach (Transform childTransform in inGameObject.transform) InjectDependencies(childTransform.gameObject);
    }
    #endregion
}
