using PuzzlePair.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PuzzlePair.Editor
{
    /// <summary>Builds uGUI hierarchies from code (canvas, buttons, panels) for the scene builders.</summary>
    public static class UiKit
    {
        public static readonly Color Ink = SceneKit.Hex("#2A2E3B");
        public static readonly Color SoftInk = SceneKit.Hex("#6B7185");
        public static readonly Color Card = Color.white;
        public static readonly Color Dim = new Color(0.06f, 0.07f, 0.11f, 0.62f);

        public static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        /// <summary>Portrait canvas (1080x1920 reference, width-matched) with a safe-area child; returns the child.</summary>
        public static RectTransform CreateCanvas(string name, out Canvas canvas)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            var safe = Rect(Child(go.transform, "SafeArea"));
            Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();
            return safe;
        }

        public static GameObject Child(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            return go;
        }

        public static RectTransform Rect(GameObject go) => (RectTransform)go.transform;

        public static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        /// <summary>Anchors at <paramref name="anchor"/> (also the pivot) with an offset and size in reference pixels.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Centre(RectTransform rt, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool sliced = false, float pixelsPerUnit = 1f)
        {
            var go = Child(parent, name);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            if (sliced)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = pixelsPerUnit;
            }
            image.raycastTarget = false;
            return image;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool bold = true)
        {
            var go = Child(parent, name);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        /// <summary>Rounded button with a drop shadow; returns the button (its Image is the face).</summary>
        public static Button Button(Transform parent, string name, Color color, Vector2 size, float corner = 1f)
        {
            var rounded = SpriteBaker.Get(SpriteBaker.Shape.RoundedRect);
            var root = Child(parent, name);
            Rect(root).sizeDelta = size;

            var shadow = Image(root.transform, "Shadow", rounded, new Color(0f, 0f, 0f, 0.16f), true, corner);
            Stretch(shadow.rectTransform);
            shadow.rectTransform.offsetMin = new Vector2(0f, -12f);
            shadow.rectTransform.offsetMax = new Vector2(0f, -12f);

            var faceImage = Image(root.transform, "Face", rounded, color, true, corner);
            Stretch(faceImage.rectTransform);
            faceImage.raycastTarget = true;

            var button = root.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = faceImage;
            var colors = button.colors;
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
            button.colors = colors;
            root.AddComponent<ButtonPressScale>();
            return button;
        }

        public static Button TextButton(Transform parent, string name, string label, Color color, Vector2 size, out TextMeshProUGUI text, float fontSize = 64f)
        {
            var button = Button(parent, name, color, size);
            text = Text(button.transform, "Label", label, fontSize, Color.white);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(10f, 8f);
            return button;
        }

        public static Button IconButton(Transform parent, string name, Sprite icon, Color face, Color iconColor, Vector2 size, out Image iconImage, float iconScale = 0.58f)
        {
            var button = Button(parent, name, face, size, 1.4f);
            iconImage = Image(button.transform, "Icon", icon, iconColor);
            Centre(iconImage.rectTransform, new Vector2(0f, 2f), size * iconScale);
            return button;
        }

        /// <summary>Round booster button with an icon and a count badge in the top-right corner.</summary>
        public static Button BoosterButton(Transform parent, string name, Sprite icon, Color iconColor, Color badgeColor, float diameter,
            out TextMeshProUGUI countLabel, out GameObject badge)
        {
            var circle = SpriteBaker.Get(SpriteBaker.Shape.Circle);
            var root = Child(parent, name);
            Rect(root).sizeDelta = new Vector2(diameter, diameter);

            var shadow = Image(root.transform, "Shadow", circle, new Color(0f, 0f, 0f, 0.16f));
            Centre(shadow.rectTransform, new Vector2(0f, -10f), Vector2.one * diameter);
            var face = Image(root.transform, "Face", circle, Color.white);
            Centre(face.rectTransform, Vector2.zero, Vector2.one * diameter);
            face.raycastTarget = true;
            var iconImage = Image(root.transform, "Icon", icon, iconColor);
            Centre(iconImage.rectTransform, Vector2.zero, Vector2.one * diameter * 0.56f);

            var badgeImage = Image(root.transform, "Badge", circle, badgeColor);
            Centre(badgeImage.rectTransform, new Vector2(diameter * 0.36f, diameter * 0.36f), Vector2.one * diameter * 0.4f);
            badge = badgeImage.gameObject;
            countLabel = Text(badgeImage.transform, "Count", "0", diameter * 0.24f, Color.white);
            Stretch(countLabel.rectTransform);

            var button = root.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = face;
            root.AddComponent<ButtonPressScale>();
            return button;
        }

        public static ToggleButton Toggle(Transform parent, string name, Sprite icon, Color face, Color iconColor, Vector2 size)
        {
            var button = IconButton(parent, name, icon, face, iconColor, size, out var iconImage);
            var slash = Image(button.transform, "Off", SpriteBaker.Get(SpriteBaker.Shape.Slash), SceneKit.Hex("#E5484D"));
            Centre(slash.rectTransform, Vector2.zero, size * 0.7f);
            var toggle = button.gameObject.AddComponent<ToggleButton>();
            SceneKit.SetRef(toggle, "_icon", iconImage);
            SceneKit.SetRef(toggle, "_offMark", slash.gameObject);
            return toggle;
        }

        /// <summary>Header shared by both games: home button, level title, tutorial tip line.</summary>
        public static TopBar CreateTopBar(RectTransform safeRoot, Color textColor, Color buttonFace, Color iconColor)
        {
            var bar = Child(safeRoot, "TopBar");
            Place(Rect(bar), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1080f, 200f));
            var home = IconButton(bar.transform, "Home", SpriteBaker.Get(SpriteBaker.Shape.Home), buttonFace, iconColor, new Vector2(140f, 140f), out _);
            Place(Rect(home.gameObject), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(140f, 140f));
            Rect(home.gameObject).pivot = new Vector2(0f, 0.5f);

            var level = Text(bar.transform, "Level", "Level 1", 68f, textColor);
            Centre(level.rectTransform, Vector2.zero, new Vector2(700f, 120f));

            var tip = Text(safeRoot, "Tip", "", 46f, textColor);
            Place(tip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(980f, 110f));

            var topBar = bar.AddComponent<TopBar>();
            SceneKit.SetRef(topBar, "_home", home);
            SceneKit.SetRef(topBar, "_level", level);
            SceneKit.SetRef(topBar, "_tip", tip);
            return topBar;
        }

        /// <summary>Modal result card (win or lose) with an animated badge and one main button.</summary>
        public static ResultPanel CreateResultPanel(Transform canvasRoot, string name, Color accent, SpriteBaker.Shape badgeIcon, string buttonLabel, bool rays = false)
        {
            var panelGo = Child(canvasRoot, name);
            Stretch(Rect(panelGo));
            panelGo.AddComponent<CanvasGroup>();
            var dim = panelGo.AddComponent<Image>();
            dim.color = Dim;
            dim.raycastTarget = true;

            var rounded = SpriteBaker.Get(SpriteBaker.Shape.RoundedRect);
            Image raysImage = null;
            if (rays)
            {
                raysImage = Image(panelGo.transform, "Rays", SpriteBaker.Get(SpriteBaker.Shape.Rays), new Color(1f, 0.95f, 0.75f, 0.55f));
                Centre(raysImage.rectTransform, new Vector2(0f, 540f), new Vector2(1100f, 1100f));
            }
            var card = Image(panelGo.transform, "Card", rounded, Card, true, 0.7f);
            Centre(card.rectTransform, new Vector2(0f, -40f), new Vector2(880f, 860f));

            var badge = Image(card.transform, "Badge", SpriteBaker.Get(SpriteBaker.Shape.Circle), accent);
            Place(badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 150f), new Vector2(300f, 300f));
            badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var ring = Image(badge.transform, "Ring", SpriteBaker.Get(SpriteBaker.Shape.Ring), Color.white);
            Centre(ring.rectTransform, Vector2.zero, new Vector2(300f, 300f));
            var icon = Image(badge.transform, "Icon", SpriteBaker.Get(badgeIcon), Color.white);
            Centre(icon.rectTransform, new Vector2(0f, -4f), new Vector2(170f, 170f));

            var title = Text(card.transform, "Title", "Title", 92f, Ink);
            Centre(title.rectTransform, new Vector2(0f, 190f), new Vector2(820f, 130f));
            var subtitle = Text(card.transform, "Subtitle", "Subtitle", 50f, SoftInk, TextAlignmentOptions.Center, false);
            Centre(subtitle.rectTransform, new Vector2(0f, 40f), new Vector2(780f, 160f));

            var button = TextButton(card.transform, "Primary", buttonLabel, accent, new Vector2(600f, 180f), out var label, 70f);
            Centre(Rect(button.gameObject), new Vector2(0f, -250f), new Vector2(600f, 180f));

            var panel = panelGo.AddComponent<ResultPanel>();
            SceneKit.SetRef(panel, "_card", card.rectTransform);
            SceneKit.SetRef(panel, "_title", title);
            SceneKit.SetRef(panel, "_subtitle", subtitle);
            SceneKit.SetRef(panel, "_badge", badge.rectTransform);
            if (raysImage != null) SceneKit.SetRef(panel, "_rays", raysImage.rectTransform);
            SceneKit.SetRef(panel, "_primary", button);
            SceneKit.SetRef(panel, "_primaryLabel", label);
            return panel;
        }
    }
}
