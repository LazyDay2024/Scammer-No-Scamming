using UnityEngine;
using UnityEngine.AI;

namespace ScammerNoScamming
{
    // An armed NPC that patrols around its start point, notices the player by sight or by the sound
    // of a shot, and then calls Attack until it loses them. Subclasses decide how it attacks.
    [RequireComponent(typeof(HitscanWeapon))]
    public abstract class Enemy : CombatNpc
    {
        [Header("Side")]
        public Faction faction = Faction.Scammer;

        [Header("Perception")]
        public float sightRange = 25f;
        public float sightHalfAngle = 60f;
        public float hearRange = 40f;
        public float eyeHeight = 1.6f;
        public float loseSightTime = 3f;

        [Header("Patrol")]
        public float patrolRadius = 5f;
        public float walkSpeed = 2f;
        public float turnSpeed = 360f;
        public float patrolPause = 1.5f;

        [Header("Combat (placeholder numbers, not decided yet)")]
        public float reactionTime = 0.6f;
        public float shotDamage = 8f;
        public float shotInterval = 1.2f;
        public float spreadDegrees = 3f;

        protected HitscanWeapon Weapon { get; private set; }
        protected PlayerCharacter Target { get; private set; }
        protected bool CanSeeTarget { get; private set; }
        protected Vector3 LastKnownPosition { get; private set; }
        protected float EngagedSince { get; private set; }

        public bool IsEngaged { get; private set; }

        // Whether the on-foot bandit model replaces the placeholder boxes. Riders keep the boxes until they get a horse model.
        protected virtual bool UsesCharacterModel => true;

        readonly RaycastHit[] hitBuffer = new RaycastHit[16];
        Weapon heardFrom;
        NavMeshAgent agent;
        NavMeshPath scratchPath;
        float patrolDeadline;
        float nextRepath;
        Transform targetEye;
        Vector3 home;
        Vector3 patrolPoint;
        float lastAlertTime = -999f;
        float nextTargetSearch;
        float patrolWaitUntil;
        bool hasPatrolPoint;
        bool provoked;

        protected override void Awake()
        {
            base.Awake();
            Faction = faction;
            Health.Damaged += OnHurt;
            home = transform.position;

            Weapon = GetComponent<HitscanWeapon>();
            Weapon.Damage = shotDamage;
            Weapon.FireInterval = shotInterval;
            Weapon.Ignore = IsAlly;
            // Added here rather than required, so the weapon already exists when the tracer looks for it.
            if (GetComponent<ShotTracer>() == null) gameObject.AddComponent<ShotTracer>();
            if (UsesCharacterModel) ScammerAnimation.Attach(this);

            // Without a baked NavMesh under it the enemy walks straight lines instead (see PatrolStraight).
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit ground, 2f, NavMesh.AllAreas))
            {
                transform.position = ground.position;
                scratchPath = new NavMeshPath();
                agent = gameObject.AddComponent<NavMeshAgent>();
                agent.updateRotation = false;
                agent.radius = 0.4f;
                agent.height = 1.8f;
                agent.stoppingDistance = 0.2f;
            }
        }

        protected override float DeathAnimationTime
        {
            get
            {
                var animation = GetComponent<ScammerAnimation>();
                return animation != null ? animation.DeathSeconds : 0f;
            }
        }

        protected override void OnDied(Health health)
        {
            if (agent != null) agent.enabled = false;
            base.OnDied(health);
        }

        protected override void OnDestroy()
        {
            if (heardFrom != null) heardFrom.Fired -= OnHeardShot;
            if (Health != null) Health.Damaged -= OnHurt;
            base.OnDestroy();
        }

        // Being shot by the player tells the enemy where they are and, from then on, sight no longer
        // has a range or a field of view (a wall still blocks it). Riders also call this for their horse.
        protected void OnHurt(Health health, DamageInfo info)
        {
            if (info.SourceFaction != Faction.Player || Health.IsDead || !ResolveTarget()) return;
            provoked = true;
            LastKnownPosition = Target.transform.position;
            lastAlertTime = Time.time;
        }

        // Where the enemy aims on the target: the player's eye line, dropped a little to the chest.
        protected Vector3 AimPoint => (targetEye != null ? targetEye.position : Target.transform.position + Vector3.up) + Vector3.down * 0.3f;
        protected Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

        protected NavMeshAgent Agent => agent;
        protected Transform TargetEye => targetEye;

        // Switches sides after spawning, e.g. riders called in as corrupt police.
        public void SetFaction(Faction side)
        {
            faction = side;
            Faction = side;
        }

        // Makes the enemy act as if it just noticed the player at this spot, for ambushes and alarms.
        public void Alert(Vector3 position)
        {
            LastKnownPosition = position;
            lastAlertTime = Time.time;
        }

        // Runs at the last known spot, stopping once it can see the player within stopDistance.
        protected void Chase(float stopDistance, float speed)
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                StopWalking();
                FaceTowards(LastKnownPosition);
                return;
            }

            Vector3 toPlayer = LastKnownPosition - transform.position;
            toPlayer.y = 0f;
            if (CanSeeTarget && toPlayer.magnitude <= stopDistance)
            {
                StopWalking();
                FaceTowards(LastKnownPosition);
                return;
            }

            agent.isStopped = false;
            agent.speed = speed;
            if (Time.time >= nextRepath)
            {
                agent.SetDestination(LastKnownPosition);
                nextRepath = Time.time + 0.25f;
            }
            FaceTowards(CanSeeTarget ? LastKnownPosition : agent.steeringTarget);
        }

        protected abstract void Attack(PlayerCharacter target);

        // What the enemy does with its feet while it is engaged. The default stands its ground and faces the player.
        protected virtual void Pursue()
        {
            StopWalking();
            FaceTowards(LastKnownPosition);
        }

        protected override void Tick()
        {
            if (!ResolveTarget())
            {
                Patrol();
                return;
            }

            CanSeeTarget = !Target.Health.IsDead && CanSee();
            if (CanSeeTarget)
            {
                LastKnownPosition = Target.transform.position;
                lastAlertTime = Time.time;
            }

            bool engaged = !Target.Health.IsDead && Time.time - lastAlertTime < loseSightTime;
            if (engaged && !IsEngaged) EngagedSince = Time.time;
            IsEngaged = engaged;

            if (!IsEngaged)
            {
                Patrol();
                return;
            }

            Pursue();
            if (CanSeeTarget && Time.time - EngagedSince >= reactionTime) Attack(Target);
        }

        // Wanders between random points inside patrolRadius of where it started, pausing between legs.
        protected virtual void Patrol()
        {
            if (patrolRadius <= 0f) return;
            if (agent != null && agent.isOnNavMesh) PatrolOnNavMesh();
            else PatrolStraight();
        }

        void PatrolOnNavMesh()
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;

            if (hasPatrolPoint)
            {
                bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
                if (!arrived && Time.time < patrolDeadline)
                {
                    FaceTowards(agent.steeringTarget);
                    return;
                }
                StopWalking();
                patrolWaitUntil = Time.time + Random.Range(patrolPause * 0.5f, patrolPause * 1.5f);
            }

            if (Time.time < patrolWaitUntil) return;

            if (TryPickPatrolPoint(out Vector3 point) && agent.SetDestination(point))
            {
                hasPatrolPoint = true;
                patrolDeadline = Time.time + 20f;
            }
            else
            {
                patrolWaitUntil = Time.time + patrolPause;
            }
        }

        // Samples a few reachable spots in the patch and takes the one most ahead of where it is looking,
        // so it carries on in roughly the same direction instead of turning back on itself.
        bool TryPickPatrolPoint(out Vector3 best)
        {
            best = default;
            float bestScore = float.MinValue;
            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = Random.insideUnitCircle * patrolRadius;
                Vector3 wanted = home + new Vector3(offset.x, 0f, offset.y);
                if (!NavMesh.SamplePosition(wanted, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;

                Vector3 toSpot = hit.position - transform.position;
                toSpot.y = 0f;
                if (toSpot.magnitude < Mathf.Min(1.5f, patrolRadius * 0.5f)) continue;
                if (!agent.CalculatePath(hit.position, scratchPath) || scratchPath.status != NavMeshPathStatus.PathComplete) continue;

                float score = Vector3.Dot(transform.forward, toSpot.normalized) + Random.value * 0.5f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = hit.position;
                }
            }
            return bestScore > float.MinValue;
        }

        protected void StopWalking()
        {
            hasPatrolPoint = false;
            if (agent != null && agent.isOnNavMesh && agent.hasPath) agent.ResetPath();
        }

        // shortcut: straight-line walking on flat ground with no obstacle avoidance, used only when there is no NavMesh; bake one with RuntimeNavMesh.
        void PatrolStraight()
        {
            if (Time.time < patrolWaitUntil) return;

            if (!hasPatrolPoint)
            {
                Vector2 offset = Random.insideUnitCircle * patrolRadius;
                patrolPoint = home + new Vector3(offset.x, 0f, offset.y);
                hasPatrolPoint = true;
            }

            Vector3 toPoint = patrolPoint - transform.position;
            toPoint.y = 0f;
            if (toPoint.magnitude < 0.3f)
            {
                hasPatrolPoint = false;
                patrolWaitUntil = Time.time + patrolPause;
                return;
            }

            FaceTowards(patrolPoint);
            // Straight at the point, not along the facing, so a slow turn cannot drift it out of the patch.
            transform.position = Vector3.MoveTowards(transform.position, transform.position + toPoint, walkSpeed * Time.deltaTime);
        }

        protected virtual void OnHeardShot(ShotResult shot)
        {
            Vector3 position = shot.NoisePosition;
            if (Health.IsDead || (position - transform.position).sqrMagnitude > hearRange * hearRange) return;
            LastKnownPosition = position;
            lastAlertTime = Time.time;
        }

        protected void FaceTowards(Vector3 point)
        {
            Vector3 flat = point - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;
            Quaternion want = Quaternion.LookRotation(flat);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
        }

        bool ResolveTarget()
        {
            if (Target != null) return true;
            if (Time.time < nextTargetSearch) return false;
            nextTargetSearch = Time.time + 0.5f;

            Target = FindFirstObjectByType<PlayerCharacter>();
            if (Target == null) return false;

            var cam = Target.GetComponentInChildren<Camera>();
            targetEye = cam != null ? cam.transform : null;
            heardFrom = Target.GetComponent<Weapon>();
            if (heardFrom != null) heardFrom.Fired += OnHeardShot;
            return true;
        }

        bool CanSee()
        {
            Vector3 eye = EyePosition;
            Vector3 toTarget = AimPoint - eye;
            if (!provoked)
            {
                if (toTarget.magnitude > sightRange) return false;

                Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
                if (Vector3.Angle(transform.forward, flat) > sightHalfAngle) return false;
            }

            return HasLineOfSight(eye, AimPoint);
        }

        // True when nothing but the target's side, this enemy and horses stands between the two points.
        protected bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            float distance = offset.magnitude;
            if (distance < 0.01f) return true;

            int count = Physics.RaycastNonAlloc(from, offset / distance, hitBuffer, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider other = hitBuffer[i].collider;
                if (other.transform.IsChildOf(transform)) continue;
                if (other.GetComponentInParent<HorseController>() != null) continue;
                if (other.GetComponentInParent<Character>() == Target) continue;
                return false;
            }
            return true;
        }

        // Shots pass through the shooter's own side so scammers do not gun each other down.
        bool IsAlly(Collider other)
        {
            var character = other.GetComponentInParent<Character>();
            return character != null && character.Faction == Faction;
        }
    }
}
