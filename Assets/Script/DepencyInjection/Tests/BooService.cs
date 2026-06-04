using System;
using UnityEngine;

[Service]
public class BooService : MonoBehaviour
{
    [DependencyInjection] private FooService m_fooService;
    
    private void Start()
    {
        m_fooService.FooSayHello();    
    }

    public void BooSayHello()
    {
        Debug.Log("Hello World, I'm BooService");
    }
}
