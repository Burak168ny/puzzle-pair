using ArrowSlide.Model;
using PuzzlePair;
using PuzzlePair.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowSlide.Editor
{
    /// <summary>Generates the Arrow Slide scene and its theme asset, and registers the game in the main menu.</summary>
    public sealed class ArrowSceneBuilder : ISceneBuilder, IGameEntry
    {
        private const string ThemePath = "Assets/_Project/Games/ArrowSlide/ArrowTheme.asset";

        public int Order => 1;
        public string ScenePath => SceneKit.ScenesFolder + "/" + SceneName + ".unity";
        public string GameId => ArrowSlideGame.GameId;
        public string SceneName => "ArrowSlide";
        public string Title => "Arrow Slide";
        public string Tagline => "Slide every arrow off the board. Mind the traffic!";
        public int LevelCount => ArrowLevels.All.Count;
        public Color Accent => SceneKit.Hex("#4C6EF5");
        public Color CardColor => SceneKit.Hex("#F4EFE6");
        public Sprite Icon => SpriteBaker.Get(SpriteBaker.Shape.ArrowIcon);

        private static readonly Color Background = SceneKit.Hex("#ECE5D8");

        [MenuItem("Tools/Puzzle Pair/Scenes/Arrow Slide", priority = 101)]
        public static void BuildMenu() => new ArrowSceneBuilder().Build();

        public void Build()
        {
            // New scene first: opening a scene unloads unused assets, which would destroy a theme loaded before it.
            var scene = SceneKit.NewScene();
            var theme = EnsureTheme();

            var rig = SceneKit.CreateCamera(Background, 420f, 280f);
            UiKit.CreateEventSystem();
            SceneKit.CreateBackdrop(rig.Camera, Background, 0.22f);
            SceneKit.CreateFxPool(60, 500);
            var prompt = SceneKit.CreateTapPrompt(theme.Hint, 40);

            var boardGo = new GameObject("Board");
            var board = boardGo.AddComponent<ArrowBoardView>();
            SceneKit.SetRef(board, "_theme", theme);

            // ------------------------------------------------------------ UI
            var safe = UiKit.CreateCanvas("Canvas", out var canvas);
            var topBar = UiKit.CreateTopBar(safe, UiKit.Ink, Color.white, UiKit.Ink);
            var tip = (RectTransform)safe.Find("Tip");
            tip.anchoredPosition = new Vector2(0f, -310f);

            var heartsGo = UiKit.Child(safe, "Hearts");
            UiKit.Place(UiKit.Rect(heartsGo), new Vector2(0.5f, 1f), new Vector2(0f, -195f), new Vector2(360f, 100f));
            var hearts = new Image[ArrowSession.DefaultLives];
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = UiKit.Image(heartsGo.transform, $"Heart{i}", SpriteBaker.Get(SpriteBaker.Shape.Heart), SceneKit.Hex("#EE4655"));
                UiKit.Centre(hearts[i].rectTransform, new Vector2((i - (hearts.Length - 1) * 0.5f) * 110f, 0f), new Vector2(86f, 86f));
            }
            var heartsView = heartsGo.AddComponent<HeartsView>();
            SceneKit.SetRefs(heartsView, "_hearts", hearts);
            SceneKit.SetRef(heartsView, "_full", SpriteBaker.Get(SpriteBaker.Shape.Heart));
            SceneKit.SetRef(heartsView, "_broken", SpriteBaker.Get(SpriteBaker.Shape.HeartBroken));
            SceneKit.SetColor(heartsView, "_fullColor", SceneKit.Hex("#EE4655"));
            SceneKit.SetColor(heartsView, "_emptyColor", SceneKit.Hex("#C9C1B3"));

            var bottom = UiKit.Child(safe, "BottomBar");
            UiKit.Place(UiKit.Rect(bottom), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1080f, 220f));

            var restart = UiKit.IconButton(bottom.transform, "Restart", SpriteBaker.Get(SpriteBaker.Shape.Retry), Color.white, UiKit.Ink, new Vector2(150f, 150f), out _);
            UiKit.Place(UiKit.Rect(restart.gameObject), new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(150f, 150f));

            var hint = UiKit.BoosterButton(bottom.transform, "Hint", SpriteBaker.Get(SpriteBaker.Shape.Bulb), SceneKit.Hex("#F2B705"), Accent, 170f, out var hintCount, out _);
            UiKit.Place(UiKit.Rect(hint.gameObject), new Vector2(1f, 0.5f), new Vector2(-60f, 0f), new Vector2(170f, 170f));

            var remaining = UiKit.Text(bottom.transform, "Remaining", "0 arrows left", 46f, UiKit.SoftInk);
            UiKit.Centre(remaining.rectTransform, Vector2.zero, new Vector2(560f, 90f));

            var win = UiKit.CreateResultPanel(canvas.transform, "WinPanel", Accent, SpriteBaker.Shape.Check, "Next", true);
            var lose = UiKit.CreateResultPanel(canvas.transform, "LosePanel", theme.Blocked, SpriteBaker.Shape.HeartBroken, "Try Again");

            // ------------------------------------------------------------ controller
            var game = new GameObject("ArrowSlideGame").AddComponent<ArrowSlideGame>();
            SceneKit.SetRef(game, "_board", board);
            SceneKit.SetRef(game, "_rig", rig);
            SceneKit.SetRef(game, "_topBar", topBar);
            SceneKit.SetRef(game, "_hearts", heartsView);
            SceneKit.SetRef(game, "_hintButton", hint);
            SceneKit.SetRef(game, "_hintCount", hintCount);
            SceneKit.SetRef(game, "_restartButton", restart);
            SceneKit.SetRef(game, "_remaining", remaining);
            SceneKit.SetRef(game, "_winPanel", win);
            SceneKit.SetRef(game, "_losePanel", lose);
            SceneKit.SetRef(game, "_tapPrompt", prompt);

            SceneKit.Save(scene, ScenePath);
        }

        private ArrowTheme EnsureTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ArrowTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<ArrowTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            theme.Ink = SceneKit.Hex("#2B3245");
            theme.Blocked = SceneKit.Hex("#E5484D");
            theme.Hint = SceneKit.Hex("#2F80ED");
            theme.Dot = SceneKit.Hex("#CFC5B3");
            theme.BoardFill = SceneKit.Hex("#F8F5EE");
            theme.Shadow = new Color(0.17f, 0.2f, 0.27f, 0.13f);
            theme.Confetti = new[]
            {
                SceneKit.Hex("#4C6EF5"), SceneKit.Hex("#F2B705"), SceneKit.Hex("#E5484D"),
                SceneKit.Hex("#2FB380"), SceneKit.Hex("#FF8A3D"), SceneKit.Hex("#9B5DE5"),
            };
            theme.LineWidth = 0.15f;
            theme.HeadSize = 0.56f;
            theme.DotSize = 0.1f;
            theme.LineMaterial = SceneKit.EnsureMaterial("ArrowLine");
            theme.HeadSprite = SpriteBaker.Get(SpriteBaker.Shape.Triangle);
            theme.DotSprite = SpriteBaker.Get(SpriteBaker.Shape.Circle);
            theme.BoardSprite = SpriteBaker.Get(SpriteBaker.Shape.RoundedRect);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }
    }
}
