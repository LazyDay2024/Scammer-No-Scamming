using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Put Health on the prop root; mesh colliders below it are shootable by HitscanWeapon.
    // Camp tanks live in RaidTemplates, so each attempt receives undamaged clones.
    [DisallowMultipleComponent, RequireComponent(typeof(Health))]
    public sealed class ExplosiveProp : MonoBehaviour
    {
        public float radius = 7f;
        public float maximumDamage = 6f;
        public Vector3 localBlastCenter = Vector3.up * .6f;
        public LayerMask damageMask = ~0;
        public Material burstMaterial;

        Health health;
        bool exploded;
        Faction sourceFaction;

        void Awake()
        {
            health = GetComponent<Health>();
            // Keep this component alive for the flash/audio tail after hiding the tank mesh.
            health.deactivateOnDeath = false;
        }

        void OnEnable()
        {
            if (health == null) health = GetComponent<Health>();
            health.Damaged += RememberSource;
            health.Died += Explode;
        }

        void OnDisable()
        {
            if (health == null) return;
            health.Damaged -= RememberSource;
            health.Died -= Explode;
        }

        void RememberSource(Health target, DamageInfo info) => sourceFaction = info.SourceFaction;

        public static float DamageAtDistance(float distance, float blastRadius, float peakDamage)
        {
            if (!Finite(distance) || !Finite(blastRadius) || !Finite(peakDamage) || blastRadius <= 0f || peakDamage <= 0f)
                return 0f;
            return peakDamage * (1f - Mathf.Clamp01(Mathf.Max(0f, distance) / blastRadius));
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        void Explode(Health target)
        {
            if (exploded) return; // Chain reactions may reach this tank again during this callback.
            exploded = true;
            Vector3 centre = transform.TransformPoint(localBlastCenter);
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            StartCoroutine(Effects(centre));
            if (!Finite(radius) || radius <= 0f) return;

            // A character has several hit zones. Damage its Health once, using its closest collider.
            // Use a complete overlap query here: truncated buffers silently lose targets in crowds.
            var targets = new Dictionary<Health, Vector3>();
            foreach (var collider in Physics.OverlapSphere(centre, radius, damageMask, QueryTriggerInteraction.Ignore))
            {
                var zone = collider.GetComponentInParent<HitZone>();
                var victim = zone != null && zone.Owner != null ? zone.Owner : collider.GetComponentInParent<Health>();
                if (victim == null || victim == health || victim.IsDead) continue;
                Vector3 point = collider.ClosestPoint(centre);
                if (!targets.TryGetValue(victim, out var previous) || (point - centre).sqrMagnitude < (previous - centre).sqrMagnitude)
                    targets[victim] = point;
            }
            foreach (var entry in targets)
            {
                var victim = entry.Key;
                if (victim == null || victim.IsDead) continue;
                Vector3 point = entry.Value;
                Vector3 direction = point - centre;
                // Solid walls and cover protect workers as well as guards.
                if (Physics.Raycast(centre, direction.normalized, out var hit, direction.magnitude, damageMask, QueryTriggerInteraction.Ignore))
                {
                    var zone = hit.collider.GetComponentInParent<HitZone>();
                    var obstructionHealth = zone != null && zone.Owner != null ? zone.Owner : hit.collider.GetComponentInParent<Health>();
                    if (obstructionHealth != victim) continue;
                }
                float amount = DamageAtDistance(direction.magnitude, radius, maximumDamage);
                if (amount > 0f)
                    ((IDamageable)victim).TakeDamage(new DamageInfo(amount, HitZoneType.Body, sourceFaction, point) { Direction = direction.normalized });
            }
        }

        IEnumerator Effects(Vector3 centre)
        {
            var effect = new GameObject("Propane blast");
            effect.transform.SetParent(transform, true);
            effect.transform.position = centre;
            var flash = effect.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, .48f, .12f);
            flash.range = 15f;
            flash.shadows = LightShadows.None;
            var audio = ProceduralFeelAudio.Source(effect, true, 100f);
            audio.pitch = .42f; // Stretch the existing procedural shot's crack and bass tail into a boom.
            audio.PlayOneShot(ProceduralFeelAudio.Get(FeelSound.Shot));

            var particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.4f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(.4f, 1.2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 90;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .3f;
            var colour = particles.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(.3f, .23f, .18f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colour.color = gradient;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = burstMaterial;
            renderer.enabled = burstMaterial != null;
            particles.Play();
            particles.Emit(new ParticleSystem.EmitParams { startColor = new Color(1f, .38f, .07f), startLifetime = .55f }, 22);
            particles.Emit(new ParticleSystem.EmitParams { startColor = new Color(.63f, .48f, .32f, .7f) }, 48);
            float elapsed = 0f;
            while (elapsed < .2f)
            {
                flash.intensity = Mathf.Lerp(14f, 0f, elapsed / .2f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            flash.enabled = false;
            Destroy(effect, 3f);
        }
    }
}
