using UnityEngine;

public class BooDependency : MonoBehaviour
{
    [DependencyInjection] private FooDependency m_fooDependency;
    [DependencyInjection] private BooService m_booService;
    [DependencyInjection] private CustomService m_customService;
    [DependencyInjection] private SoService m_soService;

    private void Start()
    {
        if (m_fooDependency)
            m_fooDependency.FooSayHello();
        else
            Debug.LogWarning("FooDependency is invalid!");

        if (m_booService)
            m_booService.BooSayHello();
        else
            Debug.LogWarning("BooService is invalid!");
        
        if (m_customService != null)
            m_customService.CustomSayHello();
        else
            Debug.LogWarning("BooService is invalid!");
        
        if (m_soService)
            m_soService.SoSayHello();
        else
            Debug.LogWarning("BooService is invalid!");
    }
}
