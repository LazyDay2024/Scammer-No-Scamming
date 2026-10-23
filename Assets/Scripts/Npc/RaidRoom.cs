using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    public class RaidRoom : MonoBehaviour
    {
        public List<ArmedEnemy> Enemies = new List<ArmedEnemy>();
        public bool Cleared { get; private set; }
        public event Action OnCleared;
        public static event Action<RaidRoom> AnyCleared;

        void Awake() => FindEnemies();

        void OnEnable() => Npc.Killed += OnNpcKilled;

        void Start() => CheckCleared();

        void OnDisable() => Npc.Killed -= OnNpcKilled;

        // Recheck in case a caller changed the list or Health while the room was disabled.
        void Update()
        {
            if (!Cleared) CheckCleared();
        }

        void FindEnemies()
        {
            if (Enemies == null) Enemies = new List<ArmedEnemy>();
            if (Enemies.Count == 0) Enemies.AddRange(GetComponentsInChildren<ArmedEnemy>(true));
        }

        void OnNpcKilled(Npc npc)
        {
            if (npc is ArmedEnemy enemy && Enemies != null && Enemies.Contains(enemy)) CheckCleared();
        }

        public bool CheckCleared()
        {
            if (Cleared) return true;
            FindEnemies();
            // A blank area is not a completed raid, and a missing reference is not proof of a kill.
            if (Enemies.Count == 0) return false;
            foreach (var enemy in Enemies)
                if (enemy == null || !enemy.IsDead) return false;
            Cleared = true;
            OnCleared?.Invoke();
            AnyCleared?.Invoke(this);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyCleared = null;
    }
}
