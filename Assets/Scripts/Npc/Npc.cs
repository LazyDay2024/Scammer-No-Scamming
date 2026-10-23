using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ScammerNoScamming
{
    [RequireComponent(typeof(Health))]
    public abstract class Npc : MonoBehaviour
    {
        public Faction Faction = Faction.Scammer;
        public Collider bodyCollider;
        public float corpseDelay = 8f;
        public float sinkDuration = 1.5f;
        public float sinkDepth = 2f;

        public static readonly List<Npc> All = new List<Npc>();
        public static event Action<Npc, DamageInfo> Shot;
        public static event Action<Npc> Killed;

        public Health Health { get; private set; }
        public bool IsDead => Health != null && Health.IsDead;
        protected INpcAnimation Animation => animationComponent != null ? animation : NullNpcAnimation.Instance;

        MonoBehaviour animationComponent;
        INpcAnimation animation = NullNpcAnimation.Instance;
        bool deathHandled;
        bool resetStateCaptured;
        Vector3 spawnLocalPosition;
        Quaternion spawnLocalRotation;
        Collider[] resetColliders;
        bool[] colliderEnabled;
        NavMeshAgent[] resetAgents;
        bool[] agentEnabled;

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Health.deactivateOnDeath = false;
            CaptureResetState();
            FindAnimation();

            if (bodyCollider == null)
            {
                foreach (var collider in GetComponentsInChildren<Collider>(true))
                {
                    if (collider.isTrigger) continue;
                    bodyCollider = collider;
                    break;
                }
            }
        }

        void FindAnimation()
        {
            foreach (var component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!(component is INpcAnimation candidate)) continue;
                animationComponent = component;
                animation = candidate;
                break;
            }

        }

        void CaptureResetState()
        {
            if (resetStateCaptured) return;
            resetStateCaptured = true;
            spawnLocalPosition = transform.localPosition;
            spawnLocalRotation = transform.localRotation;
            resetColliders = GetComponentsInChildren<Collider>(true);
            colliderEnabled = new bool[resetColliders.Length];
            for (int i = 0; i < resetColliders.Length; i++) colliderEnabled[i] = resetColliders[i].enabled;
            resetAgents = GetComponentsInChildren<NavMeshAgent>(true);
            agentEnabled = new bool[resetAgents.Length];
            for (int i = 0; i < resetAgents.Length; i++) agentEnabled[i] = resetAgents[i].enabled;
        }

        public virtual void ResetForAction()
        {
            // Hidden trap NPCs may not have received Awake yet on their first attempt.
            if (Health == null) Health = GetComponent<Health>();
            CaptureResetState();
            StopAllCoroutines();
            deathHandled = false;
            foreach (var agent in resetAgents) if (agent != null) agent.enabled = false;
            transform.SetLocalPositionAndRotation(spawnLocalPosition, spawnLocalRotation);
            Health.deactivateOnDeath = false;
            Health.RestoreFull(Health.max);
            for (int i = 0; i < resetColliders.Length; i++)
                if (resetColliders[i] != null) resetColliders[i].enabled = colliderEnabled[i];
            for (int i = 0; i < resetAgents.Length; i++)
            {
                var agent = resetAgents[i];
                if (agent == null) continue;
                agent.enabled = agentEnabled[i];
                if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
            }
            FindAnimation();
            if (animationComponent is NpcAnimator playback) playback.ResetForAction();
            Animation.SetSpeed(0f);
            Animation.Aim(false);
            Animation.HandsUp(false);
            Animation.Cower(false);
        }

        protected virtual void OnEnable()
        {
            FindAnimation();
            if (!All.Contains(this)) All.Add(this);
            Health.Damaged += OnDamaged;
            Health.Died += HandleDeath;
            NpcGunfire.EnsureExists();
        }

        protected virtual void OnDisable()
        {
            All.Remove(this);
            if (Health == null) return;
            Health.Damaged -= OnDamaged;
            Health.Died -= HandleDeath;
        }

        void OnDamaged(Health health, DamageInfo info)
        {
            if (info.Amount <= 0f) return;
            Animation.Hit();
            if (info.SourceFaction == Faction.Player) Shot?.Invoke(this, info);
        }

        void HandleDeath(Health health)
        {
            if (deathHandled) return;
            deathHandled = true;
            OnDeath();
            Killed?.Invoke(this);
            if (Application.isPlaying && isActiveAndEnabled) StartCoroutine(SinkCorpse());
        }

        protected virtual void OnDeath()
        {
            Animation.SetSpeed(0f);
            Animation.Aim(false);
            Animation.Die();
            foreach (var agent in GetComponentsInChildren<NavMeshAgent>(true)) agent.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
        }

        IEnumerator SinkCorpse()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, corpseDelay));
            Vector3 start = transform.position;
            float elapsed = 0f;
            float duration = Mathf.Max(0f, sinkDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = start + Vector3.down * (Mathf.Max(0f, sinkDepth) * Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            gameObject.SetActive(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
            Shot = null;
            Killed = null;
        }
    }
}
