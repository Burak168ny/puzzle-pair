using PixelParade.Model;
using UnityEngine;

namespace PixelParade
{
    /// <summary>
    /// World positions of everything in a level: picture cells, belt nodes, slots and lanes.
    /// One place for the geometry, so views, input and effects always agree.
    /// </summary>
    public sealed class PixelLayout
    {
        private readonly PixelTheme _theme;
        private readonly Vector2[] _nodes;
        private readonly Vector2[] _inward;

        public PixelLayout(PixelSim sim, PixelTheme theme)
        {
            _theme = theme;
            Width = sim.Picture.Width;
            Height = sim.Picture.Height;
            LaneCount = sim.LaneCount;

            HalfBeltX = Width * 0.5f + theme.BeltGap;
            HalfBeltY = Height * 0.5f + theme.BeltGap;
            float corner = theme.BeltGap * 0.32f; // pulls corner nodes onto the rounded track

            var belt = sim.Belt;
            _nodes = new Vector2[belt.Count];
            _inward = new Vector2[belt.Count];
            for (int i = 0; i < belt.Count; i++)
            {
                var node = belt[i];
                switch (node.Side)
                {
                    case BeltSide.Left:
                        _nodes[i] = new Vector2(-HalfBeltX, CellY(node.Line));
                        _inward[i] = Vector2.right;
                        break;
                    case BeltSide.Right:
                        _nodes[i] = new Vector2(HalfBeltX, CellY(node.Line));
                        _inward[i] = Vector2.left;
                        break;
                    case BeltSide.Top:
                        _nodes[i] = new Vector2(CellX(node.Line), HalfBeltY);
                        _inward[i] = Vector2.down;
                        break;
                    case BeltSide.Bottom:
                        _nodes[i] = new Vector2(CellX(node.Line), -HalfBeltY);
                        _inward[i] = Vector2.up;
                        break;
                    default:
                        var sign = new Vector2(node.X < 0 ? -1f : 1f, node.Y < 0 ? -1f : 1f);
                        _nodes[i] = new Vector2(sign.x * HalfBeltX, sign.y * HalfBeltY) - sign * corner;
                        _inward[i] = -sign.normalized;
                        break;
                }
            }

            SlotsY = -HalfBeltY - theme.BeltWidth * 0.5f - theme.SlotSpacing * 0.75f;
            QueueTopY = SlotsY - theme.SlotSpacing * 0.5f - theme.QueueSpacing * 0.85f;
        }

        public int Width { get; }
        public int Height { get; }
        public int LaneCount { get; }
        public float HalfBeltX { get; }
        public float HalfBeltY { get; }
        public float SlotsY { get; }
        public float QueueTopY { get; }
        public int NodeCount => _nodes.Length;

        public float CellX(int x) => x - (Width - 1) * 0.5f;
        public float CellY(int y) => y - (Height - 1) * 0.5f;
        public Vector2 Cell(int x, int y) => new Vector2(CellX(x), CellY(y));

        public Vector2 Node(int index) => _nodes[((index % _nodes.Length) + _nodes.Length) % _nodes.Length];
        public Vector2 Inward(int index) => _inward[((index % _inward.Length) + _inward.Length) % _inward.Length];

        public Vector2 Slot(int slot, int slotCount) =>
            new Vector2((slot - (slotCount - 1) * 0.5f) * _theme.SlotSpacing, SlotsY);

        public Vector2 Lane(int lane, int depth) =>
            new Vector2((lane - (LaneCount - 1) * 0.5f) * _theme.LaneSpacing, QueueTopY - depth * _theme.QueueSpacing);

        /// <summary>Where a tapped shooter waits until the entry is clear.</summary>
        public Vector2 Waiting(int index) => Node(0) + new Vector2(-0.45f - index * 0.3f, -1.1f - index * 0.2f);

        /// <summary>Everything the camera must show.</summary>
        public Rect Bounds(int slotCount)
        {
            float halfWidth = Mathf.Max(HalfBeltX + _theme.BeltWidth * 0.5f + 0.2f,
                Mathf.Max(slotCount * _theme.SlotSpacing, LaneCount * _theme.LaneSpacing) * 0.5f + 0.2f);
            float top = HalfBeltY + _theme.BeltWidth * 0.5f + 0.2f;
            float bottom = QueueTopY - (_theme.VisiblePerLane - 1) * _theme.QueueSpacing - _theme.ShooterSize * 0.7f;
            return new Rect(-halfWidth, bottom, halfWidth * 2f, top - bottom);
        }
    }
}
