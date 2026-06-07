using System;
using NaughtyAttributes;
using UnityEngine;

[Service]
public class BooService : MonoBehaviour
{
    [DependencyInjection, ShowNonSerializedField] private FooService m_fooService;
    [DependencyInjection, ShowNonSerializedField] private CustomService m_customService;
    [DependencyInjection, ShowNonSerializedField] private SoService m_soService;
    
    private void Start()
    {
        m_fooService?.FooSayHello();
        m_customService?.CustomSayHello();
        m_soService?.SoSayHello();
    }

    public void BooSayHello()
    {
        Debug.Log("Hello World, I'm BooService");
    }
}
