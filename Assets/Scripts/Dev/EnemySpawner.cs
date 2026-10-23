using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace ScammerNoScamming
{
    // Sandbox helper: press the key to drop a scammer in front of the player, facing them.
    public class EnemySpawner : MonoBehaviour
    {
        public Key key = Key.F;
        public Key riderKey = Key.G;
        public float distance = 15f;
        // Set by the scene builder; null leaves the default material (fine for tests).
        public Material enemyMaterial;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard[key].wasPressedThisFrame) Spawn();
            if (keyboard[riderKey].wasPressedThisFrame) Spawn(mounted: true);
        }

        public ShooterScammer Spawn(bool mounted = false)
        {
            var player = FindFirstObjectByType<PlayerCharacter>();
            if (player == null) return null;

            var camera = player.GetComponentInChildren<Camera>();
            Vector3 forward = camera != null ? camera.transform.forward : player.transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;

            Vector3 position = player.transform.position + forward * distance;
            position.y = 0f;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas)) position = hit.position;

            Func<Color, Material> material = enemyMaterial != null ? (Func<Color, Material>)(_ => enemyMaterial) : null;
            ShooterScammer enemy = mounted ? PlayerSandbox.CreateRider(position, material) : PlayerSandbox.CreateEnemy(position, material);
            enemy.transform.rotation = Quaternion.LookRotation(-forward);
            return enemy;
        }

        void OnGUI()
        {
            GUI.Label(new Rect(12f, Screen.height - 30f, 500f, 24f), "[" + key + "] Spawn scammer   [" + riderKey + "] Spawn mounted scammer");
        }
    }
}
