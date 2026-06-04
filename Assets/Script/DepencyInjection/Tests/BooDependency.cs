using System.Runtime.CompilerServices;
using UnityEngine;

public class BooDependency : MonoBehaviour
{
    [DependencyInjection] private FooDependency m_fooDependency;
}
