using UnityEngine;

public class CommandLoad : Command
{
    private Historic m_historic;
    private RoundManager m_roundManager;

    public CommandLoad(Historic historic, RoundManager manager)
    {
        m_historic = historic;
        m_roundManager = manager;
    }
    
    public override void Do()
    {
        ISnapshot snapShotToLoad = m_historic.GetLastUsedSnapShotAndRemoveLast();
        m_roundManager.MLastSnapshotToUse = snapShotToLoad;
    }

    public override void Undo()
    {
        ISnapshot retrievedSnapshot = m_historic.LastDeletedSnapshot;
        m_roundManager.MLastSnapshotToUse = retrievedSnapshot;
        m_historic.SaveSnapshot(retrievedSnapshot);
    }
}
