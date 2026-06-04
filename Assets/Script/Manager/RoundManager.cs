using System;
using UnityEngine;
using UnityEngine.UI;

public class RoundManager : MonoBehaviour
{
    private static RoundManager s_instance;

    
    [SerializeField] private Character m_player;
    [SerializeField] private Character m_enemy;
    
    [DependencyInjection] private UiManager m_uiManager;
    
    #region Character
    
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
    }

    private void Start()
    {
        m_player.ActionDeclaration += EndOfActionDeclaration;
        m_enemy.ActionDeclaration += EndOfActionDeclaration;
    }

    private void OnDestroy()
    {
        m_player.ActionDeclaration -= EndOfActionDeclaration;
        m_enemy.ActionDeclaration -= EndOfActionDeclaration;
    }

    private void EndOfActionDeclaration(ActionCommand action,  bool castOnSelf)
    {
        DoAction(action, castOnSelf);
        UpdateRound();
    }
    

    public void UpdateRound()
    {
        if (m_currentCharacterPlayin == 0)
        {
            m_currentCharacterPlayin = 1;
            UnlockUI(false);
            m_enemy.InitializeCharacter();
            
        }
        else
        {
            m_currentCharacterPlayin = 0;
            UnlockUI(true);
            m_player.InitializeCharacter();
        }
    }
    
    public void DoAction(ActionCommand action, bool castOnSelf)
    {
        if (m_currentCharacterPlayin == 0)
        {
            action.Execute(m_player, castOnSelf ? m_player : m_enemy);
        }
        else
        {
            action.Execute(m_enemy, castOnSelf ? m_enemy : m_player);
        }
    }

    public void UnlockUI(bool isUnlocked)
    {
        if (m_uiManager == null) return;
        if (isUnlocked)
        {
            m_uiManager.MUIAttack.GetComponent<Button>().interactable = true;
            m_uiManager.MUIDefense.GetComponent<Button>().interactable = true;
            m_uiManager.MUIMagic.GetComponent<Button>().interactable = true;
        }
        else
        {
            m_uiManager.MUIAttack.GetComponent<Button>().interactable = false;
            m_uiManager.MUIDefense.GetComponent<Button>().interactable = false;
            m_uiManager.MUIMagic.GetComponent<Button>().interactable = false;
        }
    }
}
