using System.Collections.Generic;
using UnityEngine;

public class Historic : MonoBehaviour
{
    private List<ISnapshot> snapshots = new List<ISnapshot>();

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
        snapshots.Remove(snapshots[^1]);
        return lastUsedSnapshot;
    }
}
