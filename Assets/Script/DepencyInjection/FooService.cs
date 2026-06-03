using UnityEngine;

[Service(ServiceFlags.Single)]
public class FooService : MonoBehaviour
{
   public void FooSayHello()
   {
      Debug.Log("Hello World, I'm FooManager");
   }
}
