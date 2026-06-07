using UnityEngine;

[Service]
public class CustomService
{
    [DependencyInjection] private BooService booService;
    [DependencyInjection] private FooService fooService;
    [DependencyInjection] private SoService soService;
    
    public void CustomSayHello()
    {
        Debug.Log("Hello World, I'm CustomService");
    }

    [DependencyInjection]
    public void Initialize(BooService booService, FooService fooService, SoService soService)
    {
        Debug.Log($"Initializing CustomService with {booService.GetType().Name}, {fooService.GetType().Name}, {soService.GetType().Name}");
    }
}
