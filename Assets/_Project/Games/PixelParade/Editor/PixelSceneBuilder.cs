using PixelParade.Model;
using PuzzlePair;
using PuzzlePair.Editor;
using PuzzlePair.UI;
using UnityEditor;
using UnityEngine;

namespace PixelParade.Editor
{
    /// <summary>Generates the Pixel Parade scene and its theme asset, and registers the game in the main menu.</summary>
    public sealed class PixelSceneBuilder : ISceneBuilder, IGameEntry
    {
        private const string ThemePath = "Assets/_Project/Games/PixelParade/PixelTheme.asset";

        public int Order => 2;
        public string ScenePath => SceneKit.ScenesFolder + "/" + SceneName + ".unity";
        public string GameId => PixelParadeGame.GameId;
        public string SceneName => "PixelParade";
        public string Title => "Pixel Parade";
        public string Tagline => "Send shooters around the belt and pop the picture!";
        public int LevelCount => PixelLevels.All.Count;
        public Color Accent => SceneKit.Hex("#FF5D8F");
        public Color CardColor => SceneKit.Hex("#5B5FEF");
        public Sprite Icon => SpriteBaker.Get(SpriteBaker.Shape.PixelIcon);

        private static readonly Color Background = SceneKit.Hex("#DCE6F5");
        private static readonly Color Ink = SceneKit.Hex("#2A2E45");

        [MenuItem("Tools/Puzzle Pair/Scenes/Pixel Parade", priority = 102)]
        public static void BuildMenu() => new PixelSceneBuilder().Build();

        public void Build()
        {
            // New scene first: opening a scene unloads unused assets, which would destroy a theme loaded before it.
            var scene = SceneKit.NewScene();
            var theme = EnsureTheme();

            var rig = SceneKit.CreateCamera(Background, 380f, 260f);
            UiKit.CreateEventSystem();
            SceneKit.CreateBackdrop(rig.Camera, Background, 0.3f);
            SceneKit.CreateFxPool(60, 700);
            var prompt = SceneKit.CreateTapPrompt(Ink, 40);

            var world = new GameObject("World").transform;
            var picture = new GameObject("Picture").AddComponent<PictureView>();
            picture.transform.SetParent(world, false);
            var belt = new GameObject("Belt").AddComponent<BeltView>();
            belt.transform.SetParent(world, false);
            var slots = new GameObject("Slots").AddComponent<SlotsView>();
            slots.transform.SetParent(world, false);
            var shooters = new GameObject("Shooters").transform;
            shooters.SetParent(world, false);
            var projectiles = new GameObject("Projectiles").AddComponent<ProjectilePool>();
            projectiles.transform.SetParent(world, false);

            // ------------------------------------------------------------ UI
            var safe = UiKit.CreateCanvas("Canvas", out var canvas);
            var topBar = UiKit.CreateTopBar(safe, Ink, Color.white, Ink);
            var tip = (RectTransform)safe.Find("Tip");
            tip.anchoredPosition = new Vector2(0f, -285f);

            var progress = CreateProgressBar(safe);

            var bottom = UiKit.Child(safe, "BottomBar");
            UiKit.Place(UiKit.Rect(bottom), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1080f, 220f));

            var restart = UiKit.IconButton(bottom.transform, "Restart", SpriteBaker.Get(SpriteBaker.Shape.Retry), Color.white, Ink, new Vector2(150f, 150f), out _);
            UiKit.Place(UiKit.Rect(restart.gameObject), new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(150f, 150f));

            var booster = UiKit.BoosterButton(bottom.transform, "AddSlot", SpriteBaker.Get(SpriteBaker.Shape.Plus), SceneKit.Hex("#5B5FEF"), Accent, 170f, out var boosterCount, out _);
            UiKit.Place(UiKit.Rect(booster.gameObject), new Vector2(1f, 0.5f), new Vector2(-60f, 0f), new Vector2(170f, 170f));
            var boosterLabel = UiKit.Text(booster.transform, "Label", "+1 Slot", 32f, Ink);
            UiKit.Centre(boosterLabel.rectTransform, new Vector2(0f, -106f), new Vector2(220f, 44f));

            var beltPill = UiKit.Image(bottom.transform, "BeltLoad", SpriteBaker.Get(SpriteBaker.Shape.Capsule), SceneKit.Hex("#3A4157"), true, 1f);
            UiKit.Centre(beltPill.rectTransform, Vector2.zero, new Vector2(300f, 96f));
            var beltLoad = UiKit.Text(beltPill.transform, "Text", "Belt 0/5", 44f, Color.white);
            UiKit.Stretch(beltLoad.rectTransform);

            var win = UiKit.CreateResultPanel(canvas.transform, "WinPanel", Accent, SpriteBaker.Shape.Star, "Next", true);
            var lose = UiKit.CreateResultPanel(canvas.transform, "LosePanel", SceneKit.Hex("#E5484D"), SpriteBaker.Shape.HeartBroken, "Try Again");

            // ------------------------------------------------------------ controller
            var game = new GameObject("PixelParadeGame").AddComponent<PixelParadeGame>();
            SceneKit.SetRef(game, "_theme", theme);
            SceneKit.SetRef(game, "_rig", rig);
            SceneKit.SetRef(game, "_topBar", topBar);
            SceneKit.SetRef(game, "_picture", picture);
            SceneKit.SetRef(game, "_belt", belt);
            SceneKit.SetRef(game, "_slots", slots);
            SceneKit.SetRef(game, "_projectiles", projectiles);
            SceneKit.SetRef(game, "_shooterRoot", shooters);
            SceneKit.SetRef(game, "_progress", progress);
            SceneKit.SetRef(game, "_beltLoad", beltLoad);
            SceneKit.SetRef(game, "_slotBooster", booster);
            SceneKit.SetRef(game, "_slotBoosterCount", boosterCount);
            SceneKit.SetRef(game, "_restartButton", restart);
            SceneKit.SetRef(game, "_winPanel", win);
            SceneKit.SetRef(game, "_losePanel", lose);
            SceneKit.SetRef(game, "_tapPrompt", prompt);

            SceneKit.Save(scene, ScenePath);
        }

        private ProgressBar CreateProgressBar(RectTransform safe)
        {
            var capsule = SpriteBaker.Get(SpriteBaker.Shape.Capsule);
            var back = UiKit.Image(safe, "Progress", capsule, new Color(1f, 1f, 1f, 0.75f), true, 1f);
            UiKit.Place(back.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -195f), new Vector2(620f, 44f));
            var fillArea = UiKit.Rect(UiKit.Child(back.transform, "FillArea"));
            UiKit.Stretch(fillArea, 7f);
            var fill = UiKit.Image(fillArea, "Fill", capsule, Accent, true, 1.6f);
            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(0.0001f, 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var bar = back.gameObject.AddComponent<ProgressBar>();
            SceneKit.SetRef(bar, "_fill", rt);
            return bar;
        }

        private PixelTheme EnsureTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<PixelTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<PixelTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            // Order matches PixelPalette.Keys: R Y G B O P K W D N C L T.
            theme.Palette = new[]
            {
                SceneKit.Hex("#F2545B"), SceneKit.Hex("#FFD23F"), SceneKit.Hex("#3BB273"), SceneKit.Hex("#3E8EDE"),
                SceneKit.Hex("#FF9F1C"), SceneKit.Hex("#9B5DE5"), SceneKit.Hex("#FF7EB6"), SceneKit.Hex("#F7F4EA"),
                SceneKit.Hex("#34364F"), SceneKit.Hex("#9A6240"), SceneKit.Hex("#4CC9F0"), SceneKit.Hex("#A7E05A"),
                SceneKit.Hex("#F6C792"),
            };
            theme.Frame = Color.white;
            theme.BeltGap = 1.5f;
            theme.BeltWidth = 1.5f;
            theme.SlotSpacing = 2.1f;
            theme.LaneSpacing = 2.4f;
            theme.QueueSpacing = 2f;
            theme.VisiblePerLane = 3;
            theme.ShooterSize = 1.7f;
            theme.TickInterval = 0.06f;
            theme.ProjectileSpeed = 60f;
            theme.Belt = SceneKit.Hex("#3A4157");
            theme.BeltStripe = SceneKit.Hex("#4F5873");
            theme.Slot = SceneKit.Hex("#A9B7D1");
            theme.SlotWarning = SceneKit.Hex("#F2545B");
            theme.Shadow = new Color(0.1f, 0.12f, 0.25f, 0.16f);
            theme.Confetti = new[]
            {
                SceneKit.Hex("#FF5D8F"), SceneKit.Hex("#FFD23F"), SceneKit.Hex("#4CC9F0"),
                SceneKit.Hex("#3BB273"), SceneKit.Hex("#9B5DE5"), SceneKit.Hex("#FF9F1C"),
            };
            theme.Block = SpriteBaker.Get(SpriteBaker.Shape.Block);
            theme.BlockShine = SpriteBaker.Get(SpriteBaker.Shape.BlockShine);
            theme.Circle = SpriteBaker.Get(SpriteBaker.Shape.Circle);
            theme.Ring = SpriteBaker.Get(SpriteBaker.Shape.Ring);
            theme.Square = SpriteBaker.Get(SpriteBaker.Shape.Square);
            theme.SoftShadow = SpriteBaker.Get(SpriteBaker.Shape.Shadow);
            theme.Chevron = SpriteBaker.Get(SpriteBaker.Shape.Chevron);
            theme.Rounded = SpriteBaker.Get(SpriteBaker.Shape.RoundedRect);
            theme.LineMaterial = SceneKit.EnsureMaterial("BeltLine");
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }
    }
}
