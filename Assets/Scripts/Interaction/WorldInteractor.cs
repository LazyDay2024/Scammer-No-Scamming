using UnityEngine;

namespace ScammerNoScamming
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(PlayerController), typeof(PlayerControls))]
    public class WorldInteractor : MonoBehaviour
    {
        public static bool HasFocusedInteraction { get; private set; }
        public WorldInteractable Focused { get; private set; }
        PlayerController player;
        PlayerControls controls;
        Camera view;
        bool wasMounted;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            controls = GetComponent<PlayerControls>();
            view = GetComponentInChildren<Camera>();
        }

        void Update()
        {
            Focused = null;
            if (!GameScreen.AnyOpen && controls.enabled && !player.IsMounted && view != null && Time.timeScale > 0f)
            {
                var ray = view.ViewportPointToRay(new Vector3(.5f, .5f));
                if (Physics.Raycast(ray, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore))
                {
                    var candidate = hit.collider.GetComponentInParent<WorldInteractable>();
                    if (candidate != null && candidate.isActiveAndEnabled && candidate.Available && hit.distance <= candidate.Range)
                        Focused = candidate;
                }
            }
            HasFocusedInteraction = Focused != null;
        }

        void LateUpdate()
        {
            bool mounted = player.IsMounted;
            bool pressed = controls.enabled && controls.Interact != null && controls.Interact.WasPressedThisFrame();
            if (!GameScreen.AnyOpen && Trap.IsOnFootInteract(pressed, mounted, wasMounted) && Focused != null)
                Focused.Interact();
            wasMounted = mounted;
        }

        void OnDisable() { Focused = null; HasFocusedInteraction = false; }

        void OnGUI()
        {
            if (GameScreen.AnyOpen || Focused == null || Time.timeScale <= 0f) return;
            var canvas = ThaiGui.BeginCanvas();
            GUI.Label(new Rect(930f, 507f, 60f, 50f), "+", ThaiGui.Text(22, ThaiGui.Gold, FontStyle.Normal, TextAnchor.MiddleCenter));
            var prompt = new Rect(570f, canvas.height - 125f, 780f, 60f);
            ThaiGui.Panel(prompt, .9f);
            GUI.Label(prompt, "E  ·  " + Focused.Prompt, ThaiGui.Text(28, ThaiGui.Bone, FontStyle.Bold, TextAnchor.MiddleCenter));
            if (!string.IsNullOrEmpty(Focused.ReadingBody))
            {
                var style = ThaiGui.Text(26, ThaiGui.Bone);
                float height = Mathf.Min(340f, style.CalcHeight(new GUIContent(Focused.ReadingBody), 660f) + 85f);
                var panel = new Rect(canvas.width - 760f, canvas.height - height - 150f, 710f, height);
                ThaiGui.Panel(panel, .94f);
                GUI.Label(new Rect(panel.x + 25f, panel.y + 15f, 660f, 50f), Focused.ReadingTitle, ThaiGui.Text(28, ThaiGui.Gold, FontStyle.Bold));
                GUI.Label(new Rect(panel.x + 25f, panel.y + 68f, 660f, height - 75f), Focused.ReadingBody, style);
            }
            ThaiGui.EndCanvas();
        }
    }
}
