using UnityEngine;

public class ActionCommandAttack : IActionCommand
{
    private ActionType m_ActionType = ActionType.ATTACK;
    public ActionType ActionType { 
        get => m_ActionType;
        set => m_ActionType = value;
    }
    public bool Execute(Character self, Character target)
    {
        target.RemoveHealth(self.CurrentAttack);
        
        //TODO : return state
        return true;
    }
}
