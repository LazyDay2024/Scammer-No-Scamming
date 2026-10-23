using UnityEngine;
using UnityEngine.AI;

namespace ScammerNoScamming
{
    public enum ArmedEnemyState { Idle, Alert, Attack, Search, Dead }

    [RequireComponent(typeof(HitscanWeapon))]
    public class ArmedEnemy : Npc
    {
        public static float SightMultiplier = 1f;

        [Header("Perception")]
        public float hearingRange = 35f;
        public float sightRange = 40f;
        public float sightAngle = 120f;
        public float eyeHeight = 1.6f;
        public float targetAimHeight = 0.95f;
        public LayerMask sightMask = ~0;
        public float nearbyAlertRange = 25f;
        public float alertDelay = 0.3f;
        public float searchDuration = 6f;

        [Header("Movement")]
        public Transform[] waypoints;
        public float moveSpeed = 3.5f;
        public float turnSpeed = 240f;
        public float waypointTolerance = 1f;
        public float minimumAttackDistance = 8f;
        public float maximumAttackDistance = 18f;
        public float preferredAttackDistance = 13f;
        public float navMeshSampleRadius = 3f;

        [Header("Weapon")]
        public float damage = 8f;
        public float fireInterval = 0.8f;
        public float weaponRange = 120f;
        public float initialAimError = 6f;
        public float finalAimError = 0.8f;
        public float aimTightenTime = 2f;
        public Transform muzzle;
        public float muzzleHeight = 1.35f;

        [Header("Muzzle flash and tracer")]
        public Color flashColor = new Color(1f, 0.8f, 0.3f);
        public float flashIntensity = 2f;
        public float flashRange = 4f;
        public float flashDuration = 0.05f;
        public float tracerDuration = 0.07f;
        public float tracerWidth = 0.035f;
        public float tracerEndWidth = 0.01f;

        public ArmedEnemyState State { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }
        public HitscanWeapon Weapon { get; private set; }
        public float VisibleDuration { get; private set; }

        readonly RaycastHit[] sightHits = new RaycastHit[32];
        NavMeshAgent agent;
        Light muzzleLight;
        LineRenderer tracer;
        Material tracerMaterial;
        float alertTimeLeft;
        float searchTimeLeft;
        float nextShotTime;
        float flashHideTime;
        float tracerHideTime;
        int waypointIndex;

        protected override void Awake()
        {
            base.Awake();
            if (Faction != ScammerNoScamming.Faction.Scammer && Faction != ScammerNoScamming.Faction.CorruptPolice)
                Faction = ScammerNoScamming.Faction.Scammer;
            agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.updateRotation = false;
            Weapon = GetComponent<HitscanWeapon>();
            if (Application.isPlaying && GetComponent<EnemyShotAudio>() == null)
                gameObject.AddComponent<EnemyShotAudio>();
            Weapon.Ignore = collider => collider.transform.IsChildOf(transform);
            if (muzzle == null)
            {
                foreach (var child in GetComponentsInChildren<Transform>(true))
                    if (child.name == "Muzzle") { muzzle = child; break; }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            NpcGunfire.Heard += HearShot;
            Health.Damaged += OnEnemyDamaged;
        }

        protected override void OnDisable()
        {
            NpcGunfire.Heard -= HearShot;
            if (Health != null) Health.Damaged -= OnEnemyDamaged;
            HideEffects();
            base.OnDisable();
        }

        void Start() => CreateEffects();

        public override void ResetForAction()
        {
            base.ResetForAction();
            State = ArmedEnemyState.Idle;
            LastKnownPosition = transform.position;
            VisibleDuration = alertTimeLeft = searchTimeLeft = 0f;
            nextShotTime = 0f;
            waypointIndex = 0;
            HideEffects();
        }

        void Update()
        {
            if (muzzleLight != null && Time.time >= flashHideTime) muzzleLight.enabled = false;
            if (tracer != null && Time.time >= tracerHideTime) tracer.enabled = false;
            Tick(Time.deltaTime);
        }

        public virtual void Tick(float deltaTime)
        {
            if (IsDead || State == ArmedEnemyState.Dead) return;
            var player = PlayerLocator.Player;
            bool canSee = player != null && CanSeePlayer(player);
            Vector3 playerPosition = player != null ? player.transform.position : LastKnownPosition;
            TickPerception(canSee, playerPosition, deltaTime);

            switch (State)
            {
                case ArmedEnemyState.Idle:
                    Patrol(deltaTime);
                    break;
                case ArmedEnemyState.Alert:
                    StopMoving();
                    Face(LastKnownPosition, deltaTime);
                    break;
                case ArmedEnemyState.Attack:
                    if (player == null) break;
                    KeepDistance(playerPosition);
                    Face(playerPosition, deltaTime);
                    if (canSee) AimAndFire(playerPosition + Vector3.up * targetAimHeight);
                    break;
                case ArmedEnemyState.Search:
                    MoveTo(LastKnownPosition);
                    Face(LastKnownPosition, deltaTime);
                    break;
            }
            Animation.SetSpeed(CanNavigate ? agent.velocity.magnitude : 0f);
        }

        // Perception can be stepped without a scene or NavMesh for deterministic state tests.
        public void TickPerception(bool seesPlayer, Vector3 playerPosition, float deltaTime)
        {
            if (IsDead || State == ArmedEnemyState.Dead) return;
            float dt = Mathf.Max(0f, deltaTime);
            if (seesPlayer)
            {
                LastKnownPosition = playerPosition;
                VisibleDuration += dt;
                if (State == ArmedEnemyState.Idle || State == ArmedEnemyState.Search) Alert(playerPosition);
                if (State == ArmedEnemyState.Alert)
                {
                    alertTimeLeft -= dt;
                    if (alertTimeLeft <= 0f) SetState(ArmedEnemyState.Attack);
                }
                return;
            }

            VisibleDuration = 0f;
            if (State == ArmedEnemyState.Attack)
            {
                BeginSearch();
            }
            else if (State == ArmedEnemyState.Alert)
            {
                alertTimeLeft -= dt;
                if (alertTimeLeft <= 0f) BeginSearch();
            }
            else if (State == ArmedEnemyState.Search)
            {
                searchTimeLeft -= dt;
                if (searchTimeLeft <= 0f)
                {
                    SetState(ArmedEnemyState.Idle);
                    StopMoving();
                }
            }
        }

        public void HearShot(Vector3 position)
        {
            float distanceSquared = (position - transform.position).sqrMagnitude;
            // Do not investigate our own muzzle as a new player position.
            if (distanceSquared < 0.001f || distanceSquared > hearingRange * hearingRange) return;
            Alert(position);
        }

        public void Alert(Vector3 position) => Alert(position, true);

        void Alert(Vector3 position, bool tellNeighbours)
        {
            if (IsDead || State == ArmedEnemyState.Dead || State == ArmedEnemyState.Attack) return;
            LastKnownPosition = position;
            if (State == ArmedEnemyState.Alert) return;
            alertTimeLeft = Mathf.Max(0f, alertDelay);
            SetState(ArmedEnemyState.Alert);
            if (!tellNeighbours) return;
            foreach (var npc in All)
            {
                if (!(npc is ArmedEnemy other) || other == this || other == null || other.IsDead) continue;
                if ((other.transform.position - transform.position).sqrMagnitude <= nearbyAlertRange * nearbyAlertRange)
                    other.Alert(position, false);
            }
        }

        public bool CanSeePlayer(PlayerController player)
        {
            if (player == null || !player.gameObject.activeInHierarchy) return false;
            var health = player.GetComponent<Health>();
            if (health != null && health.IsDead) return false;
            Vector3 from = transform.position + Vector3.up * eyeHeight;
            Vector3 to = player.transform.position + Vector3.up * targetAimHeight;
            Vector3 direction = to - from;
            float range = Mathf.Max(0f, sightRange * SightMultiplier);
            if (direction.sqrMagnitude > range * range) return false;
            if (Vector3.Angle(transform.forward, direction) > Mathf.Clamp(sightAngle, 0f, 360f) * 0.5f) return false;

            float distance = direction.magnitude;
            if (distance < 0.001f) return true;
            int count = Physics.RaycastNonAlloc(from, direction / distance, sightHits, distance, sightMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = sightHits[i].collider;
                if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(player.transform)) continue;
                return false;
            }
            return true;
        }

        public static float CalculateAimError(float visibleSeconds, float initialError, float finalError, float tightenSeconds)
        {
            float fraction = tightenSeconds <= 0f ? 1f : Mathf.Clamp01(visibleSeconds / tightenSeconds);
            return Mathf.Lerp(Mathf.Max(0f, initialError), Mathf.Max(0f, finalError), fraction);
        }

        protected void AimAndFire(Vector3 target)
        {
            if (Weapon == null || Time.time < nextShotTime) return;
            Vector3 origin = muzzle != null ? muzzle.position : transform.position + Vector3.up * muzzleHeight;
            Vector3 forward = target - origin;
            if (forward.sqrMagnitude < 0.001f) return;
            float cone = CalculateAimError(VisibleDuration, initialAimError, finalAimError, aimTightenTime);
            Vector2 error = Random.insideUnitCircle * Mathf.Tan(Mathf.Clamp(cone, 0f, 89f) * Mathf.Deg2Rad);
            Vector3 direction = (Quaternion.LookRotation(forward) * new Vector3(error.x, error.y, 1f)).normalized;
            Weapon.Damage = Mathf.Max(0f, damage);
            Weapon.Range = Mathf.Max(0f, weaponRange);
            Weapon.FireInterval = Mathf.Max(0f, fireInterval);
            if (!Weapon.TryFire(origin, direction, Faction, out ShotResult shot)) return;
            nextShotTime = Time.time + Mathf.Max(0f, fireInterval);
            Animation.Shoot();
            ShowEffects(origin, shot.EndPoint);
        }

        bool CanNavigate => agent != null && agent.enabled && agent.isOnNavMesh;

        void KeepDistance(Vector3 target)
        {
            Vector3 away = transform.position - target;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance > maximumAttackDistance)
                MoveTo(target + away.normalized * preferredAttackDistance);
            else if (distance < minimumAttackDistance)
                MoveTo(target + (distance > 0.01f ? away.normalized : -transform.forward) * preferredAttackDistance);
            else
                StopMoving();
        }

        void Patrol(float deltaTime)
        {
            if (waypoints == null || waypoints.Length == 0) { StopMoving(); return; }
            if (waypointIndex >= waypoints.Length) waypointIndex = 0;
            var waypoint = waypoints[waypointIndex];
            if (waypoint == null) { waypointIndex = (waypointIndex + 1) % waypoints.Length; return; }
            if ((waypoint.position - transform.position).sqrMagnitude <= waypointTolerance * waypointTolerance)
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
            MoveTo(waypoint.position);
            Face(waypoint.position, deltaTime);
        }

        void MoveTo(Vector3 position)
        {
            if (!CanNavigate) return;
            agent.speed = Mathf.Max(0f, moveSpeed);
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, Mathf.Max(0f, navMeshSampleRadius), agent.areaMask)) return;
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }

        void StopMoving()
        {
            if (CanNavigate) agent.isStopped = true;
        }

        void Face(Vector3 position, float deltaTime)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), Mathf.Max(0f, turnSpeed * deltaTime));
        }

        void SetState(ArmedEnemyState state)
        {
            State = state;
            Animation.Aim(state == ArmedEnemyState.Alert || state == ArmedEnemyState.Attack);
        }

        void BeginSearch()
        {
            searchTimeLeft = Mathf.Max(0f, searchDuration);
            SetState(ArmedEnemyState.Search);
        }

        void OnEnemyDamaged(Health health, DamageInfo info)
        {
            if (info.SourceFaction != ScammerNoScamming.Faction.Player) return;
            var player = PlayerLocator.Player;
            Alert(player != null ? player.transform.position : info.Point);
        }

        protected override void OnDeath()
        {
            SetState(ArmedEnemyState.Dead);
            HideEffects();
            base.OnDeath();
        }

        void CreateEffects()
        {
            var flash = new GameObject("Enemy Muzzle Flash");
            flash.transform.SetParent(transform, false);
            muzzleLight = flash.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.shadows = LightShadows.None;
            muzzleLight.enabled = false;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            tracerMaterial = new Material(shader) { color = flashColor };
            var trail = new GameObject("Enemy Tracer");
            trail.transform.SetParent(transform, false);
            tracer = trail.AddComponent<LineRenderer>();
            tracer.sharedMaterial = tracerMaterial;
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.startWidth = Mathf.Max(0f, tracerWidth);
            tracer.endWidth = Mathf.Max(0f, tracerEndWidth);
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracer.enabled = false;
        }

        void ShowEffects(Vector3 from, Vector3 to)
        {
            if (muzzleLight != null)
            {
                muzzleLight.transform.position = from;
                muzzleLight.color = flashColor;
                muzzleLight.intensity = Mathf.Max(0f, flashIntensity);
                muzzleLight.range = Mathf.Max(0f, flashRange);
                muzzleLight.enabled = true;
                flashHideTime = Time.time + Mathf.Max(0f, flashDuration);
            }
            if (tracer == null) return;
            tracer.SetPosition(0, from);
            tracer.SetPosition(1, to);
            tracer.enabled = true;
            tracerHideTime = Time.time + Mathf.Max(0f, tracerDuration);
        }

        void HideEffects()
        {
            if (muzzleLight != null) muzzleLight.enabled = false;
            if (tracer != null) tracer.enabled = false;
        }

        void OnDestroy()
        {
            if (tracerMaterial == null) return;
            if (Application.isPlaying) Destroy(tracerMaterial);
            else DestroyImmediate(tracerMaterial);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSightMultiplier() => SightMultiplier = 1f;
    }
}
