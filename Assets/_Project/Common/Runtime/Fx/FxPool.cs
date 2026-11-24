using UnityEngine;

namespace PuzzlePair.Fx
{
    public enum FxShape
    {
        Circle,
        Square,
        Ring,
        /// <summary>Bevelled cube, for pixel debris.</summary>
        Block,
        /// <summary>Four-point sparkle.</summary>
        Spark,
    }

    /// <summary>
    /// Fixed-capacity sprite particle system for bursts, debris, sparkles, speed streaks and confetti.
    /// One pre-allocated array of particles and renderers, swap-back removal: no allocation and
    /// no Instantiate after Awake, which keeps hundreds of pixel pops per second cheap.
    /// </summary>
    public sealed class FxPool : MonoBehaviour
    {
        [SerializeField] private Sprite _circle;
        [SerializeField] private Sprite _square;
        [SerializeField] private Sprite _ring;
        [SerializeField] private Sprite _block;
        [SerializeField] private Sprite _spark;
        [SerializeField] private int _capacity = 500;
        [SerializeField] private int _sortingOrder = 60;

        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Rotation;
            public float Spin;
            public float Age;
            public float Life;
            public float StartSize;
            public float EndSize;
            public float Gravity;
            public float Drag;
            public Color Color;
            public bool Flutter;
            /// <summary>Length/width ratio; 0 or 1 = round.</summary>
            public float Stretch;
            /// <summary>Rotate along the velocity (streaks).</summary>
            public bool Align;
            /// <summary>Fade in quickly instead of starting at full alpha (sparkles twinkle).</summary>
            public bool Twinkle;
        }

        private Particle[] _particles;
        private SpriteRenderer[] _renderers;
        private Transform[] _transforms;
        private int _count;

        public static FxPool Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            _particles = new Particle[_capacity];
            _renderers = new SpriteRenderer[_capacity];
            _transforms = new Transform[_capacity];
            for (int i = 0; i < _capacity; i++)
            {
                var go = new GameObject("p");
                go.transform.SetParent(transform, false);
                var r = go.AddComponent<SpriteRenderer>();
                r.sortingOrder = _sortingOrder;
                r.enabled = false;
                _renderers[i] = r;
                _transforms[i] = go.transform;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Radial burst of particles that fly out, fall, spin and shrink.</summary>
        public void Burst(Vector2 position, Color color, int count, float speed, float size,
            float life = 0.55f, float gravity = -14f, FxShape shape = FxShape.Square, Vector2 direction = default, float spreadDegrees = 360f)
        {
            float baseAngle = direction == Vector2.zero ? 0f : Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float angle = (baseAngle + Random.Range(-spreadDegrees, spreadDegrees) * 0.5f) * Mathf.Deg2Rad;
                float s = speed * Random.Range(0.45f, 1f);
                Spawn(new Particle
                {
                    Position = position,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * s,
                    Rotation = Random.Range(0f, 360f),
                    Spin = Random.Range(-540f, 540f),
                    Life = life * Random.Range(0.7f, 1.2f),
                    StartSize = size * Random.Range(0.6f, 1.15f),
                    EndSize = 0f,
                    Gravity = gravity,
                    Drag = 2.2f,
                    Color = color,
                }, shape);
            }
        }

        /// <summary>A single expanding, fading shape (shockwave ring or soft flash).</summary>
        public void Flash(Vector2 position, Color color, float startSize, float endSize, float life = 0.3f, FxShape shape = FxShape.Ring)
        {
            Spawn(new Particle
            {
                Position = position,
                Life = life,
                StartSize = startSize,
                EndSize = endSize,
                Color = color,
            }, shape);
        }

        /// <summary>Twinkling four-point sparkles scattered around a point.</summary>
        public void Sparkles(Vector2 position, Color color, int count, float radius, float size, float life = 0.6f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                Spawn(new Particle
                {
                    Position = position + offset,
                    Velocity = offset.normalized * Random.Range(0.3f, 1.2f) + Vector2.up * 0.6f,
                    Rotation = Random.Range(0f, 45f),
                    Spin = Random.Range(-90f, 90f),
                    Life = life * Random.Range(0.6f, 1.2f),
                    StartSize = size * Random.Range(0.6f, 1.2f),
                    EndSize = 0f,
                    Drag = 1.5f,
                    Color = color,
                    Twinkle = true,
                }, FxShape.Spark);
            }
        }

        /// <summary>Thin speed lines shooting along a direction (fast exits).</summary>
        public void Streaks(Vector2 position, Vector2 direction, Color color, int count, float speed, float length, float width, float life = 0.22f)
        {
            Vector2 dir = direction.normalized;
            Vector2 side = new Vector2(-dir.y, dir.x);
            for (int i = 0; i < count; i++)
            {
                Spawn(new Particle
                {
                    Position = position + side * Random.Range(-1f, 1f) * length * 0.35f - dir * Random.Range(0f, length * 0.5f),
                    Velocity = dir * speed * Random.Range(0.7f, 1.1f),
                    Life = life * Random.Range(0.7f, 1.2f),
                    StartSize = width,
                    EndSize = width * 0.4f,
                    Drag = 2f,
                    Color = color,
                    Stretch = length / Mathf.Max(0.001f, width) * Random.Range(0.6f, 1f),
                    Align = true,
                }, FxShape.Square);
            }
        }

        /// <summary>Confetti raining down over a world rectangle (top edge spawn).</summary>
        public void Confetti(Rect area, int count, Color[] palette)
        {
            for (int i = 0; i < count; i++)
            {
                var c = palette[Random.Range(0, palette.Length)];
                Spawn(new Particle
                {
                    Position = new Vector2(Random.Range(area.xMin, area.xMax), area.yMax + Random.Range(0f, area.height * 0.25f)),
                    Velocity = new Vector2(Random.Range(-2f, 2f), Random.Range(-1f, 4f)),
                    Rotation = Random.Range(0f, 360f),
                    Spin = Random.Range(-360f, 360f),
                    Life = Random.Range(2.2f, 3.4f),
                    StartSize = area.width * Random.Range(0.016f, 0.026f),
                    EndSize = area.width * 0.012f,
                    Gravity = -area.height * 0.42f,
                    Drag = 1.6f,
                    Color = c,
                    Flutter = true,
                }, Random.value < 0.75f ? FxShape.Square : FxShape.Circle);
            }
        }

        /// <summary>Burst of confetti from a point (fountain), used for level-complete pops.</summary>
        public void ConfettiBurst(Vector2 position, int count, float speed, float size, Color[] palette)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(40f, 140f) * Mathf.Deg2Rad;
                Spawn(new Particle
                {
                    Position = position,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed * Random.Range(0.5f, 1f),
                    Rotation = Random.Range(0f, 360f),
                    Spin = Random.Range(-600f, 600f),
                    Life = Random.Range(1.6f, 2.6f),
                    StartSize = size * Random.Range(0.7f, 1.2f),
                    EndSize = size * 0.5f,
                    Gravity = -speed * 1.1f,
                    Drag = 1.8f,
                    Color = palette[Random.Range(0, palette.Length)],
                    Flutter = true,
                }, Random.value < 0.75f ? FxShape.Square : FxShape.Circle);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _count; i++) _renderers[i].enabled = false;
            _count = 0;
        }

        private Sprite SpriteFor(FxShape shape)
        {
            switch (shape)
            {
                case FxShape.Circle: return _circle;
                case FxShape.Ring: return _ring;
                case FxShape.Block: return _block != null ? _block : _square;
                case FxShape.Spark: return _spark != null ? _spark : _circle;
                default: return _square;
            }
        }

        private void Spawn(Particle p, FxShape shape)
        {
            if (_particles == null) return;
            int index;
            if (_count < _capacity) index = _count++;
            else index = Random.Range(0, _capacity); // pool full: recycle a random live particle

            _particles[index] = p;
            var r = _renderers[index];
            r.sprite = SpriteFor(shape);
            r.color = p.Color;
            r.enabled = true;
            Apply(index);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _count - 1; i >= 0; i--)
            {
                ref var p = ref _particles[i];
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    RemoveAt(i);
                    continue;
                }

                p.Velocity.y += p.Gravity * dt;
                p.Velocity *= Mathf.Exp(-p.Drag * dt);
                if (p.Flutter) p.Velocity.x += Mathf.Sin((p.Age + i) * 7f) * 6f * dt;
                p.Position += p.Velocity * dt;
                p.Rotation += p.Spin * dt;
                Apply(i);
            }
        }

        private void Apply(int i)
        {
            ref var p = ref _particles[i];
            float t = p.Life > 0f ? p.Age / p.Life : 1f;
            float size = Mathf.Lerp(p.StartSize, p.EndSize, Easing.OutQuad(t));
            if (p.Twinkle) size *= Mathf.Clamp01(t * 6f) * (0.85f + 0.15f * Mathf.Sin(p.Age * 30f));
            var tr = _transforms[i];
            tr.localPosition = p.Position;
            float rotation = p.Align && p.Velocity.sqrMagnitude > 1e-6f ? Mathf.Atan2(p.Velocity.y, p.Velocity.x) * Mathf.Rad2Deg : p.Rotation;
            tr.localRotation = Quaternion.Euler(0f, 0f, rotation);
            float flutter = p.Flutter ? Mathf.Abs(Mathf.Cos(p.Age * 9f + i)) * 0.7f + 0.3f : 1f;
            float stretch = p.Stretch > 1f ? p.Stretch : 1f;
            tr.localScale = new Vector3(size * flutter * stretch, size, 1f);
            var c = p.Color;
            c.a *= 1f - t * t;
            _renderers[i].color = c;
        }

        private void RemoveAt(int i)
        {
            int last = _count - 1;
            if (i != last)
            {
                _particles[i] = _particles[last];
                _renderers[i].sprite = _renderers[last].sprite;
                Apply(i);
            }
            _renderers[last].enabled = false;
            _count--;
        }
    }
}
