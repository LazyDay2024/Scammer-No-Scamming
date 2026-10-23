using UnityEngine;

namespace ScammerNoScamming
{
    public struct DamageInfo
    {
        public float Amount;
        public HitZoneType Zone;
        public Faction SourceFaction;
        public Vector3 Point;

        public DamageInfo(float amount, HitZoneType zone, Faction sourceFaction, Vector3 point)
        {
            Amount = amount;
            Zone = zone;
            SourceFaction = sourceFaction;
            Point = point;
        }
    }
}
