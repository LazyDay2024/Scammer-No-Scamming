using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Heavy, RDR2-style reins. W charges a "throttle" like a capacitor: it fills with a delay when W is held and, when
    // W is let go, drains slowly, so the horse keeps trotting or galloping on its own and does not need W held down.
    // The horse's speed then lags behind the throttle (weight), and steering lags too. S brakes and then reverses.
    // Shift asks for the gallop gait; once galloping it stays a gallop until the throttle has mostly drained.
    // Gait and turn values are public so gene modifiers can scale them.
    [RequireComponent(typeof(CharacterController))]
    public class HorseController : MonoBehaviour
    {
        public static readonly List<HorseController> All = new List<HorseController>();

        [Header("Gait")]
        public float trotSpeed = 6f;
        public float gallopSpeed = 12f;
        public float reverseSpeed = 2f;
        [Tooltip("Seconds for the speed to catch up with the throttle: the weight of the horse.")]
        public float speedLag = 0.55f;
        [Tooltip("Capacitor time constant while W is held (seconds to fill ~63%).")]
        public float chargeTime = 0.9f;
        [Tooltip("Capacitor time constant after W is released at a trot (bigger = coasts longer).")]
        public float trotDischargeTime = 2.4f;
        public float gallopDischargeTime = 4.5f;
        [Tooltip("Seconds to stop with S held.")]
        public float brakeTime = 0.9f;

        [Header("Steering")]
        public float turnRate = 100f;
        [Tooltip("Seconds for the turn to catch up with A/D.")]
        public float turnLag = 0.3f;
        [Range(0f, 1f)] public float turnLossAtGallop = 0.5f;

        [Header("Jump")]
        public float jumpHeight = 1.6f;
        public float gravity = -25f;

        [SerializeField] Transform seat;

        CharacterController characterController;
        Health health;
        PlayerControls rider;
        float verticalVelocity;
        float throttle;        // 0..1 charge of the "capacitor"
        bool galloping;
        float speedVelocity, turnVelocity, turnInput;

        public Transform Seat => seat != null ? seat : transform;
        public bool HasRider => rider != null;
        public float Speed { get; private set; }
        // A horse with no Health component cannot be hurt. Once it is down it stops answering the reins.
        public bool IsDown => health != null && health.IsDead;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            if (health != null) health.deactivateOnDeath = false;
        }

        void OnEnable()
        {
            All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        void Update()
        {
            Vector2 input = Vector2.zero;
            bool sprint = false;
            bool jump = false;
            if (rider != null && !IsDown)
            {
                input = rider.Move.ReadValue<Vector2>();
                sprint = rider.Sprint.IsPressed();
                jump = rider.Jump.WasPressedThisFrame();
            }

            float dt = Time.deltaTime;
            UpdateThrottle(input.y, sprint, dt);

            float topSpeed = galloping ? gallopSpeed : trotSpeed;
            float targetSpeed = input.y < -0.05f && throttle <= 0.01f ? reverseSpeed * input.y : throttle * topSpeed;
            Speed = Mathf.SmoothDamp(Speed, targetSpeed, ref speedVelocity, speedLag, Mathf.Infinity, dt);

            // Reins lag too: the turn builds up and eases off instead of snapping.
            turnInput = Mathf.SmoothDamp(turnInput, input.x, ref turnVelocity, turnLag, Mathf.Infinity, dt);
            float speedRatio = Mathf.Clamp01(Mathf.Abs(Speed) / gallopSpeed);
            float steering = turnInput * turnRate * Mathf.Lerp(1f, 1f - turnLossAtGallop, speedRatio);
            transform.Rotate(0f, steering * dt, 0f);

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            if (jump && characterController.isGrounded)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * dt;

            var flags = characterController.Move((transform.forward * Speed + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Sides) != 0)
            {
                // A wall takes the momentum away, the throttle with it.
                Speed = Mathf.MoveTowards(Speed, 0f, gallopSpeed * 3f * dt);
                throttle = Mathf.Min(throttle, Mathf.Abs(Speed) / Mathf.Max(.01f, topSpeed));
            }
        }

        // The capacitor: W fills it toward 1, letting go drains it exponentially (slower while galloping), S dumps it.
        void UpdateThrottle(float forward, bool sprint, float dt)
        {
            if (forward > 0.05f)
            {
                throttle += (forward - throttle) * (1f - Mathf.Exp(-dt / chargeTime));
                if (sprint) galloping = true;
            }
            else if (forward < -0.05f)
            {
                throttle = Mathf.MoveTowards(throttle, 0f, dt / brakeTime);
                galloping = false;
            }
            else
            {
                throttle *= Mathf.Exp(-dt / (galloping ? gallopDischargeTime : trotDischargeTime));
                if (sprint && throttle > .3f) galloping = true; // Shift alone also asks for the gallop while coasting
            }
            if (throttle < 0.02f && forward < 0.05f) { throttle = 0f; galloping = false; }
            if (galloping && throttle < 0.35f && forward < 0.05f) galloping = false; // too slow to be a gallop
            if (!HasRider || IsDown)
            {
                throttle = Mathf.MoveTowards(throttle, 0f, dt / brakeTime);
                galloping = false;
            }
        }

        public void SetSeat(Transform seatTransform)
        {
            seat = seatTransform;
        }

        public void SetRider(PlayerControls controls)
        {
            rider = controls;
        }

        public void ClearRider()
        {
            rider = null;
        }

        public Vector3 GetDismountPoint()
        {
            Vector3 point = transform.position + transform.right * 1.8f;
            if (Physics.Raycast(point + Vector3.up * 3f, Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore))
                point = hit.point;
            return point + Vector3.up * 0.05f;
        }
    }
}
