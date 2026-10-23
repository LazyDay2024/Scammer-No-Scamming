using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds the Windows player and zips the whole folder (exe + _Data + dlls) for itch.io.
// Menu: Tools > Scammer No Scamming > Build Windows (zip)
// CLI:  Unity.exe -batchmode -quit -projectPath <p> -executeMethod WindowsBuild.Build [-scenes A.unity,B.unity]
public static class WindowsBuild
{
    const string ExeName = "ScammerNoScamming";
    const string OutputFolder = "Builds/Windows";
    // Used when no -scenes argument is given and Build Settings only has Unity's empty sample scene.
    const string FallbackScene = "Assets/Scenes/PlayerSandbox.unity";

    [MenuItem("Tools/Scammer No Scamming/Build Windows (zip)")]
    public static void Build()
    {
        string[] scenes = ScenesToBuild();
        foreach (var scene in scenes)
            if (!File.Exists(scene)) Fail("Scene not found: " + scene);

        if (Directory.Exists(OutputFolder)) Directory.Delete(OutputFolder, true);
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputFolder + "/" + ExeName + ".exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            Fail("Build " + report.summary.result + " with " + report.summary.totalErrors + " errors");

        // Burst debug symbols are for us, not players; Unity marks the folder "DoNotShip".
        foreach (var folder in Directory.GetDirectories(OutputFolder, "*_DoNotShip"))
            Directory.Delete(folder, true);

        string zip = "Builds/" + ExeName + "-Windows-" + PlayerSettings.bundleVersion + ".zip";
        if (File.Exists(zip)) File.Delete(zip);
        ZipFile.CreateFromDirectory(OutputFolder, zip);
        Debug.Log("Windows build OK: " + string.Join(", ", scenes) + " -> " + Path.GetFullPath(zip));
    }

    static string[] ScenesToBuild()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-scenes");
        if (index >= 0 && index + 1 < args.Length)
            return args[index + 1].Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

        string[] listed = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)
            .Where(p => !p.EndsWith("SampleScene.unity")).ToArray();
        return listed.Length > 0 ? listed : new[] { FallbackScene };
    }

    static void Fail(string message)
    {
        // In batchmode an exception makes Unity exit non-zero, so CI and scripts see the failure.
        throw new Exception("WindowsBuild: " + message);
    }
}
