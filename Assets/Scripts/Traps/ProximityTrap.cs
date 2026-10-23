using System;
using UnityEngine;

namespace ScammerNoScamming
{
    [Flags]
    public enum TriggerKind
    {
        Enter = 1,
        Look = 2
    }

    // An ambush that springs when the player comes close (on foot or riding) or stares at it.
    // A player who does neither is never touched.
    public class ProximityTrap : Trap
    {
        public TriggerKind triggers = TriggerKind.Enter | TriggerKind.Look;
        public float enterRadius = 10f;
        public float lookRange = 15f;
        public float lookAngle = 20f;

        protected override bool ShouldSpring(PlayerCharacter player)
        {
            Vector3 flat = transform.position - player.transform.position;
            flat.y = 0f;
            if ((triggers & TriggerKind.Enter) != 0 && flat.magnitude <= enterRadius) return true;
            if ((triggers & TriggerKind.Look) == 0) return false;

            var view = player.GetComponentInChildren<Camera>();
            if (view == null) return false;
            Vector3 toTrap = transform.position + Vector3.up - view.transform.position;
            // shortcut: the look check ignores walls, so staring through a building still springs it; add a line-of-sight ray if that feels unfair.
            return toTrap.magnitude <= lookRange && Vector3.Angle(view.transform.forward, toTrap) <= lookAngle;
        }
    }
}
