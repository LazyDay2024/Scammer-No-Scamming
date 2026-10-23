using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScammerNoScamming
{
    public enum TrapKind
    {
        FakeSupply,
        PhishingLink,
        Disguised,
        Ambush,
        CallCenter
    }

    // Base for the traps on the road to a hideout. Every trap must show a tell the player can notice
    // first (art or behaviour); the code only decides what springs it and what happens.
    // Other systems listen to the static events instead of traps knowing about Trouble, UI or enemies.
    public abstract class Trap : MonoBehaviour
    {
        public static event Action<Trap> Sprung;
        public static event Action<Trap> Defused;

        [Tooltip("Negative uses the trap's default damage.")]
        public float damage = -1f;
        [Tooltip("Disabled objects (hidden scammers etc.) switched on when the trap springs.")]
        public GameObject[] activateOnSpring = Array.Empty<GameObject>();

        public abstract TrapKind Kind { get; }
        public float Damage => damage >= 0f ? damage : DefaultDamage;
        protected virtual float DefaultDamage => 25f;
        public bool HasSprung { get; private set; }
        public bool IsDefused { get; private set; }

        // Springs once. target is the player root; damage goes to the first IDamageable on it, if any.
        public void Spring(GameObject target)
        {
            if (HasSprung || IsDefused) return;
            HasSprung = true;

            if (target != null && Damage > 0f)
            {
                var damageable = Alive(target.GetComponentInChildren<IDamageable>())
                    ?? Alive(target.GetComponentInParent<IDamageable>());
                damageable?.TakeDamage(new DamageInfo(Damage, HitZoneType.Body, Faction.Scammer, target.transform.position));
            }
            foreach (var go in activateOnSpring)
                if (go != null) go.SetActive(true);

            OnSprung(target);
            TrapLog.RecordSprung(Kind);
            Sprung?.Invoke(this);
        }

        // The player saw through it (for example shot the disguised scammer first).
        public void Defuse()
        {
            if (HasSprung || IsDefused) return;
            IsDefused = true;
            TrapLog.RecordDefused(Kind);
            Defused?.Invoke(this);
        }

        protected virtual void OnSprung(GameObject target) { }

        // Re-arms a sprung trap for the next case and hides the scammers it woke. A defused trap stays
        // defused (for example a disguised scammer that was shot down).
        public virtual void ResetTrap()
        {
            if (IsDefused) return;
            HasSprung = false;
            foreach (var go in activateOnSpring)
                if (go != null) go.SetActive(false);
        }

        // Small "press E" hint near interactable traps; the lure text itself is part of the trap.
        protected void DrawPrompt(string text)
        {
            var canvas = ThaiGui.BeginCanvas();
            var r = new Rect(canvas.width * 0.5f - 420f, canvas.height * 0.7f, 840f, 70f);
            GUI.DrawTexture(r, ThaiGui.Solid(new Color(0.04f, 0.07f, 0.08f, 0.75f)));
            GUI.Label(r, text, ThaiGui.Text(32, ThaiGui.Bone, FontStyle.Bold, TextAnchor.MiddleCenter));
            ThaiGui.EndCanvas();
        }

        protected bool PlayerWithin(float range)
        {
            var player = FindPlayer();
            if (player == null || player.IsMounted) return false;
            Vector3 offset = player.transform.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= range * range;
        }

        // Unity overloads == for destroyed objects but ?. and ?? do not, so normalise first.
        static IDamageable Alive(IDamageable damageable) =>
            damageable is Object unityObject && unityObject == null ? null : damageable;

        // E is shared with PlayerMount, which reads it in Update. Traps read it in LateUpdate, after the
        // mount state for this frame is settled, and ignore presses that mounted or dismounted the player.
        public static bool IsOnFootInteract(bool pressed, bool mountedNow, bool mountedLastFrame)
        {
            return pressed && !mountedNow && !mountedLastFrame;
        }

        bool wasMounted;

        // Call once per frame from LateUpdate.
        protected bool InteractPressedOnFoot(PlayerController player)
        {
            bool mounted = player.IsMounted;
            var controls = player.GetComponent<PlayerControls>();
            bool pressed = controls != null && controls.Interact != null && controls.Interact.WasPressedThisFrame();
            bool result = IsOnFootInteract(pressed, mounted, wasMounted);
            wasMounted = mounted;
            return result;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Sprung = null;
            Defused = null;
        }

        protected static PlayerController FindPlayer() => PlayerLocator.Player;
    }
}
