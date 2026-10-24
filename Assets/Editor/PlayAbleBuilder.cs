using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ScammerNoScamming;

// Puts the working gameplay (player, ridable horse, revolver, enemies on F/G, death replay, corrupt police raid)
// into Assets/Scenes/PlayAble.unity, which is the HorseCity map. Same pieces the sandbox and Frontier builders use.
// Safe to rerun: it removes what it added last time.
public static class PlayAbleBuilder
{
    const string ScenePath = "Assets/Scenes/PlayAble.unity";
    static readonly string[] Added = { "Player", "Horse", "EnemySpawner", "ActionReplay", "Corrupt police raid", "PlayAbleNavMesh" };

    [MenuItem("Tools/Scammer No Scamming/Add Gameplay To PlayAble Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
            if (System.Array.IndexOf(Added, root.name) >= 0 || (root.name == "Main Camera" && root.GetComponent<Camera>() != null))
                Object.DestroyImmediate(root);

        var spawn = SpawnPoint();
        var player = PlayerSandbox.CreatePlayer();
        player.transform.SetPositionAndRotation(spawn, Quaternion.identity);
        var camera = player.GetComponentInChildren<Camera>();
        camera.farClipPlane = 900f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.allowHDR = true;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true; // the map ships a dusk volume
        player.GetComponent<PlayerWeaponController>().tracerMaterial =
            PlayerSandboxBuilder.GetMaterial("Tracer", new Color(1f, .9f, .4f), "Universal Render Pipeline/Unlit");

        var horse = PlayerSandbox.CreateHorse(CaseFlowSceneBuilder.Flat);
        horse.transform.SetPositionAndRotation(spawn + Vector3.right * 3f, Quaternion.identity);
        horse.AddComponent<HorseGenes>();
        WorldSceneArt.Horse(horse);

        var spawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner>();
        spawner.gameObject.AddComponent<PlayerHurtKeys>();
        spawner.enemyMaterial = CaseFlowSceneBuilder.Flat(new Color(.75f, .2f, .2f));
        new GameObject("ActionReplay").AddComponent<ActionReplay>();

        var raid = new GameObject("Corrupt police raid");
        var police = raid.AddComponent<AmbushGroup>();
        police.kind = AmbushKind.Riders;
        police.count = 4;
        police.minRadius = 35f;
        police.maxRadius = 45f;
        police.material = CaseFlowSceneBuilder.Flat(new Color(.15f, .2f, .45f));
        raid.AddComponent<CorruptPoliceRaid>();

        // Enemies chase over a NavMesh baked when play starts, after the whole map exists.
        var nav = new GameObject("PlayAbleNavMesh");
        nav.AddComponent<NavMeshSurface>().collectObjects = CollectObjects.All;
        nav.AddComponent<RuntimeNavMesh>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        CaseFlowSceneBuilder.RegisterFirst(ScenePath);
        Debug.Log("Gameplay added to " + ScenePath + ", player at " + spawn);
    }

    // Nearest open street-level spot to the plaza pad: a downward ray from above would land on a rooftop.
    static Vector3 SpawnPoint()
    {
        var pad = GameObject.Find("Plaza_Pad");
        var centre = pad != null ? pad.transform.position : Vector3.zero;
        Physics.SyncTransforms();
        for (float r = 0f; r < 300f; r += 4f)
            for (int i = 0; i < Mathf.Max(1, (int)(r / 2)); i++)
            {
                float a = i * Mathf.PI * 2f / Mathf.Max(1, (int)(r / 2));
                var from = centre + new Vector3(Mathf.Cos(a) * r, 5f, Mathf.Sin(a) * r);
                if (Physics.Raycast(from, Vector3.down, out var hit, 20f) && hit.normal.y > .95f
                    && !Physics.CheckCapsule(hit.point + Vector3.up * .5f, hit.point + Vector3.up * 2f, 3f, ~0, QueryTriggerInteraction.Ignore))
                    return hit.point + Vector3.up * .05f;
            }
        Debug.LogWarning("PlayAble: no open ground found near the plaza; spawning at its origin");
        return centre + Vector3.up * .05f;
    }
}
