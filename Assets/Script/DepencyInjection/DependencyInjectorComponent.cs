using System.Collections.Generic;
using UnityEngine;

public enum InjectionScope
{
    GameObject,
    Children,
    Parent,
    Scene
}

public class DependencyInjectorComponent : MonoBehaviour
{
    [SerializeField] private List<MonoBehaviour> m_serviceComponents = new List<MonoBehaviour>();
    [SerializeField] private InjectionScope m_injectionScope = InjectionScope.GameObject;
}
