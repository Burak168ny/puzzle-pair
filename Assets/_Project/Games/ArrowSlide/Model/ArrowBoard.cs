using System;
using System.Collections.Generic;

namespace ArrowSlide.Model
{
    public enum MoveKind
    {
        /// <summary>Nothing is in the way: the arrow slides off the board.</summary>
        Escape,
        /// <summary>The head would run into an arrow (possibly its own body) and bounces back.</summary>
        Blocked,
    }

    /// <summary>What happens if an arrow is tapped. Distances are in cells travelled by the head.</summary>
    public readonly struct MoveCheck
    {
        public readonly MoveKind Kind;
        /// <summary>
        /// Escape: steps until the whole arrow has left the board.
        /// Blocked: free steps before the head touches the obstacle (0 when it is right in front).
        /// </summary>
        public readonly int Distance;
        /// <summary>Id of the arrow in the way (may be the tapped arrow itself), -1 on escape.</summary>
        public readonly int BlockerId;
        /// <summary>The occupied cell the head would run into (blocked moves only).</summary>
        public readonly Cell BlockCell;

        public MoveCheck(MoveKind kind, int distance, int blockerId, Cell blockCell)
        {
            Kind = kind;
            Distance = distance;
            BlockerId = blockerId;
            BlockCell = blockCell;
        }

        public bool IsFree => Kind == MoveKind.Escape;
        public override string ToString() => Kind == MoveKind.Escape ? $"Escape({Distance})" : $"Blocked({Distance}, by {BlockerId} at {BlockCell})";
    }

    /// <summary>
    /// The board: a rectangle of cells and the arrows on it. Engine-free and deterministic.
    /// Removing an arrow can only free space, never block another one, so the order of legal
    /// removals never matters for solvability (see <see cref="ArrowSolver"/>).
    /// </summary>
    public sealed class ArrowBoard
    {
        private readonly Arrow[] _arrows;
        private readonly bool[] _removed;
        private readonly int[] _occupancy; // arrow id per cell, -1 = empty

        public ArrowBoard(int width, int height, IReadOnlyList<Arrow> arrows)
        {
            if (width <= 0 || height <= 0) throw new ArgumentException("Board must have a positive size.");
            Width = width;
            Height = height;
            _arrows = new Arrow[arrows.Count];
            _removed = new bool[arrows.Count];
            _occupancy = new int[width * height];
            for (int i = 0; i < _occupancy.Length; i++) _occupancy[i] = -1;

            for (int i = 0; i < arrows.Count; i++)
            {
                var arrow = arrows[i];
                if (arrow.Id != i) throw new ArgumentException($"Arrow ids must be 0..n-1 in order (got {arrow.Id} at {i}).");
                _arrows[i] = arrow;
                foreach (var cell in arrow.Cells)
                {
                    if (!IsInside(cell)) throw new ArgumentException($"Arrow {i} leaves the board at {cell}.");
                    int index = Index(cell);
                    if (_occupancy[index] >= 0) throw new ArgumentException($"Arrows {_occupancy[index]} and {i} overlap at {cell}.");
                    _occupancy[index] = i;
                }
            }
            RemainingCount = arrows.Count;
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<Arrow> Arrows => _arrows;
        public int RemainingCount { get; private set; }

        public bool IsInside(Cell cell) => cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;
        public bool IsRemoved(int arrowId) => _removed[arrowId];

        /// <summary>Id of the arrow covering <paramref name="cell"/>, or -1.</summary>
        public int ArrowAt(Cell cell) => IsInside(cell) ? _occupancy[Index(cell)] : -1;

        /// <summary>Simulates the snake move of an arrow without changing the board.</summary>
        public MoveCheck Check(int arrowId)
        {
            if (_removed[arrowId]) throw new InvalidOperationException($"Arrow {arrowId} is already off the board.");

            var arrow = _arrows[arrowId];
            var cell = arrow.Head;
            int step = 0;
            while (true)
            {
                cell = cell.Step(arrow.Heading);
                step++;
                if (!IsInside(cell))
                {
                    // step-1 board cells ahead of the head, then the whole body has to follow.
                    return new MoveCheck(MoveKind.Escape, step - 1 + arrow.Length, -1, default);
                }

                int occupant = _occupancy[Index(cell)];
                if (occupant < 0) continue;
                if (occupant != arrowId) return new MoveCheck(MoveKind.Blocked, step - 1, occupant, cell);

                // Own body: body cell i is vacated after i+1 steps, the head arrives after `step` steps.
                if (arrow.IndexOf(cell) >= step) return new MoveCheck(MoveKind.Blocked, step - 1, arrowId, cell);
            }
        }

        public void Remove(int arrowId)
        {
            if (_removed[arrowId]) throw new InvalidOperationException($"Arrow {arrowId} is already removed.");
            _removed[arrowId] = true;
            foreach (var cell in _arrows[arrowId].Cells) _occupancy[Index(cell)] = -1;
            RemainingCount--;
        }

        public void Restore(int arrowId)
        {
            if (!_removed[arrowId]) throw new InvalidOperationException($"Arrow {arrowId} is on the board.");
            foreach (var cell in _arrows[arrowId].Cells)
                if (_occupancy[Index(cell)] >= 0) throw new InvalidOperationException($"Cannot restore arrow {arrowId}: {cell} is taken.");
            _removed[arrowId] = false;
            foreach (var cell in _arrows[arrowId].Cells) _occupancy[Index(cell)] = arrowId;
            RemainingCount++;
        }

        /// <summary>Ids of the arrows that can leave right now, in id order.</summary>
        public List<int> GetFreeArrows()
        {
            var result = new List<int>();
            for (int i = 0; i < _arrows.Length; i++)
                if (!_removed[i] && Check(i).IsFree) result.Add(i);
            return result;
        }

        public ArrowBoard Clone()
        {
            var copy = new ArrowBoard(Width, Height, _arrows);
            for (int i = 0; i < _arrows.Length; i++)
                if (_removed[i]) copy.Remove(i);
            return copy;
        }

        private int Index(Cell cell) => cell.Y * Width + cell.X;
    }
}
