namespace PixelParade.Model
{
    public enum BeltSide
    {
        Corner,
        Left,
        Top,
        Right,
        Bottom,
    }

    /// <summary>A stop on the belt. Side nodes face one row (Left/Right) or column (Top/Bottom) of the picture.</summary>
    public readonly struct BeltNode
    {
        /// <summary>Grid position just outside the picture (x in -1..Width, y in -1..Height).</summary>
        public readonly int X;
        public readonly int Y;
        public readonly BeltSide Side;
        /// <summary>Row (Left/Right) or column (Top/Bottom) the node looks along; -1 on corners.</summary>
        public readonly int Line;

        public BeltNode(int x, int y, BeltSide side, int line)
        {
            X = x;
            Y = y;
            Side = side;
            Line = line;
        }
    }

    /// <summary>
    /// The loop around the picture, one node per row/column plus four corners.
    /// Riders enter at the bottom-left corner (node 0) and travel counter-clockwise:
    /// right along the bottom, up the right side, left along the top, down the left side.
    /// </summary>
    public sealed class BeltPath
    {
        private readonly BeltNode[] _nodes;

        public BeltPath(int width, int height)
        {
            Width = width;
            Height = height;
            _nodes = new BeltNode[2 * width + 2 * height + 4];
            int i = 0;
            _nodes[i++] = new BeltNode(-1, -1, BeltSide.Corner, -1);
            for (int x = 0; x < width; x++) _nodes[i++] = new BeltNode(x, -1, BeltSide.Bottom, x);
            _nodes[i++] = new BeltNode(width, -1, BeltSide.Corner, -1);
            for (int y = 0; y < height; y++) _nodes[i++] = new BeltNode(width, y, BeltSide.Right, y);
            _nodes[i++] = new BeltNode(width, height, BeltSide.Corner, -1);
            for (int x = width - 1; x >= 0; x--) _nodes[i++] = new BeltNode(x, height, BeltSide.Top, x);
            _nodes[i++] = new BeltNode(-1, height, BeltSide.Corner, -1);
            for (int y = height - 1; y >= 0; y--) _nodes[i++] = new BeltNode(-1, y, BeltSide.Left, y);
        }

        public int Width { get; }
        public int Height { get; }

        /// <summary>Nodes per lap.</summary>
        public int Count => _nodes.Length;

        public BeltNode this[int index] => _nodes[index];
    }
}
