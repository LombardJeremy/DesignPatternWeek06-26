using UnityEngine;

//Main Snapshot -> Apply fct
public interface ISnapshot
{
    void Apply();
}

//Snapshot Provider required to access all object that CAN be snapshoted
interface ISnapshotProvider
{
    ISnapshot GetSnapshot();
    void ApplySnapshot(ISnapshot snapshot);
}
