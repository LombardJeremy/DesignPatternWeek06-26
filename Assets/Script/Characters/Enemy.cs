using UnityEngine;

public class Enemy : Character
{
    
    public override void InitializeCharacter()
    {
        base.InitializeCharacter();
        int seed = Random.Range(0, 3);
        DeclareAction((ActionType)seed);
    }
}
