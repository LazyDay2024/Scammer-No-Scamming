using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // Owns the numbers and the fire cooldown; subclasses decide how a shot travels.
    public abstract class Weapon : MonoBehaviour
    {
        [SerializeField] float damage = 1f;
        [SerializeField] float range = 120f;
        [SerializeField] float fireInterval = 0.2f;

        float nextFireTime;

        public event Action<ShotResult> Fired;

        public float Damage { get => damage; set => damage = value; }
        public float Range { get => range; set => range = value; }
        public float FireInterval { get => fireInterval; set => fireInterval = value; }
        public RevolverAmmo Ammo => GetComponent<RevolverAmmo>();

        public bool TryFire(Vector3 origin, Vector3 direction, Faction owner, out ShotResult result)
        {
            result = default;
            if (Time.time < nextFireTime) return false;
            // Look up at shot time: sandbox components can be added after this weapon's Awake.
            var ammo = Ammo;
            if (ammo != null && !ammo.TryConsumeRound()) return false;
            nextFireTime = Time.time + fireInterval;

            result = Resolve(origin, direction, owner);
            result.NoisePosition = transform.position;
            Fired?.Invoke(result);
            return true;
        }

        protected abstract ShotResult Resolve(Vector3 origin, Vector3 direction, Faction owner);
    }
}
