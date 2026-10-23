using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class StationInteractionTests
    {
        readonly List<GameObject> objects = new List<GameObject>();
        CaseDatabase database;

        [TearDown]
        public void TearDown()
        {
            foreach (var go in objects) if (go != null) Object.Destroy(go);
            objects.Clear();
            if (database != null)
            {
                foreach (var definition in database.Pool) Object.Destroy(definition);
                Object.Destroy(database);
            }
            PlayerLocator.Set(null);
            Time.timeScale = 1f;
        }

        GameObject Player()
        {
            var player = PlayerSandbox.CreatePlayer();
            objects.Add(player);
            PlayerLocator.Set(player.GetComponent<PlayerController>());
            return player;
        }

        [UnityTest]
        public IEnumerator PhysicalStationUsesExistingGeneScreenAndCompletesCaseAndLesson()
        {
            var player = Player();
            var flowObject = new GameObject("CaseFlow");
            objects.Add(flowObject);
            flowObject.AddComponent<CaseFlowDemo>().enabled = false;
            var screen = flowObject.GetComponent<GeneSelectionScreen>();
            int screenCount = Object.FindObjectsByType<GeneSelectionScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            var go = new GameObject("StationIntegrationTest");
            objects.Add(go);
            var station = go.AddComponent<StationInvestigation>();
            database = CaseDatabase.LoadDefault();
            var definition = database.Find("CASE_01_FAKE_SHOP");
            var loadout = new GeneLoadout();
            int analysisCalls = 0, dispatchCalls = 0, nextCalls = 0;
            AnalysisResult result = null;
            GeneLoadout dispatched = null;
            station.BeginCase(definition, loadout, value => { analysisCalls++; result = value; },
                value => { dispatchCalls++; dispatched = value; });
            yield return null;

            Assert.IsNotNull(player.GetComponent<WorldInteractor>());
            int safety = 100;
            while (station.StageName == "Testimony" && safety-- > 0) station.Victim.Interact();
            Assert.AreEqual("Questions", station.StageName, "testimony must hand control to the physical answer board");
            foreach (var question in definition.Questions)
            {
                bool map = question.Reward == IntelKind.HideoutLocation;
                var target = map ? station.MapPins[question.CorrectIndex] : station.AnswerTargets[question.CorrectIndex];
                Assert.IsTrue(target.gameObject.activeInHierarchy);
                target.Interact();
                Assert.AreEqual("Feedback", station.StageName);
                station.AdvanceTarget.Interact();
            }
            Assert.AreEqual("Genes", station.StageName);
            Assert.AreEqual(1, analysisCalls);
            Assert.AreEqual(definition.Questions.Count, result.CorrectCount);
            Assert.AreEqual(0f, result.TroublePenalty);
            Assert.IsTrue(result.IsUnlocked(IntelKind.HideoutLocation));
            Assert.IsTrue(screen.IsOpen, "the station must reuse the screen on CaseFlow");
            Assert.AreEqual(screenCount, Object.FindObjectsByType<GeneSelectionScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
            Assert.IsFalse(player.GetComponent<PlayerControls>().enabled);
            Assert.AreEqual(0f, Time.timeScale);
            foreach (var child in go.GetComponentsInChildren<Transform>(true))
            {
                Assert.IsFalse(child.name.StartsWith("GeneBottle"));
                Assert.AreNotEqual("GeneShelf", child.name);
                Assert.AreNotEqual("ShelfLeg", child.name);
                Assert.AreNotEqual("BottleStopper", child.name);
            }

            screen.ChooseGene(0);
            Assert.AreEqual(1, loadout.Count);
            var chosen = loadout.Selected[0];
            screen.Confirm();
            screen.Confirm();
            Assert.AreEqual(1, dispatchCalls, "confirm must dispatch exactly once");
            Assert.AreSame(loadout, dispatched);
            Assert.AreSame(chosen, dispatched.Selected[0]);
            Assert.AreEqual("Closed", station.StageName);
            yield return null;
            Assert.IsFalse(screen.IsOpen);
            Assert.IsTrue(player.GetComponent<PlayerControls>().enabled);
            Assert.AreEqual(1f, Time.timeScale);

            station.ShowLesson(LessonPage.Build(definition, database.Text,
                new[] { TrapKind.FakeSupply }, new[] { TrapKind.CallCenter }), () => nextCalls++);
            safety = 100;
            while (station.StageName == "Lesson" && safety-- > 0) station.CaseFileTarget.Interact();
            station.CaseFileTarget.Interact();
            Assert.AreEqual("Closed", station.StageName);
            Assert.AreEqual(1, nextCalls, "all file pages must lead to exactly one next-case callback");
            Assert.IsFalse(GameScreen.AnyOpen);
            Assert.IsTrue(player.GetComponent<PlayerControls>().enabled);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator ResetDuringGeneSelectionClosesScreenAndCannotDispatchOldCallback()
        {
            var player = Player();
            var go = new GameObject("StationResetTest");
            objects.Add(go);
            var screen = go.AddComponent<GeneSelectionScreen>();
            var station = go.AddComponent<StationInvestigation>();
            database = CaseDatabase.LoadDefault();
            var definition = database.Find("CASE_01_FAKE_SHOP");
            int oldDispatches = 0, newDispatches = 0;
            station.BeginCase(definition, new GeneLoadout(), _ => { }, _ => oldDispatches++);
            yield return null;
            CompleteAnalysis(station, definition);
            Assert.IsTrue(screen.IsOpen);

            station.Reset();
            screen.ChooseGene(0);
            screen.Confirm();
            yield return null;
            Assert.IsFalse(screen.IsOpen);
            Assert.AreEqual("Closed", station.StageName);
            Assert.AreEqual(0, oldDispatches);
            Assert.IsTrue(player.GetComponent<PlayerControls>().enabled);
            Assert.AreEqual(1f, Time.timeScale);

            var nextLoadout = new GeneLoadout();
            station.BeginCase(definition, nextLoadout, _ => { }, chosen =>
            {
                newDispatches++;
                Assert.AreSame(nextLoadout, chosen);
            });
            CompleteAnalysis(station, definition);
            screen.ChooseGene(0);
            screen.Confirm();
            screen.Confirm();
            Assert.AreEqual(0, oldDispatches);
            Assert.AreEqual(1, newDispatches);
            yield return null;
        }

        static void CompleteAnalysis(StationInvestigation station, CaseDefinition definition)
        {
            for (int guard = 0; guard < 100 && station.StageName == "Testimony"; guard++) station.Victim.Interact();
            Assert.AreEqual("Questions", station.StageName);
            foreach (var question in definition.Questions)
            {
                var targets = question.Reward == IntelKind.HideoutLocation ? station.MapPins : station.AnswerTargets;
                targets[question.CorrectIndex].Interact();
                station.AdvanceTarget.Interact();
            }
            Assert.AreEqual("Genes", station.StageName);
        }

        [UnityTest]
        public IEnumerator CenterRayFocusStopsAtAnObstructingCollider()
        {
            var player = Player();
            var interactor = player.AddComponent<WorldInteractor>();
            yield return null;
            var camera = player.GetComponentInChildren<Camera>();
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objects.Add(target);
            target.transform.position = camera.transform.position + camera.transform.forward * 2.5f;
            target.transform.localScale = Vector3.one * .5f;
            var action = target.AddComponent<WorldInteractable>();
            Physics.SyncTransforms();
            yield return null;
            Assert.AreSame(action, interactor.Focused);

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objects.Add(wall);
            wall.transform.position = camera.transform.position + camera.transform.forward;
            wall.transform.localScale = new Vector3(2f, 2f, .15f);
            Physics.SyncTransforms();
            yield return null;
            Assert.IsNull(interactor.Focused, "E must never reach an interactable through a wall");
            Assert.IsFalse(WorldInteractor.HasFocusedInteraction);
        }
    }
}
