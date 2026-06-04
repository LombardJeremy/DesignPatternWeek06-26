using System;
using UnityEngine;

[Service(ServiceFlags.Instantiate)]
public class FooService : MonoBehaviour
{
   [DependencyInjection] private BooService m_booService;
   
   private void Start()
   {
      m_booService.BooSayHello();
   }

   public void FooSayHello()
   {
      Debug.Log("Hello World, I'm FooService");
   }
}
