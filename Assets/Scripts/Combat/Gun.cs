using System;
using UnityEngine;

namespace WeRInTrouble
{
    // Hitscan from the centre of the camera. Skips the shooter's own colliders,
    // including the horse being ridden, so aiming down never hits the mount.
    [RequireComponent(typeof(PlayerControls))]
    public class Gun : MonoBehaviour
    {
        public float damage = 1f;
        public float range = 120f;
        public float fireInterval = 0.2f;
        public bool automatic;
        public bool requireLockedCursor = true;
        public LayerMask hitMask = ~0;

        [Header("Tracer")]
        public Material tracerMaterial;
        public Vector3 muzzleOffset = new Vector3(0.25f, -0.2f, 0.6f);
        public float tracerDuration = 0.05f;

        // Position the shot was fired from, so other systems can react to the noise.
        public event Action<Vector3> Fired;

        public float LastHitTime { get; private set; } = -10f;

        readonly RaycastHit[] hits = new RaycastHit[32];
        PlayerControls controls;
        PlayerMount mount;
        Camera aimCamera;
        LineRenderer tracer;
        float nextFireTime;
        float tracerHideTime;

        void Awake()
        {
            controls = GetComponent<PlayerControls>();
            mount = GetComponent<PlayerMount>();
        }

        void Start()
        {
            aimCamera = GetComponentInChildren<Camera>();
            CreateTracer();
        }

        void Update()
        {
            if (tracer != null && tracer.enabled && Time.time >= tracerHideTime)
                tracer.enabled = false;

            if (aimCamera == null) return;
            if (requireLockedCursor && Cursor.lockState != CursorLockMode.Locked) return;

            bool wantsToFire = automatic ? controls.Fire.IsPressed() : controls.Fire.WasPressedThisFrame();
            if (wantsToFire && Time.time >= nextFireTime)
                Shoot();
        }

        void Shoot()
        {
            nextFireTime = Time.time + fireInterval;

            Transform cam = aimCamera.transform;
            Vector3 origin = cam.position;
            Vector3 direction = cam.forward;
            Vector3 end = origin + direction * range;

            int count = Physics.RaycastNonAlloc(origin, direction, hits, range, hitMask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            Collider target = null;
            for (int i = 0; i < count; i++)
            {
                if (IsOwnCollider(hits[i].collider) || hits[i].distance >= nearest) continue;
                nearest = hits[i].distance;
                end = hits[i].point;
                target = hits[i].collider;
            }

            if (target != null)
            {
                var health = target.GetComponentInParent<Health>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                    LastHitTime = Time.time;
                }
            }

            ShowTracer(cam.TransformPoint(muzzleOffset), end);
            Fired?.Invoke(transform.position);
        }

        bool IsOwnCollider(Collider other)
        {
            if (other.transform.IsChildOf(transform)) return true;
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
