using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

// Lets the AI team drive the Unity Editor the owner has open, so every step happens in front of them
// instead of in a hidden batch process. Write commands (one per line) to D:/AI/runtime/editor-bridge/cmd.txt:
//   refresh | menu <Menu/Path> | exec <Type.Method> | open <scene path> | play | stop
//   shot <png>  (Game view, play mode)   sceneshot <png>  (Scene view)   look x y z yaw pitch distance
//   select <GameObject name>
// Results and errors are appended to result.txt / log.txt in the same folder.
[InitializeOnLoad]
public static class EditorBridge
{
    const string Folder = "D:/AI/runtime/editor-bridge";
    static double next;

    static EditorBridge()
    {
        if (Application.isBatchMode) return;
        Directory.CreateDirectory(Folder);
        EditorApplication.update += Poll;
        Application.logMessageReceived += Log;
        CompilationPipeline.assemblyCompilationFinished += (assembly, messages) =>
        {
            foreach (var m in messages.Where(m => m.type == CompilerMessageType.Error))
                Append("log.txt", "COMPILE ERROR " + m.message);
        };
        Append("result.txt", "ready (domain loaded)");
    }

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + .4;
        string path = Path.Combine(Folder, "cmd.txt");
        if (!File.Exists(path)) return;
        string[] lines;
        try { lines = File.ReadAllLines(path); File.Delete(path); }
        catch (IOException) { return; } // still being written
        foreach (string line in lines)
            if (!string.IsNullOrWhiteSpace(line)) Run(line.Trim());
    }

    static void Run(string line)
    {
        try
        {
            int space = line.IndexOf(' ');
            string verb = space < 0 ? line : line.Substring(0, space);
            string arg = space < 0 ? "" : line.Substring(space + 1).Trim();
            switch (verb)
            {
                case "refresh": AssetDatabase.Refresh(); break;
                case "menu": if (!EditorApplication.ExecuteMenuItem(arg)) throw new ArgumentException("no menu item " + arg); break;
                case "exec": Exec(arg); break;
                case "open": EditorSceneManager.OpenScene(arg); break;
                case "play": EditorApplication.isPlaying = true; break;
                case "stop": EditorApplication.isPlaying = false; break;
                case "shot": Shot(Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>(), arg); break;
                case "sceneshot": Shot(SceneView.lastActiveSceneView?.camera, arg); break;
                case "look": Look(arg); break;
                case "tp": Teleport(arg); break;
                case "invoke": // invoke <ComponentType> <method>: calls a no-argument method on the first instance in the scene
                    var ip = arg.Split(' ');
                    var componentType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ScammerNoScamming." + ip[0]) ?? a.GetType(ip[0]))
                        .FirstOrDefault(t => t != null) ?? throw new ArgumentException("no type " + ip[0]);
                    var instance = UnityEngine.Object.FindAnyObjectByType(componentType) ?? throw new ArgumentException("no instance of " + ip[0]);
                    var callee = componentType.GetMethod(ip[1], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        ?? throw new ArgumentException("no method " + ip[1]);
                    callee.Invoke(instance, null);
                    break;
                case "inspectall": // every renderer under the object: name, shader, colour, main texture
                    var rootObject = GameObject.Find(arg) ?? throw new ArgumentException("not found " + arg);
                    foreach (var rr in rootObject.GetComponentsInChildren<Renderer>(true))
                        Append("result.txt", "  " + rr.name + " on=" + rr.enabled + " " + string.Join(" | ", rr.sharedMaterials.Select(m => m == null ? "null" :
                            $"{m.name}:{m.shader.name} base={(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "-")} tex={(m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "none")}")));
                    break;
                case "inspect":
                    var found = GameObject.Find(arg) ?? throw new ArgumentException("not found " + arg);
                    var r = found.GetComponent<Renderer>();
                    Append("result.txt", $"{found.name} pos={found.transform.position} scale={found.transform.lossyScale} active={found.activeInHierarchy} " +
                        (r == null ? "no renderer" : $"enabled={r.enabled} bounds={r.bounds} probes={r.lightProbeUsage} " +
                        string.Join(" | ", r.sharedMaterials.Select(m => m == null ? "null" : $"{m.name}:{m.shader.name} base={(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "-")} kw={string.Join(",", m.shaderKeywords)}"))));
                    break;
                case "sun": // sun <intensity> <shadows 0|1>
                    var sp = arg.Split(' ');
                    RenderSettings.sun.intensity = float.Parse(sp[0]);
                    RenderSettings.sun.shadows = sp.Length > 1 && sp[1] == "0" ? LightShadows.None : LightShadows.Soft;
                    break;
                case "diag":
                    var sh = RenderSettings.ambientProbe;
                    Append("result.txt", $"ambientMode={RenderSettings.ambientMode} probeDC=({sh[0, 0]:0.00},{sh[1, 0]:0.00},{sh[2, 0]:0.00}) " +
                        $"intensity={RenderSettings.ambientIntensity} skybox={RenderSettings.skybox?.name} sun={RenderSettings.sun?.name} " +
                        $"atmos={UnityEngine.Object.FindAnyObjectByType<ScammerNoScamming.FrontierAtmosphere>() != null} lights={UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length}");
                    break;
                case "closeui":
                    foreach (var screen in UnityEngine.Object.FindObjectsByType<ScammerNoScamming.GameScreen>(FindObjectsSortMode.None)) screen.Close();
                    Time.timeScale = 1f;
                    break;
                case "select":
                    var go = GameObject.Find(arg);
                    if (go == null) throw new ArgumentException("not found " + arg);
                    Selection.activeGameObject = go;
                    SceneView.lastActiveSceneView?.FrameSelected();
                    break;
                default: throw new ArgumentException("unknown command");
            }
            Append("result.txt", "ok " + line + (EditorUtility.scriptCompilationFailed ? "  [scripts have compile errors]" : ""));
        }
        catch (Exception e)
        {
            Append("result.txt", "ERR " + line + " :: " + (e.InnerException ?? e).Message);
        }
    }

    static void Exec(string target)
    {
        int dot = target.LastIndexOf('.');
        string typeName = target.Substring(0, dot), method = target.Substring(dot + 1);
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(typeName) ?? a.GetType("ScammerNoScamming." + typeName))
            .FirstOrDefault(t => t != null) ?? throw new ArgumentException("no type " + typeName);
        var info = type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new ArgumentException("no static method " + method);
        info.Invoke(null, null);
    }

    static void Look(string arg)
    {
        var v = arg.Split(' ').Select(float.Parse).ToArray();
        var view = SceneView.lastActiveSceneView ?? throw new InvalidOperationException("no Scene view open");
        view.LookAt(new Vector3(v[0], v[1], v[2]), Quaternion.Euler(v[4], v[3], 0f), v.Length > 5 ? v[5] : 10f);
        view.Repaint();
    }

    // Play mode: tp x y z yaw pitch  moves the player (on foot) and aims the camera.
    static void Teleport(string arg)
    {
        var v = arg.Split(' ').Select(float.Parse).ToArray();
        var player = ScammerNoScamming.PlayerLocator.Player ?? throw new InvalidOperationException("no player (enter play mode)");
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.SetPositionAndRotation(new Vector3(v[0], v[1], v[2]), Quaternion.Euler(0f, v[3], 0f));
        if (cc != null) cc.enabled = true;
        // PlayerController keeps its own yaw/pitch and re-applies them every frame.
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(ScammerNoScamming.PlayerController).GetField("yaw", flags)?.SetValue(player, v[3]);
        if (v.Length > 4) typeof(ScammerNoScamming.PlayerController).GetField("pitch", flags)?.SetValue(player, v[4]);
    }

    // Renders the camera itself, so it works even when the editor window is not focused (no IMGUI overlay).
    static void Shot(Camera camera, string file)
    {
        if (camera == null) throw new InvalidOperationException("no camera");
        int w = 1600, h = 900;
        var target = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        var previous = camera.targetTexture;
        camera.targetTexture = target;
        camera.Render();
        camera.targetTexture = previous;
        RenderTexture.active = target;
        var image = new Texture2D(w, h, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        RenderTexture.active = null;
        File.WriteAllBytes(file, image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
    }

    static void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            Append("log.txt", type + " " + message + "\n" + stack);
    }

    static void Append(string file, string text)
    {
        try { File.AppendAllText(Path.Combine(Folder, file), DateTime.Now.ToString("HH:mm:ss ") + text + "\n"); }
        catch (IOException) { }
    }
}
