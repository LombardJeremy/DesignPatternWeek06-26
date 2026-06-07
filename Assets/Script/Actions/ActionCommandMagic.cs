using UnityEngine;

public class ActionCommandMagic : IActionCommand
{
    private ActionType m_ActionType = ActionType.ATTACK;
    public ActionType ActionType { 
        get => m_ActionType;
        set => m_ActionType = value;
    }
    public bool Execute(Character self, Character target)
    {
        target.RemoveHealth(self.CurrentAttack * 2);
        
        //TODO : return state
        return true;
    }
}
