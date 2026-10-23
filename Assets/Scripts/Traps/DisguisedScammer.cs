using UnityEngine;

namespace ScammerNoScamming
{
    // Looks like a villager. Its behavioural tell: once the player is within noticeRange it turns to
    // face them and keeps staring. If the player holds eye contact (looks at it within lookAngle and
    // attackRange) for stareTime seconds it lunges. Shooting it down first defuses the trap.
    // Has its own Health; art provides the visual tells.
    [RequireComponent(typeof(Health))]
    public class DisguisedScammer : Trap
    {
        public float noticeRange = 15f;
        public float attackRange = 8f;
        public float lookAngle = 12f;
        public float stareTime = 1.5f;
        public float turnSpeed = 180f;

        Health health;
        Camera playerCamera;

        public override TrapKind Kind => TrapKind.Disguised;
        public float Stare { get; private set; }

        protected override float DefaultDamage => 40f;

        void Awake()
        {
            health = GetComponent<Health>();
        }

        void OnEnable()
        {
            if (health != null) health.Died += OnDied;
        }

        void OnDisable()
        {
            if (health != null) health.Died -= OnDied;
        }

        void OnDied(Health _) => Defuse();

        public override void ResetForAction()
        {
            base.ResetForAction();
            Stare = 0f;
        }

        void Update()
        {
            var player = FindPlayer();
            if (HasSprung || IsDefused || player == null) return;

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > noticeRange * noticeRange)
            {
                Stare = 0f;
                return;
            }
            if (toPlayer.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toPlayer),
                    turnSpeed * Time.deltaTime);

            if (playerCamera == null) playerCamera = player.GetComponentInChildren<Camera>();
            var cam = playerCamera;
            bool eyeContact = cam != null && IsEyeContact(cam.transform.position, cam.transform.forward)
                && HasLineOfSight(cam.transform.position, player.transform);
            if (UpdateStare(eyeContact, Time.deltaTime))
                Spring(player.gameObject);
        }

        // True when the viewer at eyePosition, looking along eyeForward, is close and looking right at us.
        public bool IsEyeContact(Vector3 eyePosition, Vector3 eyeForward)
        {
            Vector3 head = transform.position + Vector3.up * 1.6f;
            Vector3 toHead = head - eyePosition;
            if (toHead.sqrMagnitude > attackRange * attackRange) return false;
            return Vector3.Angle(eyeForward, toHead) <= lookAngle;
        }

        // Nothing but the player or this scammer between the eye and our head.
        bool HasLineOfSight(Vector3 eyePosition, Transform playerRoot)
        {
            Vector3 head = transform.position + Vector3.up * 1.6f;
            if (!Physics.Linecast(eyePosition, head, out var hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(playerRoot);
        }

        // Builds up while there is eye contact and drains twice as fast when it breaks. Returns true
        // when it should lunge.
        public bool UpdateStare(bool eyeContact, float dt)
        {
            Stare = eyeContact ? Stare + dt : Mathf.Max(0f, Stare - dt * 2f);
            return Stare >= stareTime;
        }
    }
}
