using UnityEngine;

namespace ScammerNoScamming
{
    // A lure (dropped money bag, crying "victim" in an alley). Nothing happens unless the player goes
    // to it: entering triggerRadius springs the trap and wakes the hidden scammers in activateOnSpring.
    public class AmbushBait : Trap
    {
        public float triggerRadius = 3f;

        public override TrapKind Kind => TrapKind.Ambush;

        protected override float DefaultDamage => 0f;

        void Update()
        {
            var player = FindPlayer();
            if (HasSprung || player == null) return;
            if (IsInside(player.transform.position))
                Spring(player.gameObject);
        }

        public bool IsInside(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= triggerRadius * triggerRadius;
        }
    }
}
