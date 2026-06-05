using UnityEngine;

public class FooDependency : MonoBehaviour
{
    [DependencyInjection] private FooService m_fooService;

    private void Start()
    {
        if (m_fooService)
        {
            m_fooService.FooSayHello();
        }
        else
        {
            Debug.Log("Foo Service is null");
        }
    }

    public void FooSayHello()
    {
        Debug.Log("Hello World, I'm Foo Dependency!");
    }
}
