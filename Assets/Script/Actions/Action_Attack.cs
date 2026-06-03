using UnityEngine;

public class Action_Attack : MonoBehaviour, Action
{
    private ActionType m_ActionType = ActionType.ATTACK;
    public ActionType ActionType { 
        get => m_ActionType;
        set => m_ActionType = value;
    }
    public bool Execute(Character self, Character target)
    {
        //TODO : Attack Execution sophisticated
        target.RemoveHealth(self.CurrentAttack);
        
        //TODO : return state
        return true;
    }
}
