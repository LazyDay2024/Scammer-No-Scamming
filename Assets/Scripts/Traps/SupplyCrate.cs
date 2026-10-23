using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // Press E next to it on foot. A real crate (official seal) raises Collected so the player system can
    // refill; a fake one ("ฟรี! ด่วน! เหลือ 1 กล่อง", no seal) springs: damage plus its hidden scammers appear.
    public class SupplyCrate : Trap
    {
        public static event Action<SupplyCrate, GameObject> Collected;

        public bool isFake = true;
        public float interactRange = 2.5f;

        public override TrapKind Kind => TrapKind.FakeSupply;
        public bool IsUsed { get; private set; }

        public bool InRange(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= interactRange * interactRange;
        }

        void Update()
        {
            var player = FindPlayer();
            if (IsUsed || player == null || player.IsMounted || !InRange(player.transform.position)) return;
            var controls = player.GetComponent<PlayerControls>();
            if (controls != null && controls.Interact != null && controls.Interact.WasPressedThisFrame())
                Use(player.gameObject);
        }

        public void Use(GameObject user)
        {
            if (IsUsed) return;
            IsUsed = true;
            if (isFake) Spring(user);
            else Collected?.Invoke(this, user);
        }
    }
}
