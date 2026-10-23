using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Existing fire events belong to instances, including weapons spawned during a raid.
    public sealed class NpcGunfire : MonoBehaviour
    {
        public static event Action<Vector3> Heard;
        public float discoveryInterval = 0.25f;

        static NpcGunfire instance;
        float nextDiscoveryTime;
        readonly HashSet<Weapon> weapons = new HashSet<Weapon>();
        readonly Dictionary<PlayerWeaponController, Action<Vector3>> controllers = new Dictionary<PlayerWeaponController, Action<Vector3>>();
        readonly List<Weapon> removedWeapons = new List<Weapon>();
        readonly List<PlayerWeaponController> removedControllers = new List<PlayerWeaponController>();

        public static void EnsureExists()
        {
            if (!Application.isPlaying) return;
            if (instance == null) instance = FindAnyObjectByType<NpcGunfire>();
            if (instance == null) instance = new GameObject("NPC Gunfire Listener").AddComponent<NpcGunfire>();
            instance.RefreshSubscriptions();
        }

        void OnEnable()
        {
            if (instance != null && instance != this)
            {
                enabled = false;
                return;
            }
            instance = this;
            RefreshSubscriptions();
        }

        void Update()
        {
            if (Time.unscaledTime >= nextDiscoveryTime) RefreshSubscriptions();
        }

        public void RefreshSubscriptions()
        {
            nextDiscoveryTime = Time.unscaledTime + Mathf.Max(0.01f, discoveryInterval);
            foreach (var weapon in FindObjectsByType<Weapon>(FindObjectsSortMode.None))
                if (weapons.Add(weapon)) weapon.Fired += OnFired;
            foreach (var controller in FindObjectsByType<PlayerWeaponController>(FindObjectsSortMode.None))
            {
                if (controllers.ContainsKey(controller)) continue;
                var captured = controller;
                Action<Vector3> callback = position =>
                {
                    // PlayerWeaponController relays the same shot after Weapon.Fired.
                    if (captured == null || (captured.Weapon != null && weapons.Contains(captured.Weapon))) return;
                    Heard?.Invoke(position);
                };
                controllers.Add(controller, callback);
                controller.Fired += callback;
            }

            removedWeapons.Clear();
            foreach (var weapon in weapons)
                if (weapon == null) removedWeapons.Add(weapon);
            foreach (var weapon in removedWeapons) weapons.Remove(weapon);
            removedControllers.Clear();
            foreach (var entry in controllers)
                if (entry.Key == null) removedControllers.Add(entry.Key);
            foreach (var controller in removedControllers) controllers.Remove(controller);
        }

        void OnFired(ShotResult result) => Heard?.Invoke(result.NoisePosition);

        void OnDisable()
        {
            foreach (var weapon in weapons)
                if (weapon != null) weapon.Fired -= OnFired;
            foreach (var entry in controllers)
                if (entry.Key != null) entry.Key.Fired -= entry.Value;
            weapons.Clear();
            controllers.Clear();
            if (instance == this) instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            Heard = null;
        }
    }
}
