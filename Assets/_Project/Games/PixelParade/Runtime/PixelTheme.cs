using UnityEngine;

namespace PixelParade
{
    /// <summary>Colours, sizes, timings and shared sprites of the belt game.</summary>
    [CreateAssetMenu(menuName = "Puzzle Pair/Pixel Theme", fileName = "PixelTheme")]
    public sealed class PixelTheme : ScriptableObject
    {
        [Header("Palette (index = PixelPalette key)")]
        [Tooltip("R Y G B O P K W D N C L T")]
        public Color[] Palette = new Color[13];

        [Header("Scene colours")]
        public Color Frame = Color.white;
        public Color Belt = new Color(0.23f, 0.26f, 0.34f);
        public Color BeltStripe = new Color(0.33f, 0.37f, 0.47f);
        public Color Slot = new Color(0.76f, 0.8f, 0.88f);
        public Color SlotWarning = new Color(0.95f, 0.35f, 0.38f);
        public Color Shadow = new Color(0f, 0f, 0f, 0.18f);
        public Color[] Confetti = { Color.red, Color.yellow, Color.cyan };

        [Header("Layout (world units, one pixel = 1)")]
        public float BeltGap = 1.5f;
        public float BeltWidth = 1.5f;
        public float SlotSpacing = 2.1f;
        public float LaneSpacing = 2.4f;
        public float QueueSpacing = 2f;
        public int VisiblePerLane = 3;
        public float ShooterSize = 1.7f;

        [Header("Timing")]
        [Tooltip("Seconds per simulation tick (one belt node).")]
        public float TickInterval = 0.06f;
        public float ProjectileSpeed = 60f;

        [Header("Assets")]
        public Sprite Block;
        public Sprite BlockShine;
        public Sprite Circle;
        public Sprite Ring;
        public Sprite Square;
        public Sprite SoftShadow;
        public Sprite Chevron;
        public Sprite Rounded;
        public Material LineMaterial;

        public Color ColorOf(int paletteIndex) =>
            paletteIndex >= 0 && paletteIndex < Palette.Length ? Palette[paletteIndex] : Color.magenta;
    }
}
