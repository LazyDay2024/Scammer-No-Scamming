using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ScammerNoScamming
{
    public enum AmbushKind
    {
        OnFoot,
        Riders
    }

    // Spawns the enemies for an ambush and tells them where the player is. Either at fixed points
    // (hiding spots, decoys) or in a ring around the player. Without a prefab it builds the sandbox
    // placeholder bodies, so a saved scene works with no setup.
    public class AmbushGroup : MonoBehaviour
    {
        public AmbushKind kind;
        public Enemy prefab;
        // Material for the placeholder bodies; null keeps the default.
        public Material material;
        public int count = 3;
        public float minRadius = 30f;
        public float maxRadius = 40f;
        // When set, one enemy appears at each of these instead of in a ring.
        public Transform[] points;

        public List<Enemy> Fire(PlayerCharacter target)
        {
            var spawned = new List<Enemy>();
            bool fixedPoints = points != null && points.Length > 0;
            int total = fixedPoints ? points.Length : count;
            float startAngle = UnityEngine.Random.value * 360f;
            for (int i = 0; i < total; i++)
            {
                Vector3 spot = fixedPoints ? points[i].position : RingPoint(target.transform.position, startAngle + i * 360f / total);
                if (NavMesh.SamplePosition(spot, out NavMeshHit hit, 10f, NavMesh.AllAreas)) spot = hit.position;

                Enemy enemy = Spawn(spot);
                Vector3 toPlayer = target.transform.position - spot;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f) enemy.transform.rotation = Quaternion.LookRotation(toPlayer);
                enemy.Alert(target.transform.position);
                spawned.Add(enemy);
            }
            return spawned;
        }

        Enemy Spawn(Vector3 spot)
        {
            if (prefab != null) return Instantiate(prefab, spot, Quaternion.identity);
            Func<Color, Material> paint = material != null ? (Func<Color, Material>)(_ => material) : null;
            return kind == AmbushKind.Riders ? PlayerSandbox.CreateRider(spot, paint) : PlayerSandbox.CreateEnemy(spot, paint);
        }

        Vector3 RingPoint(Vector3 center, float angleDegrees)
        {
            float radius = UnityEngine.Random.Range(minRadius, maxRadius);
            return center + Quaternion.Euler(0f, angleDegrees, 0f) * Vector3.forward * radius;
        }
    }
}
