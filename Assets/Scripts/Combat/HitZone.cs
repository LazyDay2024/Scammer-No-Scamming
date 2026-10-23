using UnityEngine;

namespace ScammerNoScamming
{
    // A collider region that scales incoming damage before passing it to the owning Health.
    // Put it on the object that carries the collider; a collider with no HitZone damages Health as Body.
    public class HitZone : MonoBehaviour, IDamageable
    {
        [SerializeField] HitZoneType zone = HitZoneType.Body;
        [SerializeField] float multiplier = 1f;
        [SerializeField] Health owner;

        public HitZoneType Zone => zone;
        public float Multiplier => multiplier;
        public Health Owner => owner;

        // Starting values only; the real multipliers are still to be decided.
        public static float DefaultMultiplier(HitZoneType type)
        {
            switch (type)
            {
                case HitZoneType.Head: return 2f;
                case HitZoneType.Feet: return 0.5f;
                default: return 1f;
            }
        }

        void Reset()
        {
            multiplier = DefaultMultiplier(zone);
        }

        public void Configure(HitZoneType type, float damageMultiplier, Health health = null)
        {
            zone = type;
            multiplier = damageMultiplier;
            if (health != null) owner = health;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (owner == null) owner = GetComponentInParent<Health>();
            if (owner == null) return;

            info.Zone = zone;
            info.Amount *= multiplier;
            owner.TakeDamage(info);
        }
    }
}
