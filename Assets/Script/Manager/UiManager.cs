using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[Service]
public class UiManager : MonoBehaviour
{
    [SerializeField] private GameObject m_uiAttack; //0
    [SerializeField] private GameObject m_uiDefense; //1
    [SerializeField] private GameObject m_uiMagic; //2
    private GameObject[] m_uiList; 

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

    [DependencyInjection] private Player m_player; //Don't use in Awake

    private int m_targetUI = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (m_uiAttack == null &&  m_uiDefense == null && m_uiMagic == null)
        {
            throw new System.Exception("UI must be set");
        }

        //Append In List
        m_uiList.Append(m_uiAttack);
        m_uiList.Append(m_uiDefense);
        m_uiList.Append(m_uiMagic);
    }
    

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
}
