using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Sits in the action scene and fills the current case's hideout with scammers, riders, forced
    // workers and victims. Uses GameManager's case, or fallbackCase when the scene is played on its own.
    public class CaseStage : MonoBehaviour
    {
        public static CaseStage Active { get; private set; }

        public CaseDefinition fallbackCase;
        // Placeholder body materials; null keeps the default.
        public Material scammerMaterial;
        public Material workerMaterial;
        public Material victimMaterial;

        public readonly List<Enemy> Scammers = new List<Enemy>();
        public readonly List<Civilian> Civilians = new List<Civilian>();

        public CaseDefinition Case { get; private set; }
        public Hideout Hideout { get; private set; }

        void Awake()
        {
            Active = this;
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        // After every Awake, so the runtime NavMesh is already baked when the enemies arrive.
        void Start()
        {
            Populate(GameManager.Current != null ? GameManager.Current.CurrentCase : fallbackCase);
        }

        public void Populate(CaseDefinition definition)
        {
            if (definition == null || Case != null) return;

            foreach (var hideout in FindObjectsByType<Hideout>(FindObjectsSortMode.None))
                if (hideout.index == definition.hideoutIndex) Hideout = hideout;
            if (Hideout == null)
            {
                Debug.LogError("CaseStage: no Hideout with index " + definition.hideoutIndex, this);
                return;
            }
            Case = definition;

            int spot = 0;
            for (int i = 0; i < definition.scammers; i++)
                Scammers.Add(PlayerSandbox.CreateEnemy(NextSpot(Hideout.spawnPoints, spot++), Paint(scammerMaterial)));
            for (int i = 0; i < definition.riders; i++)
                Scammers.Add(PlayerSandbox.CreateRider(NextSpot(Hideout.spawnPoints, spot++), Paint(scammerMaterial)));
            for (int i = 0; i < definition.forcedWorkers; i++)
                Civilians.Add(PlayerSandbox.CreateForcedWorker(NextSpot(Hideout.spawnPoints, spot++), Paint(workerMaterial)));
            for (int i = 0; i < definition.victims; i++)
                Civilians.Add(PlayerSandbox.CreateVictim(NextSpot(Hideout.hidingCorners, i), Paint(victimMaterial)));

            foreach (var scammer in Scammers) scammer.patrolRadius = 3f;
        }

        // Cycles through the points, stepping sideways on each lap so nobody spawns inside someone else.
        static Vector3 NextSpot(Transform[] points, int n)
        {
            if (points == null || points.Length == 0) return Vector3.zero;
            Transform point = points[n % points.Length];
            return point.position + point.right * (1.2f * (n / points.Length));
        }

        static Func<Color, Material> Paint(Material material)
        {
            return material != null ? (Func<Color, Material>)(_ => material) : null;
        }
    }
}
