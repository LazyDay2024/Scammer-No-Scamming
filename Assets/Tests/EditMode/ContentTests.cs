using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ScammerNoScamming.Tests
{
    public class ContentTests
    {
        CaseDatabase db;

        [SetUp]
        public void SetUp() => db = CaseDatabase.LoadDefault();

        [TearDown]
        public void TearDown()
        {
            foreach (var c in db.Pool) Object.DestroyImmediate(c);
            Object.DestroyImmediate(db);
        }

        [Test]
        public void ContentLoadsThreeCasesAndOneBoss()
        {
            Assert.AreEqual(3, db.Regular.Count());
            Assert.IsNotNull(db.Boss);
            Assert.IsFalse(string.IsNullOrEmpty(db.Text.Opening));
            Assert.IsNotEmpty(db.Text.Tutorial);
            Assert.IsNotEmpty(db.Text.EndingClean);
            Assert.IsNotEmpty(db.Text.EndingMessy);
        }

        [Test]
        public void EveryQuestionHasExactlyOneRightAnswerAndEveryCaseCanRevealItsHideout()
        {
            foreach (var c in db.Pool)
            {
                Assert.IsNotEmpty(c.VictimStory, c.CaseId);
                Assert.IsNotEmpty(c.Clues, c.CaseId);
                Assert.IsNotEmpty(c.HideoutId, c.CaseId);
                foreach (var q in c.Questions)
                    Assert.AreEqual(1, q.Options.Count(o => o.IsCorrect), c.CaseId + ": " + q.Prompt);
                Assert.IsTrue(c.Questions.Any(q => q.Reward == IntelKind.HideoutLocation), c.CaseId + " never reveals its hideout");
                Assert.AreEqual(3, c.Lesson.RedFlags.Count, c.CaseId);
                Assert.AreEqual(3, c.Lesson.HowToRespond.Count, c.CaseId);
            }
        }

        [Test]
        public void HideoutsAreDistinctAndEveryTrapHasALesson()
        {
            Assert.AreEqual(db.Pool.Count, db.Pool.Select(c => c.HideoutId).Distinct().Count());
            foreach (TrapKind kind in System.Enum.GetValues(typeof(TrapKind)))
                Assert.IsNotNull(db.Text.TrapLesson(kind), kind.ToString());
        }

        [Test]
        public void StatHasASource()
        {
            StringAssert.Contains("121,921", db.Text.Stat.Description);
            StringAssert.StartsWith("https://", db.Text.Stat.SourceUrl);
        }

        [Test]
        public void RunHasEachCaseOnceEndsWithTheBossAndTheOrderVaries()
        {
            var run = db.PickRun(3, 42);
            Assert.AreEqual(4, run.Count);
            Assert.IsTrue(run[3].IsBoss);
            Assert.AreEqual(run.Count, run.Distinct().Count());
            var firsts = Enumerable.Range(0, 30).Select(seed => db.PickRun(3, seed)[0].CaseId).Distinct().Count();
            Assert.Greater(firsts, 1, "every run opened with the same case");
        }
    }

    public class AnalysisTests
    {
        CaseDefinition MakeCase()
        {
            var c = ScriptableObject.CreateInstance<CaseDefinition>();
            for (int i = 0; i < 3; i++)
            {
                var q = new AnalysisQuestion { Prompt = "q" + i, Reward = i == 2 ? IntelKind.HideoutLocation : IntelKind.None };
                q.Options.Add(new AnswerOption { Text = "a", IsCorrect = false });
                q.Options.Add(new AnswerOption { Text = "b", IsCorrect = true });
                c.Questions.Add(q);
            }
            return c;
        }

        [Test]
        public void WrongAnswersAddTroubleAndRightLocationAnswerUnlocksTheHideout()
        {
            var c = MakeCase();
            var session = new AnalysisSession(c, 10f);
            Assert.IsFalse(session.Answer(0));
            Assert.IsTrue(session.Answer(1));
            Assert.IsFalse(session.IsComplete);
            Assert.IsTrue(session.Answer(1));
            Assert.IsTrue(session.IsComplete);
            Assert.AreEqual(2, session.Result.CorrectCount);
            Assert.AreEqual(1, session.Result.WrongCount);
            Assert.AreEqual(10f, session.Result.TroublePenalty);
            Assert.IsTrue(session.Result.IsUnlocked(IntelKind.HideoutLocation));
            Assert.IsFalse(session.Answer(1), "answers after the last question are ignored");
            Object.DestroyImmediate(c);
        }

        [Test]
        public void MissingTheLocationQuestionKeepsTheHideoutHidden()
        {
            var c = MakeCase();
            var session = new AnalysisSession(c);
            session.Answer(1);
            session.Answer(1);
            session.Answer(0);
            Assert.IsFalse(session.Result.IsUnlocked(IntelKind.HideoutLocation));
            Object.DestroyImmediate(c);
        }

        [Test]
        public void LessonListsSprungTrapsAndWhatWasSpotted()
        {
            var db = CaseDatabase.LoadDefault();
            var page = LessonPage.Build(db.Find("CASE_01_FAKE_SHOP"), db.Text,
                new[] { TrapKind.FakeSupply }, new[] { TrapKind.Disguised });
            Assert.AreEqual(1, page.TrapNotes.Count);
            Assert.AreEqual(1, page.Spotted.Count);
            Assert.AreEqual(3, page.RedFlags.Count);
            Assert.IsNotEmpty(page.Hotlines);
            foreach (var c in db.Pool) Object.DestroyImmediate(c);
            Object.DestroyImmediate(db);
        }

        [Test]
        public void EndingIsCleanOnlyWithNoInnocentsHitAndLowTrouble()
        {
            Assert.AreEqual(EndingType.Clean, EndingEvaluator.Evaluate(0, 40f, 100f));
            Assert.AreEqual(EndingType.Messy, EndingEvaluator.Evaluate(1, 0f, 100f));
            Assert.AreEqual(EndingType.Messy, EndingEvaluator.Evaluate(0, 100f, 100f));
        }

        [Test]
        public void HideoutPointsFollowZoneAndDirection()
        {
            var east = CityLayoutPoints.Point(CityZone.Shophouse, CityDirection.East, 0f);
            Assert.Greater(east.x, CityBuilder.InnerRingRoad);
            Assert.Less(east.x, CityBuilder.ViaductRadius);
            Assert.AreEqual(0f, east.z, 1e-3f);
            var north = CityLayoutPoints.Point(CityZone.Tower, CityDirection.North, 0f);
            Assert.Greater(north.z, CityBuilder.PlazaRadius);
            Assert.Less(north.z, CityBuilder.InnerRingRoad);
        }
    }
}
