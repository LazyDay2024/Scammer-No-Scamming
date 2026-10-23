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
        public IEnumerator PhysicalStationCompletesCaseAndLessonWithoutOpeningScreens()
        {
            var player = Player();
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

            station.GeneBottles[0].Interact();
            Assert.AreEqual(1, loadout.Count);
            var chosen = loadout.Selected[0];
            station.DispatchTarget.Interact();
            station.DispatchTarget.Interact();
            Assert.AreEqual(1, dispatchCalls, "the physical dispatch prop must fire once");
            Assert.AreSame(loadout, dispatched);
            Assert.AreSame(chosen, dispatched.Selected[0]);
            Assert.AreEqual("Closed", station.StageName);

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
