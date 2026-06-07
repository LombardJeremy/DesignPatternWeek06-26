using UnityEngine;

[Service]
public class CustomService
{
    public void CustomSayHello()
    {
        Debug.Log("Hello World, I'm CustomService");
    }

    [DependencyInjection]
    public void Initialize(BooService booService, FooService fooService)
    {
        //Debug.Log($"Initializing CustomService with {booService.GetType().Name} and  {fooService.GetType().Name}");
    }
}
