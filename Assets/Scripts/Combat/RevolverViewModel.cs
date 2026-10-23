using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ScammerNoScamming
{
    [DisallowMultipleComponent]
    public sealed class RevolverViewModel : MonoBehaviour
    {
        public GameObject revolverPrefab;
        public Material metalMaterial, handleMaterial;
        public Texture2D metalTexture, handleTexture;
        public Vector3 modelEuler = new Vector3(0f, 90f, 0f);
        [Tooltip("Use modelEuler as the whole local rotation instead of adding it to the FBX root rotation.")]
        public bool absoluteEuler;
        public Vector3 restPosition = new Vector3(0.22f, -0.2f, 0.42f);
        public Vector3 cylinderSpinAxis = Vector3.up;
        public Transform Muzzle { get; private set; }
        public Transform Rig { get; private set; }
        Weapon weapon;
        RevolverAmmo ammo;
        Weapon subscribedWeapon;
        RevolverAmmo subscribedAmmo;
        PlayerControls controls;
        PlayerMount mount;
        CharacterController body;
        FeedbackSpring positionKick, rotationKick;
        Vector2 sway;
        float stride, bobWeight;
        float cockAt = -1f;
        bool cockPending, shellsPlayed;
        Transform cylinder;
        Quaternion cylinderRest;
        Vector3 reloadPosition, reloadEuler;
        AudioSource mechanismAudio;
        Material steel, wood;
        readonly List<Material> convertedMaterials = new List<Material>();

        void Awake()
        {
            weapon = GetComponent<Weapon>();
            ammo = GetComponent<RevolverAmmo>();
            controls = GetComponent<PlayerControls>();
            mount = GetComponent<PlayerMount>();
            body = GetComponent<CharacterController>();
        }
        void OnEnable() => BindEvents();

        void BindEvents()
        {
            if (!isActiveAndEnabled) return;
            if (weapon == null) weapon = GetComponent<Weapon>();
            if (ammo == null) ammo = GetComponent<RevolverAmmo>();
            if (subscribedWeapon != weapon)
            {
                if (subscribedWeapon != null) subscribedWeapon.Fired -= OnFired;
                subscribedWeapon = weapon;
                if (subscribedWeapon != null) subscribedWeapon.Fired += OnFired;
            }
            if (subscribedAmmo == ammo) return;
            UnbindAmmo();
            subscribedAmmo = ammo;
            if (subscribedAmmo == null) return;
            subscribedAmmo.ReloadStarted += OnReloadStarted;
            subscribedAmmo.ReloadCompleted += OnReloadCompleted;
            subscribedAmmo.ReloadInterrupted += OnReloadInterrupted;
            subscribedAmmo.EmptyTriggered += OnEmptyTriggered;
        }
        void Start() => Build();

        public void Build()
        {
            // Legacy scenes may gain ammo from the controller's Awake, after this view's OnEnable.
            BindEvents();
            if (Rig != null) return;
            var camera = GetComponentInChildren<Camera>();
            if (camera == null) return;
            mechanismAudio = ProceduralFeelAudio.Source(gameObject, false);
            // Warm the fixed cache once, rather than allocating the first time a reload plays.
            ProceduralFeelAudio.Get(FeelSound.CylinderClick);
            ProceduralFeelAudio.Get(FeelSound.ShellDrop);
            ProceduralFeelAudio.Get(FeelSound.Cock);
            Rig = new GameObject("Revolver view model").transform;
            Rig.SetParent(camera.transform, false);
            Rig.localPosition = restPosition;
            if (revolverPrefab != null)
            {
                var model = Instantiate(revolverPrefab, Rig);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = absoluteEuler ? Quaternion.Euler(modelEuler) : Quaternion.Euler(modelEuler) * revolverPrefab.transform.localRotation; // keep the FBX root rotation
                model.transform.localScale = Vector3.one;
                foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = false;
                    DestroyGenerated(collider);
                }
                var renderers = model.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var source = materials[i];
                        bool handle = source != null && source.name.IndexOf("handle", System.StringComparison.OrdinalIgnoreCase) >= 0;
                        materials[i] = TexturedMaterial(source, handle);
                    }
                    renderer.sharedMaterials = materials;
                }
                foreach (var child in model.GetComponentsInChildren<Transform>(true))
                    if (child != model.transform && child.name.IndexOf("cylinder", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        cylinder = child;
                        break;
                    }
                if (renderers.Length > 0)
                {
                    // Normalize in the rig's space. A world AABB grows when the camera turns,
                    // so converting that box back would change the gun's apparent size and centre.
                    Bounds bounds = default;
                    bool first = true;
                    foreach (var renderer in renderers)
                    {
                        var local = renderer.localBounds;
                        Matrix4x4 toRig = Rig.worldToLocalMatrix * renderer.localToWorldMatrix;
                        for (int corner = 0; corner < 8; corner++)
                        {
                            Vector3 point = toRig.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents,
                                new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f,
                                    (corner & 4) == 0 ? -1f : 1f)));
                            if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                            else bounds.Encapsulate(point);
                        }
                    }
                    float size = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                    Vector3 centre = bounds.center;
                    float scale = 0.34f / Mathf.Max(size, 0.001f);
                    model.transform.localScale *= scale;
                    model.transform.localPosition = -centre * scale;
                }
            }
            else BuildFallback();
            if (cylinder != null) cylinderRest = cylinder.localRotation;
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

        Material TexturedMaterial(Material source, bool handle)
        {
            // Serialized URP assets retain the shader/textures in builds; convert only this instance.
            var template = handle ? handleMaterial : metalMaterial;
            var shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return source;
            var material = template != null ? new Material(template) : new Material(shader);
            var texture = handle ? handleTexture : metalTexture;
            Texture baseMap = texture;
            if (baseMap == null && source != null)
            {
                if (source.HasProperty("_BaseMap")) baseMap = source.GetTexture("_BaseMap");
                if (baseMap == null && source.HasProperty("_MainTex")) baseMap = source.GetTexture("_MainTex");
            }
            if (baseMap != null) material.SetTexture("_BaseMap", baseMap);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", handle ? 0f : 0.85f);
            material.SetFloat("_Smoothness", handle ? 0.35f : 0.55f);
            convertedMaterials.Add(material);
            return material;
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
            cylinder = Rig.Find("Cylinder");
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
            if (collider != null) { collider.enabled = false; DestroyGenerated(collider); }
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
        }

        void OnFired(ShotResult shot)
        {
            positionKick.Value.z = Mathf.Max(-0.08f, positionKick.Value.z - 0.035f);
            positionKick.Velocity += new Vector3(0f, 0.14f, -0.7f);
            rotationKick.Value.x = Mathf.Max(-22f, rotationKick.Value.x - 9f);
            rotationKick.Velocity += new Vector3(-100f, 8f, -16f);
            cockAt = Time.time + 0.105f;
            cockPending = true;
        }

        void PlayMechanism(FeelSound sound, float volume, float pitch = 1f)
        {
            if (mechanismAudio == null || GameScreen.AnyOpen) return;
            mechanismAudio.pitch = pitch;
            mechanismAudio.PlayOneShot(ProceduralFeelAudio.Get(sound), volume);
        }

        void OnEmptyTriggered() => PlayMechanism(FeelSound.CylinderClick, 0.18f, 0.8f);

        void OnReloadStarted()
        {
            shellsPlayed = false;
            cockPending = false;
            PlayMechanism(FeelSound.CylinderClick, 0.22f);
        }

        void OnReloadCompleted()
        {
            PlayMechanism(FeelSound.CylinderClick, 0.25f, 0.9f);
            ResetReloadPose();
        }

        void OnReloadInterrupted()
        {
            if (mechanismAudio != null) mechanismAudio.Stop();
            cockPending = false;
            ResetReloadPose();
        }

        void ResetReloadPose()
        {
            reloadPosition = reloadEuler = Vector3.zero;
            if (cylinder != null) cylinder.localRotation = cylinderRest;
            if (Rig != null) Rig.SetLocalPositionAndRotation(restPosition, Quaternion.identity);
        }

        void UpdateMechanism(float dt, bool visible)
        {
            if (cockPending && Time.time >= cockAt)
            {
                cockPending = false;
                if (visible)
                {
                    rotationKick.Velocity += new Vector3(-26f, 0f, 12f);
                    positionKick.Velocity += new Vector3(0f, 0.025f, -0.06f);
                    PlayMechanism(FeelSound.Cock, 0.17f);
                }
            }
            bool reloading = ammo != null && ammo.Reloading;
            float progress = reloading ? ammo.ReloadProgress : 0f;
            if (reloading && !shellsPlayed && progress >= 0.32f)
            {
                shellsPlayed = true;
                PlayMechanism(FeelSound.ShellDrop, 0.24f);
            }
            float pose = reloading ? Ease(0f, 0.23f, progress) * (1f - Ease(0.74f, 1f, progress)) : 0f;
            Vector3 targetPosition = new Vector3(-0.16f, -0.13f, -0.05f) * pose;
            Vector3 targetEuler = new Vector3(26f, -18f, -50f) * pose;
            float spin = reloading ? Ease(0.24f, 0.72f, progress) * 360f : 0f;
            if (cylinder != null)
                cylinder.localRotation = cylinderRest * Quaternion.AngleAxis(spin, cylinderSpinAxis);
            else
                targetEuler.z += spin; // Single-mesh fallback: rotate the whole gun.
            float blend = 1f - Mathf.Exp(-28f * dt);
            reloadPosition = Vector3.Lerp(reloadPosition, targetPosition, blend);
            // Keep the fallback spin continuous through 360 degrees during the reload.
            reloadEuler = Vector3.Lerp(reloadEuler, targetEuler, blend);
        }

        static float Ease(float start, float end, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));

        void LateUpdate()
        {
            if (Rig == null) return;
            bool visible = !GameScreen.AnyOpen;
            if (Rig.gameObject.activeSelf != visible) Rig.gameObject.SetActive(visible);
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            UpdateMechanism(dt, visible);
            if (!visible) { positionKick = rotationKick = default; sway = Vector2.zero; return; }
            if (Time.deltaTime <= 0f) return;
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
            Rig.localPosition = restPosition + reloadPosition + positionKick.Value + idle + FeedbackMotion.Bob(stride) * (bobWeight * 0.009f)
                + new Vector3(-sway.x, -sway.y, 0f) * 0.006f;
            Rig.localRotation = Quaternion.Euler(reloadEuler + rotationKick.Value + new Vector3(sway.y, -sway.x, -sway.x * 0.5f));
        }

        void OnDisable()
        {
            if (subscribedWeapon != null) subscribedWeapon.Fired -= OnFired;
            subscribedWeapon = null;
            UnbindAmmo();
            if (mechanismAudio != null) mechanismAudio.Stop();
            cockPending = false;
            ResetReloadPose();
            if (Rig != null) Rig.gameObject.SetActive(false);
        }

        void UnbindAmmo()
        {
            if (subscribedAmmo != null)
            {
                subscribedAmmo.ReloadStarted -= OnReloadStarted;
                subscribedAmmo.ReloadCompleted -= OnReloadCompleted;
                subscribedAmmo.ReloadInterrupted -= OnReloadInterrupted;
                subscribedAmmo.EmptyTriggered -= OnEmptyTriggered;
            }
            subscribedAmmo = null;
        }
        void OnDestroy()
        {
            if (Rig != null) DestroyGenerated(Rig.gameObject);
            if (mechanismAudio != null) DestroyGenerated(mechanismAudio);
            if (steel != null) DestroyGenerated(steel);
            if (wood != null) DestroyGenerated(wood);
            foreach (var material in convertedMaterials) if (material != null) DestroyGenerated(material);
        }

        static void DestroyGenerated(Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
