using System;
using System.Collections;
using System.Collections.Generic;
using PuzzlePair;
using UnityEngine;

namespace ArrowSlide
{
    /// <summary>
    /// Draws one arrow as a rounded polyline plus an arrowhead and animates it like a snake.
    /// The arrow lives on a "trail": its own cells followed by a straight run in its heading.
    /// <see cref="Offset"/> slides the visible window along that trail, so escaping, bumping
    /// and the intro are all just animations of one number.
    /// </summary>
    public sealed class ArrowView : MonoBehaviour
    {
        private ArrowTheme _theme;
        private LineRenderer _line;
        private LineRenderer _shadowLine;
        private SpriteRenderer _head;
        private SpriteRenderer _headShadow;
        private LineRenderer _glowLine;
        private LineRenderer _speedTrail;
        private Vector3[] _trail;
        private int _length;
        private float _cellSize;
        private float _offset;
        private float _reveal = 1f;
        private Vector3 _shadowOffset;
        private Coroutine _colorRoutine;
        private Coroutine _hintRoutine;
        private Coroutine _joltRoutine;

        public int Id { get; private set; }
        /// <summary>Playing a move: taps on it are ignored meanwhile.</summary>
        public bool Busy { get; private set; }
        public bool Escaped { get; private set; }
        public float Offset => _offset;

        public void Init(int id, IReadOnlyList<Vector2> trail, int length, float cellSize, ArrowTheme theme, int sortingOrder)
        {
            Id = id;
            _theme = theme;
            _length = length;
            _cellSize = cellSize;
            _trail = new Vector3[trail.Count];
            for (int i = 0; i < trail.Count; i++) _trail[i] = trail[i];
            _shadowOffset = theme.ShadowOffset * cellSize;

            // Every shadow sits below every arrow so neighbours never get shaded.
            _shadowLine = CreateLine("Shadow", theme.Shadow, sortingOrder - 6);
            _headShadow = CreateHead("HeadShadow", theme.Shadow, sortingOrder - 5);
            _line = CreateLine("Line", theme.Ink, sortingOrder);
            _head = CreateHead("Head", theme.Ink, sortingOrder + 1);

            // Soft halo under a hinted arrow.
            _glowLine = CreateLine("Glow", new Color(theme.Hint.r, theme.Hint.g, theme.Hint.b, 0.25f), sortingOrder - 1);
            _glowLine.widthMultiplier = theme.LineWidth * cellSize * 3.4f;
            _glowLine.enabled = false;

            // Fading speed trail behind the tail while escaping.
            _speedTrail = CreateLine("Trail", theme.Ink, sortingOrder - 1);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(theme.Ink, 0f), new GradientColorKey(theme.Ink, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.35f, 1f) });
            _speedTrail.colorGradient = gradient;
            _speedTrail.widthCurve = AnimationCurve.Linear(0f, 0.25f, 1f, 1f);
            _speedTrail.enabled = false;
            Redraw();
        }

        // ------------------------------------------------------------------ drawing

        private LineRenderer CreateLine(string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = _theme.LineMaterial;
            line.widthMultiplier = _theme.LineWidth * _cellSize;
            line.numCornerVertices = 6;
            line.numCapVertices = 6;
            line.alignment = LineAlignment.TransformZ;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sortingOrder = order;
            line.startColor = line.endColor = color;
            return line;
        }

        private SpriteRenderer CreateHead(string name, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = _theme.HeadSprite;
            r.color = color;
            r.sortingOrder = order;
            go.transform.localScale = Vector3.one * (_theme.HeadSize * _cellSize);
            return r;
        }

        /// <summary>Moves the arrow <paramref name="offset"/> cells along its trail (negative = pulled back).</summary>
        public void SetOffset(float offset)
        {
            _offset = offset;
            Redraw();
        }

        /// <summary>Intro: grows the body from the tail (0) to full length (1).</summary>
        public void SetReveal(float reveal)
        {
            _reveal = Mathf.Clamp01(reveal);
            bool visible = _reveal > 0.001f;
            _line.enabled = _shadowLine.enabled = _head.enabled = _headShadow.enabled = visible;
            Redraw();
        }

        private void Redraw()
        {
            if (_line == null) return;
            float start = _offset;
            float end = _offset + (_length - 1) * _reveal;

            int count = 0;
            int first = Mathf.FloorToInt(start) + 1;
            int last = Mathf.CeilToInt(end) - 1;
            int capacity = 2 + Mathf.Max(0, last - first + 1);
            _line.positionCount = capacity;
            _shadowLine.positionCount = capacity;

            SetPoint(count++, PointAt(start));
            for (int k = first; k <= last; k++) SetPoint(count++, _trail[Mathf.Clamp(k, 0, _trail.Length - 1)]);
            SetPoint(count++, PointAt(end));
            if (count != capacity)
            {
                _line.positionCount = count;
                _shadowLine.positionCount = count;
            }

            Vector3 dir = DirectionAt(end);
            Vector3 tip = PointAt(end) + dir * (0.1f * _theme.HeadSize * _cellSize);
            var rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            _head.transform.localPosition = tip;
            _head.transform.localRotation = rotation;
            _headShadow.transform.localPosition = tip + _shadowOffset;
            _headShadow.transform.localRotation = rotation;
            if (_glowLine != null && _glowLine.enabled) FillLine(_glowLine, start, end);
        }

        /// <summary>Puts the trail section [start, end] (in cells along the trail) into a line.</summary>
        private void FillLine(LineRenderer line, float start, float end)
        {
            int first = Mathf.FloorToInt(start) + 1;
            int last = Mathf.CeilToInt(end) - 1;
            line.positionCount = 2 + Mathf.Max(0, last - first + 1);
            int n = 0;
            line.SetPosition(n++, PointAt(start));
            for (int k = first; k <= last; k++) line.SetPosition(n++, _trail[Mathf.Clamp(k, 0, _trail.Length - 1)]);
            line.SetPosition(n, PointAt(end));
        }

        private void SetPoint(int index, Vector3 p)
        {
            _line.SetPosition(index, p);
            _shadowLine.SetPosition(index, p + _shadowOffset);
        }

        private Vector3 PointAt(float arc)
        {
            int i = Mathf.Clamp(Mathf.FloorToInt(arc), 0, _trail.Length - 2);
            return Vector3.LerpUnclamped(_trail[i], _trail[i + 1], arc - i);
        }

        private Vector3 DirectionAt(float arc)
        {
            int i = Mathf.Clamp(Mathf.FloorToInt(arc - 0.0001f), 0, _trail.Length - 2);
            return (_trail[i + 1] - _trail[i]).normalized;
        }

        /// <summary>World position of the arrowhead (for effects).</summary>
        public Vector3 HeadWorldPosition => transform.TransformPoint(PointAt(_offset + (_length - 1) * _reveal));

        // ------------------------------------------------------------------ colour

        private void SetColor(Color color)
        {
            _line.startColor = _line.endColor = color;
            _head.color = color;
        }

        public void Flash(Color color, float duration)
        {
            if (_colorRoutine != null) StopCoroutine(_colorRoutine);
            _colorRoutine = StartCoroutine(FlashRoutine(color, duration));
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            yield return Tween.Run(duration, t =>
            {
                float k = t < 0.2f ? t / 0.2f : 1f - (t - 0.2f) / 0.8f;
                SetColor(Color.Lerp(BaseColor(), color, k));
            });
            SetColor(BaseColor());
            _colorRoutine = null;
        }

        private Color BaseColor() => _hintRoutine != null ? _theme.Hint : _theme.Ink;

        public void SetHint(bool on)
        {
            if (on == (_hintRoutine != null)) return;
            _glowLine.enabled = on;
            if (on)
            {
                _hintRoutine = StartCoroutine(HintRoutine());
                Redraw();
            }
            else
            {
                StopCoroutine(_hintRoutine);
                _hintRoutine = null;
                SetColor(_theme.Ink);
                _line.widthMultiplier = _theme.LineWidth * _cellSize;
            }
        }

        private IEnumerator HintRoutine()
        {
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime;
                float pulse = 0.5f + 0.5f * Mathf.Sin(time * 7f);
                if (_colorRoutine == null) SetColor(Color.Lerp(_theme.Hint, _theme.Ink, pulse * 0.25f));
                _line.widthMultiplier = _theme.LineWidth * _cellSize * (1f + 0.35f * pulse);
                _glowLine.widthMultiplier = _theme.LineWidth * _cellSize * (2.6f + 1.6f * pulse);
                var glow = _theme.Hint;
                glow.a = 0.14f + 0.16f * pulse;
                _glowLine.startColor = _glowLine.endColor = glow;
                yield return null;
            }
        }

        // ------------------------------------------------------------------ motion

        /// <summary>Intro: the arrow draws itself from its tail.</summary>
        public IEnumerator Appear(float delay)
        {
            SetReveal(0f);
            if (delay > 0f) yield return new WaitForSeconds(delay);
            yield return Tween.Run(0.28f, SetReveal, Easing.OutCubic);
        }

        /// <summary>
        /// Slides off the board: short anticipation, then accelerates. <paramref name="onLeaveBoard"/>
        /// fires when the head crosses the board edge.
        /// </summary>
        public IEnumerator Escape(int distance, int insideAhead, float exitExtra, Action<Vector3, Vector2> onLeaveBoard)
        {
            Busy = true;
            SetHint(false);
            yield return Tween.Run(0.05f, t => SetOffset(-0.14f * Easing.OutQuad(t)));
            _speedTrail.enabled = true;

            float offset = -0.14f;
            float speed = _theme.EscapeStartSpeed;
            float target = distance + exitExtra;
            float edge = insideAhead + 0.5f; // head arc offset when it leaves the last board cell
            bool left = false;
            while (offset < target)
            {
                float dt = Time.deltaTime;
                speed = Mathf.Min(speed + _theme.EscapeAcceleration * dt, _theme.EscapeMaxSpeed);
                offset += speed * dt;
                SetOffset(offset);
                float trail = Mathf.Min(3.5f, speed * 0.1f);
                FillLine(_speedTrail, Mathf.Max(0f, offset - trail), Mathf.Max(0.01f, offset));
                if (!left && offset >= edge)
                {
                    left = true;
                    Vector3 at = transform.TransformPoint(PointAt(edge + _length - 1));
                    onLeaveBoard?.Invoke(at, DirectionAt(edge + _length - 1));
                }
                yield return null;
            }

            Escaped = true;
            gameObject.SetActive(false);
        }

        /// <summary>Runs forward into the obstacle, calls <paramref name="onImpact"/>, then springs back.</summary>
        public IEnumerator Bump(int freeSteps, Action onImpact)
        {
            Busy = true;
            float forward = freeSteps + 0.3f;
            float outTime = 0.05f + forward / _theme.BumpSpeed;
            yield return Tween.Run(outTime, t => SetOffset(forward * t), Easing.InQuad);
            onImpact?.Invoke();
            Flash(_theme.Blocked, 0.55f);
            yield return Tween.Run(0.22f + forward * 0.025f, t => SetOffset(forward * (1f - t)), Easing.OutBack);
            SetOffset(0f);
            Busy = false;
        }

        /// <summary>Small knock when another arrow runs into this one.</summary>
        public void Jolt(Vector2 direction)
        {
            if (_joltRoutine != null) StopCoroutine(_joltRoutine);
            _joltRoutine = StartCoroutine(JoltRoutine(direction));
        }

        private IEnumerator JoltRoutine(Vector2 direction)
        {
            Vector3 push = (Vector3)direction.normalized * (0.12f * _cellSize);
            yield return Tween.Run(0.25f, t => transform.localPosition = push * Easing.Punch(t));
            transform.localPosition = Vector3.zero;
            _joltRoutine = null;
        }
    }
}
