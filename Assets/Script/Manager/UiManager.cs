using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    #region Main ui Objects

    [SerializeField] private GameObject m_uiAttack; //0
    [SerializeField] private GameObject m_uiDefense; //1
    [SerializeField] private GameObject m_uiMagic; //2
    private List<GameObject> m_uiList = new List<GameObject>(); 

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

    #endregion


}
