using System;
using System.Collections.Generic;

namespace ArrowSlide.Model
{
    public enum SessionState
    {
        Playing,
        Won,
        Lost,
    }

    public enum TapOutcome
    {
        /// <summary>Tap on a removed arrow, or the session is over.</summary>
        Ignored,
        Escaped,
        Blocked,
    }

    public readonly struct TapResult
    {
        public readonly TapOutcome Outcome;
        public readonly int ArrowId;
        public readonly MoveCheck Move;
        public readonly int LivesLeft;
        public readonly SessionState State;

        public TapResult(TapOutcome outcome, int arrowId, MoveCheck move, int livesLeft, SessionState state)
        {
            Outcome = outcome;
            ArrowId = arrowId;
            Move = move;
            LivesLeft = livesLeft;
            State = state;
        }
    }

    /// <summary>
    /// One attempt at a level: the board, hearts and hints. A blocked tap costs a heart;
    /// the level is won when every arrow has left the board and lost when the hearts run out.
    /// </summary>
    public sealed class ArrowSession
    {
        public const int DefaultLives = 3;
        public const int DefaultHints = 2;

        private readonly List<int> _escapeOrder = new List<int>();

        public ArrowSession(ArrowBoard board, int lives = DefaultLives, int hints = DefaultHints)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            if (lives <= 0) throw new ArgumentOutOfRangeException(nameof(lives));
            MaxLives = lives;
            Lives = lives;
            HintsLeft = hints;
            State = board.RemainingCount == 0 ? SessionState.Won : SessionState.Playing;
        }

        public ArrowBoard Board { get; }
        public int MaxLives { get; }
        public int Lives { get; private set; }
        public int HintsLeft { get; private set; }
        public int Mistakes { get; private set; }
        public SessionState State { get; private set; }

        /// <summary>Arrows in the order they escaped.</summary>
        public IReadOnlyList<int> EscapeOrder => _escapeOrder;

        public TapResult Tap(int arrowId)
        {
            if (State != SessionState.Playing || arrowId < 0 || arrowId >= Board.Arrows.Count || Board.IsRemoved(arrowId))
                return new TapResult(TapOutcome.Ignored, arrowId, default, Lives, State);

            var move = Board.Check(arrowId);
            if (move.IsFree)
            {
                Board.Remove(arrowId);
                _escapeOrder.Add(arrowId);
                if (Board.RemainingCount == 0) State = SessionState.Won;
                return new TapResult(TapOutcome.Escaped, arrowId, move, Lives, State);
            }

            Lives--;
            Mistakes++;
            if (Lives <= 0) State = SessionState.Lost;
            return new TapResult(TapOutcome.Blocked, arrowId, move, Lives, State);
        }

        /// <summary>
        /// The most useful free arrow: the one whose removal frees the most other arrows
        /// (ties: lowest id). -1 if none, which only happens on an unsolvable board.
        /// </summary>
        public int FindHint()
        {
            var free = Board.GetFreeArrows();
            if (free.Count == 0) return -1;

            int best = free[0];
            int bestGain = -1;
            int freeNow = free.Count;
            foreach (int id in free)
            {
                Board.Remove(id);
                int gain = Board.GetFreeArrows().Count - (freeNow - 1);
                Board.Restore(id);
                if (gain > bestGain)
                {
                    bestGain = gain;
                    best = id;
                }
            }
            return best;
        }

        /// <summary>Spends a hint and returns the arrow to highlight, or -1 when none are left.</summary>
        public int UseHint()
        {
            if (State != SessionState.Playing || HintsLeft <= 0) return -1;
            int id = FindHint();
            if (id >= 0) HintsLeft--;
            return id;
        }

        /// <summary>Second chance after losing: refill the hearts and keep the board.</summary>
        public void Revive()
        {
            if (State != SessionState.Lost) throw new InvalidOperationException("Only a lost session can be revived.");
            Lives = MaxLives;
            State = SessionState.Playing;
        }
    }
}
