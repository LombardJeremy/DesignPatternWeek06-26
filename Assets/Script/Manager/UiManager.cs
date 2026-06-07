using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    #region Main ui Objects

    [SerializeField] private GameObject m_uiAttack; //0
    [SerializeField] private GameObject m_uiDefense; //1
    [SerializeField] private GameObject m_uiMagic; //2
    private List<GameObject> m_uiList = new List<GameObject>();

    [SerializeField] private GameObject m_uiTurnIndicatorText;
    [SerializeField] private TextMeshProUGUI m_uiWinLooseText;
    
    [SerializeField] private TextMeshProUGUI m_uiPlayerHP;
    [SerializeField] private TextMeshProUGUI m_uiEnemyHP;
    
    [SerializeField] private TextMeshProUGUI m_uiRoundCounter;

    #endregion
    #region Getter/Setter

    public GameObject MUIAttack
    {
        get => m_uiAttack;
        set => m_uiAttack = value;
    }

    public GameObject MUIDefense
    {
        get => m_uiDefense;
        set => m_uiDefense = value;
    }

    public GameObject MUIMagic
    {
        get => m_uiMagic;
        set => m_uiMagic = value;
    }

    #endregion
    
    //Inject Player Class for Action after Input
    [DependencyInjection] private Player m_player; //Don't use in Awake
    [DependencyInjection] private Enemy m_enemy; //Don't use in Awake
    
    #region Main Fct

    void Awake()
    {
        if (m_uiAttack == null &&  m_uiDefense == null && m_uiMagic == null)
        {
            throw new System.Exception("UI must be set");
        }

        //Append In List
        m_uiList.Add(m_uiAttack);
        m_uiList.Add(m_uiDefense);
        m_uiList.Add(m_uiMagic);
    }

    #endregion
    #region UiActions

    public void DoActionAttack()
    {
        m_player.DeclareAction(ActionType.ATTACK);
    }
    public void DoActionDefense()
    {
        m_player.DeclareAction(ActionType.DEFENSE);
    }
    public void DoActionMagic()
    {
        m_player.DeclareAction(ActionType.MAGIC);
    }

    public void SetPlayerUIEnabled(bool enabled)
    {
        if (enabled)
        {
            MUIAttack.GetComponent<Button>().interactable = true;
            MUIDefense.GetComponent<Button>().interactable = true;
            MUIMagic.GetComponent<Button>().interactable = true;
            m_uiTurnIndicatorText.GetComponent<TextMeshProUGUI>().text = "Current Turn : Player Turn";
        }
        else
        {
            MUIAttack.GetComponent<Button>().interactable = false;
            MUIDefense.GetComponent<Button>().interactable = false;
            MUIMagic.GetComponent<Button>().interactable = false;
            m_uiTurnIndicatorText.GetComponent<TextMeshProUGUI>().text = "Current Turn : Enemy Turn";   
        }

        UpdateHpUi();
    }

    public void UpdateHpUi()
    {
        m_uiPlayerHP.text = "Player HP : " + m_player.CurrentHealth.ToString();
        m_uiEnemyHP.text = "Player HP : " + m_enemy.CurrentHealth.ToString();
    }

    public void UpdateRound(int roundCounter)
    {
        m_uiRoundCounter.text = "Round Counter : " + roundCounter;
    }

    public void GoBackOneRound()
    {
        //TODO
        
    }

    public void SetWinLooseText(bool IsWin)
    {
        
        m_uiWinLooseText.gameObject.SetActive(true);
        if (IsWin)
        {
            m_uiWinLooseText.text = "You Win!";
        }
        else
        {
            m_uiWinLooseText.text = "You Loose!";
        }
    }

    #endregion


}
