using System;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    private static RoundManager s_instance;

    
    [SerializeField] private Character m_player;
    [SerializeField] private Character m_enemy;

    private int m_currentCharacterPlayin = 0;
    
    
    void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            s_instance = this;
        }
        m_player.EndOfTurnEvent.AddListener(EndOfPlayerTurn);
        m_enemy.EndOfTurnEvent.AddListener(EndOfEnemyTurn);
    }

    private void OnDestroy()
    {
        m_player.EndOfTurnEvent.RemoveListener(EndOfPlayerTurn);
        m_enemy.EndOfTurnEvent.RemoveListener(EndOfEnemyTurn);
    }

    private void EndOfPlayerTurn()
    {
        //TODO
        UpdateRound();
        throw new System.NotImplementedException();
    }

    private void EndOfEnemyTurn()
    {
        //TODO
        UpdateRound();
        throw new System.NotImplementedException();
    }
    

    public void UpdateRound()
    {
        if (m_currentCharacterPlayin == 0)
        {
            m_currentCharacterPlayin = 1;
        }
        else
        {
            m_currentCharacterPlayin = 0;
        }
    }
}
