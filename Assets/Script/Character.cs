using System;
using UnityEngine;
using UnityEngine.Events;

public abstract class Character : MonoBehaviour
{
    #region Stats
    [SerializeField] private int m_maxHealth = 100;
    private int m_currentHealth = 0;
    
    public int CurrentHealth { get => m_currentHealth; set => m_currentHealth = value; }
    public int MaxHealth { get => m_maxHealth; set => m_maxHealth = value; }

    public void RemoveHealth(int amount) { CurrentHealth -= amount; }
    public void AddHealth(int amount) { CurrentHealth += amount; }

    [SerializeField] private int m_currentAttack = 0;

    public int CurrentAttack
    {
        get => m_currentAttack;
        set => m_currentAttack = value;
    }
    #endregion
    #region Actions

    private ActionCommand m_attack;
    private ActionCommand m_defend;
    private ActionCommand m_magic;
    public event Action<ActionCommand, bool> ActionDeclaration; 

    #endregion
    #region Main Functions
    
    public virtual void InitializeCharacter() { }
    
    public void DeclareAction(ActionType actionType)
    {
        switch (actionType)
        {
            case ActionType.DEFAULT:
                break;
            case ActionType.ATTACK:
                ActionDeclaration?.Invoke(m_attack, false);
                break;
            case ActionType.DEFENSE:
                ActionDeclaration?.Invoke(m_defend, true);
                break;
            case ActionType.MAGIC:
                ActionDeclaration?.Invoke(m_magic, false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null);
        }
    }
    #endregion
    
    #region Unity Functions
    void Awake()
    {
        CurrentHealth =  m_maxHealth;
    }
    #endregion
    
}
