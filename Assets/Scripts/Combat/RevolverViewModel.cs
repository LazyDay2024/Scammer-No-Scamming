using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ScammerNoScamming
{
    [DisallowMultipleComponent]
    public sealed class RevolverViewModel : MonoBehaviour
    {
        public GameObject revolverPrefab;
        public Vector3 modelEuler = new Vector3(0f, 90f, 0f);
        public Vector3 restPosition = new Vector3(0.22f, -0.2f, 0.42f);
        public Transform Muzzle { get; private set; }
        public Transform Rig { get; private set; }
        Weapon weapon;
        PlayerControls controls;
        PlayerMount mount;
        CharacterController body;
        FeedbackSpring positionKick, rotationKick;
        Vector2 sway;
        float stride, bobWeight;
        Material steel, wood;
        readonly List<Material> convertedMaterials = new List<Material>();

        void Awake()
        {
            weapon = GetComponent<Weapon>();
            controls = GetComponent<PlayerControls>();
            mount = GetComponent<PlayerMount>();
            body = GetComponent<CharacterController>();
        }
        void OnEnable() { if (weapon != null) weapon.Fired += OnFired; }
        void Start() => Build();

        public void Build()
        {
            if (Rig != null) return;
            var camera = GetComponentInChildren<Camera>();
            if (camera == null) return;
            Rig = new GameObject("Revolver view model").transform;
            Rig.SetParent(camera.transform, false);
            Rig.localPosition = restPosition;
            if (revolverPrefab != null)
            {
                var model = Instantiate(revolverPrefab, Rig);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(modelEuler);
                model.transform.localScale = Vector3.one;
                foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = false;
                    Destroy(collider);
                }
                // The source FBX embeds Built-in/Standard materials. Convert only this instance
                // so the first-person weapon renders in URP without editing the CC0 asset.
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                var renderers = model.GetComponentsInChildren<Renderer>();
                if (shader != null)
                    foreach (var renderer in renderers)
                    {
                        var materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            var source = materials[i];
                            if (source != null && source.shader != null && source.shader.name.StartsWith("Universal Render Pipeline/")) continue;
                            var material = new Material(shader) { color = source != null && source.HasProperty("_Color") ? source.GetColor("_Color") : new Color(0.2f, 0.22f, 0.23f) };
                            if (source != null && source.HasProperty("_MainTex")) material.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
                            materials[i] = material;
                            convertedMaterials.Add(material);
                        }
                        renderer.sharedMaterials = materials;
                    }
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    float size = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                    Vector3 centre = Rig.InverseTransformPoint(bounds.center);
                    float scale = 0.34f / Mathf.Max(size, 0.001f);
                    model.transform.localScale *= scale;
                    model.transform.localPosition = -centre * scale;
                }
            }
            else BuildFallback();
            foreach (var renderer in Rig.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            Muzzle = new GameObject("View model muzzle").transform;
            Muzzle.SetParent(Rig, false);
            Muzzle.localPosition = new Vector3(0f, 0.035f, 0.19f);
            Rig.gameObject.SetActive(!GameScreen.AnyOpen);
        }

        void BuildFallback()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                steel = new Material(shader) { color = new Color(0.13f, 0.15f, 0.16f) };
                if (steel.HasProperty("_Metallic")) steel.SetFloat("_Metallic", 0.8f);
                wood = new Material(shader) { color = new Color(0.23f, 0.1f, 0.045f) };
            }
            Part("Barrel", PrimitiveType.Cylinder, new Vector3(0f, 0.035f, 0.085f), new Vector3(0.033f, 0.09f, 0.033f), new Vector3(90f, 0f, 0f), steel);
            Part("Cylinder", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.085f, 0.037f, 0.085f), new Vector3(90f, 0f, 0f), steel);
            Part("Grip", PrimitiveType.Cube, new Vector3(0f, -0.085f, -0.055f), new Vector3(0.055f, 0.14f, 0.065f), new Vector3(-20f, 0f, 0f), wood);
            Part("Frame", PrimitiveType.Cube, new Vector3(0f, -0.025f, 0.01f), new Vector3(0.045f, 0.045f, 0.16f), Vector3.zero, steel);
            Part("Sight", PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.15f), new Vector3(0.008f, 0.015f, 0.018f), Vector3.zero, steel);
        }

        void Part(string label, PrimitiveType type, Vector3 position, Vector3 scale, Vector3 angles, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = label;
            part.transform.SetParent(Rig, false);
            part.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(angles));
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; Destroy(collider); }
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
        }

        void OnFired(ShotResult shot)
        {
            positionKick.Value.z = Mathf.Max(-0.08f, positionKick.Value.z - 0.035f);
            positionKick.Velocity += new Vector3(0f, 0.14f, -0.7f);
            rotationKick.Value.x = Mathf.Max(-22f, rotationKick.Value.x - 9f);
            rotationKick.Velocity += new Vector3(-100f, 8f, -16f);
        }

        void LateUpdate()
        {
            if (Rig == null) return;
            bool visible = !GameScreen.AnyOpen;
            if (Rig.gameObject.activeSelf != visible) Rig.gameObject.SetActive(visible);
            if (!visible) { positionKick = rotationKick = default; sway = Vector2.zero; return; }
            if (Time.deltaTime <= 0f) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            Vector2 look = controls != null && controls.enabled ? controls.Look.ReadValue<Vector2>() : Vector2.zero;
            sway = Vector2.Lerp(sway, Vector2.ClampMagnitude(look, 25f) * 0.035f, 1f - Mathf.Exp(-10f * dt));
            var horse = mount != null ? mount.CurrentHorse : null;
            float speed = horse != null ? Mathf.Abs(horse.Speed) : body != null && body.enabled && body.isGrounded
                ? Vector3.ProjectOnPlane(body.velocity, Vector3.up).magnitude : 0f;
            stride = (stride + speed * dt / (horse != null ? 4.5f : 3.3f)) % 1f;
            bobWeight = Mathf.Lerp(bobWeight, Mathf.Clamp01(speed / 3f), 1f - Mathf.Exp(-8f * dt));
            positionKick.Step(dt, 19f);
            rotationKick.Step(dt, 17f);
            Vector3 idle = new Vector3(Mathf.Sin(Time.time * 1.4f), Mathf.Sin(Time.time * 1.8f), 0f) * 0.0012f;
            Rig.localPosition = restPosition + positionKick.Value + idle + FeedbackMotion.Bob(stride) * (bobWeight * 0.009f)
                + new Vector3(-sway.x, -sway.y, 0f) * 0.006f;
            Rig.localRotation = Quaternion.Euler(rotationKick.Value + new Vector3(sway.y, -sway.x, -sway.x * 0.5f));
        }

        void OnDisable()
        {
            if (weapon != null) weapon.Fired -= OnFired;
            if (Rig != null) Rig.gameObject.SetActive(false);
        }
        void OnDestroy()
        {
            if (Rig != null) Destroy(Rig.gameObject);
            if (steel != null) Destroy(steel);
            if (wood != null) Destroy(wood);
            foreach (var material in convertedMaterials) if (material != null) Destroy(material);
        }
    }
}
