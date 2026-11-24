using System;
using System.Collections;
using System.Collections.Generic;
using ArrowSlide.Model;
using PuzzlePair;
using PuzzlePair.Fx;
using UnityEngine;

namespace ArrowSlide
{
    /// <summary>
    /// Presents an <see cref="ArrowBoard"/>: the board panel, the dot grid and one
    /// <see cref="ArrowView"/> per arrow. It never changes the model; the game controller
    /// tells it what happened and it plays the matching animation.
    /// </summary>
    public sealed class ArrowBoardView : MonoBehaviour
    {
        [SerializeField] private ArrowTheme _theme;
        [SerializeField] private float _cellSize = 1f;

        private readonly List<ArrowView> _arrows = new List<ArrowView>();
        private readonly List<SpriteRenderer> _dots = new List<SpriteRenderer>();
        private SpriteRenderer _panel;
        private SpriteRenderer _panelShadow;
        private ArrowBoard _board;
        private int _hinted = -1;

        public ArrowTheme Theme => _theme;
        public float CellSize => _cellSize;

        /// <summary>World rectangle of the board including room for arrowheads.</summary>
        public Rect Bounds
        {
            get
            {
                if (_board == null) return new Rect(-1f, -1f, 2f, 2f);
                // Includes the board panel around the cells.
                float w = (_board.Width + 0.9f) * _cellSize, h = (_board.Height + 0.9f) * _cellSize;
                return new Rect(-w * 0.5f, -h * 0.5f, w, h);
            }
        }

        public bool IsAnimating
        {
            get
            {
                foreach (var a in _arrows)
                    if (a.Busy && !a.Escaped) return true;
                return false;
            }
        }

        public Vector3 CellToWorld(Cell cell) => transform.TransformPoint(CellToLocal(cell));

        private Vector2 CellToLocal(Cell cell) =>
            new Vector2((cell.X - (_board.Width - 1) * 0.5f) * _cellSize, (cell.Y - (_board.Height - 1) * 0.5f) * _cellSize);

        // ------------------------------------------------------------------ build

        public void Build(ArrowBoard board)
        {
            Clear();
            _board = board;
            BuildPanel();
            BuildDots();

            int extra = board.Width + board.Height + 16;
            foreach (var arrow in board.Arrows)
            {
                var trail = new List<Vector2>(arrow.Length + extra);
                foreach (var cell in arrow.Cells) trail.Add(CellToLocal(cell));
                var step = new Vector2(arrow.Heading.Dx(), arrow.Heading.Dy()) * _cellSize;
                var head = trail[trail.Count - 1];
                for (int i = 1; i <= extra; i++) trail.Add(head + step * i);

                var go = new GameObject($"Arrow {arrow.Id}");
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<ArrowView>();
                view.Init(arrow.Id, trail, arrow.Length, _cellSize, _theme, 10);
                _arrows.Add(view);
            }
        }

        private void BuildPanel()
        {
            var size = new Vector2(_board.Width + 0.9f, _board.Height + 0.9f) * _cellSize;
            _panelShadow = MakeSliced("PanelShadow", _theme.Shadow, -3, size, new Vector2(0f, -0.14f * _cellSize));
            _panel = MakeSliced("Panel", _theme.BoardFill, -2, size, Vector2.zero);
        }

        private SpriteRenderer MakeSliced(string name, Color color, int order, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = _theme.BoardSprite;
            r.drawMode = SpriteDrawMode.Sliced;
            r.size = size;
            r.color = color;
            r.sortingOrder = order;
            return r;
        }

        private void BuildDots()
        {
            for (int y = 0; y < _board.Height; y++)
            for (int x = 0; x < _board.Width; x++)
            {
                var go = new GameObject("Dot");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = CellToLocal(new Cell(x, y));
                go.transform.localScale = Vector3.one * (_theme.DotSize * _cellSize);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = _theme.DotSprite;
                r.color = _theme.Dot;
                r.sortingOrder = 0;
                _dots.Add(r);
            }
        }

        public void Clear()
        {
            StopAllCoroutines(); // intro and move animations run on this component
            transform.localScale = Vector3.one;
            foreach (var a in _arrows)
                if (a != null) Destroy(a.gameObject);
            foreach (var d in _dots)
                if (d != null) Destroy(d.gameObject);
            if (_panel != null) Destroy(_panel.gameObject);
            if (_panelShadow != null) Destroy(_panelShadow.gameObject);
            _arrows.Clear();
            _dots.Clear();
            _hinted = -1;
            _board = null;
        }

        /// <summary>Board panel pops in, dots fade, arrows draw themselves from the centre outwards.</summary>
        public IEnumerator PlayIntro()
        {
            transform.localScale = Vector3.one * 0.92f;
            StartCoroutine(Tween.Run(0.4f, t => transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, Easing.OutBack(t))));

            foreach (var d in _dots)
            {
                var c = d.color;
                c.a = 0f;
                d.color = c;
            }
            StartCoroutine(Tween.Run(0.35f, t =>
            {
                foreach (var d in _dots)
                {
                    var c = d.color;
                    c.a = t;
                    d.color = c;
                }
            }));

            float longest = 0f;
            foreach (var view in _arrows)
            {
                var head = _board.Arrows[view.Id].Head;
                float distance = CellToLocal(head).magnitude / _cellSize;
                float delay = 0.08f + distance * 0.035f;
                longest = Mathf.Max(longest, delay);
                StartCoroutine(view.Appear(delay));
            }
            yield return new WaitForSeconds(longest + 0.3f);
        }

        // ------------------------------------------------------------------ input

        /// <summary>Arrow under a world point (forgiving: the nearest arrow cell within ~0.7 cells), or -1.</summary>
        public int HitTest(Vector3 world)
        {
            if (_board == null) return -1;
            Vector2 local = transform.InverseTransformPoint(world);
            float fx = local.x / _cellSize + (_board.Width - 1) * 0.5f;
            float fy = local.y / _cellSize + (_board.Height - 1) * 0.5f;
            int cx = Mathf.RoundToInt(fx), cy = Mathf.RoundToInt(fy);

            int best = -1;
            float bestDistance = 0.7f;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                var cell = new Cell(cx + dx, cy + dy);
                int id = _board.ArrowAt(cell);
                if (id < 0) continue;
                float d = new Vector2(fx - cell.X, fy - cell.Y).magnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = id;
                }
            }
            return best;
        }

        public bool IsBusy(int arrowId) => _arrows[arrowId].Busy;

        // ------------------------------------------------------------------ moves

        public void PlayEscape(int arrowId, MoveCheck move, Action<Vector3, Vector2> onLeaveBoard)
        {
            if (_hinted == arrowId) ClearHint();
            var arrow = _board.Arrows[arrowId];
            int insideAhead = move.Distance - arrow.Length;
            var view = _arrows[arrowId];
            view.transform.SetAsLastSibling();
            StartCoroutine(view.Escape(move.Distance, insideAhead, 14f, onLeaveBoard));
        }

        public void PlayBump(int arrowId, MoveCheck move, Action<Vector3> onImpact)
        {
            var arrow = _board.Arrows[arrowId];
            var view = _arrows[arrowId];
            var direction = new Vector2(arrow.Heading.Dx(), arrow.Heading.Dy());
            Vector3 impactPoint = CellToWorld(move.BlockCell) - (Vector3)direction * (0.5f * _cellSize);
            StartCoroutine(view.Bump(move.Distance, () =>
            {
                if (move.BlockerId != arrowId) _arrows[move.BlockerId].Jolt(direction);
                if (move.BlockerId != arrowId) _arrows[move.BlockerId].Flash(_theme.Blocked, 0.45f);
                onImpact?.Invoke(impactPoint);
            }));
        }

        public void ShowHint(int arrowId)
        {
            ClearHint();
            _hinted = arrowId;
            _arrows[arrowId].SetHint(true);
            FxPool.Instance?.Flash(_arrows[arrowId].HeadWorldPosition, _theme.Hint, 0.3f * _cellSize, 2.2f * _cellSize, 0.5f);
        }

        public void ClearHint()
        {
            if (_hinted >= 0 && _hinted < _arrows.Count) _arrows[_hinted].SetHint(false);
            _hinted = -1;
        }

        public Vector3 HeadPosition(int arrowId) => _arrows[arrowId].HeadWorldPosition;

        /// <summary>Win: the empty board bounces and twinkles.</summary>
        public void Celebrate()
        {
            StartCoroutine(Tween.Punch(transform, 0.05f, 0.45f, Vector3.one));
            var fx = FxPool.Instance;
            if (fx == null || _board == null) return;
            var b = Bounds;
            for (int i = 0; i < 14; i++)
            {
                var p = new Vector2(UnityEngine.Random.Range(b.xMin, b.xMax), UnityEngine.Random.Range(b.yMin, b.yMax));
                fx.Sparkles(transform.TransformPoint(p), i % 2 == 0 ? Color.white : new Color(1f, 0.85f, 0.35f), 1, 0.3f, 0.7f * _cellSize, 0.9f);
            }
        }
    }
}
