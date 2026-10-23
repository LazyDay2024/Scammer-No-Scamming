using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // W/S speed up and slow down, A/D steer, Shift gallops, Space jumps. Gait and turn values
    // are public so gene modifiers can scale them later.
    [RequireComponent(typeof(CharacterController))]
    public class HorseController : MonoBehaviour
    {
        public static readonly List<HorseController> All = new List<HorseController>();

        [Header("Gait")]
        public float trotSpeed = 6f;
        public float gallopSpeed = 12f;
        public float reverseSpeed = 2f;
        public float acceleration = 8f;
        public float braking = 12f;

        [Header("Steering")]
        public float turnRate = 100f;
        [Range(0f, 1f)] public float turnLossAtGallop = 0.5f;

        [Header("Jump")]
        public float jumpHeight = 1.6f;
        public float gravity = -25f;

        [SerializeField] Transform seat;

        CharacterController characterController;
        PlayerControls rider;
        float verticalVelocity;

        public Transform Seat => seat != null ? seat : transform;
        public bool HasRider => rider != null;
        public float Speed { get; private set; }

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
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
            bool gallop = false;
            bool jump = false;
            if (rider != null)
            {
                input = rider.Move.ReadValue<Vector2>();
                gallop = rider.Sprint.IsPressed();
                jump = rider.Jump.WasPressedThisFrame();
            }

            float dt = Time.deltaTime;

            float targetSpeed = input.y > 0f ? (gallop ? gallopSpeed : trotSpeed) * input.y : reverseSpeed * input.y;
            float rate = Mathf.Abs(targetSpeed) > Mathf.Abs(Speed) ? acceleration : braking;
            Speed = Mathf.MoveTowards(Speed, targetSpeed, rate * dt);

            float speedRatio = Mathf.Clamp01(Mathf.Abs(Speed) / gallopSpeed);
            float steering = input.x * turnRate * Mathf.Lerp(1f, 1f - turnLossAtGallop, speedRatio);
            transform.Rotate(0f, steering * dt, 0f);

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            if (jump && characterController.isGrounded)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * dt;

            var flags = characterController.Move((transform.forward * Speed + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Sides) != 0)
                Speed = Mathf.MoveTowards(Speed, 0f, gallopSpeed * 3f * dt);
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
