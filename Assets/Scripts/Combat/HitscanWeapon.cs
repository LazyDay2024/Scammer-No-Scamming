using System;
using UnityEngine;

namespace ScammerNoScamming
{
    public class HitscanWeapon : Weapon
    {
        [SerializeField] LayerMask hitMask = ~0;

        readonly RaycastHit[] hits = new RaycastHit[32];

        // Colliders the shot passes through, such as the shooter's own body and the horse it rides.
        public Predicate<Collider> Ignore { get; set; }

        protected override ShotResult Resolve(Vector3 origin, Vector3 direction, Faction owner)
        {
            var result = new ShotResult { Origin = origin, EndPoint = origin + direction * Range };

            int count = Physics.RaycastNonAlloc(origin, direction, hits, Range, hitMask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            RaycastHit best = default;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                if (IsIgnored(hits[i].collider) || hits[i].distance >= nearest) continue;
                nearest = hits[i].distance;
                best = hits[i];
                found = true;
            }
            if (!found) return result;

            result.EndPoint = best.point;
            result.Hit = true;
            result.Normal = best.normal;
            var target = best.collider.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                var zone = target as HitZone;
                var health = zone != null && zone.Owner != null ? zone.Owner : best.collider.GetComponentInParent<Health>();
                bool wasAlive = health != null && !health.IsDead;
                target.TakeDamage(new DamageInfo(Damage, HitZoneType.Body, owner, best.point) { Direction = direction.normalized });
                result.HitDamageable = true;
                result.Killed = wasAlive && health != null && health.IsDead;
            }
            return result;
        }

        bool IsIgnored(Collider other)
        {
            if (other.transform.IsChildOf(transform)) return true;
            return Ignore != null && Ignore(other);
        }
    }
}
