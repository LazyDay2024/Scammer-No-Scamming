using System;
using UnityEngine;

namespace WeRInTrouble
{
    public class Health : MonoBehaviour
    {
        public float max = 3f;
        public bool deactivateOnDeath = true;

        public event Action<Health> Died;

        public float Current { get; private set; }
        public bool IsDead { get; private set; }

        void Awake()
        {
            Current = max;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            Current -= amount;
            if (Current > 0f) return;

            IsDead = true;
            Died?.Invoke(this);
            if (deactivateOnDeath) gameObject.SetActive(false);
        }
    }
}
