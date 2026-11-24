using UnityEngine;

namespace PuzzlePair
{
    /// <summary>Standard easing curves on t in [0, 1].</summary>
    public static class Easing
    {
        public static float Linear(float t) => t;
        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float InQuad(float t) => t * t;
        public static float InCubic(float t) => t * t * t;
        public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

        public static float OutBack(float t) => OutBack(t, 1.70158f);

        public static float OutBack(float t, float s)
        {
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }

        public static float InBack(float t) => t * t * (2.70158f * t - 1.70158f);

        /// <summary>Elastic overshoot that settles at 1, for "pop in" scales.</summary>
        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        /// <summary>0 at both ends, 1 in the middle: the height profile of a jump.</summary>
        public static float Arc(float t) => 4f * t * (1f - t);

        /// <summary>0 -> 1 -> 0 with a quick rise and slow fall, for punches and flashes.</summary>
        public static float Punch(float t) => t < 0.25f ? t / 0.25f : 1f - (t - 0.25f) / 0.75f;
    }
}
