using System.Collections.Generic;
using UnityEngine;

public class Historic : MonoBehaviour
{
    private List<ISnapshot> snapshots = new List<ISnapshot>();
    
    private ISnapshot m_lastDeletedSnapshot;
    public ISnapshot  LastDeletedSnapshot => m_lastDeletedSnapshot;

    public void SaveSnapshot(ISnapshot snapshot)
    {
        snapshots.Add(snapshot);
    }

    public List<ISnapshot> GetAllSnapshots()
    {
        return snapshots;
    }

    public void ClearSnapshots()
    {
        snapshots.Clear();
    }
    
    public ISnapshot GetLastUsedSnapShotAndRemoveLast()
    {
        ISnapshot lastUsedSnapshot = snapshots[^2];
        ISnapshot lastSnapshot = snapshots[^1];
        snapshots.Remove(lastSnapshot);
        m_lastDeletedSnapshot = lastSnapshot;
        return lastUsedSnapshot;
    }

    public void RemoveSnapshot(ISnapshot snapshot)
    {
        snapshots.Remove(snapshot);
        m_lastDeletedSnapshot = snapshot;
    }
}
