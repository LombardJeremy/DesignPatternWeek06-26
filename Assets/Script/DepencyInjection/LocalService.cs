using System.Collections.Generic;
using UnityEngine;

public enum InjectionScope
{
    GameObject,
    Children,
    Parents,
    Scene
}

public class LocalServiceProvider : MonoBehaviour
{
    [field: SerializeField] public List<MonoBehaviour> ServiceComponents { get; private set; }= new List<MonoBehaviour>();
    
    [field: SerializeField] public InjectionScope InjectionScope { get; private set; } = InjectionScope.GameObject;
}
