using UnityEngine;

interface ISnapshot
{
    void Apply();
}

interface ISnapshotProvider
{
    ISnapshot GetSnapshot();
    void ApplySnapshot(ISnapshot snapshot);
}
