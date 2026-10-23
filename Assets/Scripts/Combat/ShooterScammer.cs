using UnityEngine;

namespace ScammerNoScamming
{
    // The ordinary scammer: patrols its patch on foot, and once it notices the player runs at them and shoots.
    public class ShooterScammer : Enemy
    {
        [Header("Chase (placeholder numbers, not decided yet)")]
        public float chaseSpeed = 3.5f;
        // Stops closing in at this distance while it can see the player, and shoots from there.
        public float keepDistance = 10f;

        protected override void Pursue()
        {
            Chase(keepDistance, chaseSpeed);
        }

        protected override void Attack(PlayerCharacter target)
        {
            Vector3 origin = EyePosition;
            Vector2 jitter = Random.insideUnitCircle * spreadDegrees;
            Vector3 direction = Quaternion.LookRotation(AimPoint - origin) * Quaternion.Euler(jitter.y, jitter.x, 0f) * Vector3.forward;
            Weapon.TryFire(origin, direction, Faction, out _);
        }
    }
}
