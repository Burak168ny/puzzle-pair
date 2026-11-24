using System;
using System.Collections.Generic;
using System.Text;

namespace ArrowSlide.Model
{
    /// <summary>
    /// Reads a board drawn as text, one string per row from top to bottom (spaces are ignored):
    /// <code>
    ///   .   empty cell
    ///   &gt; &lt; ^ v   arrow body: points to the next cell of the same arrow (towards the head)
    ///   R L U D   arrow head, pointing Right / Left / Up / Down
    /// </code>
    /// Every body cell must lead to a head; a head points along its last segment.
    /// The format is easy to draw by hand and easy to diff in code review.
    /// </summary>
    public static class ArrowLevelParser
    {
        public static ArrowBoard Parse(params string[] rows) => Parse((IReadOnlyList<string>)rows);

        public static ArrowBoard Parse(IReadOnlyList<string> rows)
        {
            if (rows == null || rows.Count == 0) throw new FormatException("A level needs at least one row.");

            var grid = new List<string>(rows.Count);
            foreach (var row in rows) grid.Add(row.Replace(" ", string.Empty));
            int height = grid.Count;
            int width = grid[0].Length;
            for (int r = 0; r < height; r++)
                if (grid[r].Length != width) throw new FormatException($"Row {r} has {grid[r].Length} cells, expected {width}.");

            char At(Cell c) => grid[height - 1 - c.Y][c.X];
            bool Inside(Cell c) => c.X >= 0 && c.X < width && c.Y >= 0 && c.Y < height;

            // predecessor[cell] = the body cell that points into it.
            var predecessor = new Dictionary<Cell, Cell>();
            var heads = new List<(Cell cell, Dir heading)>();
            int occupied = 0;

            for (int r = 0; r < height; r++)
            for (int x = 0; x < width; x++)
            {
                var cell = new Cell(x, height - 1 - r);
                char ch = At(cell);
                if (ch == '.') continue;
                occupied++;

                if (TryHead(ch, out var heading))
                {
                    heads.Add((cell, heading));
                    continue;
                }
                if (!TryBody(ch, out var dir)) throw new FormatException($"Unknown character '{ch}' at {cell}.");

                var next = cell.Step(dir);
                if (!Inside(next) || At(next) == '.')
                    throw new FormatException($"Body cell {cell} points at {next}, which is empty or off the board.");
                if (predecessor.ContainsKey(next))
                    throw new FormatException($"Two body cells point into {next}.");
                predecessor[next] = cell;
            }

            var arrows = new List<Arrow>(heads.Count);
            int used = 0;
            foreach (var (head, heading) in heads)
            {
                var cells = new List<Cell> { head };
                var current = head;
                while (predecessor.TryGetValue(current, out var previous))
                {
                    cells.Add(previous);
                    current = previous;
                    if (cells.Count > width * height) throw new FormatException($"Arrow ending at {head} loops.");
                }
                cells.Reverse(); // tail -> head
                if (cells.Count < 2) throw new FormatException($"The arrow head at {head} has no body.");
                used += cells.Count;
                arrows.Add(new Arrow(arrows.Count, cells, heading));
            }

            if (used != occupied)
                throw new FormatException($"{occupied - used} body cell(s) never reach a head (a loop or a stray body).");

            return new ArrowBoard(width, height, arrows);
        }

        /// <summary>Renders a board back to the text format (used by tests and the debug log).</summary>
        public static string[] Format(ArrowBoard board)
        {
            var chars = new char[board.Height, board.Width];
            for (int y = 0; y < board.Height; y++)
            for (int x = 0; x < board.Width; x++)
                chars[y, x] = '.';

            foreach (var arrow in board.Arrows)
            {
                if (board.IsRemoved(arrow.Id)) continue;
                for (int i = 0; i < arrow.Length; i++)
                {
                    var c = arrow.Cells[i];
                    if (i == arrow.Length - 1)
                    {
                        chars[c.Y, c.X] = HeadChar(arrow.Heading);
                        continue;
                    }
                    var n = arrow.Cells[i + 1];
                    chars[c.Y, c.X] = BodyChar(DirExtensions.FromDelta(n.X - c.X, n.Y - c.Y));
                }
            }

            var rows = new string[board.Height];
            for (int r = 0; r < board.Height; r++)
            {
                var sb = new StringBuilder(board.Width);
                for (int x = 0; x < board.Width; x++) sb.Append(chars[board.Height - 1 - r, x]);
                rows[r] = sb.ToString();
            }
            return rows;
        }

        private static bool TryHead(char ch, out Dir dir)
        {
            switch (ch)
            {
                case 'U': dir = Dir.Up; return true;
                case 'R': dir = Dir.Right; return true;
                case 'D': dir = Dir.Down; return true;
                case 'L': dir = Dir.Left; return true;
                default: dir = default; return false;
            }
        }

        private static bool TryBody(char ch, out Dir dir)
        {
            switch (ch)
            {
                case '^': dir = Dir.Up; return true;
                case '>': dir = Dir.Right; return true;
                case 'v': dir = Dir.Down; return true;
                case '<': dir = Dir.Left; return true;
                default: dir = default; return false;
            }
        }

        private static char HeadChar(Dir dir) => dir == Dir.Up ? 'U' : dir == Dir.Right ? 'R' : dir == Dir.Down ? 'D' : 'L';
        private static char BodyChar(Dir dir) => dir == Dir.Up ? '^' : dir == Dir.Right ? '>' : dir == Dir.Down ? 'v' : '<';
    }
}
