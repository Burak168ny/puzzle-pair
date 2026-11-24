using UnityEngine;

namespace ArrowSlide
{
    /// <summary>Colours, sizes and shared assets of the arrow game, edited in one place.</summary>
    [CreateAssetMenu(menuName = "Puzzle Pair/Arrow Theme", fileName = "ArrowTheme")]
    public sealed class ArrowTheme : ScriptableObject
    {
        [Header("Colours")]
        public Color Ink = new Color(0.17f, 0.2f, 0.27f);
        public Color Blocked = new Color(0.9f, 0.28f, 0.3f);
        public Color Hint = new Color(0.18f, 0.5f, 0.93f);
        public Color Dot = new Color(0.81f, 0.78f, 0.71f);
        public Color BoardFill = new Color(0.97f, 0.96f, 0.93f);
        public Color Shadow = new Color(0f, 0f, 0f, 0.1f);
        public Color[] Confetti = { Color.red, Color.yellow, Color.cyan };

        [Header("Shape (in cells)")]
        [Range(0.05f, 0.5f)] public float LineWidth = 0.15f;
        [Range(0.2f, 1f)] public float HeadSize = 0.56f;
        [Range(0.02f, 0.3f)] public float DotSize = 0.1f;
        public Vector2 ShadowOffset = new Vector2(0.05f, -0.08f);

        [Header("Motion (cells per second)")]
        public float EscapeStartSpeed = 8f;
        public float EscapeMaxSpeed = 36f;
        public float EscapeAcceleration = 110f;
        public float BumpSpeed = 18f;

        [Header("Assets")]
        public Material LineMaterial;
        public Sprite HeadSprite;
        public Sprite DotSprite;
        public Sprite BoardSprite;
    }
}
