using UnityEngine;

namespace ScammerNoScamming
{
    // Looks like a villager. Its behavioural tell: once the player is within noticeRange it turns to
    // face them and keeps staring. If the player holds eye contact (looks at it within lookAngle and
    // attackRange) for stareTime seconds it lunges. Shooting it down first defuses the trap.
    // Needs a Health (Faction Scammer) on the same object; art provides the visual tells.
    public class DisguisedScammer : Trap
    {
        public float noticeRange = 15f;
        public float attackRange = 8f;
        public float lookAngle = 12f;
        public float stareTime = 1.5f;
        public float turnSpeed = 180f;

        Health health;

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

            var cam = player.GetComponentInChildren<Camera>();
            bool eyeContact = cam != null && IsEyeContact(cam.transform.position, cam.transform.forward);
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

        // Builds up while there is eye contact and drains twice as fast when it breaks. Returns true
        // when it should lunge.
        public bool UpdateStare(bool eyeContact, float dt)
        {
            Stare = eyeContact ? Stare + dt : Mathf.Max(0f, Stare - dt * 2f);
            return Stare >= stareTime;
        }
    }
}
