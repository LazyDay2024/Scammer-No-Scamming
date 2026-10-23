using UnityEngine;

namespace ScammerNoScamming
{
    public struct ShotResult
    {
        public Vector3 Origin;
        public Vector3 EndPoint;
        public bool HitDamageable;
        public bool Hit;
        public Vector3 Normal;
        public bool Killed;
        // Where the shot was heard from, so other systems can react to the noise.
        public Vector3 NoisePosition;
    }
}
