using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    [SerializeField] private GameObject m_uiAttack; //0
    [SerializeField] private GameObject m_uiDefense; //1
    [SerializeField] private GameObject m_uiMagic; //2
    private GameObject[] m_uiList; 
    
    [DependencyInjection] private RoundManager m_roundManager; //Don't use in Awake

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
        m_roundManager.DoAction(ActionType.ATTACK);
    }
    public void DoActionDefense()
    {
        m_roundManager.DoAction(ActionType.DEFENSE);
    }
    public void DoActionMagic()
    {
        m_roundManager.DoAction(ActionType.MAGIC);
    }

    public void ChangeActionUI(ActionCommand action)
    {
        switch (action.ActionType)
        {
            case ActionType.DEFAULT:
                m_targetUI = 0;
                break;
            case ActionType.ATTACK:
                (m_uiAttack.transform.position, m_uiList[m_targetUI].transform.position) = (m_uiList[m_targetUI].transform.position, m_uiAttack.transform.position);
                m_targetUI = 0;
                break;
            case ActionType.DEFENSE:
                (m_uiDefense.transform.position, m_uiList[m_targetUI].transform.position) = (m_uiList[m_targetUI].transform.position, m_uiDefense.transform.position);
                m_targetUI = 1;
                break;
            case ActionType.MAGIC:
                (m_uiMagic.transform.position, m_uiList[m_targetUI].transform.position) = (m_uiList[m_targetUI].transform.position, m_uiMagic.transform.position);
                m_targetUI = 2;
                break;
            default:
                break;
        }
    }
}
