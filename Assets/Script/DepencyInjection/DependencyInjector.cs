using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DependencyInjector : MonoBehaviour
{
    private List<Type> m_globalServiceTypes = new List<Type>();
    private Dictionary<Type, Component> m_globalServices = new Dictionary<Type, Component>();
    
    private void Awake()
    {
        GetGlobalServiceTypes();
        
        CreateGlobalServices();
        
        GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        
        // GetMultipleServicesFromRoots(rootGameObjects);
        
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
            GameObject go = new GameObject();
            
            DontDestroyOnLoad(go);
            go.name = singleServiceType.Name;
            
            Component createdService = go.AddComponent(singleServiceType);
            
            m_globalServices.TryAdd(singleServiceType, createdService);
        }
    }

    private void GetMultipleServicesFromRoots(GameObject[] rootGameObjects)
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
                m_globalServices.TryAdd(componentType, component);
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
                    m_globalServices.TryGetValue(fieldInfo.FieldType, out Component foundService);

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
