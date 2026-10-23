using UnityEngine;

namespace ScammerNoScamming
{
    public struct DamageInfo
    {
        public float Amount;
        public HitZoneType Zone;
        public Faction SourceFaction;
        public Vector3 Point;
        // Travel direction, kept optional for existing damage callers.
        public Vector3 Direction;

        public DamageInfo(float amount, HitZoneType zone, Faction sourceFaction, Vector3 point)
        {
            Amount = amount;
            Zone = zone;
            SourceFaction = sourceFaction;
            Point = point;
            Direction = Vector3.zero;
        }
    }
}
