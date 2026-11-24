using System;

namespace ArrowSlide.Model
{
    /// <summary>Grid direction. The board's y axis points up.</summary>
    public enum Dir
    {
        Up,
        Right,
        Down,
        Left,
    }

    public static class DirExtensions
    {
        public static int Dx(this Dir dir) => dir == Dir.Right ? 1 : dir == Dir.Left ? -1 : 0;
        public static int Dy(this Dir dir) => dir == Dir.Up ? 1 : dir == Dir.Down ? -1 : 0;

        public static Dir Opposite(this Dir dir) => (Dir)(((int)dir + 2) % 4);

        /// <summary>Direction of a unit step, e.g. (1, 0) is Right.</summary>
        public static Dir FromDelta(int dx, int dy)
        {
            if (dx == 1 && dy == 0) return Dir.Right;
            if (dx == -1 && dy == 0) return Dir.Left;
            if (dx == 0 && dy == 1) return Dir.Up;
            if (dx == 0 && dy == -1) return Dir.Down;
            throw new ArgumentException($"({dx}, {dy}) is not a unit grid step.");
        }
    }

    /// <summary>Integer grid coordinate; (0, 0) is the bottom-left cell.</summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X;
        public readonly int Y;

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Cell Step(Dir dir) => new Cell(X + dir.Dx(), Y + dir.Dy());

        public bool IsAdjacentTo(Cell other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y) == 1;

        public bool Equals(Cell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
        public override string ToString() => $"({X}, {Y})";
    }
}
