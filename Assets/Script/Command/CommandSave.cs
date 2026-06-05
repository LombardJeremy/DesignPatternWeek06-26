using UnityEngine;

public class CommandSave : Command
{
    private Historic m_historic;
    private ISnapshot m_snapshotToSave;
    
    public CommandSave(Historic historic, ISnapshot snapshotToSave)
    {
        m_historic = historic;
        m_snapshotToSave  = snapshotToSave;
    }
    
    public override void Do()
    {
        m_historic.SaveSnapshot(m_snapshotToSave);
    }

    public override void Undo()
    {
        m_historic.RemoveSnapshot(m_snapshotToSave);
    }
}
