using System;
using UnityEngine;

namespace ScammerNoScamming
{
    public class Health : MonoBehaviour, IDamageable
    {
        public float max = 3f;
        public bool deactivateOnDeath = true;

        public event Action<Health> Died;
        public event Action<Health, DamageInfo> Damaged;

        public float Current { get; private set; }
        public bool IsDead { get; private set; }

        void Awake()
        {
            Current = max;
        }

        // Refills to the new maximum, so call it before a case starts, not mid-fight.
        public void SetMax(float value)
        {
            max = value;
            Current = value;
        }

        // Explicit action reset: SetMax deliberately retains its original refill-only behavior.
        public void RestoreFull(float maximum)
        {
            max = Mathf.Max(1f, maximum);
            Current = max;
            IsDead = false;
        }

        // For damage modifiers inside Damaged and regeneration; never revives a dead character.
        public void SetCurrent(float value)
        {
            if (!IsDead) Current = Mathf.Clamp(value, 0f, max);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;

            Current -= info.Amount;
            Damaged?.Invoke(this, info);
            GameEvents.RaiseDamaged(this, info);
            if (Current > 0f) return;

            IsDead = true;
            Died?.Invoke(this);
            if (deactivateOnDeath) gameObject.SetActive(false);
        }
    }
}
