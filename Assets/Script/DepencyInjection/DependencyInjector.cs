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
    [SerializeField] private List<Service> m_services = new List<Service>();
    
    private void Awake()
    {
        InjectDependenciesFromRoots();
    }

    private void InjectDependenciesFromRoots()
    {
        GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject rootGameObject in rootGameObjects)
        {
            InjectDependencies(rootGameObject);
        }
    }

    [Button]
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
                    Service foundService = m_services.Find(service => service.GetType() == fieldInfo.FieldType);

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
