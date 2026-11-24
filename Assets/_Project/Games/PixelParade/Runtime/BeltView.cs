using System.Collections.Generic;
using UnityEngine;

namespace PixelParade
{
    /// <summary>
    /// The conveyor around the picture: a rounded track drawn with a LineRenderer and chevrons
    /// that scroll at exactly the riders' speed, so the belt looks like it carries them.
    /// </summary>
    public sealed class BeltView : MonoBehaviour
    {
        private const int CornerSegments = 8;

        private readonly List<Vector2> _path = new List<Vector2>();
        private readonly List<float> _lengths = new List<float>();
        private readonly List<Transform> _chevrons = new List<Transform>();
        private LineRenderer _track;
        private LineRenderer _edge;
        private PixelTheme _theme;
        private float _total;
        private float _scroll;
        private float _speed;

        public bool Running { get; set; } = true;

        public void Build(PixelLayout layout, PixelTheme theme)
        {
            Clear();
            _theme = theme;
            _speed = 1f / Mathf.Max(0.01f, theme.TickInterval); // one node (about one unit) per tick
            BuildPath(layout.HalfBeltX, layout.HalfBeltY, theme.BeltGap * 0.75f);

            _edge = CreateLine("Edge", theme.Shadow, -6, theme.BeltWidth + 0.16f, new Vector3(0f, -0.12f, 0f));
            _track = CreateLine("Track", theme.Belt, -5, theme.BeltWidth, Vector3.zero);

            int count = Mathf.FloorToInt(_total / 0.9f);
            float spacing = _total / count;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Chevron");
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * (theme.BeltWidth * 0.42f);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = theme.Chevron;
                r.color = theme.BeltStripe;
                r.sortingOrder = -4;
                _chevrons.Add(go.transform);
            }
            _scroll = 0f;
            Place(spacing);
        }

        public void Clear()
        {
            foreach (var c in _chevrons)
                if (c != null) Destroy(c.gameObject);
            _chevrons.Clear();
            if (_track != null) Destroy(_track.gameObject);
            if (_edge != null) Destroy(_edge.gameObject);
            _path.Clear();
            _lengths.Clear();
        }

        /// <summary>Counter-clockwise rounded rectangle starting at the bottom-left, like the belt nodes.</summary>
        private void BuildPath(float hx, float hy, float radius)
        {
            void Arc(Vector2 centre, float fromDeg, float toDeg)
            {
                for (int i = 0; i <= CornerSegments; i++)
                {
                    float a = Mathf.Lerp(fromDeg, toDeg, i / (float)CornerSegments) * Mathf.Deg2Rad;
                    _path.Add(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
            }

            Arc(new Vector2(-hx + radius, -hy + radius), 180f, 270f); // bottom-left, heading right
            Arc(new Vector2(hx - radius, -hy + radius), 270f, 360f);
            Arc(new Vector2(hx - radius, hy - radius), 0f, 90f);
            Arc(new Vector2(-hx + radius, hy - radius), 90f, 180f);
            _path.Add(_path[0]);

            _lengths.Add(0f);
            for (int i = 1; i < _path.Count; i++) _lengths.Add(_lengths[i - 1] + Vector2.Distance(_path[i - 1], _path[i]));
            _total = _lengths[_lengths.Count - 1];
        }

        private LineRenderer CreateLine(string name, Color color, int order, float width, Vector3 offset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = offset;
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.sharedMaterial = _theme.LineMaterial;
            line.widthMultiplier = width;
            line.alignment = LineAlignment.TransformZ;
            line.numCornerVertices = 4;
            line.sortingOrder = order;
            line.startColor = line.endColor = color;
            line.positionCount = _path.Count - 1;
            for (int i = 0; i < _path.Count - 1; i++) line.SetPosition(i, _path[i]);
            return line;
        }

        private void Update()
        {
            if (_chevrons.Count == 0) return;
            if (Running) _scroll = Mathf.Repeat(_scroll + _speed * Time.deltaTime, _total);
            Place(_total / _chevrons.Count);
        }

        private void Place(float spacing)
        {
            for (int i = 0; i < _chevrons.Count; i++)
            {
                float d = Mathf.Repeat(_scroll + i * spacing, _total);
                Evaluate(d, out var position, out var tangent);
                _chevrons[i].localPosition = position;
                _chevrons[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
            }
        }

        private void Evaluate(float distance, out Vector2 position, out Vector2 tangent)
        {
            int lo = 0, hi = _lengths.Count - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (_lengths[mid] <= distance) lo = mid;
                else hi = mid;
            }
            float span = _lengths[hi] - _lengths[lo];
            float t = span > 1e-5f ? (distance - _lengths[lo]) / span : 0f;
            position = Vector2.Lerp(_path[lo], _path[hi], t);
            tangent = (_path[hi] - _path[lo]).normalized;
        }
    }
}
