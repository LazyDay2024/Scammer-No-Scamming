using UnityEngine;
using UnityEngine.AI;

namespace ScammerNoScamming
{
    // An ArmedEnemy with terrain movement: its horse is never driven by player input or a NavMesh.
    public class MountedScammer : ArmedEnemy
    {
        [Header("Mount")]
        public Transform horseRoot;
        public Transform riderRoot;
        public Health horseHealth;
        public Animator horseAnimator;
        public float ridingSpeed = 12f;
        public float footSpeed = 3.5f;
        public float dismountOffset = 2f;
        public float horseWalkSpeed = 4f;
        public float gallopPlayback = 2.2f;

        [Header("Mounted flank")]
        public float flankSide = 1f;
        public float minimumFlankDistance = 15f;
        public float maximumFlankDistance = 30f;
        public float lateralFlankDistance = 6f;

        [Header("Terrain")]
        public LayerMask groundMask = ~0;
        public float groundProbeHeight = 8f;
        public float groundProbeDepth = 35f;
        [Range(0f, 1f)] public float minimumGroundNormal = .5f;
        public float maximumGroundStep = 1f;

        public bool IsMounted { get; private set; }

        readonly RaycastHit[] groundHits = new RaycastHit[64];
        BanditAnimator bandit;
        Health subscribedHorse;
        bool resetCaptured;
        bool pursuing;
        int pathIndex;
        Transform horseParent;
        Vector3 horseLocalPosition, horseLocalScale, riderLocalPosition;
        Quaternion horseLocalRotation, riderLocalRotation;
        Collider[] horseColliders;
        bool[] horseColliderEnabled;
        bool horseAnimatorEnabled;
        float horseAnimatorSpeed = 1f;
        float mountedEyeHeight, mountedMuzzleHeight;
        BoxCollider seatedBody;
        Vector3 seatedBodyPosition, seatedBodyCenter, seatedBodySize;

        protected override void Awake()
        {
            CaptureMountedState();
            base.Awake();
            Faction = ScammerNoScamming.Faction.Scammer;
            DisablePlayerMovement();
            ConfigureMountedAnimation();
            if (Weapon != null)
                Weapon.Ignore = other => other.transform.IsChildOf(transform) ||
                    (IsMounted && horseRoot != null && other.transform.IsChildOf(horseRoot));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeHorse();
        }

        protected override void OnDisable()
        {
            if (subscribedHorse != null)
            {
                subscribedHorse.Died -= OnHorseDied;
                subscribedHorse.Damaged -= OnHorseDamaged;
            }
            subscribedHorse = null;
            base.OnDisable();
        }

        void CaptureMountedState()
        {
            if (resetCaptured) return;
            resetCaptured = true;
            if (horseHealth == null && horseRoot != null) horseHealth = horseRoot.GetComponent<Health>();
            if (horseAnimator == null && horseRoot != null) horseAnimator = horseRoot.GetComponentInChildren<Animator>(true);
            bandit = GetComponentInChildren<BanditAnimator>(true);
            IsMounted = horseRoot != null && horseHealth != null;
            mountedEyeHeight = eyeHeight;
            mountedMuzzleHeight = muzzleHeight;
            if (riderRoot != null)
            {
                riderLocalPosition = riderRoot.localPosition;
                riderLocalRotation = riderRoot.localRotation;
                seatedBody = bodyCollider as BoxCollider;
                if (seatedBody != null && seatedBody.transform.parent == riderRoot)
                {
                    seatedBodyPosition = seatedBody.transform.localPosition;
                    seatedBodyCenter = seatedBody.center;
                    seatedBodySize = seatedBody.size;
                }
                else seatedBody = null;
            }
            if (horseRoot != null)
            {
                horseParent = horseRoot.parent;
                horseLocalPosition = horseRoot.localPosition;
                horseLocalRotation = horseRoot.localRotation;
                horseLocalScale = horseRoot.localScale;
                horseColliders = horseRoot.GetComponentsInChildren<Collider>(true);
                horseColliderEnabled = new bool[horseColliders.Length];
                for (int i = 0; i < horseColliders.Length; i++) horseColliderEnabled[i] = horseColliders[i].enabled;
            }
            if (horseHealth != null) horseHealth.deactivateOnDeath = false;
            if (horseAnimator != null)
            {
                horseAnimatorEnabled = horseAnimator.enabled;
                horseAnimatorSpeed = horseAnimator.speed;
                horseAnimator.applyRootMotion = false;
            }
        }

        void SubscribeHorse()
        {
            if (subscribedHorse == horseHealth) return;
            if (subscribedHorse != null)
            {
                subscribedHorse.Died -= OnHorseDied;
                subscribedHorse.Damaged -= OnHorseDamaged;
            }
            subscribedHorse = horseHealth;
            if (subscribedHorse == null) return;
            subscribedHorse.deactivateOnDeath = false;
            subscribedHorse.Died += OnHorseDied;
            subscribedHorse.Damaged += OnHorseDamaged;
        }

        void DisablePlayerMovement()
        {
            foreach (var horse in GetComponentsInChildren<HorseController>(true)) horse.enabled = false;
            foreach (var controller in GetComponentsInChildren<CharacterController>(true)) controller.enabled = false;
            foreach (var agent in GetComponentsInChildren<NavMeshAgent>(true)) agent.enabled = false;
        }

        void ConfigureMountedAnimation()
        {
            if (bandit != null) bandit.Mounted = IsMounted;
        }

        public override void ResetForAction()
        {
            // Encounter templates can reset an inactive actor before its first Awake.
            CaptureMountedState();
            if (horseRoot != null)
            {
                horseRoot.SetParent(horseParent, false);
                horseRoot.SetLocalPositionAndRotation(horseLocalPosition, horseLocalRotation);
                horseRoot.localScale = horseLocalScale;
                horseRoot.gameObject.SetActive(true);
                for (int i = 0; i < horseColliders.Length; i++)
                    if (horseColliders[i] != null) horseColliders[i].enabled = horseColliderEnabled[i];
            }
            IsMounted = horseRoot != null && horseHealth != null;
            if (horseHealth != null)
            {
                horseHealth.deactivateOnDeath = false;
                horseHealth.RestoreFull(horseHealth.max);
            }
            if (riderRoot != null) riderRoot.SetLocalPositionAndRotation(riderLocalPosition, riderLocalRotation);
            if (seatedBody != null)
            {
                seatedBody.transform.localPosition = seatedBodyPosition;
                seatedBody.center = seatedBodyCenter;
                seatedBody.size = seatedBodySize;
            }
            eyeHeight = mountedEyeHeight;
            muzzleHeight = mountedMuzzleHeight;
            ConfigureMountedAnimation();
            base.ResetForAction();
            DisablePlayerMovement();
            if (horseAnimator != null)
            {
                horseAnimator.enabled = horseAnimatorEnabled;
                horseAnimator.speed = horseAnimatorSpeed;
                if (horseAnimator.runtimeAnimatorController != null && horseAnimator.isActiveAndEnabled)
                {
                    horseAnimator.Rebind();
                    horseAnimator.Update(0f);
                    horseAnimator.SetFloat("Speed", 0f);
                }
            }
            pursuing = false;
            pathIndex = 0;
            if (isActiveAndEnabled) SubscribeHorse();
        }

        public override void Tick(float deltaTime)
        {
            if (IsDead || State == ArmedEnemyState.Dead) return;
            float dt = Mathf.Max(0f, deltaTime);
            if (ShouldDismount(IsMounted, horseHealth != null && horseHealth.IsDead, IsDead)) Dismount();
            var player = PlayerLocator.Player;
            bool canSee = player != null && CanSeePlayer(player);
            Vector3 target = player != null ? player.transform.position : LastKnownPosition;
            if (State != ArmedEnemyState.Idle) pursuing = true;
            TickPerception(canSee, target, dt);
            if (State != ArmedEnemyState.Idle) pursuing = true;
            Vector3 previous = transform.position;

            if (IsMounted)
            {
                if (pursuing)
                {
                    Vector3 destination = LastKnownPosition;
                    if (player != null)
                        destination = CalculateFlankDestination(transform.position, target, RoadForward(target, player.transform.forward),
                            flankSide, minimumFlankDistance, maximumFlankDistance, lateralFlankDistance);
                    if (TryPathDestination(destination, player != null ? target : (Vector3?)null,
                        out Vector3 pathDestination)) destination = pathDestination;
                    MoveOverGround(destination, ridingSpeed, dt, true, player != null ? target : (Vector3?)null);
                    if (player != null && riderRoot != null)
                    {
                        Vector3 aim = target - riderRoot.position;
                        aim.y = 0f;
                        if (aim.sqrMagnitude > .001f)
                            riderRoot.rotation = Quaternion.RotateTowards(riderRoot.rotation,
                                Quaternion.LookRotation(aim), Mathf.Max(0f, turnSpeed) * dt);
                    }
                    // Sight and gunfire stay honest even when the rider is following the road behind rocks.
                    if (canSee && State == ArmedEnemyState.Attack) AimAndFire(target + Vector3.up * targetAimHeight);
                }
            }
            else
            {
                // Same perception, accuracy, spacing and weapon as an ArmedEnemy; terrain motion replaces its agent.
                if (State == ArmedEnemyState.Attack && player != null)
                {
                    Vector3 away = transform.position - target;
                    away.y = 0f;
                    float distance = away.magnitude;
                    if (distance < minimumAttackDistance || distance > maximumAttackDistance)
                    {
                        Vector3 direction = distance > .01f ? away / distance : -transform.forward;
                        MoveOverGround(target + direction * preferredAttackDistance, footSpeed, dt, false);
                    }
                    FaceTarget(target, dt);
                    if (canSee) AimAndFire(target + Vector3.up * targetAimHeight);
                }
                else if (State == ArmedEnemyState.Search)
                    MoveOverGround(LastKnownPosition, footSpeed, dt, true);
                else if (State == ArmedEnemyState.Alert) FaceTarget(LastKnownPosition, dt);
            }

            float speed = dt > 0f ? Vector3.Distance(previous, transform.position) / dt : 0f;
            Animation.SetSpeed(speed);
            UpdateHorseAnimation(IsMounted ? speed : 0f);
        }

        bool TryPathDestination(Vector3 flankDestination, Vector3? playerPosition, out Vector3 destination)
        {
            destination = transform.position;
            if (playerPosition.HasValue && !IsWaypointOutsideMinimumDistance(transform.position,
                transform.position, playerPosition.Value, minimumFlankDistance)) return false;
            while (waypoints != null && pathIndex < waypoints.Length)
            {
                var point = waypoints[pathIndex];
                if (point == null) { pathIndex++; continue; }
                Vector3 offset = point.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= Mathf.Max(1f, waypointTolerance * waypointTolerance)) { pathIndex++; continue; }
                Vector3 tangent = RoadForward(point.position, transform.forward);
                if (pathIndex > 0 && Vector3.Dot(offset, tangent) < 0f) { pathIndex++; continue; }
                // Retain the index while unsafe, so a moving player can open this road segment later.
                // Check the entire segment, including the initial egress and join, not only its endpoint.
                if (playerPosition.HasValue && !IsWaypointOutsideMinimumDistance(transform.position,
                    point.position, playerPosition.Value, minimumFlankDistance)) return false;
                if (pathIndex >= 2)
                {
                    // Preserve the initial rock egress, then use only road points between us and the flank.
                    if (Vector3.Dot(flankDestination - point.position, tangent) < 0f) return false;
                }
                destination = point.position;
                return true;
            }
            return false;
        }

        Vector3 RoadForward(Vector3 position, Vector3 fallback)
        {
            float nearest = float.MaxValue;
            Vector3 direction = fallback;
            // Skip the two authored rock-egress points when selecting the canyon road's heading.
            for (int i = 2; waypoints != null && i < waypoints.Length; i++)
            {
                if (waypoints[i] == null || waypoints[i - 1] == null) continue;
                Vector3 from = waypoints[i - 1].position;
                Vector3 segment = waypoints[i].position - from;
                segment.y = 0f;
                if (segment.sqrMagnitude < .001f) continue;
                Vector3 relative = position - from;
                relative.y = 0f;
                float progress = Mathf.Clamp01(Vector3.Dot(relative, segment) / segment.sqrMagnitude);
                float distance = (relative - segment * progress).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                direction = segment.normalized;
            }
            return direction;
        }

        void MoveOverGround(Vector3 destination, float speed, float dt, bool faceMovement, Vector3? mountedPlayer = null)
        {
            Vector3 offset = destination - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < .01f || dt <= 0f) return;
            Vector3 next = transform.position + Vector3.ClampMagnitude(offset, Mathf.Max(0f, speed) * dt);
            if (mountedPlayer.HasValue && !CanTakeMountedStep(transform.position, next,
                mountedPlayer.Value, minimumFlankDistance)) return;
            if (!TryGround(next, out Vector3 grounded)) return;
            float stepLength = Vector3.Distance(next, transform.position);
            if (Mathf.Abs(grounded.y - transform.position.y) > Mathf.Max(.1f, maximumGroundStep) + stepLength) return;
            transform.position = grounded;
            if (faceMovement) FaceTarget(destination, dt);
        }

        bool TryGround(Vector3 point, out Vector3 grounded)
        {
            grounded = point;
            float height = Mathf.Max(.1f, groundProbeHeight);
            int count = Physics.RaycastNonAlloc(point + Vector3.up * height, Vector3.down, groundHits,
                height + Mathf.Max(.1f, groundProbeDepth), groundMask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                var collider = hit.collider;
                if (collider == null || collider.transform.IsChildOf(transform) ||
                    (horseRoot != null && collider.transform.IsChildOf(horseRoot)) ||
                    collider.GetComponentInParent<Npc>() != null || collider.GetComponentInParent<PlayerController>() != null ||
                    hit.normal.y < minimumGroundNormal || hit.distance >= nearest) continue;
                grounded = hit.point;
                nearest = hit.distance;
                found = true;
            }
            return found;
        }

        void FaceTarget(Vector3 target, float dt)
        {
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < .001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction),
                Mathf.Max(0f, turnSpeed) * dt);
        }

        void UpdateHorseAnimation(float speed)
        {
            if (horseAnimator == null || !horseAnimator.isActiveAndEnabled || horseAnimator.runtimeAnimatorController == null) return;
            horseAnimator.SetFloat("Speed", speed);
            horseAnimator.speed = speed > .1f
                ? Mathf.Lerp(1f, Mathf.Max(1f, gallopPlayback), Mathf.InverseLerp(horseWalkSpeed, Mathf.Max(horseWalkSpeed + .1f, ridingSpeed), speed))
                : 1f;
        }

        void OnHorseDamaged(Health health, DamageInfo info)
        {
            if (info.SourceFaction != ScammerNoScamming.Faction.Player) return;
            var player = PlayerLocator.Player;
            Alert(player != null ? player.transform.position : info.Point);
        }

        void OnHorseDied(Health health)
        {
            if (ShouldDismount(IsMounted, health.IsDead, IsDead)) Dismount();
        }

        void Dismount()
        {
            IsMounted = false;
            ConfigureMountedAnimation();
            UpdateHorseAnimation(0f);
            if (horseAnimator != null) horseAnimator.enabled = false;
            if (horseRoot != null)
            {
                Vector3 exit = transform.position + transform.right * (flankSide < 0f ? -dismountOffset : dismountOffset);
                // The corpse remains inside the encounter template, so retry cleanup owns it too.
                horseRoot.SetParent(transform.parent, true);
                horseRoot.rotation *= Quaternion.Euler(0f, 0f, 90f);
                horseRoot.position += Vector3.up * .5f;
                foreach (var collider in horseColliders) if (collider != null) collider.enabled = false;
                if (TryGround(exit, out Vector3 grounded)) transform.position = grounded;
            }
            if (riderRoot != null) riderRoot.SetLocalPositionAndRotation(Vector3.zero, riderLocalRotation);
            if (seatedBody != null)
            {
                seatedBody.transform.localPosition = new Vector3(seatedBodyPosition.x, .99f, seatedBodyPosition.z);
                seatedBody.center = Vector3.zero;
                seatedBody.size = new Vector3(seatedBodySize.x, 1.15f, seatedBodySize.z);
            }
            float seatHeight = Mathf.Max(0f, riderLocalPosition.y);
            eyeHeight = Mathf.Max(.5f, mountedEyeHeight - seatHeight);
            muzzleHeight = Mathf.Max(.5f, mountedMuzzleHeight - seatHeight);
            pathIndex = waypoints != null ? waypoints.Length : 0;
        }

        protected override void OnDeath()
        {
            UpdateHorseAnimation(0f);
            if (horseAnimator != null && IsMounted) horseAnimator.speed = 0f;
            base.OnDeath();
        }

        public static bool ShouldDismount(bool mounted, bool horseDead, bool riderDead)
            => mounted && horseDead && !riderDead;

        public static bool IsWaypointOutsideMinimumDistance(Vector3 riderPosition, Vector3 waypointPosition,
            Vector3 playerPosition, float minimumDistance)
        {
            Vector3 segment = waypointPosition - riderPosition;
            segment.y = 0f;
            Vector3 relativePlayer = playerPosition - riderPosition;
            relativePlayer.y = 0f;
            float fraction = segment.sqrMagnitude > .0001f
                ? Mathf.Clamp01(Vector3.Dot(relativePlayer, segment) / segment.sqrMagnitude) : 0f;
            float closestDistance = (relativePlayer - segment * fraction).magnitude;
            return closestDistance >= Mathf.Max(0f, minimumDistance) - .001f;
        }

        public static bool CanTakeMountedStep(Vector3 riderPosition, Vector3 nextPosition,
            Vector3 playerPosition, float minimumDistance)
        {
            Vector3 offset = riderPosition - playerPosition;
            offset.y = 0f;
            float minimum = Mathf.Max(0f, minimumDistance);
            if (offset.magnitude >= minimum - .001f)
                return IsWaypointOutsideMinimumDistance(riderPosition, nextPosition, playerPosition, minimum);
            Vector3 step = nextPosition - riderPosition;
            step.y = 0f;
            // A player can suddenly move into our spacing band; permit only motion directly out of it.
            return (offset + step).sqrMagnitude > offset.sqrMagnitude + .000001f &&
                Vector3.Dot(offset, step) >= 0f;
        }

        public static Vector3 CalculateFlankDestination(Vector3 riderPosition, Vector3 playerPosition,
            Vector3 playerForward, float side, float minimumDistance, float maximumDistance, float lateralDistance = 6f)
        {
            playerForward.y = 0f;
            if (playerForward.sqrMagnitude < .001f) playerForward = Vector3.forward;
            playerForward.Normalize();
            Vector3 flank = Vector3.Cross(Vector3.up, playerForward) * (side < 0f ? -1f : 1f);
            float min = Mathf.Max(0f, minimumDistance);
            float max = Mathf.Max(min, maximumDistance);
            Vector3 offset = riderPosition - playerPosition;
            offset.y = 0f;
            float distance = offset.magnitude;
            float preferred = (min + max) * .5f;
            float lateral = Mathf.Clamp(Mathf.Abs(lateralDistance), 0f, preferred);
            float sideDistance = Vector3.Dot(offset, flank);
            // Stay inside the road corridor: most separation is behind the player, with a small side offset.
            if (distance >= min - .001f && distance <= max + .001f && Vector3.Dot(offset, playerForward) <= 0f &&
                sideDistance >= lateral * .5f && sideDistance <= lateral + 3f)
                return new Vector3(riderPosition.x, playerPosition.y, riderPosition.z);
            float trailing = Mathf.Sqrt(Mathf.Max(0f, preferred * preferred - lateral * lateral));
            return playerPosition - playerForward * trailing + flank * lateral;
        }
    }
}
