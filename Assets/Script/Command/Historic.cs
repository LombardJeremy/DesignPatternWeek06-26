using System.Collections.Generic;

public class Historic
{
    private readonly List<ISnapshot> snapshots = new();
    public ISnapshot LastDeletedSnapshot { get; private set; }

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
        ISnapshot lastUsedSnapshot;
        if (snapshots.Count < 2)
            lastUsedSnapshot = snapshots[^1];
        else
            lastUsedSnapshot = snapshots[^2];
        var lastSnapshot = snapshots[^1];
        snapshots.Remove(lastSnapshot);
        LastDeletedSnapshot = lastSnapshot;
        return lastUsedSnapshot;
    }

    public void RemoveSnapshot(ISnapshot snapshot)
    {
        snapshots.Remove(snapshot);
        LastDeletedSnapshot = snapshot;
    }
}