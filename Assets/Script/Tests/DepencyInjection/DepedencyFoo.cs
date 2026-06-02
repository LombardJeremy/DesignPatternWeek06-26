using System;
using UnityEngine;

public class DepedencyFoo : MonoBehaviour
{
    [DepedencyInjection] private FooService m_fooService;


    private void Start()
    {
        
    }
}
