using UnityEngine;

public class Enemy : Character
{
    //Enemy Brain (very delightful brain)
    public override void InitializeCharacter()
    {
        base.InitializeCharacter();
        int seed = Random.Range(1, 4);
        DeclareAction((ActionType)seed);
    }
}
