using System;
using System.Collections;
using UnityEngine;

namespace PuzzlePair
{
    /// <summary>
    /// Tiny coroutine tweens. No external tween library: every animation in the project is a
    /// <c>StartCoroutine(Tween.Run(...))</c> with an easing curve, which keeps the dependencies at zero.
    /// </summary>
    public static class Tween
    {
        public static IEnumerator Run(float duration, Action<float> step, Func<float, float> ease = null, bool unscaled = false)
        {
            ease ??= Easing.Linear;
            if (duration <= 0f)
            {
                step(ease(1f));
                yield break;
            }

            float time = 0f;
            while (time < duration)
            {
                step(ease(time / duration));
                yield return null;
                time += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            }
            step(ease(1f));
        }

        public static IEnumerator Delay(float seconds, Action then, bool unscaled = false)
        {
            if (unscaled) yield return new WaitForSecondsRealtime(seconds);
            else yield return new WaitForSeconds(seconds);
            then();
        }

        /// <summary>Scale punch: grows by <paramref name="amount"/> and settles back to <paramref name="baseScale"/>.</summary>
        public static IEnumerator Punch(Transform target, float amount, float duration, Vector3 baseScale)
        {
            return Run(duration, t =>
            {
                if (target != null) target.localScale = baseScale * (1f + amount * Easing.Punch(t));
            });
        }

        /// <summary>Horizontal shake, used for "no" feedback on UI and world objects.</summary>
        public static IEnumerator Shake(Transform target, float amplitude, float duration, bool unscaled = false)
        {
            Vector3 origin = target.localPosition;
            yield return Run(duration, t =>
            {
                if (target == null) return;
                float damp = 1f - t;
                target.localPosition = origin + Vector3.right * (Mathf.Sin(t * 40f) * amplitude * damp);
            }, null, unscaled);
            if (target != null) target.localPosition = origin;
        }
    }
}
