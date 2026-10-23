using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerNoScamming
{
    // Builds the player test world from code. The editor menu saves it as a scene and
    // the PlayMode tests build it fresh, so both exercise the same setup.
    public static class PlayerSandbox
    {
        public class Result
        {
            public GameObject Player;
            public GameObject Horse;
            public List<Health> Targets = new List<Health>();
            public EnemySpawner Spawner;
            public ActionReplay Replay;
        }

        // materialFor lets the editor builder supply saved material assets; null keeps the default.
        public static Result Create(Func<Color, Material> materialFor = null)
        {
            var result = new Result();

            CreateGroundAndLight(materialFor).AddComponent<RuntimeNavMesh>();

            result.Player = CreatePlayer();
            result.Horse = CreateHorse(materialFor);

            var hurdle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hurdle.name = "Hurdle";
            hurdle.transform.position = new Vector3(0f, 0.4f, 30f);
            hurdle.transform.localScale = new Vector3(6f, 0.8f, 0.4f);
            Tint(hurdle, new Color(0.45f, 0.3f, 0.2f), materialFor);

            for (int i = 0; i < 3; i++)
                result.Targets.Add(CreateTarget(new Vector3((i - 1) * 4f, 0.9f, 12f + i * 6f), materialFor));

            result.Spawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner>();
            result.Replay = new GameObject("ActionReplay").AddComponent<ActionReplay>();
            return result;
        }

        // A 200 m square of grass under a sun. Add RuntimeNavMesh to it once everything that should block walking exists.
        public static GameObject CreateGroundAndLight(Func<Color, Material> materialFor = null)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            Tint(ground, new Color(0.45f, 0.55f, 0.35f), materialFor);
            return ground;
        }

        public static GameObject CreatePlayer()
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.05f, 0f);

            var characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.SetParent(pivot.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 70f;
            camera.nearClipPlane = 0.05f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
            cameraObject.AddComponent<AudioListener>();

            player.AddComponent<PlayerCharacter>();
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerMount>();
            player.AddComponent<PlayerWeaponController>();
            player.AddComponent<PlayerHud>();
            return player;
        }

        public static GameObject CreateHorse(Func<Color, Material> materialFor)
        {
            var horse = new GameObject("Horse");
            horse.transform.position = new Vector3(2f, 0.05f, 1f);

            var characterController = horse.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.6f;
            characterController.center = new Vector3(0f, 0.9f, 0f);

            var seat = new GameObject("Seat");
            seat.transform.SetParent(horse.transform, false);
            seat.transform.localPosition = new Vector3(0f, 1.45f, -0.2f);

            var coat = new Color(0.4f, 0.25f, 0.15f);
            AddPart(horse, "Body", new Vector3(0f, 1f, 0f), new Vector3(0.9f, 0.9f, 2.2f), coat, materialFor);
            AddPart(horse, "Neck", new Vector3(0f, 1.6f, 1f), new Vector3(0.35f, 0.9f, 0.4f), coat, materialFor);
            AddPart(horse, "Head", new Vector3(0f, 1.95f, 1.45f), new Vector3(0.35f, 0.4f, 0.8f), coat, materialFor);

            horse.AddComponent<Health>().SetMax(10f); // placeholder; must exist before HorseController reads it
            horse.AddComponent<HorseController>().SetSeat(seat.transform);
            return horse;
        }

        // A standing scammer built from the same hit zones as the target dummies, with its feet at the root.
        public static ShooterScammer CreateEnemy(Vector3 position, Func<Color, Material> materialFor = null)
        {
            return BuildFootBody("Scammer", position, new Color(0.75f, 0.2f, 0.2f), materialFor).AddComponent<ShooterScammer>();
        }

        // People the player must not shoot: victims (yellow) and forced workers (green).
        public static Victim CreateVictim(Vector3 position, Func<Color, Material> materialFor = null)
        {
            return BuildFootBody("Victim", position, new Color(0.85f, 0.75f, 0.3f), materialFor).AddComponent<Victim>();
        }

        public static ForcedWorker CreateForcedWorker(Vector3 position, Func<Color, Material> materialFor = null)
        {
            return BuildFootBody("Forced worker", position, new Color(0.35f, 0.65f, 0.4f), materialFor).AddComponent<ForcedWorker>();
        }

        static GameObject BuildFootBody(string name, Vector3 position, Color skin, Func<Color, Material> materialFor)
        {
            var body = new GameObject(name);
            body.transform.position = position;
            var health = body.AddComponent<Health>();
            health.SetMax(3f);

            AddZone(body, health, "Body", HitZoneType.Body, new Vector3(0f, 0.7f, 0f), new Vector3(0.8f, 1.4f, 0.8f), skin, materialFor);
            AddZone(body, health, "Head", HitZoneType.Head, new Vector3(0f, 1.65f, 0f), new Vector3(0.45f, 0.45f, 0.45f), skin, materialFor);
            return body;
        }

        // A scammer on a horse: the horse and the rider each carry their own Health and hit zones.
        public static RiderScammer CreateRider(Vector3 position, Func<Color, Material> materialFor = null)
        {
            var root = new GameObject("MountedScammer");
            root.transform.position = position;
            var riderHealth = root.AddComponent<Health>();
            riderHealth.SetMax(3f);

            var horse = new GameObject(RiderScammer.HorseName);
            horse.transform.SetParent(root.transform, false);
            var horseHealth = horse.AddComponent<Health>();
            var coat = new Color(0.4f, 0.25f, 0.15f);
            AddZone(horse, horseHealth, "Body", HitZoneType.Body, new Vector3(0f, 1f, 0f), new Vector3(0.9f, 0.9f, 2.2f), coat, materialFor);
            AddZone(horse, horseHealth, "Head", HitZoneType.Head, new Vector3(0f, 1.95f, 1.45f), new Vector3(0.35f, 0.4f, 0.8f), coat, materialFor);

            var rider = new GameObject(RiderScammer.RiderName);
            rider.transform.SetParent(root.transform, false);
            rider.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            var skin = new Color(0.75f, 0.2f, 0.2f);
            AddZone(rider, riderHealth, "Body", HitZoneType.Body, new Vector3(0f, 0.7f, 0f), new Vector3(0.8f, 1.4f, 0.8f), skin, materialFor);
            AddZone(rider, riderHealth, "Head", HitZoneType.Head, new Vector3(0f, 1.65f, 0f), new Vector3(0.45f, 0.45f, 0.45f), skin, materialFor);

            return root.AddComponent<RiderScammer>();
        }

        static Health CreateTarget(Vector3 position, Func<Color, Material> materialFor)
        {
            var target = new GameObject("Target");
            target.transform.position = position;
            var health = target.AddComponent<Health>();

            var skin = new Color(0.75f, 0.2f, 0.2f);
            AddZone(target, health, "Body", HitZoneType.Body, Vector3.zero, new Vector3(0.8f, 1.4f, 0.8f), skin, materialFor);
            AddZone(target, health, "Head", HitZoneType.Head, new Vector3(0f, 0.95f, 0f), new Vector3(0.45f, 0.45f, 0.45f), skin, materialFor);
            return health;
        }

        static void AddZone(GameObject parent, Health health, string name, HitZoneType type, Vector3 localPosition, Vector3 scale, Color color, Func<Color, Material> materialFor)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.AddComponent<HitZone>().Configure(type, HitZone.DefaultMultiplier(type), health);
            Tint(part, color, materialFor);
        }

        static void AddPart(GameObject parent, string name, Vector3 localPosition, Vector3 scale, Color color, Func<Color, Material> materialFor)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            RemoveCollider(part);
            Tint(part, color, materialFor);
        }

        static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(collider);
            else UnityEngine.Object.DestroyImmediate(collider);
        }

        static void Tint(GameObject target, Color color, Func<Color, Material> materialFor)
        {
            if (materialFor != null)
                target.GetComponent<Renderer>().sharedMaterial = materialFor(color);
        }
    }
}
