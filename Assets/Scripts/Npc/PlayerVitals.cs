using System;
using UnityEngine;

namespace ScammerNoScamming
{
    [RequireComponent(typeof(Health))]
    public class PlayerVitals : MonoBehaviour
    {
        public float maxHealth = 100f;
        public float damageMultiplier = 1f;
        public float regenerationPerSecond = 5f;
        public float regenerationDelay = 6f;
        public float flashDuration = 0.35f;
        public float flashOpacity = 0.3f;
        public float directionDuration = 0.8f;
        public float directionRadius = 70f;
        public bool createMountedHurtbox = true;

        public event Action Died;
        public Health Health => health;
        public float LastDamageTime { get; private set; } = float.NegativeInfinity;

        Health health;
        PlayerControls controls;
        Camera view;
        PlayerCameraFeel cameraFeel;
        CapsuleCollider hurtbox;
        HorseController ignoredHorse;
        bool deathReported;
        float damageAngle;

        void Awake()
        {
            health = GetComponent<Health>();
            controls = GetComponent<PlayerControls>();
            view = GetComponentInChildren<Camera>();
            if (view != null) cameraFeel = view.GetComponent<PlayerCameraFeel>();
            health.deactivateOnDeath = false;
            if (!health.IsDead) health.SetMax(Mathf.Max(1f, maxHealth));
            if (createMountedHurtbox) CreateHurtbox();
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            if (health == null) return;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void Update()
        {
            if (health.IsDead) return;
            float restored = RegenerationAmount(health.Current, health.max, LastDamageTime,
                Time.time, Time.deltaTime, regenerationDelay, regenerationPerSecond);
            if (restored > 0f) SetCurrent(health.Current + restored);
        }

        void LateUpdate()
        {
            IgnoreHorseCollisions();
        }

        void OnTransformParentChanged()
        {
            IgnoreHorseCollisions();
        }

        // Only the part of this frame after the delay is eligible for regeneration.
        public static float RegenerationAmount(float current, float maximum, float lastDamageTime,
            float now, float deltaTime, float delay, float rate)
        {
            if (current <= 0f || current >= maximum || deltaTime <= 0f || rate <= 0f) return 0f;
            float start = Mathf.Max(now - deltaTime, lastDamageTime + Mathf.Max(0f, delay));
            return Mathf.Min(maximum - current, Mathf.Max(0f, now - start) * rate);
        }

        void OnDamaged(Health sender, DamageInfo info)
        {
            if (sender.IsDead) return;
            float before = sender.Current + info.Amount;
            float amount = info.Amount > 0f ? info.Amount * Mathf.Max(0f, damageMultiplier) : info.Amount;
            // Health checks death after Damaged, so this also corrects a raw lethal hit in time.
            SetCurrent(Mathf.Clamp(before - amount, 0f, sender.max));
            if (amount <= 0f) return;

            LastDamageTime = Time.time;
            Transform facing = view != null ? view.transform : transform;
            Vector3 entry = facing.InverseTransformDirection(info.Point - transform.position);
            damageAngle = Mathf.Atan2(entry.x, entry.z) * Mathf.Rad2Deg;
        }

        void SetCurrent(float value)
        {
            health.SetCurrent(value);
        }

        public void ResetForAction(RunModifiers modifiers)
        {
            modifiers = modifiers ?? RunModifiers.None;
            maxHealth = 100f * Mathf.Max(0.01f, modifiers.PlayerMaxHealth);
            damageMultiplier = Mathf.Max(0f, modifiers.DamageTaken);
            health.RestoreFull(maxHealth);
            LastDamageTime = float.NegativeInfinity;
            deathReported = false;
            if (controls != null) controls.enabled = true;
        }

        void OnDied(Health sender)
        {
            if (deathReported) return;
            deathReported = true;
            if (controls != null) controls.enabled = false;
            Died?.Invoke();
        }

        void CreateHurtbox()
        {
            var controller = GetComponent<CharacterController>();
            if (controller == null) return;
            // The walking controller is disabled in the saddle, but bullets still need a body.
            var body = new GameObject("Player Hurtbox");
            body.transform.SetParent(transform, false);
            body.layer = gameObject.layer;
            hurtbox = body.AddComponent<CapsuleCollider>();
            hurtbox.center = controller.center;
            hurtbox.height = controller.height;
            hurtbox.radius = controller.radius;
            foreach (var own in GetComponentsInChildren<Collider>())
                if (own != hurtbox) Physics.IgnoreCollision(hurtbox, own);
            IgnoreHorseCollisions();
        }

        void IgnoreHorseCollisions()
        {
            if (hurtbox == null) return;
            var horse = GetComponentInParent<HorseController>();
            if (horse == ignoredHorse) return;
            ignoredHorse = horse;
            if (horse == null) return;
            foreach (var other in horse.GetComponentsInChildren<Collider>())
                if (other != hurtbox) Physics.IgnoreCollision(hurtbox, other);
        }

        void OnGUI()
        {
            if (GameScreen.AnyOpen) return;
            float elapsed = Time.time - LastDamageTime;
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            // New rigs draw an edge vignette; keep the legacy flash for older rigs.
            if ((cameraFeel == null || !cameraFeel.isActiveAndEnabled) && flashDuration > 0f && elapsed < flashDuration)
            {
                GUI.color = new Color(1f, 0f, 0f, flashOpacity * (1f - elapsed / flashDuration));
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            }
            if (directionDuration > 0f && elapsed < directionDuration)
            {
                Vector2 centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                GUIUtility.RotateAroundPivot(damageAngle, centre);
                GUI.color = new Color(1f, 0.1f, 0.1f, 1f - elapsed / directionDuration);
                GUI.DrawTexture(new Rect(centre.x - 12f, centre.y - directionRadius, 24f, 5f), Texture2D.whiteTexture);
            }
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }
    }
}
