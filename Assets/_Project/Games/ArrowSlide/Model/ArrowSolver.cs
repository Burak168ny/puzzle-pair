using System.Collections.Generic;

namespace ArrowSlide.Model
{
    /// <summary>Solvability and difficulty numbers for a board.</summary>
    public readonly struct SolveReport
    {
        public readonly bool Solvable;
        /// <summary>
        /// Rounds of "remove every arrow that is free right now" needed to clear the board:
        /// the depth of the blocking chain, a good proxy for how far ahead a player must look.
        /// </summary>
        public readonly int Waves;
        public readonly int ArrowCount;
        public readonly int InitiallyFree;
        /// <summary>Arrows left when the solver got stuck (0 when solvable).</summary>
        public readonly int Stuck;
        /// <summary>A valid escape order (wave by wave).</summary>
        public readonly IReadOnlyList<int> Order;

        public SolveReport(bool solvable, int waves, int arrowCount, int initiallyFree, int stuck, IReadOnlyList<int> order)
        {
            Solvable = solvable;
            Waves = waves;
            ArrowCount = arrowCount;
            InitiallyFree = initiallyFree;
            Stuck = stuck;
            Order = order;
        }

        public override string ToString() =>
            $"{(Solvable ? "solvable" : $"STUCK with {Stuck}")}, {ArrowCount} arrows, {Waves} waves, {InitiallyFree} free at start";
    }

    /// <summary>
    /// Removing an arrow only ever frees cells, so if an arrow can escape now it can still escape
    /// after any other removal. A greedy "remove whatever is free" pass is therefore a complete
    /// solver: it clears the board exactly when the board is solvable (no blocking cycle).
    /// </summary>
    public static class ArrowSolver
    {
        public static SolveReport Analyze(ArrowBoard board)
        {
            var work = board.Clone();
            var order = new List<int>();
            int waves = 0;
            int initiallyFree = -1;

            while (work.RemainingCount > 0)
            {
                var free = work.GetFreeArrows();
                if (initiallyFree < 0) initiallyFree = free.Count;
                if (free.Count == 0) break;
                foreach (int id in free) work.Remove(id);
                order.AddRange(free);
                waves++;
            }

            if (initiallyFree < 0) initiallyFree = 0;
            return new SolveReport(work.RemainingCount == 0, waves, board.Arrows.Count, initiallyFree, work.RemainingCount, order);
        }

        public static bool IsSolvable(ArrowBoard board) => Analyze(board).Solvable;
    }
}
