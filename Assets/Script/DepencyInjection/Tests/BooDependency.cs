using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class BooDependency : MonoBehaviour
{
    [DependencyInjection] private FooDependency m_fooDependency;

    private void Start()
    {
        if (m_fooDependency)
        {
            m_fooDependency.FooSayHello();
        }
        else
        {
            Debug.LogWarning("FooDependency is invalid!");
        }
    }
}
