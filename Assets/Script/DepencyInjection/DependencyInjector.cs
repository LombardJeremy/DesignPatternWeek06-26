using System;
using System.Collections.Generic;
using System.Reflection;
using NaughtyAttributes;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DependencyInjector : MonoBehaviour
{
    private Dictionary<Type, MonoBehaviour> m_services = new Dictionary<Type, MonoBehaviour>();
    
    private void Awake()
    {
        GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        
        GetServicesFromRoots(rootGameObjects);
        
        InjectDependenciesFromRoots(rootGameObjects);
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
                    m_services.TryGetValue(fieldInfo.FieldType, out MonoBehaviour foundService);

                    if (foundService != null)
                    {
                        fieldInfo.SetValue(component, foundService);
                    }
                }
            }
        }
        
        foreach (Transform childTransform in inGameObject.transform) InjectDependencies(childTransform.gameObject);
    }
}
