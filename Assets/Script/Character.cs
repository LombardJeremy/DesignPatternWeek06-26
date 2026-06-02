using UnityEngine;

public abstract class Character : MonoBehaviour
{
    #region Stats
    [SerializeField] private int m_maxHealth = 100;
    private int m_currentHealth = 0;
    
    public int CurrentHealth { get => m_currentHealth; set => m_currentHealth = value; }
    public int MaxHealth { get => m_maxHealth; set => m_maxHealth = value; }
    #endregion
    
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        CurrentHealth =  m_maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DoAction()
    {
        
    }
}
