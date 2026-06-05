using System;
using UnityEngine;
using UnityEngine.UI;

public class RoundManager : MonoBehaviour
{
    private static RoundManager s_instance;
    
    [SerializeField] private Character m_player;
    [SerializeField] private Character m_enemy;
    //Inject UIManager to lock and unlock UI
    [DependencyInjection] private UiManager m_uiManager;
    
    #region Character
    
    private Character currentTarget;
    public Character CurrentTarget { get => currentTarget; set => currentTarget = value; }
    private int m_currentCharacterPlayin = 0;
    
    #endregion
    #region Main FCT

    void Awake() //Only 1 copy
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

    #endregion
    #region GameLoop FCT

    //Fct depending on character event to update round
    private void EndOfActionDeclaration(ActionCommand action,  bool castOnSelf)
    {
        DoAction(action, castOnSelf);
        UpdateRound();
    }
    
    //Simple Update between each round
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
    
    //DoAction depending on the player playing & if it's on himself
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

    public void UnlockUI(bool isUnlocked) //Lock / Unlock UI
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

    #endregion


}
