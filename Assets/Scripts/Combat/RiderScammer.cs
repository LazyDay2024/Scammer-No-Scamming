using UnityEngine;

namespace ScammerNoScamming
{
    // A scammer on a horse that chases the player and shoots on the move. The horse and the rider have
    // separate Health: shooting the horse costs the horse, shooting the rider costs the rider.
    // When the horse goes down the rider is thrown and carries on the fight on foot.
    //
    // Expected children, found by name: "Horse" (own Health, HitZones bound to it) and "Rider"
    // (the rider's body parts, sitting on the horse; its local height is the seat height).
    public class RiderScammer : ShooterScammer
    {
        public const string HorseName = "Horse";
        public const string RiderName = "Rider";

        [Header("Riding (placeholder numbers, not decided yet)")]
        public float trotSpeed = 5f;
        public float gallopSpeed = 9f;
        public float footWalkSpeed = 2f;
        public float footChaseSpeed = 3.5f;
        public float keepDistance = 8f;
        // Scales damage taken by the horse, so it can be tougher or softer than the rider.
        public float horseDamageMultiplier = 0.5f;
        public float horseMaxHealth = 6f;

        Transform horse;
        Transform rider;
        float seatHeight;

        public bool IsMounted { get; private set; }
        public Health HorseHealth { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            horse = transform.Find(HorseName);
            rider = transform.Find(RiderName);
            HorseHealth = horse != null ? horse.GetComponent<Health>() : null;
            if (HorseHealth == null || rider == null)
            {
                Debug.LogError("RiderScammer needs 'Horse' (with Health) and 'Rider' children", this);
                return;
            }

            IsMounted = true;
            seatHeight = rider.localPosition.y;
            eyeHeight += seatHeight;
            walkSpeed = trotSpeed;
            if (Agent != null) Agent.radius = 0.7f;

            HorseHealth.SetMax(horseMaxHealth);
            HorseHealth.deactivateOnDeath = false;
            foreach (var zone in horse.GetComponentsInChildren<HitZone>())
                zone.Configure(zone.Zone, zone.Multiplier * horseDamageMultiplier);
            HorseHealth.Died += OnHorseDied;
        }

        protected override void OnDestroy()
        {
            if (HorseHealth != null) HorseHealth.Died -= OnHorseDied;
            base.OnDestroy();
        }

        // Rides at the last place it saw the player and stops to shoot once it is close enough.
        protected override void Pursue()
        {
            Chase(keepDistance, IsMounted ? gallopSpeed : footChaseSpeed);
        }

        void OnHorseDied(Health health)
        {
            IsMounted = false;
            walkSpeed = footWalkSpeed;
            eyeHeight -= seatHeight;
            rider.localPosition = Vector3.zero;
            if (Agent != null) Agent.radius = 0.4f;

            foreach (var body in horse.GetComponentsInChildren<Collider>()) body.enabled = false;
            horse.SetParent(null, true);
            StartCoroutine(Collapse(horse));
            // The coroutine stops if the rider is destroyed first, so make sure the horse still goes.
            Destroy(horse.gameObject, CollapseTime + 1f);
        }
    }
}
