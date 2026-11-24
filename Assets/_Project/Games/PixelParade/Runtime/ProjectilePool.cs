using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelParade
{
    /// <summary>Pooled bullets that fly from a shooter to the pixel the model already destroyed.</summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        private sealed class Shot
        {
            public SpriteRenderer Renderer;
            public Vector3 From;
            public Vector3 To;
            public float Time;
            public float Duration;
            public Action OnArrive;
        }

        private readonly List<Shot> _active = new List<Shot>();
        private readonly Stack<SpriteRenderer> _free = new Stack<SpriteRenderer>();
        private Sprite _sprite;
        private float _speed = 40f;

        public int InFlight => _active.Count;

        public void Setup(Sprite sprite, float speed)
        {
            _sprite = sprite;
            _speed = speed;
        }

        public void Fire(Vector3 from, Vector3 to, Color color, Action onArrive)
        {
            var r = _free.Count > 0 ? _free.Pop() : Create();
            r.gameObject.SetActive(true);
            r.color = Color.Lerp(color, Color.white, 0.35f);
            r.transform.position = from;
            r.transform.localScale = new Vector3(0.42f, 0.22f, 1f);
            Vector3 d = to - from;
            r.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            _active.Add(new Shot { Renderer = r, From = from, To = to, Duration = Mathf.Max(0.03f, d.magnitude / _speed), OnArrive = onArrive });
        }

        public void Clear()
        {
            foreach (var shot in _active) Release(shot.Renderer);
            _active.Clear();
        }

        private SpriteRenderer Create()
        {
            var go = new GameObject("Bullet");
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = _sprite;
            r.sortingOrder = 50;
            return r;
        }

        private void Release(SpriteRenderer r)
        {
            r.gameObject.SetActive(false);
            _free.Push(r);
        }

        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var shot = _active[i];
                shot.Time += Time.deltaTime;
                float t = Mathf.Clamp01(shot.Time / shot.Duration);
                shot.Renderer.transform.position = Vector3.Lerp(shot.From, shot.To, t);
                if (t < 1f) continue;

                _active.RemoveAt(i);
                Release(shot.Renderer);
                shot.OnArrive?.Invoke();
            }
        }
    }
}
