using System;
using UnityEngine;

[Service]
public class RoundManager : MonoBehaviour
{
    private static RoundManager s_instance;

    
    [SerializeField] private Character m_player;
    [SerializeField] private Character m_enemy;
    
    #region Actions

    private ActionCommand m_attack;
    private ActionCommand m_defend;
    private ActionCommand m_magic;
    
    private Character currentTarget;
    public Character CurrentTarget { get => currentTarget; set => currentTarget = value; }

    #endregion

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

    private void Start()
    {
        if (m_attack == null && m_defend == null && m_magic == null)
        {
            throw new SystemException("No Action Set");
        }
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
    
    public void DoAction(ActionType actionType)
    {
        Character client;
        if (m_currentCharacterPlayin == 0)
        {
            client = m_player;
            currentTarget = m_enemy;
        }
        else
        {
            client = m_enemy;
            currentTarget = m_player;
        }
        switch (actionType)
        {
            case ActionType.DEFAULT:
                break;
            case ActionType.ATTACK:
                m_attack.Execute(client, CurrentTarget);
                break;
            case ActionType.DEFENSE:
                m_defend.Execute(client, client);
                break;
            case ActionType.MAGIC:
                m_magic.Execute(client, CurrentTarget);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null);
        }

    }
}
