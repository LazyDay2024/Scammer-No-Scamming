using System;
using UnityEngine;

namespace ScammerNoScamming
{
    public enum TrapKind
    {
        FakeSupply,
        PhishingLink,
        Disguised,
        Ambush,
        CallCenter
    }

    // Base for the traps on the road to a hideout. Every trap must show a tell the player can notice
    // first (art or behaviour); the code only decides what springs it and what happens.
    // Other systems listen to the static events instead of traps knowing about Trouble, UI or enemies.
    public abstract class Trap : MonoBehaviour
    {
        public static event Action<Trap> Sprung;
        public static event Action<Trap> Defused;

        [Tooltip("Negative uses the trap's default damage.")]
        public float damage = -1f;
        [Tooltip("Disabled objects (hidden scammers etc.) switched on when the trap springs.")]
        public GameObject[] activateOnSpring = Array.Empty<GameObject>();

        public abstract TrapKind Kind { get; }
        public float Damage => damage >= 0f ? damage : DefaultDamage;
        protected virtual float DefaultDamage => 25f;
        public bool HasSprung { get; private set; }
        public bool IsDefused { get; private set; }

        // Springs once. target is the player root; damage goes to the first IDamageable on it, if any.
        public void Spring(GameObject target)
        {
            if (HasSprung || IsDefused) return;
            HasSprung = true;

            if (target != null && Damage > 0f)
            {
                var damageable = target.GetComponentInChildren<IDamageable>() ?? target.GetComponentInParent<IDamageable>();
                damageable?.TakeDamage(new DamageInfo(Damage, HitZoneType.Body, Faction.Scammer, target.transform.position));
            }
            foreach (var go in activateOnSpring)
                if (go != null) go.SetActive(true);

            OnSprung(target);
            TrapLog.RecordSprung(Kind);
            Sprung?.Invoke(this);
        }

        // The player saw through it (for example shot the disguised scammer first).
        public void Defuse()
        {
            if (HasSprung || IsDefused) return;
            IsDefused = true;
            TrapLog.RecordDefused(Kind);
            Defused?.Invoke(this);
        }

        protected virtual void OnSprung(GameObject target) { }

        protected static PlayerController FindPlayer() => PlayerLocator.Player;
    }
}
