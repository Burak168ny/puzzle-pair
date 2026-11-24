using System.Linq;
using PuzzlePair.MainMenu;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PuzzlePair.Editor
{
    /// <summary>Generates the MainMenu scene: title, one card per <see cref="IGameEntry"/>, settings toggles.</summary>
    public sealed class MainMenuBuilder : ISceneBuilder
    {
        public int Order => 0;
        public string ScenePath => SceneKit.ScenesFolder + "/" + SceneNames.MainMenu + ".unity";

        private static readonly Color Background = SceneKit.Hex("#222738");

        [MenuItem("Tools/Puzzle Pair/Scenes/Main Menu", priority = 100)]
        public static void BuildMenu() => new MainMenuBuilder().Build();

        public void Build()
        {
            var scene = SceneKit.NewScene();
            SceneKit.CreateCamera(Background, 0f, 0f);
            UiKit.CreateEventSystem();
            var safe = UiKit.CreateCanvas("Canvas", out _);

            // Soft decorative blobs behind the content.
            var circle = SpriteBaker.Get(SpriteBaker.Shape.Circle);
            var blobA = UiKit.Image(safe, "BlobA", circle, new Color(1f, 1f, 1f, 0.035f));
            UiKit.Centre(blobA.rectTransform, new Vector2(-420f, 640f), new Vector2(900f, 900f));
            var blobB = UiKit.Image(safe, "BlobB", circle, new Color(1f, 1f, 1f, 0.03f));
            UiKit.Centre(blobB.rectTransform, new Vector2(460f, -700f), new Vector2(1100f, 1100f));

            var titleGo = UiKit.Child(safe, "Title");
            var titleRect = UiKit.Place(UiKit.Rect(titleGo), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1000f, 300f));
            var title = UiKit.Text(titleGo.transform, "Text", "Puzzle Pair", 132f, Color.white);
            UiKit.Centre(title.rectTransform, new Vector2(0f, 40f), new Vector2(1000f, 170f));
            var subtitle = UiKit.Text(titleGo.transform, "Subtitle", "Two little puzzle games", 46f, SceneKit.Hex("#A9B0C6"), TextAlignmentOptions.Center, false);
            UiKit.Centre(subtitle.rectTransform, new Vector2(0f, -70f), new Vector2(1000f, 80f));

            var games = ProjectSetup.CreateAll<IGameEntry>()
                .OrderBy(g => g is ISceneBuilder b ? b.Order : 99)
                .ToList();

            var menu = new GameObject("MainMenu").AddComponent<MainMenuController>();
            var so = new SerializedObject(menu);
            var cards = so.FindProperty("_cards");
            cards.arraySize = games.Count;

            float spacing = 620f;
            float top = (games.Count - 1) * spacing * 0.5f - 80f;
            for (int i = 0; i < games.Count; i++)
            {
                var game = games[i];
                var card = BuildCard(safe, game, new Vector2(0f, top - i * spacing), out var progress, out var icon);
                var element = cards.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("GameId").stringValue = game.GameId;
                element.FindPropertyRelative("SceneName").stringValue = game.SceneName;
                element.FindPropertyRelative("LevelCount").intValue = game.LevelCount;
                element.FindPropertyRelative("Button").objectReferenceValue = card;
                element.FindPropertyRelative("Progress").objectReferenceValue = progress;
                element.FindPropertyRelative("Icon").objectReferenceValue = icon;
            }

            var toggles = UiKit.Child(safe, "Settings");
            UiKit.Place(UiKit.Rect(toggles), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(500f, 160f));
            var face = SceneKit.Hex("#323850");
            var iconColor = Color.white;
            var sound = UiKit.Toggle(toggles.transform, "Sound", SpriteBaker.Get(SpriteBaker.Shape.Speaker), face, iconColor, new Vector2(150f, 150f));
            UiKit.Centre(UiKit.Rect(sound.gameObject), new Vector2(-100f, 0f), new Vector2(150f, 150f));
            var haptics = UiKit.Toggle(toggles.transform, "Vibration", SpriteBaker.Get(SpriteBaker.Shape.Vibrate), face, iconColor, new Vector2(150f, 150f));
            UiKit.Centre(UiKit.Rect(haptics.gameObject), new Vector2(100f, 0f), new Vector2(150f, 150f));

            so.FindProperty("_sound").objectReferenceValue = sound;
            so.FindProperty("_haptics").objectReferenceValue = haptics;
            so.FindProperty("_title").objectReferenceValue = titleRect;
            so.ApplyModifiedPropertiesWithoutUndo();

            SceneKit.Save(scene, ScenePath);
        }

        private static UnityEngine.UI.Button BuildCard(RectTransform parent, IGameEntry game, Vector2 position,
            out TMP_Text progress, out RectTransform icon)
        {
            var size = new Vector2(940f, 540f);
            var button = UiKit.Button(parent, game.Title, game.CardColor, size, 0.6f);
            UiKit.Centre(UiKit.Rect(button.gameObject), position, size);

            bool darkCard = game.CardColor.grayscale < 0.55f;
            var ink = darkCard ? Color.white : UiKit.Ink;
            var soft = darkCard ? new Color(1f, 1f, 1f, 0.8f) : UiKit.SoftInk;

            var iconBack = UiKit.Image(button.transform, "IconBack", SpriteBaker.Get(SpriteBaker.Shape.Circle), new Color(1f, 1f, 1f, darkCard ? 0.16f : 0.7f));
            UiKit.Centre(iconBack.rectTransform, new Vector2(-250f, 10f), new Vector2(340f, 340f));
            var iconImage = UiKit.Image(button.transform, "Icon", game.Icon, game.Icon != null && game.Icon.name == "PixelIcon" ? Color.white : game.Accent);
            UiKit.Centre(iconImage.rectTransform, new Vector2(-250f, 10f), new Vector2(250f, 250f));
            icon = iconImage.rectTransform;

            var title = UiKit.Text(button.transform, "Name", game.Title, 78f, ink, TextAlignmentOptions.Left);
            UiKit.Centre(title.rectTransform, new Vector2(170f, 120f), new Vector2(520f, 110f));
            var tagline = UiKit.Text(button.transform, "Tagline", game.Tagline, 40f, soft, TextAlignmentOptions.TopLeft, false);
            UiKit.Centre(tagline.rectTransform, new Vector2(170f, -5f), new Vector2(520f, 130f));

            var pill = UiKit.Image(button.transform, "Pill", SpriteBaker.Get(SpriteBaker.Shape.Capsule), game.Accent, true, 1f);
            UiKit.Centre(pill.rectTransform, new Vector2(170f, -160f), new Vector2(520f, 100f));
            var label = UiKit.Text(pill.transform, "Progress", "Level 1 / 5", 42f, Color.white);
            UiKit.Stretch(label.rectTransform);
            progress = label;
            return button;
        }
    }
}
