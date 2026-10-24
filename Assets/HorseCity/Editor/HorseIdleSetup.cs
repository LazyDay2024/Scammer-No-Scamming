using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HorseCity.EditorTools
{
    /// <summary>
    /// Builds a small idle Animator Controller from the LPHorse clips and gives it to every
    /// LPHorse in the open scene, plus the HorseIdle component for random offsets and fidgets.
    /// </summary>
    public static class HorseIdleSetup
    {
        const string CtrlPath = "Assets/Horse/Animation/HorseIdle.controller";
        const string Anim = "Assets/LPHorse_Version_2_9/Version_2_9/Animations/";

        static readonly (string state, string clip)[] Fidgets =
        {
            ("TailWag",   "Additive_Animations/Tail/Rig_Tail_Wag.anim"),
            ("TailFlick", "Additive_Animations/Tail/Rig_Tail_Flick_LeftCenter.anim"),
            ("HeadNod",   "Additive_Animations/Head/Secondary/Rig_Head_Nod_Light.anim"),
            ("Snort",     "Additive_Animations/Head/Secondary/Rig_Head_SnortLeft.anim"),
            ("HeadShake", "Additive_Animations/Head/Secondary/Rig_Head_Shake_Light.anim"),
        };

        [MenuItem("HORSE/Give Horses Idle Animation")]
        static void Setup()
        {
            var ctrl = BuildController();
            int n = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!t.name.StartsWith("LPHorse")) continue;
                if (t.parent && t.parent.name.StartsWith("LPHorse")) continue; // only horse roots

                // the Animator must sit on the object whose child is "Rig" (clip paths start at Rig/...)
                var rig = FindDeep(t, "Rig");
                var host = rig ? rig.parent.gameObject : t.gameObject;
                var anim = host.GetComponent<Animator>();
                if (!anim) anim = Undo.AddComponent<Animator>(host);
                Undo.RecordObject(anim, "Horse idle");
                anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                if (!host.GetComponent<HorseCity.HorseIdle>()) Undo.AddComponent<HorseCity.HorseIdle>(host);
                n++;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"HorseIdleSetup: gave {n} horse(s) the idle controller ({CtrlPath}).");
        }

        static AnimatorController BuildController()
        {
            System.IO.Directory.CreateDirectory("Assets/Horse/Animation");
            AssetDatabase.DeleteAsset(CtrlPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CtrlPath);

            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "Primary_Cycles/Rig_Idle_Center.anim");
            var sm = ctrl.layers[0].stateMachine;
            var idle = sm.AddState("Idle");
            idle.motion = idleClip;
            sm.defaultState = idle;

            ctrl.AddLayer("Fidget");
            var layers = ctrl.layers;
            layers[1].blendingMode = AnimatorLayerBlendingMode.Additive;
            layers[1].defaultWeight = 1f;
            ctrl.layers = layers;

            var fsm = ctrl.layers[1].stateMachine;
            var empty = fsm.AddState("Empty");
            fsm.defaultState = empty;
            foreach (var (state, clip) in Fidgets)
            {
                var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + clip);
                if (!c) { Debug.LogWarning("HorseIdleSetup: missing clip " + clip); continue; }
                var s = fsm.AddState(state);
                s.motion = c;
                var tr = s.AddTransition(empty);
                tr.hasExitTime = true; tr.exitTime = 0.95f; tr.duration = 0.3f;
            }
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        static Transform FindDeep(Transform t, string name)
        {
            foreach (Transform c in t)
            {
                if (c.name == name) return c;
                var r = FindDeep(c, name);
                if (r) return r;
            }
            return null;
        }
    }
}
