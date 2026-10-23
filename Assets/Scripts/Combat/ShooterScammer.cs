using UnityEngine;

namespace ScammerNoScamming
{
    // The ordinary scammer: patrols its patch on foot and shoots the player on sight.
    public class ShooterScammer : Enemy
    {
        protected override void Attack(PlayerCharacter target)
        {
            Vector3 origin = EyePosition;
            Vector2 jitter = Random.insideUnitCircle * spreadDegrees;
            Vector3 direction = Quaternion.LookRotation(AimPoint - origin) * Quaternion.Euler(jitter.y, jitter.x, 0f) * Vector3.forward;
            Weapon.TryFire(origin, direction, Faction, out _);
        }
    }
}
