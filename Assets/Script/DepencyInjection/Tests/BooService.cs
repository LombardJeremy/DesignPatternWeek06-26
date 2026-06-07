using System;
using NaughtyAttributes;
using UnityEngine;

[Service]
public class BooService : MonoBehaviour
{
    [DependencyInjection, ShowNonSerializedField] private FooService m_fooService;
    
    private void Start()
    {
        if(m_fooService) m_fooService.FooSayHello();    
    }

    public void BooSayHello()
    {
        Debug.Log("Hello World, I'm BooService");
    }
}
