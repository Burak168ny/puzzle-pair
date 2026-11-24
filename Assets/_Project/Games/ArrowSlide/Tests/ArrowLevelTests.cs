using ArrowSlide.Model;
using NUnit.Framework;

namespace ArrowSlide.Tests
{
    public sealed class ArrowLevelTests
    {
        [Test]
        public void Campaign_HasFiveLevels()
        {
            Assert.AreEqual(5, ArrowLevels.All.Count);
        }

        [Test]
        public void EveryLevel_ParsesAndIsSolvable()
        {
            foreach (var level in ArrowLevels.All)
            {
                var report = ArrowSolver.Analyze(level.CreateBoard());
                Assert.IsTrue(report.Solvable, $"{level.Name}: {report}");
                Assert.GreaterOrEqual(report.Waves, 2, $"{level.Name} must need some order ({report})");
                Assert.Greater(report.InitiallyFree, 0, level.Name);
            }
        }

        [Test]
        public void Levels_GrowInArrowCount()
        {
            int previous = 0;
            foreach (var level in ArrowLevels.All)
            {
                int count = level.CreateBoard().Arrows.Count;
                Assert.Greater(count, previous, level.Name);
                previous = count;
            }
        }

        [Test]
        public void FirstLevel_HasATutorialTip()
        {
            Assert.IsFalse(string.IsNullOrEmpty(ArrowLevels.All[0].Tip));
        }

        [Test]
        public void Solver_DetectsABlockingCycle()
        {
            // Two arrows pointing at each other can never leave.
            var board = ArrowLevelParser.Parse(">RL<");
            var report = ArrowSolver.Analyze(board);

            Assert.IsFalse(report.Solvable);
            Assert.AreEqual(2, report.Stuck);
            Assert.AreEqual(0, report.InitiallyFree);
        }

        [Test]
        public void Solver_OrderIsAValidPlaythrough()
        {
            foreach (var level in ArrowLevels.All)
            {
                var report = ArrowSolver.Analyze(level.CreateBoard());
                var session = new ArrowSession(level.CreateBoard());
                foreach (int id in report.Order)
                    Assert.AreEqual(TapOutcome.Escaped, session.Tap(id).Outcome, level.Name);
                Assert.AreEqual(SessionState.Won, session.State, level.Name);
            }
        }
    }
}
