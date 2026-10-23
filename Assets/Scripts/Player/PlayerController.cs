using UnityEngine;

namespace ScammerNoScamming
{
    // First-person walking and looking. While mounted, walking stops and the body turns
    // only within a limited arc relative to the saddle.
    [RequireComponent(typeof(CharacterController), typeof(PlayerControls))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Walk")]
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7f;
        public float jumpHeight = 1.1f;
        public float gravity = -20f;

        [Header("Look")]
        public float lookSensitivity = 0.1f;
        public float pitchLimit = 85f;
        public float mountedYawLimit = 110f;

        [Header("Camera")]
        public Transform cameraPivot;
        public float standingEyeHeight = 1.6f;
        public float mountedEyeHeight = 0.9f;

        CharacterController characterController;
        PlayerControls controls;
        Transform homeParent;
        float yaw;
        float pitch;
        float seatYaw;
        float verticalVelocity;

        public bool IsMounted { get; private set; }

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            controls = GetComponent<PlayerControls>();
            yaw = transform.eulerAngles.y;

            if (cameraPivot == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null)
                    cameraPivot = cam.transform.parent != transform ? cam.transform.parent : cam.transform;
            }
        }

        void Start()
        {
            LockCursor();
        }

        void Update()
        {
            // Browsers only grant pointer lock after a click, so re-lock on the next press.
            if (Cursor.lockState != CursorLockMode.Locked && controls.Fire.WasPressedThisFrame())
                LockCursor();

            if (Cursor.lockState == CursorLockMode.Locked)
                ApplyLook();

            if (!IsMounted)
                Walk();
        }

        void LateUpdate()
        {
            if (cameraPivot != null)
                cameraPivot.localPosition = new Vector3(0f, IsMounted ? mountedEyeHeight : standingEyeHeight, 0f);
        }

        public void EnterMount(Transform seat)
        {
            IsMounted = true;
            homeParent = transform.parent;
            characterController.enabled = false;
            transform.SetParent(seat, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            seatYaw = 0f;
            verticalVelocity = 0f;
        }

        public void ExitMount(Vector3 position, float worldYaw)
        {
            IsMounted = false;
            transform.SetParent(homeParent, true);
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, worldYaw, 0f));
            yaw = worldYaw;
            verticalVelocity = 0f;
            characterController.enabled = true;
        }

        static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void ApplyLook()
        {
            Vector2 delta = controls.Look.ReadValue<Vector2>() * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);

            if (IsMounted)
            {
                seatYaw = Mathf.Clamp(seatYaw + delta.x, -mountedYawLimit, mountedYawLimit);
                transform.localRotation = Quaternion.Euler(0f, seatYaw, 0f);
            }
            else
            {
                yaw += delta.x;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void Walk()
        {
            Vector2 input = controls.Move.ReadValue<Vector2>();
            Vector3 direction = transform.right * input.x + transform.forward * input.y;
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            float speed = controls.Sprint.IsPressed() ? sprintSpeed : walkSpeed;

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            if (characterController.isGrounded && controls.Jump.WasPressedThisFrame())
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * Time.deltaTime;

            characterController.Move((direction * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }
    }
}
