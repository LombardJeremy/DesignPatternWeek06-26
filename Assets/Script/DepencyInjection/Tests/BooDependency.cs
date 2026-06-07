using UnityEngine;

public class BooDependency : MonoBehaviour
{
    [DependencyInjection] private FooDependency m_fooDependency;
    [DependencyInjection] private BooService m_booService;
    [DependencyInjection] private CustomService m_customService;
    [DependencyInjection] private SoService m_soService;

    private void Start()
    {
        //Init(m_fooDependency, m_booService, m_customService, m_soService);
    }

    [DependencyInjection]
    private void Init(FooDependency  fooDependency, BooService booService,  CustomService customService, SoService soService)
    {
        if (fooDependency)
            fooDependency.FooSayHello();
        else
            Debug.LogWarning("FooDependency is invalid!");

        if (booService)
            booService.BooSayHello();
        else
            Debug.LogWarning("BooService is invalid!");
        
        if (customService != null)
            customService.CustomSayHello();
        else
            Debug.LogWarning("BooService is invalid!");
        
        if (soService)
            soService.SoSayHello();
        else
            Debug.LogWarning("BooService is invalid!");
    }
}
