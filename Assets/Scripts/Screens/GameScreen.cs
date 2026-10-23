using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Base for our full-screen IMGUI screens (menu, analysis, genes, lesson, ending). While any screen
    // is open the game is paused, the player's input is off and the cursor is free.
    // Named GameScreen so it does not clash with UIScreen from the class diagram (the friend's uGUI work).
    public abstract class GameScreen : MonoBehaviour
    {
        static readonly HashSet<GameScreen> open = new HashSet<GameScreen>();
        static float savedTimeScale = 1f;

        public static bool AnyOpen => open.Count > 0;
        public bool IsOpen { get; private set; }

        protected void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (open.Count == 0) Focus(true);
            open.Add(this);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            open.Remove(this);
            if (open.Count == 0) Focus(false);
        }

        protected virtual void OnDisable() => Close();

        static void Focus(bool screenOpen)
        {
            if (screenOpen)
            {
                savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = savedTimeScale;
            }
            var player = PlayerLocator.Player;
            var controls = player != null ? player.GetComponent<PlayerControls>() : null;
            if (controls != null) controls.enabled = !screenOpen;
            Cursor.lockState = screenOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = screenOpen;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            open.Clear();
            savedTimeScale = 1f;
        }

        void OnGUI()
        {
            if (!IsOpen) return;
            // PlayerController locks the cursor in Start, which can run after a screen opened.
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            GUI.depth = -10;
            var canvas = ThaiGui.BeginCanvas();
            Draw(canvas);
            ThaiGui.EndCanvas();
        }

        protected abstract void Draw(Rect canvas);
    }
}
