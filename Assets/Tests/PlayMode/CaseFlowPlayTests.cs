using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScammerNoScamming.Tests
{
    public class CaseFlowPlayTests
    {
        GameObject player;
        GameObject other;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (other != null) Object.Destroy(other);
            if (player != null) Object.Destroy(player);
            PlayerLocator.Set(null);
        }

        [UnityTest]
        public IEnumerator FlowOpensTheMenuPausedAndFreesTheCursor()
        {
            player = PlayerSandbox.CreatePlayer();
            PlayerLocator.Set(player.GetComponent<PlayerController>());
            other = new GameObject("CaseFlow");
            other.AddComponent<CaseFlowDemo>();
            yield return null;
            yield return null;

            Assert.IsTrue(GameScreen.AnyOpen);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsFalse(player.GetComponent<PlayerControls>().enabled);

            other.GetComponent<MainMenuScreen>().Close();
            Assert.IsFalse(GameScreen.AnyOpen);
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(player.GetComponent<PlayerControls>().enabled);
        }

        [UnityTest]
        public IEnumerator AnsweringTheCallSlowsThePlayerThenRecovers()
        {
            player = PlayerSandbox.CreatePlayer();
            var controller = player.GetComponent<PlayerController>();
            PlayerLocator.Set(controller);
            float walk = controller.walkSpeed;
            other = new GameObject("Booth");
            var booth = other.AddComponent<CallCenterBooth>();
            booth.panicDuration = 0.2f;
            yield return null;

            booth.Spring(player);
            Assert.IsTrue(booth.IsPanicking);
            Assert.Less(controller.walkSpeed, walk);
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(booth.IsPanicking);
            Assert.AreEqual(walk, controller.walkSpeed, 1e-4f);
        }
    }
}
