using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class BooDependency : MonoBehaviour
{
    [DependencyInjection] private FooDependency m_fooDependency;
    [DependencyInjection] private BooService m_booService;

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

        if (m_booService)
        {
            m_booService.BooSayHello();
        }
        else
        {
            Debug.LogWarning("BooService is invalid!");
        }
    }
}
