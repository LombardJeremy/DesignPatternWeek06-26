using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager s_instance;

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            
        }
        else
        {
            s_instance = this;
        }
    }
}
