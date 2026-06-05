using System.Collections.Generic;
using UnityEngine;

public enum InjectionScope
{
    GameObject,
    //Children,
    //Parent,
    //Scene
}

public class DependencyInjectorComponent : MonoBehaviour
{
    [field: SerializeField] public List<MonoBehaviour> ServiceComponents { get; private set; }= new List<MonoBehaviour>();
    
    [field: SerializeField] public InjectionScope InjectionScope { get; private set; } = InjectionScope.GameObject;
}
