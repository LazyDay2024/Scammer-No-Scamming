using UnityEngine;

namespace ScammerNoScamming
{
    // Shared base for anything with health and a side: the player and every NPC.
    [RequireComponent(typeof(Health))]
    public abstract class Character : MonoBehaviour
    {
        public Health Health { get; private set; }
        public Faction Faction { get; protected set; }

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            // Characters play their own death instead of vanishing the moment health runs out.
            Health.deactivateOnDeath = false;
            Health.Died += OnDied;
        }

        protected virtual void OnDestroy()
        {
            if (Health != null) Health.Died -= OnDied;
        }

        protected virtual void OnDied(Health health) { }
    }
}
