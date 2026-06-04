using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager s_instance;
    
    [SerializeField] private RoundManager m_roundManager;

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

    private void Start()
    {
        m_roundManager.UpdateRound();
    }
}
