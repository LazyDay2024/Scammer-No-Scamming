using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Windows build for itch.io (download version). Menu or batchmode:
//   Unity.exe -batchmode -quit -projectPath . -executeMethod WindowsBuild.Build
// Output: Builds/Windows/ScammerNoScamming.exe (zip the whole Windows folder, including _Data).
public static class WindowsBuild
{
    public const string Output = "Builds/Windows/ScammerNoScamming.exe";

    [MenuItem("Tools/Scammer No Scamming/Build Windows")]
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });
        var summary = report.summary;
        Debug.Log($"Windows build {summary.result}: {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, scenes: {string.Join(", ", scenes)}");
        if (Application.isBatchMode && summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
