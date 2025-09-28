using AOSharp.Common.GameData;
using System;
using System.Collections.Generic;

namespace MobOverlay
{
    internal class PlayerEntry
    {
        internal string Name;
        internal Profession Profession;
        internal float HealthPercent;
        internal bool LineOfSight;
        internal Identity Identity;
        internal Queue<Vector3> PositionHistory;
        internal int GroupIndex;
        internal bool IsLeader;
        internal DateTime LastSeen;
        private const int _queueLimit = 5;

        internal PlayerEntry(string name, Profession profession, Identity identity)
        {
            Name = name;
            Profession = profession;
            Identity = identity;
            PositionHistory = new Queue<Vector3>();
            GroupIndex = -1;
            LastSeen = DateTime.Now;
            HealthPercent = 0f;
            LineOfSight = false;
            IsLeader = false;
        }

        internal void UpdatePosition(Vector3 item)
        {
            if (PositionHistory.Count >= _queueLimit)
                PositionHistory.Dequeue();

            PositionHistory.Enqueue(item);
        }

        internal bool UpdateIndex(int newIndex, out int oldIndex)
        {
            oldIndex = GroupIndex;
            GroupIndex = newIndex;
            return oldIndex != newIndex;
        }
    }
}
