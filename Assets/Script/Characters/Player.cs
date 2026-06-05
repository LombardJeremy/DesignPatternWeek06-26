using System;
using UnityEngine;

[Service]
public class Player : Character, ISnapshotProvider
{
    class PlayerSnapShot : ISnapshot 
    {
        private Player _master;
        private int oldHP;
        public PlayerSnapShot(Player p)
        {
            _master = p;
            oldHP = _master._hp;
        }

        public void Apply()
        {
            _master._hp = oldHP;
        }
    }
    
    int _hp;

    ISnapshot ISnapshotProvider.GetSnapshot() => new PlayerSnapShot(this);

    void ISnapshotProvider.ApplySnapshot(ISnapshot snapshot) => snapshot.Apply();
}

