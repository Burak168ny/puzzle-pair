using System;
using ArrowSlide.Model;
using NUnit.Framework;

namespace ArrowSlide.Tests
{
    public sealed class ArrowBoardTests
    {
        private static ArrowBoard Board(params string[] rows) => ArrowLevelParser.Parse(rows);

        [Test]
        public void Parse_ReadsCellsTailToHead_WithYUp()
        {
            var board = Board(
                "...",
                ">>R");

            Assert.AreEqual(3, board.Width);
            Assert.AreEqual(2, board.Height);
            Assert.AreEqual(1, board.Arrows.Count);
            var arrow = board.Arrows[0];
            Assert.AreEqual(Dir.Right, arrow.Heading);
            Assert.AreEqual(new Cell(0, 0), arrow.Tail);
            Assert.AreEqual(new Cell(2, 0), arrow.Head);
            Assert.AreEqual(0, board.ArrowAt(new Cell(1, 0)));
            Assert.AreEqual(-1, board.ArrowAt(new Cell(1, 1)));
        }

        [Test]
        public void Parse_FollowsBends()
        {
            var board = Board(
                "U.",
                "^.",
                "^<");

            var arrow = board.Arrows[0];
            Assert.AreEqual(4, arrow.Length);
            Assert.AreEqual(new Cell(1, 0), arrow.Tail);
            Assert.AreEqual(new Cell(0, 2), arrow.Head);
            Assert.AreEqual(Dir.Up, arrow.Heading);
        }

        // Rows are separated by '/'.
        [TestCase(">>./...", TestName = "Parse_Rejects_BodyIntoEmpty")]
        [TestCase("R../...", TestName = "Parse_Rejects_HeadWithoutBody")]
        [TestCase(">R/.^", TestName = "Parse_Rejects_TwoBodiesIntoOneCell")]
        [TestCase(">U/...", TestName = "Parse_Rejects_RaggedRows")]
        [TestCase("></..", TestName = "Parse_Rejects_LoopWithoutHead")]
        [TestCase("x./..", TestName = "Parse_Rejects_UnknownCharacter")]
        public void Parse_RejectsMalformedBoards(string board)
        {
            Assert.Throws<FormatException>(() => ArrowLevelParser.Parse(board.Split('/')));
        }

        [Test]
        public void Arrow_HeadMustPointAlongLastSegment()
        {
            Assert.Throws<ArgumentException>(() => ArrowLevelParser.Parse(new[] { ">U" }));
        }

        [Test]
        public void Format_RoundTripsEveryCampaignLevel()
        {
            foreach (var level in ArrowLevels.All)
            {
                var formatted = ArrowLevelParser.Format(level.CreateBoard());
                CollectionAssert.AreEqual(level.Rows, formatted, level.Name);
            }
        }

        [Test]
        public void Check_FreeArrow_TravelsBoardCellsAheadPlusItsLength()
        {
            var board = Board(">R..");
            var move = board.Check(0);

            Assert.AreEqual(MoveKind.Escape, move.Kind);
            Assert.AreEqual(2 + 2, move.Distance); // two empty cells, then its two cells follow
            Assert.AreEqual(-1, move.BlockerId);
        }

        [Test]
        public void Check_BlockedArrow_ReportsBlockerAndFreeSteps()
        {
            var board = Board(
                "...U",
                ">R.^");
            int horizontal = board.ArrowAt(new Cell(0, 0));
            int vertical = board.ArrowAt(new Cell(3, 0));

            var move = board.Check(horizontal);
            Assert.AreEqual(MoveKind.Blocked, move.Kind);
            Assert.AreEqual(1, move.Distance);
            Assert.AreEqual(vertical, move.BlockerId);
            Assert.AreEqual(new Cell(3, 0), move.BlockCell);
        }

        [Test]
        public void Check_ReportsTheFirstBlocker_ThenTheNext_UntilTheRayIsClear()
        {
            var board = Board(
                "..U.U",
                ">R^.^");
            int arrow = board.ArrowAt(new Cell(0, 0));
            int near = board.ArrowAt(new Cell(2, 0));
            int far = board.ArrowAt(new Cell(4, 0));

            Assert.AreEqual(near, board.Check(arrow).BlockerId);
            board.Remove(near);
            Assert.AreEqual(far, board.Check(arrow).BlockerId);
            Assert.AreEqual(2, board.Check(arrow).Distance);
            board.Remove(far);
            Assert.IsTrue(board.Check(arrow).IsFree);
        }

        [Test]
        public void ArrowIds_FollowHeadsInReadingOrder()
        {
            var board = Board(
                "...U",
                ">R.^");
            Assert.AreEqual(0, board.ArrowAt(new Cell(3, 1)));
            Assert.AreEqual(1, board.ArrowAt(new Cell(1, 0)));
        }

        [Test]
        public void Check_HeadMayFollowItsOwnTail()
        {
            // The head's first step lands on the tail cell, which the tail leaves on that same step.
            var board = Board(
                ">Rv",
                "^<<");

            var move = board.Check(0);
            Assert.AreEqual(MoveKind.Escape, move.Kind);
            Assert.AreEqual(1 + 6, move.Distance);
        }

        [Test]
        public void Check_HeadCannotRunIntoItsOwnBody()
        {
            var board = Board(
                ">Rv<",
                "^<<.");

            var move = board.Check(0);
            Assert.AreEqual(MoveKind.Blocked, move.Kind);
            Assert.AreEqual(0, move.BlockerId);
            Assert.AreEqual(0, move.Distance);
        }

        [Test]
        public void RemoveAndRestore_UpdateOccupancyAndCount()
        {
            var board = Board(">R", "L<");
            board.Remove(0);

            Assert.AreEqual(1, board.RemainingCount);
            Assert.AreEqual(-1, board.ArrowAt(new Cell(0, 1)));
            Assert.Throws<InvalidOperationException>(() => board.Remove(0));

            board.Restore(0);
            Assert.AreEqual(2, board.RemainingCount);
            Assert.AreEqual(0, board.ArrowAt(new Cell(1, 1)));
        }

        [Test]
        public void Clone_IsIndependent()
        {
            var board = Board(">R", "L<");
            var clone = board.Clone();
            clone.Remove(1);

            Assert.IsFalse(board.IsRemoved(1));
            Assert.AreEqual(2, board.RemainingCount);
            Assert.AreEqual(1, clone.RemainingCount);
        }
    }
}
