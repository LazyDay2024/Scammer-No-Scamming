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

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Health.deactivateOnDeath = false;
            foreach (var component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!(component is INpcAnimation candidate)) continue;
                animationComponent = component;
                animation = candidate;
                break;
            }

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

        protected virtual void OnEnable()
        {
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
                collider.enabled = collider == bodyCollider;
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
