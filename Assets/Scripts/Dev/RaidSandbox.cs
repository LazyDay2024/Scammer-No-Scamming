using System;
using UnityEngine;

namespace ScammerNoScamming
{
    // Placeholder action map until the real city is redone: a plane with the police station in the
    // middle and three scammer hideouts in the corners, plus the trap course on the way out.
    // The editor builder saves it as the Raid scene; CaseStage fills the current case's hideout.
    public static class RaidSandbox
    {
        public static readonly (string label, Vector3 center)[] Corners =
        {
            ("North-West", new Vector3(-70f, 0f, 70f)),
            ("North-East", new Vector3(70f, 0f, 70f)),
            ("South-East", new Vector3(70f, 0f, -70f))
        };

        public static CaseStage Create(Func<Color, Material> materialFor = null)
        {
            var ground = PlayerSandbox.CreateGroundAndLight(materialFor);

            PlayerSandbox.Prop("Station", new Vector3(0f, 0.1f, -6f), new Vector3(8f, 0.2f, 6f), new Color(0.3f, 0.35f, 0.55f), materialFor);
            PlayerSandbox.Prop("Supply box (station)", new Vector3(-3f, 0.5f, -6f), Vector3.one, new Color(0.25f, 0.45f, 0.25f), materialFor).AddComponent<SupplyBox>();

            for (int i = 0; i < Corners.Length; i++)
                CreateHideout(i, Corners[i].label, Corners[i].center, materialFor);

            PlayerSandbox.CreatePlayer();
            PlayerSandbox.CreateHorse(materialFor);
            PlayerSandbox.AddTrapCourse(materialFor);
            new GameObject("ActionReplay").AddComponent<ActionReplay>();
            new GameObject("EnemySpawner").AddComponent<EnemySpawner>();
            var stage = new GameObject("CaseStage").AddComponent<CaseStage>();

            // Last, so the bake sees the hideout walls and the trap props.
            ground.AddComponent<RuntimeNavMesh>();
            return stage;
        }

        // Three walls with the open side facing the station, six standing spots and two back corners.
        public static Hideout CreateHideout(int index, string label, Vector3 center, Func<Color, Material> materialFor = null)
        {
            var root = new GameObject("Hideout " + index + " (" + label + ")");
            Vector3 toStation = -center;
            toStation.y = 0f;
            root.transform.SetPositionAndRotation(center, Quaternion.LookRotation(toStation.normalized));

            var wall = new Color(0.4f, 0.3f, 0.3f);
            Wall(root, new Vector3(0f, 1.5f, -6f), new Vector3(12f, 3f, 0.5f), wall, materialFor);
            Wall(root, new Vector3(-6f, 1.5f, 0f), new Vector3(0.5f, 3f, 12f), wall, materialFor);
            Wall(root, new Vector3(6f, 1.5f, 0f), new Vector3(0.5f, 3f, 12f), wall, materialFor);

            var hideout = root.AddComponent<Hideout>();
            hideout.index = index;
            hideout.label = label;
            hideout.spawnPoints = new[]
            {
                Point(root, "Spot", new Vector3(-3f, 0f, -2f)), Point(root, "Spot", new Vector3(0f, 0f, -2f)), Point(root, "Spot", new Vector3(3f, 0f, -2f)),
                Point(root, "Spot", new Vector3(-3f, 0f, 2f)), Point(root, "Spot", new Vector3(0f, 0f, 2f)), Point(root, "Spot", new Vector3(3f, 0f, 2f))
            };
            hideout.hidingCorners = new[]
            {
                Point(root, "Corner", new Vector3(-4.8f, 0f, -4.8f)), Point(root, "Corner", new Vector3(4.8f, 0f, -4.8f))
            };
            return hideout;
        }

        static void Wall(GameObject root, Vector3 localPosition, Vector3 scale, Color color, Func<Color, Material> materialFor)
        {
            var wall = PlayerSandbox.Prop("Wall", Vector3.zero, scale, color, materialFor);
            wall.transform.SetParent(root.transform, false);
            wall.transform.localPosition = localPosition;
        }

        static Transform Point(GameObject root, string name, Vector3 localPosition)
        {
            var point = new GameObject(name).transform;
            point.SetParent(root.transform, false);
            point.localPosition = localPosition;
            return point;
        }
    }
}
