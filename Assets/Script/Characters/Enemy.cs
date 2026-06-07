using System.Collections;
using UnityEngine;

public class Enemy : Character
{
    
    //Enemy Brain (very delightful brain)
    // ReSharper disable Unity.PerformanceAnalysis
    public override void InitializeCharacter()
    {
        StartCoroutine(Delay());
    }

    // ReSharper disable Unity.PerformanceAnalysis
    IEnumerator Delay()
    {
        yield return new WaitForSeconds(1f);
        int seed = Random.Range(1, 4);
        DeclareAction((ActionType)seed);
    }
}
