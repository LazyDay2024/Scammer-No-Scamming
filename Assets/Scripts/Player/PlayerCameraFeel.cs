using UnityEngine;

namespace ScammerNoScamming
{
    // Camera is a child of the look pivot: visual offsets never move the controller/colliders.
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class PlayerCameraFeel : MonoBehaviour
    {
        [Range(0f, 1f)] public float motionAmount = 1f;
        [Range(0f, 1f)] public float shakeAmount = 1f;
        [Range(0f, 1f)] public float vignetteAmount = 1f;
        Camera view;
        PlayerMount mount;
        PlayerControls controls;
        CharacterController body;
        Weapon weapon;
        Health health;
        FeedbackSpring kick;
        Vector3 homePosition;
        Quaternion homeRotation;
        float homeFov, stride, bobWeight, trauma, pulse, noiseTime, roll;
        Texture2D vignette;

        void Awake()
        {
            view = GetComponent<Camera>();
            homePosition = transform.localPosition;
            homeRotation = transform.localRotation;
            homeFov = view.fieldOfView;
            mount = GetComponentInParent<PlayerMount>();
            controls = GetComponentInParent<PlayerControls>();
            body = GetComponentInParent<CharacterController>();
            weapon = GetComponentInParent<Weapon>();
        }

        void OnEnable()
        {
            if (weapon != null) weapon.Fired += OnFired;
            BindHealth();
        }

        void BindHealth()
        {
            if (health != null) return;
            health = GetComponentInParent<Health>();
            if (health != null) health.Damaged += OnDamaged;
        }

        void OnFired(ShotResult shot)
        {
            kick.Value.x = Mathf.Max(-2.5f, kick.Value.x - 0.65f);
            kick.Velocity.x -= 7f;
            trauma = Mathf.Min(1f, trauma + 0.2f);
        }

        void OnDamaged(Health sender, DamageInfo info)
        {
            if (info.Amount <= 0f) return;
            Vector3 direction = info.Direction.sqrMagnitude > 0.001f ? -info.Direction : info.Point - transform.position;
            Vector3 local = transform.InverseTransformDirection(direction.normalized);
            kick.Velocity += new Vector3(-10f, local.x * 5f, -local.x * 12f);
            trauma = Mathf.Min(1f, trauma + 0.55f);
            pulse = Mathf.Min(1f, pulse + 0.7f);
        }

        void LateUpdate()
        {
            BindHealth(); // Scene builders can attach PlayerVitals after CreatePlayer.
            if (GameScreen.AnyOpen)
            {
                ResetPose();
                kick = default;
                trauma = pulse = 0f;
                return;
            }
            if (Time.deltaTime <= 0f) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            var horse = mount != null ? mount.CurrentHorse : null;
            float speed = horse != null ? Mathf.Abs(horse.Speed)
                : body != null && body.enabled ? Vector3.ProjectOnPlane(body.velocity, Vector3.up).magnitude : 0f;
            bool grounded = body != null && body.enabled && body.isGrounded;
            float moving = horse != null || grounded ? Mathf.Clamp01(speed / 3f) : 0f;
            bobWeight = Mathf.Lerp(bobWeight, moving, 1f - Mathf.Exp(-8f * dt));
            stride = (stride + speed * dt / (horse != null ? 4.5f : 3.3f)) % 1f;
            Vector3 bob = FeedbackMotion.Bob(stride) * (horse != null ? 0.011f : 0.007f) * bobWeight;
            float turn = horse != null && controls != null && controls.enabled ? controls.Move.ReadValue<Vector2>().x : 0f;
            roll = Mathf.Lerp(roll, -turn * Mathf.Clamp01(speed / 8f) * 1.25f, 1f - Mathf.Exp(-5f * dt));
            bool sprint = controls != null && controls.enabled && controls.Sprint.IsPressed();
            float widen = horse != null ? Mathf.InverseLerp(horse.trotSpeed, horse.gallopSpeed, speed)
                : sprint ? Mathf.InverseLerp(4.5f, 7f, speed) : 0f;
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, homeFov + 3.5f * widen * motionAmount, 1f - Mathf.Exp(-4f * dt));
            kick.Step(dt, 19f);
            trauma = Mathf.MoveTowards(trauma, 0f, dt * 2.8f);
            pulse = Mathf.MoveTowards(pulse, 0f, dt * 1.8f);
            noiseTime += dt * 28f;
            float shake = trauma * trauma * shakeAmount;
            Vector3 angles = kick.Value * shakeAmount + new Vector3(Mathf.Sin(noiseTime * 1.3f), Mathf.Sin(noiseTime * 1.7f), 0f) * (shake * 0.65f);
            angles.z += roll * motionAmount;
            transform.localPosition = homePosition + bob * motionAmount;
            transform.localRotation = homeRotation * Quaternion.Euler(angles);
        }

        void ResetPose()
        {
            transform.SetLocalPositionAndRotation(homePosition, homeRotation);
            if (view != null) view.fieldOfView = homeFov;
        }

        void OnDisable()
        {
            if (weapon != null) weapon.Fired -= OnFired;
            if (health != null) health.Damaged -= OnDamaged;
            health = null;
            ResetPose();
            kick = default;
            trauma = pulse = 0f;
        }

        void OnGUI()
        {
            if (GameScreen.AnyOpen || pulse <= 0f || vignetteAmount <= 0f) return;
            if (vignette == null)
            {
                vignette = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "Damage vignette", wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color[64 * 64];
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float radius = new Vector2((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f).magnitude;
                        pixels[y * 64 + x] = new Color(0.65f, 0.025f, 0.01f, Mathf.SmoothStep(0f, 0.55f, Mathf.InverseLerp(0.45f, 1.3f, radius)));
                    }
                vignette.SetPixels(pixels);
                vignette.Apply(false, true);
            }
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, pulse * vignetteAmount);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), vignette);
            GUI.color = previous;
        }
        void OnDestroy() { if (vignette != null) Destroy(vignette); }
    }
}
