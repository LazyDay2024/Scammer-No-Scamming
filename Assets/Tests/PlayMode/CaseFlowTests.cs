using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class CaseFlowTests
    {
        readonly List<string> loaded = new List<string>();
        readonly List<Object> created = new List<Object>();
        System.Action<string> realLoader;

        [SetUp]
        public void SetUp()
        {
            realLoader = GameManager.LoadScene;
            GameManager.LoadScene = path => loaded.Add(path);
        }

        [TearDown]
        public void TearDown()
        {
            GameManager.LoadScene = realLoader;
            if (GameManager.Current != null) Object.Destroy(GameManager.Current.gameObject);
            foreach (var enemy in Object.FindObjectsByType<Npc>(FindObjectsSortMode.None))
                Object.Destroy(enemy.gameObject);
            foreach (var thing in created)
                if (thing != null) Object.Destroy(thing);
            created.Clear();
            loaded.Clear();
            TroubleMeter.ResetAll();
        }

        CaseDefinition MakeCase(string title, bool boss = false, int hideout = 0)
        {
            var c = ScriptableObject.CreateInstance<CaseDefinition>();
            c.title = title;
            c.isBoss = boss;
            c.hideoutIndex = hideout;
            created.Add(c);
            return c;
        }

        [UnityTest]
        public IEnumerator A_run_is_one_random_case_then_the_boss_then_the_ending()
        {
            var a = MakeCase("A");
            var b = MakeCase("B");
            var boss = MakeCase("Boss", boss: true);

            var game = GameManager.StartRun(new[] { a, boss, b });
            yield return null;
            Assert.That(game.CurrentCase, Is.EqualTo(a).Or.EqualTo(b), "the first case is never the boss");
            Assert.AreEqual(GameManager.Phase.Briefing, game.Stage);
            Assert.AreEqual(GameManager.CaseBoardScene, loaded[loaded.Count - 1]);

            game.HideoutRevealed = true;
            game.BeginAction();
            Assert.AreEqual(GameManager.Phase.Action, game.Stage);
            Assert.AreEqual(GameManager.RaidScene, loaded[loaded.Count - 1]);

            game.CompleteCase();
            Assert.AreEqual(GameManager.DebriefScene, loaded[loaded.Count - 1]);

            game.Continue();
            Assert.AreEqual(boss, game.CurrentCase);
            Assert.AreEqual(GameManager.Phase.Briefing, game.Stage);
            Assert.IsFalse(game.HideoutRevealed, "every case starts with its hideout unknown");

            game.BeginAction();
            game.CompleteCase();
            game.Continue();
            Assert.AreEqual(GameManager.Phase.Ending, game.Stage);
        }

        [UnityTest]
        public IEnumerator Every_non_boss_case_can_come_up()
        {
            var a = MakeCase("A");
            var b = MakeCase("B");
            var seen = new HashSet<CaseDefinition>();
            for (int i = 0; i < 40 && seen.Count < 2; i++)
                seen.Add(GameManager.StartRun(new[] { a, b }).CurrentCase);
            yield return null;
            Assert.AreEqual(2, seen.Count);
        }

        [UnityTest]
        public IEnumerator A_new_case_clears_trouble_but_a_death_replay_does_not_touch_the_case()
        {
            var game = GameManager.StartRun(new[] { MakeCase("A"), MakeCase("Boss", boss: true) });
            var first = game.CurrentCase;
            game.HideoutRevealed = true;
            TroubleMeter.Add(TroubleSource.WrongAnswer, 10f);
            game.BeginAction();
            TroubleMeter.Add(TroubleSource.Gunshot, 5f);
            yield return null;

            GameEvents.RaiseActionReplaying(); // what a death does
            Assert.AreSame(first, game.CurrentCase);
            Assert.IsTrue(game.HideoutRevealed, "analysis results survive a death");
            Assert.AreEqual(10f, TroubleMeter.Current, 0.001f);

            game.CompleteCase();
            game.Continue();
            Assert.AreEqual(0f, TroubleMeter.Current, 0.001f, "the boss case starts clean");
        }

        [UnityTest]
        public IEnumerator Case_stage_fills_only_the_cases_hideout()
        {
            var ground = PlayerSandbox.CreateGroundAndLight();
            created.Add(ground);
            var hideouts = new List<Hideout>();
            for (int i = 0; i < RaidSandbox.Corners.Length; i++)
            {
                hideouts.Add(RaidSandbox.CreateHideout(i, RaidSandbox.Corners[i].label, RaidSandbox.Corners[i].center));
                created.Add(hideouts[i].gameObject);
            }
            ground.AddComponent<RuntimeNavMesh>();
            var definition = MakeCase("Test", hideout: 2);
            definition.scammers = 3;
            definition.riders = 1;
            definition.forcedWorkers = 2;
            definition.victims = 1;
            var stage = new GameObject("Stage").AddComponent<CaseStage>();
            created.Add(stage.gameObject);
            yield return null;

            stage.Populate(definition);
            stage.Populate(definition); // a second call must not double the people

            Assert.AreSame(hideouts[2], stage.Hideout);
            Assert.AreEqual(4, stage.Scammers.Count);
            Assert.AreEqual(3, stage.Civilians.Count);
            Assert.AreEqual(1, stage.Scammers.FindAll(s => s is RiderScammer).Count);
            foreach (var npc in stage.Scammers)
                Assert.Less(Vector3.Distance(npc.transform.position, hideouts[2].transform.position), 12f, "inside the right hideout");
            foreach (var npc in stage.Civilians)
                Assert.Less(Vector3.Distance(npc.transform.position, hideouts[2].transform.position), 12f);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator The_saved_raid_scene_loads_and_fills_its_fallback_case()
        {
            if (!File.Exists(GameManager.RaidScene)) Assert.Ignore("Run Tools > Scammer No Scamming > Build Case Flow Scenes first");

            var load = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(GameManager.RaidScene, new LoadSceneParameters(LoadSceneMode.Additive));
            yield return load;
            yield return null;
            yield return null;

            try
            {
                Assert.IsNotNull(CaseStage.Active);
                Assert.IsNotNull(CaseStage.Active.Case, "with no GameManager it plays its fallback case");
                Assert.AreEqual(CaseStage.Active.Case.scammers + CaseStage.Active.Case.riders, CaseStage.Active.Scammers.Count);
            }
            finally
            {
                SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(GameManager.RaidScene));
            }
            yield return null;
        }
#endif
    }
}
