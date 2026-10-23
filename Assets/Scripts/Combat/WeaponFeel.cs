using UnityEngine;
using UnityEngine.Rendering;

namespace ScammerNoScamming
{
    // Fixed particle/audio pools: no per-shot GameObjects, materials or clips.
    [DisallowMultipleComponent]
    public sealed class WeaponFeel : MonoBehaviour
    {
        public Shader particleShader;
        public float shotVolume = 0.48f;
        Weapon weapon;
        RevolverViewModel viewModel;
        AudioSource shotAudio;
        readonly AudioSource[] impactAudio = new AudioSource[4];
        readonly Vector3[] impactPositions = new Vector3[4];
        int nextImpact;
        Light flashLight;
        ParticleSystem flash, smoke, impacts;
        Material additive, alpha;
        float flashUntil;

        void Awake()
        {
            weapon = GetComponent<Weapon>();
            viewModel = GetComponent<RevolverViewModel>();
        }
        void OnEnable() { if (weapon != null) weapon.Fired += OnFired; }
        void Start() => Initialize();

        void Initialize()
        {
            if (shotAudio != null) return;
            shotAudio = ProceduralFeelAudio.Source(gameObject, false);
            for (int i = 0; i < impactAudio.Length; i++)
            {
                var voice = new GameObject("Impact audio " + i);
                voice.transform.SetParent(transform, false);
                impactAudio[i] = ProceduralFeelAudio.Source(voice, true, 45f);
            }
            if (viewModel != null) viewModel.Build();
            var lightObject = new GameObject("Revolver flash light");
            lightObject.transform.SetParent(transform, false);
            flashLight = lightObject.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f, 0.62f, 0.22f);
            flashLight.intensity = 2.4f;
            flashLight.range = 3.5f;
            flashLight.shadows = LightShadows.None;
            flashLight.enabled = false;
            if (particleShader == null) particleShader = Shader.Find("ScammerNoScamming/FeelParticle");
            if (particleShader == null) return;
            alpha = new Material(particleShader);
            additive = new Material(particleShader);
            additive.SetFloat("_DstBlend", (float)BlendMode.One);
            flash = CreatePool("Muzzle flare and sparks", additive, 128, false);
            smoke = CreatePool("Gun smoke", alpha, 64, true);
            impacts = CreatePool("Bullet impacts", alpha, 192, true);
        }

        ParticleSystem CreatePool(string label, Material material, int capacity, bool grow)
        {
            var pool = new GameObject(label);
            pool.transform.SetParent(transform, false);
            var particles = pool.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = capacity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            if (grow)
            {
                var size = particles.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.3f, 1f, 1.5f));
            }
            var renderer = pool.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }

        void OnFired(ShotResult shot)
        {
            Initialize();
            if (GameScreen.AnyOpen) return;
            shotAudio.pitch = Random.Range(0.96f, 1.04f);
            shotAudio.PlayOneShot(ProceduralFeelAudio.Get(FeelSound.Shot), shotVolume);
            Vector3 direction = (shot.EndPoint - shot.Origin).normalized;
            Vector3 muzzle = viewModel != null && viewModel.Muzzle != null ? viewModel.Muzzle.position : shot.Origin + direction * 0.5f;
            flashLight.transform.position = muzzle;
            flashLight.enabled = true;
            flashUntil = Time.unscaledTime + 0.035f;
            Emit(flash, muzzle, direction * 0.8f, new Color(1f, 0.7f, 0.22f, 0.95f), 0.16f, 0.035f, 2);
            Emit(smoke, muzzle, direction * 0.18f + Vector3.up * 0.18f, new Color(0.5f, 0.48f, 0.44f, 0.22f), 0.13f, 0.7f, 5);
            if (!shot.Hit) return;
            Color color = shot.HitDamageable ? new Color(0.32f, 0.025f, 0.015f, 0.65f) : new Color(0.56f, 0.43f, 0.28f, 0.6f);
            Vector3 point = shot.EndPoint + shot.Normal * 0.015f;
            Emit(impacts, point, shot.Normal * 0.5f + Vector3.up * 0.12f, color, shot.HitDamageable ? 0.12f : 0.18f, 0.4f, 9);
            if (!shot.HitDamageable)
                Emit(flash, point, shot.Normal * 1.4f, new Color(1f, 0.7f, 0.3f), 0.035f, 0.12f, 4);
            int index = nextImpact++ % impactAudio.Length;
            impactPositions[index] = shot.EndPoint;
            var voice = impactAudio[index];
            voice.transform.position = shot.EndPoint;
            voice.pitch = shot.HitDamageable ? 0.7f : Random.Range(1f, 1.2f);
            voice.PlayOneShot(ProceduralFeelAudio.Get(FeelSound.Impact), shot.HitDamageable ? 0.12f : 0.2f);
        }

        static void Emit(ParticleSystem pool, Vector3 point, Vector3 velocity, Color color, float size, float life, int count)
        {
            if (pool == null) return;
            if (!pool.isPlaying) pool.Play();
            for (int i = 0; i < count; i++)
            {
                var particle = new ParticleSystem.EmitParams
                {
                    position = point,
                    velocity = velocity + Random.insideUnitSphere * velocity.magnitude * 0.55f,
                    startColor = color,
                    startSize = size * Random.Range(0.7f, 1.3f),
                    startLifetime = life * Random.Range(0.75f, 1.2f),
                    rotation = Random.Range(0f, 360f)
                };
                pool.Emit(particle, 1);
            }
        }

        void Update()
        {
            if (flashLight != null && (GameScreen.AnyOpen || Time.unscaledTime >= flashUntil)) flashLight.enabled = false;
            if (GameScreen.AnyOpen && flash != null) flash.Clear();
        }
        void LateUpdate()
        {
            for (int i = 0; i < impactAudio.Length; i++)
                if (impactAudio[i] != null && impactAudio[i].isPlaying) impactAudio[i].transform.position = impactPositions[i];
        }
        void OnDisable()
        {
            if (weapon != null) weapon.Fired -= OnFired;
            if (flashLight != null) flashLight.enabled = false;
            if (shotAudio != null) shotAudio.Stop();
            foreach (var voice in impactAudio) if (voice != null) voice.Stop();
            if (flash != null) flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (smoke != null) smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (impacts != null) impacts.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void OnDestroy()
        {
            if (additive != null) Destroy(additive);
            if (alpha != null) Destroy(alpha);
        }
    }
}
