
public interface Action
{
    ActionType ActionType { get; set; }
    bool Execute(Character self, Character target);
}

public enum ActionType
{
    DEFAULT,
    ATTACK,
    DEFENSE,
    MAGIC
}
