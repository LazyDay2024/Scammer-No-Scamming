using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // Reads the fire button and aims from the centre of the camera; the weapon does the shooting.
    [RequireComponent(typeof(PlayerControls), typeof(HitscanWeapon))]
    public class PlayerWeaponController : MonoBehaviour
    {
        public bool automatic;
        public bool requireLockedCursor = true;

        [Header("Tracer")]
        public Material tracerMaterial;
        public Vector3 muzzleOffset = new Vector3(0.25f, -0.2f, 0.6f);
        public float tracerDuration = 0.05f;

        // Position the shot was heard from, so other systems can react to the noise.
        public event Action<Vector3> Fired;

        public float LastHitTime { get; private set; } = -10f;
        public HitscanWeapon Weapon => weapon;
        public RevolverAmmo Ammo => weapon != null ? weapon.Ammo : GetComponent<RevolverAmmo>();
        // API used by Nithis's trap consequences and trap course.
        public void Refill() => Ammo?.RefillReserve();
        public void FullRestock() => Ammo?.RefillAll();
        public void Drain() => Ammo?.Empty();

        PlayerControls controls;
        PlayerMount mount;
        HitscanWeapon weapon;
        Camera aimCamera;
        LineRenderer tracer;
        float tracerHideTime;

        void Awake()
        {
            controls = GetComponent<PlayerControls>();
            mount = GetComponent<PlayerMount>();
            weapon = GetComponent<HitscanWeapon>();
            if (weapon == null) weapon = gameObject.AddComponent<HitscanWeapon>();
            // Older serialized player scenes also opt in without rebuilding the sandbox.
            if (GetComponent<RevolverAmmo>() == null) gameObject.AddComponent<RevolverAmmo>();
            weapon.Ignore = IsOwnCollider;
        }

        void Start()
        {
            aimCamera = GetComponentInChildren<Camera>();
            CreateTracer();
        }

        void OnEnable() => SupplyCrate.Collected += OnSupplyCollected;

        void OnDisable() => SupplyCrate.Collected -= OnSupplyCollected;

        void OnSupplyCollected(SupplyCrate crate, GameObject user)
        {
            if (user != gameObject || crate == null || crate.isFake) return;
            Ammo?.RefillReserve();
        }

        void Update()
        {
            if (tracer != null && tracer.enabled && Time.time >= tracerHideTime)
                tracer.enabled = false;

            if (aimCamera == null) return;
            if (requireLockedCursor && Cursor.lockState != CursorLockMode.Locked) return;

            if (controls.Reload.WasPressedThisFrame()) Ammo?.TryReload();

            bool wantsToFire = automatic ? controls.Fire.IsPressed() : controls.Fire.WasPressedThisFrame();
            if (!wantsToFire) return;

            Transform cam = aimCamera.transform;
            if (!weapon.TryFire(cam.position, cam.forward, Faction.Player, out ShotResult shot)) return;

            if (shot.HitDamageable) LastHitTime = Time.time;
            var viewModel = GetComponent<RevolverViewModel>();
            Vector3 muzzlePosition = viewModel != null && viewModel.Muzzle != null
                ? viewModel.Muzzle.position : cam.TransformPoint(muzzleOffset);
            ShowTracer(muzzlePosition, shot.EndPoint);
            Fired?.Invoke(shot.NoisePosition);
            GameEvents.RaisePlayerFired(shot.NoisePosition);
        }

        bool IsOwnCollider(Collider other)
        {
            return mount != null && mount.CurrentHorse != null && other.transform.IsChildOf(mount.CurrentHorse.transform);
        }

        void CreateTracer()
        {
            Material material = tracerMaterial;
            if (material == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) return;
                material = new Material(shader) { color = new Color(1f, 0.9f, 0.4f) };
            }

            var tracerObject = new GameObject("Tracer");
            tracerObject.transform.SetParent(transform, false);
            tracer = tracerObject.AddComponent<LineRenderer>();
            tracer.sharedMaterial = material;
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.startWidth = 0.04f;
            tracer.endWidth = 0.02f;
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracer.enabled = false;
        }

        void ShowTracer(Vector3 from, Vector3 to)
        {
            if (tracer == null) return;
            tracer.SetPosition(0, from);
            tracer.SetPosition(1, to);
            tracer.enabled = true;
            tracerHideTime = Time.time + tracerDuration;
        }
    }
}
