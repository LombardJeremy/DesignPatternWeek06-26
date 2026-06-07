using UnityEngine;

[CreateAssetMenu(fileName = "SoService", menuName = "Scriptable Objects/SoService"), Service]
public class SoService : ScriptableObject
{
    public void SoSayHello()
    {
        Debug.Log("Hello World, I'm SoService");
    }
}
