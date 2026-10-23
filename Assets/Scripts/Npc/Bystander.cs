using System;
using UnityEngine;
using UnityEngine.AI;

namespace ScammerNoScamming
{
    public class Bystander : Npc
    {
        public static event Action<Bystander> Freed;

        public float hearingRange = 35f;
        public float fleeRange = 6f;
        public float surrenderRange = 12f;
        public float freeRange = 2.5f;
        public float fleeDistance = 20f;
        public float moveSpeed = 4f;
        public float turnSpeed = 360f;
        public float arrivalDistance = 0.6f;
        public float navigationSampleRadius = 3f;
        public float movementSkin = 0.03f;
        public LayerMask obstacleMask = ~0;
        public Transform[] exitPoints = Array.Empty<Transform>();

        public bool IsFreed { get; private set; }
        public bool IsFrightened { get; private set; }

        NavMeshAgent agent;
        CharacterController characterController;
        Collider body;
        Vector3 escapeDestination;
        WorldInteractable interaction;

        protected override void Awake()
        {
            if (Faction != ScammerNoScamming.Faction.ForcedWorker && Faction != ScammerNoScamming.Faction.Victim)
                Faction = ScammerNoScamming.Faction.Victim;
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
            characterController = GetComponent<CharacterController>();
            body = GetComponentInChildren<Collider>();
            interaction = GetComponent<WorldInteractable>();
            if (interaction == null) interaction = gameObject.AddComponent<WorldInteractable>();
            interaction.Prompt = "ปล่อยตัวลูกจ้าง";
            interaction.Range = freeRange;
            interaction.OnInteract = () => TryFree(PlayerLocator.Player);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            NpcGunfire.Heard += HearShot;

        }

        protected override void OnDisable()
        {
            NpcGunfire.Heard -= HearShot;
            base.OnDisable();
        }

        void Update()
        {
            if (interaction != null) interaction.Available = CanFree(PlayerLocator.Player);
            if (IsDead) return;
            if (IsFreed)
            {
                if (PlanarDistance(transform.position, escapeDestination) <= Mathf.Max(0f, arrivalDistance))
                {
                    StopMoving();
                    gameObject.SetActive(false);
                    return;
                }
                MoveTo(escapeDestination);
                return;
            }

            var player = PlayerLocator.Player;
            if (player == null)
            {
                StopMoving();
                return;
            }
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (Faction == ScammerNoScamming.Faction.ForcedWorker)
            {
                if (distance <= Mathf.Max(0f, surrenderRange)) Frighten();
                StopMoving();
            }
            else if (Faction == ScammerNoScamming.Faction.Victim && distance < Mathf.Max(0f, fleeRange))
            {
                Frighten();
                Animation.Cower(false);
                MoveTo(AwayFrom(player.transform.position));
            }
            else
            {
                StopMoving();
                if (IsFrightened) Animation.Cower(true);
            }
        }

        public void HearShot(Vector3 position)
        {
            if (!isActiveAndEnabled || IsDead || IsFreed) return;
            float range = Mathf.Max(0f, hearingRange);
            if ((position - transform.position).sqrMagnitude <= range * range) Frighten();
        }

        void Frighten()
        {
            IsFrightened = true;
            if (Faction == ScammerNoScamming.Faction.ForcedWorker) Animation.HandsUp(true);
            else Animation.Cower(true);
        }

        public bool CanFree(PlayerController player)
        {
            if (!isActiveAndEnabled || IsDead || IsFreed || Faction != ScammerNoScamming.Faction.ForcedWorker)
                return false;
            if (player == null || !player.isActiveAndEnabled || player.IsMounted) return false;
            var controls = player.GetComponent<PlayerControls>();
            if (controls == null || !controls.isActiveAndEnabled) return false;
            var health = player.GetComponent<Health>();
            if (health != null && health.IsDead) return false;
            return Vector3.Distance(transform.position, player.transform.position) <= Mathf.Max(0f, freeRange);
        }

        public bool TryFree(PlayerController player)
        {
            if (!CanFree(player)) return false;
            IsFreed = true;
            IsFrightened = false;
            escapeDestination = CalculateEscapeDestination(player.transform.position);
            Animation.HandsUp(false);
            Animation.Cower(false);
            Freed?.Invoke(this);
            return true;
        }

        public override void ResetForAction()
        {
            base.ResetForAction();
            IsFreed = false;
            IsFrightened = false;
            escapeDestination = transform.position;
        }

        public Vector3 CalculateEscapeDestination(Vector3 threat)
        {
            Transform nearest = null;
            float nearestDistance = float.PositiveInfinity;
            if (exitPoints != null)
            {
                foreach (var point in exitPoints)
                {
                    if (point == null) continue;
                    float distance = (point.position - transform.position).sqrMagnitude;
                    if (distance >= nearestDistance) continue;
                    nearestDistance = distance;
                    nearest = point;
                }
            }
            return nearest != null ? nearest.position : AwayFrom(threat);
        }

        Vector3 AwayFrom(Vector3 threat)
        {
            Vector3 away = transform.position - threat;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = transform.forward;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
            return transform.position + away.normalized * Mathf.Max(0f, fleeDistance);
        }

        void MoveTo(Vector3 destination)
        {
            Vector3 offset = destination - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.0001f)
            {
                StopMoving();
                return;
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset),
                Mathf.Max(0f, turnSpeed) * Time.deltaTime);
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                Vector3 target = destination;
                if (NavMesh.SamplePosition(destination, out NavMeshHit hit, Mathf.Max(0f, navigationSampleRadius), agent.areaMask))
                    target = hit.position;
                agent.speed = Mathf.Max(0f, moveSpeed);
                agent.isStopped = false;
                agent.SetDestination(target);
                Animation.SetSpeed(agent.velocity.magnitude);
                return;
            }

            Vector3 movement = offset.normalized * Mathf.Min(offset.magnitude, Mathf.Max(0f, moveSpeed) * Time.deltaTime);
            Vector3 before = transform.position;
            if (characterController != null && characterController.enabled)
                characterController.Move(movement);
            else
                transform.position += ClipMovement(movement);
            Animation.SetSpeed(Time.deltaTime > 0f ? (transform.position - before).magnitude / Time.deltaTime : 0f);
        }

        Vector3 ClipMovement(Vector3 movement)
        {
            if (body == null || !body.enabled || movement.sqrMagnitude < 0.000001f) return movement;
            Bounds bounds = body.bounds;
            float radius = Mathf.Max(0.01f, Mathf.Min(bounds.extents.x, bounds.extents.z) - Mathf.Max(0f, movementSkin));
            float distance = movement.magnitude;
            // The fallback has no pathfinder, but should not walk straight through a wall.
            var hits = Physics.SphereCastAll(bounds.center, radius, movement.normalized, distance + Mathf.Max(0f, movementSkin),
                obstacleMask, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - Mathf.Max(0f, movementSkin)));
            }
            return movement.normalized * distance;
        }

        void StopMoving()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            Animation.SetSpeed(0f);
        }

        protected override void OnDeath()
        {
            StopMoving();
            base.OnDeath();
        }

        static float PlanarDistance(Vector3 a, Vector3 b)
        {
            Vector3 offset = a - b;
            offset.y = 0f;
            return offset.magnitude;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Freed = null;
        }
    }
}
