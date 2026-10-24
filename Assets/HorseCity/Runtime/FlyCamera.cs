using UnityEngine;
#if HORSE_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

namespace HorseCity
{
    /// <summary>
    /// Simple preview camera for walking the city before the player and horse exist.
    /// Hold right mouse to look, WASD to move, Q/E down/up, Shift to go fast.
    /// </summary>
    public class FlyCamera : MonoBehaviour
    {
        public float speed = 12f;
        public float fastMultiplier = 5f;
        public float lookSensitivity = 0.15f;

        float _yaw, _pitch;

        void Start()
        {
            Vector3 e = transform.eulerAngles;
            _yaw = e.y;
            _pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        void Update()
        {
            Vector2 look = Vector2.zero;
            Vector3 move = Vector3.zero;
            bool fast = false, looking = false;

#if HORSE_INPUTSYSTEM
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            if (k != null)
            {
                if (k.wKey.isPressed) move.z += 1;
                if (k.sKey.isPressed) move.z -= 1;
                if (k.dKey.isPressed) move.x += 1;
                if (k.aKey.isPressed) move.x -= 1;
                if (k.eKey.isPressed) move.y += 1;
                if (k.qKey.isPressed) move.y -= 1;
                fast = k.leftShiftKey.isPressed;
            }
            if (m != null)
            {
                looking = m.rightButton.isPressed;
                look = m.delta.ReadValue();
            }
#else
            move.z = Input.GetAxisRaw("Vertical");
            move.x = Input.GetAxisRaw("Horizontal");
            if (Input.GetKey(KeyCode.E)) move.y += 1;
            if (Input.GetKey(KeyCode.Q)) move.y -= 1;
            fast = Input.GetKey(KeyCode.LeftShift);
            looking = Input.GetMouseButton(1);
            look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
#endif
            if (looking)
            {
                _yaw += look.x * lookSensitivity;
                _pitch = Mathf.Clamp(_pitch - look.y * lookSensitivity, -89f, 89f);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            float s = speed * (fast ? fastMultiplier : 1f) * Time.deltaTime;
            transform.position += transform.TransformDirection(move) * s;
        }
    }
}
