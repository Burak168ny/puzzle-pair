using System.Collections.Generic;
using ArrowSlide.Model;
using NUnit.Framework;

namespace ArrowSlide.Tests
{
    public sealed class ArrowSessionTests
    {
        // The bottom-row arrow (pointing right) is blocked by the right-column arrow (pointing up).
        private static readonly string[] Blocked =
        {
            "...U",
            ">>R^",
        };

        private static ArrowSession NewSession(out int horizontal, out int vertical, int lives = ArrowSession.DefaultLives)
        {
            var board = ArrowLevelParser.Parse(Blocked);
            horizontal = board.ArrowAt(new Cell(0, 0));
            vertical = board.ArrowAt(new Cell(3, 1));
            return new ArrowSession(board, lives);
        }

        [Test]
        public void Tap_FreeArrow_Escapes()
        {
            var session = NewSession(out _, out int vertical);
            var result = session.Tap(vertical);

            Assert.AreEqual(TapOutcome.Escaped, result.Outcome);
            Assert.IsTrue(session.Board.IsRemoved(vertical));
            Assert.AreEqual(ArrowSession.DefaultLives, session.Lives);
        }

        [Test]
        public void Tap_BlockedArrow_CostsAHeartAndStays()
        {
            var session = NewSession(out int horizontal, out int vertical);
            var result = session.Tap(horizontal);

            Assert.AreEqual(TapOutcome.Blocked, result.Outcome);
            Assert.AreEqual(vertical, result.Move.BlockerId);
            Assert.AreEqual(ArrowSession.DefaultLives - 1, session.Lives);
            Assert.IsFalse(session.Board.IsRemoved(horizontal));
            Assert.AreEqual(1, session.Mistakes);
        }

        [Test]
        public void ClearingTheBoard_WinsAndFurtherTapsAreIgnored()
        {
            var session = NewSession(out int horizontal, out int vertical);
            session.Tap(vertical);
            var last = session.Tap(horizontal);

            Assert.AreEqual(SessionState.Won, last.State);
            Assert.AreEqual(SessionState.Won, session.State);
            Assert.AreEqual(TapOutcome.Ignored, session.Tap(horizontal).Outcome);
            CollectionAssert.AreEqual(new[] { vertical, horizontal }, session.EscapeOrder);
        }

        [Test]
        public void RunningOutOfHearts_Loses_AndReviveRefillsThem()
        {
            var session = NewSession(out int horizontal, out int vertical, lives: 2);
            session.Tap(horizontal);
            var result = session.Tap(horizontal);

            Assert.AreEqual(SessionState.Lost, result.State);
            Assert.AreEqual(0, session.Lives);
            Assert.AreEqual(TapOutcome.Ignored, session.Tap(vertical).Outcome);

            session.Revive();
            Assert.AreEqual(SessionState.Playing, session.State);
            Assert.AreEqual(2, session.Lives);
            Assert.AreEqual(TapOutcome.Escaped, session.Tap(vertical).Outcome);
        }

        [Test]
        public void TapOnRemovedOrInvalidArrow_IsIgnored()
        {
            var session = NewSession(out _, out int vertical);
            session.Tap(vertical);

            Assert.AreEqual(TapOutcome.Ignored, session.Tap(vertical).Outcome);
            Assert.AreEqual(TapOutcome.Ignored, session.Tap(-1).Outcome);
            Assert.AreEqual(TapOutcome.Ignored, session.Tap(99).Outcome);
            Assert.AreEqual(ArrowSession.DefaultLives, session.Lives);
        }

        [Test]
        public void Hint_PicksTheFreeArrowThatUnlocksTheMost()
        {
            // The right-column arrow frees the middle row; the bottom-row arrow frees nothing.
            var board = ArrowLevelParser.Parse(
                "...U",
                ">>R^",
                "L<..");
            var session = new ArrowSession(board, hints: 1);

            int hint = session.UseHint();
            Assert.AreEqual(board.ArrowAt(new Cell(3, 2)), hint);
            Assert.AreEqual(0, session.HintsLeft);
            Assert.AreEqual(-1, session.UseHint());
            Assert.IsTrue(board.Check(hint).IsFree);
        }

        [Test]
        public void Hint_FollowedRepeatedly_AlwaysClearsCampaignLevels()
        {
            foreach (var level in ArrowLevels.All)
            {
                var session = new ArrowSession(level.CreateBoard());
                var guard = new HashSet<int>();
                while (session.State == SessionState.Playing)
                {
                    int id = session.FindHint();
                    Assert.GreaterOrEqual(id, 0, level.Name);
                    Assert.IsTrue(guard.Add(id), level.Name);
                    Assert.AreEqual(TapOutcome.Escaped, session.Tap(id).Outcome, level.Name);
                }
                Assert.AreEqual(SessionState.Won, session.State, level.Name);
                Assert.AreEqual(0, session.Mistakes, level.Name);
            }
        }
    }
}
