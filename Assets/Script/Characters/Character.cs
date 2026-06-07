using System;
using UnityEngine;
using UnityEngine.Events;

public abstract class Character : MonoBehaviour, ISnapshotProvider
{
    class CharacterSnapshot : ISnapshot
    {
        private Character m_master;
        private int m_currentHealth;
        public CharacterSnapshot(Character character)
        {
            m_master = character;
            m_currentHealth = character.CurrentHealth;
        }
        public void Apply()
        {
            m_master.CurrentHealth = m_currentHealth;
        }
    }
    
    ISnapshot ISnapshotProvider.GetSnapshot() => new CharacterSnapshot(this);
    void ISnapshotProvider.ApplySnapshot(ISnapshot snapshot) => snapshot.Apply();
    
    #region Stats
    [SerializeField] private int m_maxHealth = 100;
    [SerializeField] private int m_currentHealth = 0;
    
    public int CurrentHealth { get => m_currentHealth; set => m_currentHealth = value; }
    public int MaxHealth { get => m_maxHealth; set => m_maxHealth = value; }

    public void RemoveHealth(int amount)
    {
        CurrentHealth -= amount;
        Debug.Log(amount);
    }
    public void AddHealth(int amount) { CurrentHealth += amount; }

    [SerializeField] private int m_currentAttack = 0;

    public int CurrentAttack
    {
        get => m_currentAttack;
        set => m_currentAttack = value;
    }
    public bool IsDead { get => CurrentHealth <= 0; }
    #endregion
    #region Actions

    private ActionCommandAttack m_attack;
    private ActionCommandDefense m_defend;
    private ActionCommandMagic m_magic;
    public event Action<IActionCommand, bool> ActionDeclaration; 

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
        m_attack = new ActionCommandAttack();
        m_defend = new ActionCommandDefense();
        m_magic = new ActionCommandMagic();
    }
    #endregion
}
