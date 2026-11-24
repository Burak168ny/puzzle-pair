using System;
using System.Collections.Generic;

namespace ArrowSlide.Model
{
    /// <summary>
    /// A bent line of cells with an arrowhead. When tapped it moves like a snake: the head travels
    /// straight along <see cref="Heading"/> and the body follows its own path.
    /// </summary>
    public sealed class Arrow
    {
        private readonly Cell[] _cells;

        public Arrow(int id, IReadOnlyList<Cell> cellsTailToHead, Dir heading)
        {
            if (cellsTailToHead == null || cellsTailToHead.Count < 2)
                throw new ArgumentException("An arrow needs at least two cells.", nameof(cellsTailToHead));

            _cells = new Cell[cellsTailToHead.Count];
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = cellsTailToHead[i];
                if (i > 0 && !_cells[i].IsAdjacentTo(_cells[i - 1]))
                    throw new ArgumentException($"Arrow {id}: cells {_cells[i - 1]} and {_cells[i]} are not neighbours.");
            }

            var last = _cells[_cells.Length - 1];
            var beforeLast = _cells[_cells.Length - 2];
            if (DirExtensions.FromDelta(last.X - beforeLast.X, last.Y - beforeLast.Y) != heading)
                throw new ArgumentException($"Arrow {id}: the head must point along its last segment.");

            Id = id;
            Heading = heading;
        }

        public int Id { get; }
        public Dir Heading { get; }

        /// <summary>Cells from tail (index 0) to head.</summary>
        public IReadOnlyList<Cell> Cells => _cells;

        public int Length => _cells.Length;
        public Cell Head => _cells[_cells.Length - 1];
        public Cell Tail => _cells[0];

        /// <summary>Index of <paramref name="cell"/> in the body (0 = tail), or -1.</summary>
        public int IndexOf(Cell cell)
        {
            for (int i = 0; i < _cells.Length; i++)
                if (_cells[i] == cell) return i;
            return -1;
        }
    }
}
