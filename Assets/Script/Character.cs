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
    

    private UnityEvent EndOfTurn;
    public UnityEvent EndOfTurnEvent { get => EndOfTurn; set => EndOfTurn = value; }

    #region Main Functions

    protected virtual void OnEndOfTurn()
    {
        EndOfTurn?.Invoke();
    }
    #endregion
    
    #region Unity Functions
    void Awake()
    {
        CurrentHealth =  m_maxHealth;
    }
    #endregion
    
}
