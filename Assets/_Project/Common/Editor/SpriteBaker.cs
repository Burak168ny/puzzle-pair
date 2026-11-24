using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PuzzlePair.Editor
{
    /// <summary>
    /// Bakes every sprite of both games (shapes, icons, the shaded pixel block) from signed distance
    /// functions into PNGs under Common/Art/Sprites, so the repository contains no third-party art.
    /// Sprites are white (or grey-shaded) and tinted at runtime.
    /// </summary>
    public static class SpriteBaker
    {
        public enum Shape
        {
            Circle,
            Ring,
            RoundedRect,
            Block,
            Square,
            Triangle,
            Heart,
            HeartBroken,
            Star,
            Home,
            Retry,
            Bulb,
            Plus,
            Speaker,
            Vibrate,
            Slash,
            Check,
            Shadow,
            Chevron,
            ArrowIcon,
            PixelIcon,
            Capsule,
            BlockShine,
            Rays,
            Gradient,
            Vignette,
            Spark,
        }

        public const string Folder = "Assets/_Project/Common/Art/Sprites";

        public static Sprite Get(Shape shape) => GetOrBake(shape, false);

        [MenuItem("Tools/Puzzle Pair/Rebake Sprites", priority = 40)]
        public static void RebakeAll()
        {
            foreach (Shape shape in Enum.GetValues(typeof(Shape))) GetOrBake(shape, true);
            Debug.Log($"[PuzzlePair] Sprites baked to {Folder}.");
        }

        public static Sprite GetOrBake(Shape shape, bool force)
        {
            string path = $"{Folder}/{shape}.png";
            if (!force)
            {
                var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (existing != null) return existing;
            }

            Directory.CreateDirectory(Folder);
            int size = SizeOf(shape);
            File.WriteAllBytes(path, Render(shape, size).EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = shape == Shape.Block || shape == Shape.Circle;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            // World sprites are exactly one unit; the 9-sliced rounded rect is a UI sprite.
            importer.spritePixelsPerUnit = shape == Shape.RoundedRect ? 100 : size;
            if (shape == Shape.RoundedRect) importer.spriteBorder = new Vector4(40, 40, 40, 40);
            if (shape == Shape.Capsule) importer.spriteBorder = new Vector4(30, 30, 30, 30);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static int SizeOf(Shape shape)
        {
            switch (shape)
            {
                case Shape.RoundedRect: return 96;
                case Shape.Capsule: return 64;
                case Shape.Square: return 16;
                case Shape.Block: return 64;
                case Shape.Chevron: return 64;
                case Shape.PixelIcon: return 256;
                case Shape.ArrowIcon: return 256;
                case Shape.BlockShine: return 64;
                case Shape.Rays: return 256;
                case Shape.Gradient: return 64;
                case Shape.Vignette: return 256;
                case Shape.Spark: return 64;
                default: return 128;
            }
        }

        // ---------------------------------------------------------------- rendering

        private static Texture2D Render(Shape shape, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float pixel = 2f / size; // one pixel in normalized units, for anti-aliasing
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                pixels[y * size + x] = Shade(shape, p, pixel);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static Color32 Shade(Shape shape, Vector2 p, float pixel)
        {
            switch (shape)
            {
                case Shape.Shadow:
                {
                    float r = p.magnitude;
                    float a = Mathf.Pow(1f - Smooth(0.1f, 1f, r), 1.6f);
                    return new Color(1f, 1f, 1f, a);
                }
                case Shape.Block:
                    return BlockShade(p, pixel);
                case Shape.PixelIcon:
                    return PixelIconShade(p, pixel);
                case Shape.BlockShine:
                    return BlockShineShade(p, pixel);
                case Shape.Rays:
                {
                    // Twelve soft light beams fading out from the centre.
                    float angle = Mathf.Atan2(p.y, p.x);
                    float beam = Smooth(-0.2f, 0.25f, Mathf.Cos(angle * 12f));
                    float fall = Mathf.Pow(Mathf.Clamp01(1f - p.magnitude), 1.2f);
                    return new Color(1f, 1f, 1f, beam * fall);
                }
                case Shape.Gradient:
                {
                    // Lit from above: white at the top, slightly darker at the bottom.
                    float g = Mathf.Lerp(0.86f, 1f, Smooth(-1f, 1f, p.y));
                    return new Color(g, g, g, 1f);
                }
                case Shape.Vignette:
                    return new Color(1f, 1f, 1f, Smooth(0.55f, 1.4f, p.magnitude));
                default:
                {
                    float d = Sdf(shape, p);
                    return new Color(1f, 1f, 1f, Coverage(d, pixel));
                }
            }
        }

        private static float Coverage(float d, float pixel) => Mathf.Clamp01(0.5f - d / pixel);

        /// <summary>Rounded tile with a bevel lit from the top-left; grey levels multiply with the tint.</summary>
        private static Color BlockShade(Vector2 p, float pixel)
        {
            Func<Vector2, float> sdf = q => Box(q, new Vector2(0.9f, 0.9f), 0.26f);
            float d = sdf(p);
            float alpha = Coverage(d, pixel);
            if (alpha <= 0f) return new Color(1f, 1f, 1f, 0f);

            const float e = 0.01f;
            var n = new Vector2(sdf(p + new Vector2(e, 0f)) - sdf(p - new Vector2(e, 0f)),
                sdf(p + new Vector2(0f, e)) - sdf(p - new Vector2(0f, e)));
            if (n.sqrMagnitude > 1e-8f) n.Normalize();
            var light = new Vector2(-0.45f, 0.9f).normalized;
            float edge = Smooth(-0.26f, -0.04f, d); // 1 on the bevel, 0 on the flat face
            float lambert = Vector2.Dot(n, light);
            float shade = 0.82f + edge * 0.24f * lambert + 0.06f * p.y * (1f - edge);
            // Soft gloss blob near the top-left of the face.
            float gloss = Mathf.Exp(-((p - new Vector2(-0.35f, 0.4f)).sqrMagnitude) * 5f) * 0.12f;
            shade = Mathf.Clamp01(shade + gloss * (1f - edge));
            return new Color(shade, shade, shade, alpha);
        }

        /// <summary>
        /// White highlight layer drawn over a tinted block (tinting can only darken, so the gloss
        /// needs its own sprite): soft glow, a sharp specular dot and a lit top rim.
        /// </summary>
        private static Color BlockShineShade(Vector2 p, float pixel)
        {
            float d = Box(p, new Vector2(0.9f, 0.9f), 0.26f);
            float inside = Coverage(d, pixel);
            if (inside <= 0f) return new Color(1f, 1f, 1f, 0f);
            float glow = Mathf.Exp(-(p - new Vector2(-0.32f, 0.38f)).sqrMagnitude * 5f);
            float spec = Mathf.Exp(-(p - new Vector2(-0.46f, 0.5f)).sqrMagnitude * 45f);
            float rim = Smooth(-0.24f, -0.05f, d) * Mathf.Clamp01(p.y * 1.4f + 0.3f);
            float a = Mathf.Clamp01(glow * 0.32f + spec * 0.9f + rim * 0.5f);
            return new Color(1f, 1f, 1f, a * inside);
        }

        /// <summary>Menu icon of the pixel game: a heart drawn with bevelled pixel blocks.</summary>
        private static Color PixelIconShade(Vector2 p, float pixel)
        {
            string[] rows =
            {
                ".RR.RR.",
                "RWRRRRR",
                "RRRRRRR",
                "RRRRRRP",
                ".RRRRP.",
                "..RRP..",
                "...P...",
            };
            const int n = 7;
            float cell = 2f / n;
            int cx = Mathf.FloorToInt((p.x + 1f) / cell);
            int cy = Mathf.FloorToInt((1f - p.y) / cell);
            if (cx < 0 || cx >= n || cy < 0 || cy >= n) return new Color(0f, 0f, 0f, 0f);
            char c = rows[cy][cx];
            if (c == '.') return new Color(0f, 0f, 0f, 0f);

            var centre = new Vector2(-1f + (cx + 0.5f) * cell, 1f - (cy + 0.5f) * cell);
            var local = (p - centre) / (cell * 0.5f);
            Color block = BlockShade(local, pixel / (cell * 0.5f));
            Color tint = c == 'W' ? new Color(1f, 0.85f, 0.88f) : c == 'P' ? new Color(0.86f, 0.2f, 0.36f) : new Color(1f, 0.32f, 0.42f);
            return new Color(tint.r * block.r, tint.g * block.g, tint.b * block.b, block.a);
        }

        // ---------------------------------------------------------------- shapes

        /// <summary>Signed distance in normalized units (p in [-1, 1]), negative inside.</summary>
        private static float Sdf(Shape shape, Vector2 p)
        {
            switch (shape)
            {
                case Shape.Circle:
                    return p.magnitude - 0.94f;
                case Shape.Ring:
                    return Mathf.Abs(p.magnitude - 0.8f) - 0.12f;
                case Shape.RoundedRect:
                    return Box(p, new Vector2(0.98f, 0.98f), 0.8f);
                case Shape.Capsule:
                    return Box(p, new Vector2(0.98f, 0.98f), 0.96f);
                case Shape.Spark:
                    // Four-point sparkle.
                    return (Mathf.Sqrt(Mathf.Abs(p.x)) + Mathf.Sqrt(Mathf.Abs(p.y)) - 0.95f) * 0.6f;
                case Shape.Square:
                    return Box(p, new Vector2(0.92f, 0.92f), 0.12f);
                case Shape.Triangle:
                    return Triangle(p, new Vector2(0.92f, 0f), new Vector2(-0.62f, 0.86f), new Vector2(-0.62f, -0.86f));
                case Shape.Heart:
                    return Heart(p);
                case Shape.HeartBroken:
                {
                    float heart = Heart(p);
                    // Zig-zag crack down the middle.
                    float crackX = 0.08f * Mathf.Sign(Mathf.Sin(p.y * 9f)) * Mathf.Abs(Mathf.Sin(p.y * 9f));
                    float crack = 0.07f - Mathf.Abs(p.x - crackX);
                    return Mathf.Max(heart, crack);
                }
                case Shape.Star:
                    return Star5(p, 0.92f, 0.45f) - 0.04f;
                case Shape.Home:
                {
                    float roof = Triangle(p, new Vector2(0f, 0.85f), new Vector2(-0.85f, 0.05f), new Vector2(0.85f, 0.05f));
                    float body = Box(p - new Vector2(0f, -0.4f), new Vector2(0.55f, 0.48f), 0.08f);
                    float door = Box(p - new Vector2(0f, -0.58f), new Vector2(0.16f, 0.3f), 0.06f);
                    return Mathf.Max(Mathf.Min(roof, body), -door);
                }
                case Shape.Retry:
                    return RefreshArrow(p);
                case Shape.Bulb:
                {
                    float glass = (p - new Vector2(0f, 0.22f)).magnitude - 0.58f;
                    float neck = Box(p - new Vector2(0f, -0.38f), new Vector2(0.26f, 0.2f), 0.05f);
                    float bulb = Mathf.Min(glass, neck);
                    float screw1 = Box(p - new Vector2(0f, -0.68f), new Vector2(0.24f, 0.06f), 0.05f);
                    float screw2 = Box(p - new Vector2(0f, -0.86f), new Vector2(0.15f, 0.06f), 0.05f);
                    float gap = Mathf.Abs(p.y + 0.59f) - 0.03f; // thin gap between glass and screw
                    return Mathf.Min(Mathf.Max(bulb, -gap), Mathf.Min(screw1, screw2));
                }
                case Shape.Plus:
                    return Mathf.Min(Box(p, new Vector2(0.72f, 0.18f), 0.1f), Box(p, new Vector2(0.18f, 0.72f), 0.1f));
                case Shape.Speaker:
                {
                    float box = Box(p - new Vector2(-0.5f, 0f), new Vector2(0.18f, 0.26f), 0.05f);
                    float cone = Triangle(p, new Vector2(-0.5f, 0f), new Vector2(0.05f, 0.7f), new Vector2(0.05f, -0.7f));
                    float wave1 = Arc(p - new Vector2(0.05f, 0f), 0.42f, 0.08f, 50f);
                    float wave2 = Arc(p - new Vector2(0.05f, 0f), 0.72f, 0.08f, 50f);
                    return Mathf.Min(Mathf.Min(box, cone), Mathf.Min(wave1, wave2));
                }
                case Shape.Vibrate:
                {
                    float phone = Box(p, new Vector2(0.32f, 0.62f), 0.12f);
                    float screen = Box(p - new Vector2(0f, 0.04f), new Vector2(0.2f, 0.42f), 0.04f);
                    float body = Mathf.Max(phone, -screen);
                    float l1 = Box(p - new Vector2(-0.6f, 0f), new Vector2(0.06f, 0.3f), 0.06f);
                    float l2 = Box(p - new Vector2(0.6f, 0f), new Vector2(0.06f, 0.3f), 0.06f);
                    float l3 = Box(p - new Vector2(-0.84f, 0f), new Vector2(0.06f, 0.18f), 0.06f);
                    float l4 = Box(p - new Vector2(0.84f, 0f), new Vector2(0.06f, 0.18f), 0.06f);
                    return Mathf.Min(body, Mathf.Min(Mathf.Min(l1, l2), Mathf.Min(l3, l4)));
                }
                case Shape.Slash:
                    return Segment(p, new Vector2(-0.75f, -0.75f), new Vector2(0.75f, 0.75f)) - 0.11f;
                case Shape.Check:
                    return Mathf.Min(Segment(p, new Vector2(-0.6f, 0.02f), new Vector2(-0.16f, -0.44f)),
                        Segment(p, new Vector2(-0.16f, -0.44f), new Vector2(0.64f, 0.5f))) - 0.15f;
                case Shape.Chevron:
                    return Mathf.Min(Segment(p, new Vector2(-0.35f, 0.6f), new Vector2(0.3f, 0f)),
                        Segment(p, new Vector2(0.3f, 0f), new Vector2(-0.35f, -0.6f))) - 0.2f;
                case Shape.ArrowIcon:
                {
                    // An L-shaped "snake" arrow, the motif of the arrow game.
                    float body = Mathf.Min(Segment(p, new Vector2(-0.6f, -0.62f), new Vector2(-0.6f, 0.2f)),
                        Segment(p, new Vector2(-0.6f, 0.2f), new Vector2(0.3f, 0.2f))) - 0.13f;
                    float head = Triangle(p, new Vector2(0.88f, 0.2f), new Vector2(0.22f, 0.62f), new Vector2(0.22f, -0.22f));
                    return Mathf.Min(body, head);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }

        private static float RefreshArrow(Vector2 p)
        {
            // Ring with a gap at the top right, arrowhead closing the gap.
            float ring = Mathf.Abs(p.magnitude - 0.55f) - 0.12f;
            float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 60f)) < 35f) ring = Mathf.Max(ring, 0.05f);
            float head = Triangle(p, new Vector2(0.1f, 0.88f), new Vector2(0.08f, 0.22f), new Vector2(0.52f, 0.62f));
            return Mathf.Min(ring, head);
        }

        private static float Heart(Vector2 p)
        {
            const float scale = 0.62f;
            var q = new Vector2(Mathf.Abs(p.x) * scale, p.y * scale + 0.5f);
            float d;
            if (q.y + q.x > 1f) d = (q - new Vector2(0.25f, 0.75f)).magnitude - 0.35355f;
            else
            {
                float a = (q - new Vector2(0f, 1f)).sqrMagnitude;
                float m = 0.5f * Mathf.Max(q.x + q.y, 0f);
                float b = (q - new Vector2(m, m)).sqrMagnitude;
                d = Mathf.Sqrt(Mathf.Min(a, b)) * Mathf.Sign(q.x - q.y);
            }
            return d / scale;
        }

        private static float Star5(Vector2 p, float r, float rf)
        {
            var k1 = new Vector2(0.809017f, -0.587785f);
            var k2 = new Vector2(-k1.x, k1.y);
            p.x = Mathf.Abs(p.x);
            p -= 2f * Mathf.Max(Vector2.Dot(k1, p), 0f) * k1;
            p -= 2f * Mathf.Max(Vector2.Dot(k2, p), 0f) * k2;
            p.x = Mathf.Abs(p.x);
            p.y -= r;
            var ba = rf * new Vector2(-k1.y, k1.x) - new Vector2(0f, 1f);
            float h = Mathf.Clamp(Vector2.Dot(p, ba) / Vector2.Dot(ba, ba), 0f, r);
            return (p - ba * h).magnitude * Mathf.Sign(p.y * ba.x - p.x * ba.y);
        }

        /// <summary>Thick arc of the given radius spanning +-halfAngle degrees around +x.</summary>
        private static float Arc(Vector2 p, float radius, float thickness, float halfAngle)
        {
            float ring = Mathf.Abs(p.magnitude - radius) - thickness;
            float angle = Mathf.Abs(Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg);
            return angle > halfAngle ? Mathf.Max(ring, (angle - halfAngle) * Mathf.Deg2Rad * radius) : ring;
        }

        /// <summary>GLSL-style smoothstep (Unity's Mathf.SmoothStep interpolates values instead).</summary>
        private static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static float Box(Vector2 p, Vector2 half, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + Vector2.one * radius;
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        /// <summary>Triangle as the max of its edge half-planes; exact enough for anti-aliased icons.</summary>
        private static float Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float winding = Mathf.Sign((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
            float Edge(Vector2 from, Vector2 to)
            {
                Vector2 e = (to - from).normalized;
                Vector2 outward = new Vector2(e.y, -e.x) * winding;
                return Vector2.Dot(p - from, outward);
            }
            return Mathf.Max(Edge(a, b), Mathf.Max(Edge(b, c), Edge(c, a)));
        }
    }
}
