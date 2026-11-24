using System;
using System.Collections.Generic;

namespace PixelParade.Model
{
    /// <summary>
    /// Colour keys used in level text. The model only knows indices; the view maps them to colours.
    /// R red, Y yellow, G green, B blue, O orange, P purple, K pink, W white, D dark, N brown, C cyan, L lime, T tan.
    /// </summary>
    public static class PixelPalette
    {
        public const string Keys = "RYGBOPKWDNCLT";
        public const char Empty = '.';

        public static int Count => Keys.Length;

        public static int IndexOf(char key)
        {
            int index = Keys.IndexOf(key);
            if (index < 0) throw new FormatException($"Unknown colour key '{key}'.");
            return index;
        }

        public static char KeyOf(int color) => Keys[color];
    }

    /// <summary>The picture in the middle of the belt: a grid of colour indices, -1 where empty. (0, 0) is bottom-left.</summary>
    public sealed class PixelPicture
    {
        private readonly int[] _cells;

        public PixelPicture(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentException("Picture must have a positive size.");
            Width = width;
            Height = height;
            _cells = new int[width * height];
            for (int i = 0; i < _cells.Length; i++) _cells[i] = -1;
        }

        public int Width { get; }
        public int Height { get; }

        /// <summary>Number of coloured (not yet destroyed) pixels.</summary>
        public int Count { get; private set; }

        public int Index(int x, int y) => y * Width + x;
        public int XOf(int index) => index % Width;
        public int YOf(int index) => index / Width;

        public int Get(int x, int y) => _cells[Index(x, y)];
        public int GetAt(int index) => _cells[index];

        public void Set(int x, int y, int color)
        {
            int index = Index(x, y);
            if (_cells[index] >= 0) Count--;
            _cells[index] = color;
            if (color >= 0) Count++;
        }

        public void ClearAt(int index)
        {
            if (_cells[index] < 0) throw new InvalidOperationException($"Pixel {index} is already empty.");
            _cells[index] = -1;
            Count--;
        }

        public int CountOf(int color)
        {
            int n = 0;
            foreach (int c in _cells)
                if (c == color) n++;
            return n;
        }

        public PixelPicture Clone()
        {
            var copy = new PixelPicture(Width, Height);
            for (int i = 0; i < _cells.Length; i++)
                if (_cells[i] >= 0) copy.Set(XOf(i), YOf(i), _cells[i]);
            return copy;
        }

        /// <summary>Rows from top to bottom; '.' is empty, letters are <see cref="PixelPalette"/> keys. Spaces are ignored.</summary>
        public static PixelPicture Parse(IReadOnlyList<string> rows)
        {
            if (rows == null || rows.Count == 0) throw new FormatException("A picture needs at least one row.");
            var clean = new List<string>(rows.Count);
            foreach (var row in rows) clean.Add(row.Replace(" ", string.Empty));
            int width = clean[0].Length;
            int height = clean.Count;
            var picture = new PixelPicture(width, height);
            for (int r = 0; r < height; r++)
            {
                if (clean[r].Length != width) throw new FormatException($"Row {r} has {clean[r].Length} pixels, expected {width}.");
                for (int x = 0; x < width; x++)
                {
                    char ch = clean[r][x];
                    if (ch != PixelPalette.Empty) picture.Set(x, height - 1 - r, PixelPalette.IndexOf(ch));
                }
            }
            return picture;
        }
    }
}
