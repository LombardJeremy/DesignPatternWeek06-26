using UnityEngine;

public class ActionCommandDefense : IActionCommand
{
    private ActionType m_ActionType = ActionType.ATTACK;
    public ActionType ActionType { 
        get => m_ActionType;
        set => m_ActionType = value;
    }
    public bool Execute(Character self, Character target)
    {
        //TODO : Attack Execution sophisticated
        target.AddHealth(self.CurrentAttack);
        
        //TODO : return state
        return true;
    }
}
