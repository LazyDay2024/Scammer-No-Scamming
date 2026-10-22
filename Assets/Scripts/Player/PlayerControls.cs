using UnityEngine;
using UnityEngine.InputSystem;

namespace WeRInTrouble
{
    // Keyboard + mouse bindings built in code, so the player prefab needs no input asset wiring.
    public class PlayerControls : MonoBehaviour
    {
        public InputAction Move { get; private set; }
        public InputAction Look { get; private set; }
        public InputAction Fire { get; private set; }
        public InputAction Jump { get; private set; }
        public InputAction Sprint { get; private set; }
        public InputAction Interact { get; private set; }

        InputAction[] all;

        void Awake()
        {
            Move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            Look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta", expectedControlType: "Vector2");
            Fire = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            Jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            Sprint = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            Interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");

            all = new[] { Move, Look, Fire, Jump, Sprint, Interact };
        }

        void OnEnable()
        {
            foreach (var action in all) action.Enable();
        }

        void OnDisable()
        {
            foreach (var action in all) action.Disable();
        }

        void OnDestroy()
        {
            foreach (var action in all) action.Dispose();
        }
    }
}
