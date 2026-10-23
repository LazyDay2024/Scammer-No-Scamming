using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ScammerNoScamming;

// Builds the playable case flow: CaseBoard -> Raid -> Debrief scenes, sample case assets, and the
// Build Settings scene list (CaseBoard first, so a build starts there).
// Case assets that already exist are left alone, so edited case text survives a rebuild.
public static class CaseFlowBuilder
{
    const string CaseFolder = "Assets/Resources/" + GameManager.CaseFolder;

    [MenuItem("Tools/Scammer No Scamming/Build Case Flow Scenes")]
    public static void Build()
    {
        var cases = SampleCases();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        UiCamera();
        new GameObject("Case board").AddComponent<CaseBoardPlaceholder>();
        EditorSceneManager.SaveScene(scene, GameManager.CaseBoardScene);

        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var stage = RaidSandbox.Create(PlayerSandboxBuilder.MaterialFor);
        stage.fallbackCase = cases[0];
        stage.scammerMaterial = PlayerSandboxBuilder.MaterialFor(new Color(0.75f, 0.2f, 0.2f));
        stage.workerMaterial = PlayerSandboxBuilder.MaterialFor(new Color(0.35f, 0.65f, 0.4f));
        stage.victimMaterial = PlayerSandboxBuilder.MaterialFor(new Color(0.85f, 0.75f, 0.3f));
        Object.FindFirstObjectByType<PlayerWeaponController>().tracerMaterial =
            PlayerSandboxBuilder.GetMaterial("Tracer", new Color(1f, 0.9f, 0.4f), "Universal Render Pipeline/Unlit");
        Object.FindFirstObjectByType<EnemySpawner>().enemyMaterial = stage.scammerMaterial;
        EditorSceneManager.SaveScene(scene, GameManager.RaidScene);

        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        UiCamera();
        new GameObject("Debrief").AddComponent<DebriefPlaceholder>();
        EditorSceneManager.SaveScene(scene, GameManager.DebriefScene);

        EditorBuildSettings.scenes = new[] { GameManager.CaseBoardScene, GameManager.RaidScene, GameManager.DebriefScene }
            .Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Case flow scenes saved and set as the Build Settings scene list");
    }

    static void UiCamera()
    {
        var camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.12f, 0.16f);
    }

    // Placeholder content until C writes the real cases; the scam types are the ones proposed so far.
    static CaseDefinition[] SampleCases()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(CaseFolder)) AssetDatabase.CreateFolder("Assets/Resources", GameManager.CaseFolder);

        return new[]
        {
            Case("Case_FakePolice", "[placeholder] The fake police call", "Fake police", 0, 3, 0, 1, 1,
                "[placeholder] A caller said my account was used for money laundering and told me to move my savings to a 'safe account'.",
                new[] { "Caller ID showed a police number", "Asked for an urgent transfer", "Told the victim to keep it secret" },
                "[placeholder] Real police never ask you to transfer money. Hang up and call the official hotline."),
            Case("Case_OnlineJob", "[placeholder] The easy online job", "Online job / investment", 1, 3, 1, 2, 1,
                "[placeholder] I got paid for small tasks at first, then had to deposit more and more to 'unlock' my earnings.",
                new[] { "Small payouts at the start", "Deposits needed to withdraw", "Pressure to recruit friends" },
                "[placeholder] If you must pay to get paid, it is a scam. Stop depositing and report it."),
            Case("Case_SmsLink", "[placeholder] The parcel SMS link", "SMS fake link", 2, 2, 1, 1, 2,
                "[placeholder] An SMS said my parcel was stuck. I opened the link, entered my bank login, and my account was emptied.",
                new[] { "Odd sender and misspelled web address", "Asked for a bank login", "Urgent deadline" },
                "[placeholder] Do not open links in unexpected messages. Type the official address yourself."),
            Case("Case_Boss", "[placeholder] Jao Sua Cloud's data center", "Boss", 1, 4, 2, 2, 1,
                "[placeholder] All the trails lead to one data center that is draining the country's fuel and power.",
                new[] { "Every gang sends money to the same place" },
                "[placeholder] Scam networks are organised businesses. Reporting every case helps take them down.",
                isBoss: true)
        };
    }

    static CaseDefinition Case(string file, string title, string type, int hideout, int scammers, int riders, int workers, int victims,
        string story, string[] clues, string lesson, bool isBoss = false)
    {
        string path = CaseFolder + "/" + file + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<CaseDefinition>(path);
        if (existing != null) return existing;

        var c = ScriptableObject.CreateInstance<CaseDefinition>();
        c.title = title;
        c.scamType = type;
        c.isBoss = isBoss;
        c.hideoutIndex = hideout;
        c.scammers = scammers;
        c.riders = riders;
        c.forcedWorkers = workers;
        c.victims = victims;
        c.victimStory = story;
        c.clues = clues;
        c.lesson = lesson;
        AssetDatabase.CreateAsset(c, path);
        return c;
    }
}
