using UnityEngine;

[Service]
public class FooService : MonoBehaviour
{
   //[DependencyInjection] private BooService m_booService;
   
   private void Start()
   {
      //if(m_booService) m_booService.BooSayHello();
   }

   public void FooSayHello()
   {
      Debug.Log("Hello World, I'm FooService");
   }
}
