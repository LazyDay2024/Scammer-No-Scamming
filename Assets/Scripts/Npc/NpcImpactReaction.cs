using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Visual-only impulse, layered after animation. Never displaces a living NPC's hitboxes/agent.
    [DisallowMultipleComponent, RequireComponent(typeof(Health))]
    public sealed class NpcImpactReaction : MonoBehaviour
    {
        readonly List<Transform> visuals = new List<Transform>();
        readonly List<Vector3> positions = new List<Vector3>();
        readonly List<Quaternion> rotations = new List<Quaternion>();
        readonly List<Renderer> originals = new List<Renderer>();
        readonly List<GameObject> proxies = new List<GameObject>();
        Health health;
        FeedbackSpring flinch;
        Vector3 shotDirection, fallDirection;
        float deathTime;
        float fallAngle = 78f;
        bool fallen;

        void Awake() => health = GetComponent<Health>();
        void OnEnable()
        {
            health.Damaged += OnDamage;
            health.Died += OnDeath;
        }

        void CaptureVisuals()
        {
            if (visuals.Count > 0) return;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || renderer is LineRenderer) continue;
                Transform visual = renderer.transform;
                while (visual.parent != null && visual.parent != transform) visual = visual.parent;
                if (renderer.transform == transform) visual = transform;
                // Primitive fallback NPCs can share a transform with their collider. Copy only
                // rendering into a child rather than moving that collider (or the agent).
                if (visual == transform || visual.GetComponentInChildren<Collider>(true) != null
                    || visual.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>(true) != null)
                {
                    visual = RenderProxy(renderer);
                    if (visual == null) continue;
                }
                if (visuals.Contains(visual)) continue;
                visuals.Add(visual);
                positions.Add(visual.localPosition);
                rotations.Add(visual.localRotation);
            }
        }

        Transform RenderProxy(Renderer source)
        {
            var proxy = new GameObject("Hit reaction visual");
            proxy.transform.SetParent(source.transform, false);
            Renderer copy;
            if (source is SkinnedMeshRenderer skin)
            {
                var target = proxy.AddComponent<SkinnedMeshRenderer>();
                target.sharedMesh = skin.sharedMesh;
                target.bones = skin.bones;
                target.rootBone = skin.rootBone;
                target.localBounds = skin.localBounds;
                copy = target;
            }
            else if (source is MeshRenderer && source.TryGetComponent<MeshFilter>(out var mesh))
            {
                proxy.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                copy = proxy.AddComponent<MeshRenderer>();
            }
            else { Destroy(proxy); return null; }
            copy.sharedMaterials = source.sharedMaterials;
            copy.shadowCastingMode = source.shadowCastingMode;
            copy.receiveShadows = source.receiveShadows;
            copy.enabled = source.enabled;
            // Only enabled source renderers need restoring if the component is removed.
            if (source.enabled) originals.Add(source);
            source.enabled = false;
            proxies.Add(proxy);
            return proxy.transform;
        }

        void OnDamage(Health sender, DamageInfo info)
        {
            if (info.Amount <= 0f) return;
            CaptureVisuals();
            Vector3 direction = info.Direction;
            if (direction.sqrMagnitude < 0.001f) direction = transform.position + Vector3.up - info.Point;
            direction.y = 0f;
            shotDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : -transform.forward;
            flinch.Velocity += transform.InverseTransformDirection(shotDirection) * 2.4f;
            flinch.Velocity = Vector3.ClampMagnitude(flinch.Velocity, 5f);
        }

        void OnDeath(Health sender)
        {
            CaptureVisuals();
            fallDirection = transform.InverseTransformDirection(shotDirection.sqrMagnitude > 0.001f ? shotDirection : -transform.forward);
            deathTime = Time.time;
            var playback = GetComponent<NpcAnimator>();
            // A real Defeat clip already collapses the body; add directional weight rather
            // than rotating its prone pose through the floor. Missing clips get a full fall.
            fallAngle = playback != null && playback.defeat != null ? 20f : 78f;
            fallen = true;
        }

        void LateUpdate()
        {
            // Case reset may reactivate a pooled NPC without disabling this component.
            if (fallen && !health.IsDead) ResetPose();
            flinch.Step(Time.deltaTime, 22f);
            float progress = fallen ? Mathf.Clamp01((Time.time - deathTime) / 0.55f) : 0f;
            float ease = progress * progress * (3f - 2f * progress);
            Quaternion fall = Quaternion.Euler(fallDirection.z * fallAngle * ease, 0f, -fallDirection.x * fallAngle * ease);
            for (int i = 0; i < visuals.Count; i++)
            {
                if (visuals[i] == null) continue;
                Vector3 position = positions[i] + flinch.Value;
                if (fallen) position += fallDirection * (0.32f * ease) + Vector3.up * (0.08f * Mathf.Sin(progress * Mathf.PI));
                visuals[i].localPosition = position;
                visuals[i].localRotation = fall * Quaternion.Euler(flinch.Value.z * 16f, 0f, -flinch.Value.x * 16f) * rotations[i];
            }
        }

        public void ResetPose()
        {
            for (int i = 0; i < visuals.Count; i++)
                if (visuals[i] != null) visuals[i].SetLocalPositionAndRotation(positions[i], rotations[i]);
            flinch = default;
            fallen = false;
            shotDirection = fallDirection = Vector3.zero;
        }

        void OnDisable()
        {
            if (health != null) { health.Damaged -= OnDamage; health.Died -= OnDeath; }
            ResetPose();
        }
        void OnDestroy()
        {
            foreach (var original in originals) if (original != null) original.enabled = true;
            foreach (var proxy in proxies) if (proxy != null) Destroy(proxy);
        }
    }
}
