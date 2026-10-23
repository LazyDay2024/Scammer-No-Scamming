using UnityEngine;

namespace ScammerNoScamming
{
    // A scammer on a horse that chases the player and shoots on the move. The horse and the rider have
    // separate Health: shooting the horse costs the horse, shooting the rider costs the rider.
    // When the horse goes down the rider is thrown and carries on the fight on foot; when the rider is shot dead
    // the rider falls beside the horse, which stays standing.
    //
    // Expected children, found by name: "Horse" (own Health, HitZones bound to it) and "Rider"
    // (the rider's body parts, sitting on the horse; its local height is the seat height). When the model
    // prefabs exist (Resources/Scammer) the boxes are hidden, the bandit sits in the saddle on the horse model,
    // and the rider's hit zones are resized to the seated bandit.
    public class RiderScammer : ShooterScammer
    {
        public const string HorseName = "Horse";
        public const string RiderName = "Rider";
        // Where the rider model's feet-level origin sits, for its pelvis to rest in the saddle of the horse model.
        public const float SeatHeight = 0.72f;
        public const float SeatForward = 0.08f;
        // From that origin up to the rider's eyes when seated.
        const float SeatedEyeHeight = 1.7f;
        // Where a rider shot dead, or thrown by a falling horse, ends up beside the horse.
        const float FallBeside = 1.2f;

        [Header("Riding (placeholder numbers, not decided yet)")]
        public float trotSpeed = 5f;
        public float gallopSpeed = 9f;
        public float footWalkSpeed = 2f;
        public float footChaseSpeed = 3.5f;
        // Scales damage taken by the horse, so it can be tougher or softer than the rider.
        public float horseDamageMultiplier = 0.5f;
        public float horseMaxHealth = 6f;

        Transform horse;
        Transform rider;
        float seatHeight;
        float standingEyeHeight;
        ScammerAnimation visuals;

        protected override bool UsesCharacterModel => false;

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
            standingEyeHeight = eyeHeight;
            eyeHeight = seatHeight + SeatedEyeHeight;
            walkSpeed = trotSpeed;
            if (Agent != null) Agent.radius = 0.7f;

            HorseHealth.SetMax(horseMaxHealth);
            HorseHealth.deactivateOnDeath = false;
            foreach (var zone in horse.GetComponentsInChildren<HitZone>())
                zone.Configure(zone.Zone, zone.Multiplier * horseDamageMultiplier);
            HorseHealth.Died += OnHorseDied;
            HorseHealth.Damaged += OnHurt;

            visuals = ScammerAnimation.AttachRider(this, rider, horse);
            if (visuals != null) SetRiderZones(seated: true);
        }

        protected override void OnDestroy()
        {
            if (HorseHealth != null)
            {
                HorseHealth.Died -= OnHorseDied;
                HorseHealth.Damaged -= OnHurt;
            }
            base.OnDestroy();
        }

        // Rides at the last place it saw the player and stops to shoot once it is close enough.
        protected override void Pursue()
        {
            Chase(keepDistance, IsMounted ? gallopSpeed : footChaseSpeed);
        }

        // Shot out of the saddle: the rider falls beside the horse, which stays standing.
        protected override void OnDied(Health health)
        {
            if (IsMounted && visuals != null)
            {
                IsMounted = false;
                rider.localPosition = new Vector3(FallBeside, 0f, 0.1f);
            }
            base.OnDied(health);
        }

        void OnHorseDied(Health health)
        {
            IsMounted = false;
            walkSpeed = footWalkSpeed;
            eyeHeight = standingEyeHeight;
            rider.localPosition = visuals != null ? new Vector3(FallBeside, 0f, 0f) : Vector3.zero;
            if (Agent != null) Agent.radius = 0.4f;
            if (visuals != null)
            {
                visuals.StopHorse();
                SetRiderZones(seated: false);
            }

            foreach (var body in horse.GetComponentsInChildren<Collider>()) body.enabled = false;
            horse.SetParent(null, true);
            // A horse model lies down on its side; the placeholder box tips over like a man.
            StartCoroutine(visuals != null ? Collapse(horse, 0f, 90f, 0.45f) : Collapse(horse));
            // The coroutine stops if the rider is destroyed first, so make sure the horse still goes.
            Destroy(horse.gameObject, CollapseTime + 1f);
        }

        // The rider's two boxes follow the bandit: tucked into the saddle while riding, full height once on foot.
        void SetRiderZones(bool seated)
        {
            // Seated numbers are the bandit's pelvis (0.93) up to its shoulders (1.5) and its head (about 1.68), measured on the model.
            Place(rider.Find("Body"), seated ? new Vector3(0f, 1.22f, 0f) : new Vector3(0f, 0.7f, 0f),
                seated ? new Vector3(0.6f, 0.65f, 0.45f) : new Vector3(0.8f, 1.4f, 0.8f));
            Place(rider.Find("Head"), seated ? new Vector3(0f, 1.68f, 0f) : new Vector3(0f, 1.65f, 0f),
                seated ? new Vector3(0.32f, 0.32f, 0.32f) : new Vector3(0.45f, 0.45f, 0.45f));
        }

        static void Place(Transform zone, Vector3 position, Vector3 scale)
        {
            if (zone == null) return;
            zone.localPosition = position;
            zone.localScale = scale;
        }
    }
}
