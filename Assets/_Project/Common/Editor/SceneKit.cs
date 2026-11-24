using System.IO;
using PuzzlePair.Feedback;
using PuzzlePair.Fx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuzzlePair.Editor
{
    /// <summary>A scene the "Build All Scenes" tool generates. Implementations are found with TypeCache.</summary>
    public interface ISceneBuilder
    {
        /// <summary>Build-settings order; the main menu is 0.</summary>
        int Order { get; }
        string ScenePath { get; }
        void Build();
    }

    /// <summary>A game that gets a card in the main menu.</summary>
    public interface IGameEntry
    {
        string GameId { get; }
        string SceneName { get; }
        string Title { get; }
        string Tagline { get; }
        int LevelCount { get; }
        Color Accent { get; }
        Color CardColor { get; }
        Sprite Icon { get; }
    }

    /// <summary>Editor helpers shared by every scene builder: scene, camera, sprites, serialized fields.</summary>
    public static class SceneKit
    {
        public const string ScenesFolder = "Assets/_Project/Scenes";
        public const string MaterialsFolder = "Assets/_Project/Common/Art/Materials";
        public const string UnlitShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

        public static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        public static void Save(Scene scene, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ScenesFolder);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        public static CameraRig CreateCamera(Color background, float topInset, float bottomInset)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            go.AddComponent<AudioListener>();
            var rig = go.AddComponent<CameraRig>();
            SetFloat(rig, "_topInset", topInset);
            SetFloat(rig, "_bottomInset", bottomInset);
            return rig;
        }

        public static FxPool CreateFxPool(int sortingOrder, int capacity = 500)
        {
            var go = new GameObject("FxPool");
            var pool = go.AddComponent<FxPool>();
            SetRef(pool, "_circle", SpriteBaker.Get(SpriteBaker.Shape.Circle));
            SetRef(pool, "_square", SpriteBaker.Get(SpriteBaker.Shape.Square));
            SetRef(pool, "_ring", SpriteBaker.Get(SpriteBaker.Shape.Ring));
            SetRef(pool, "_block", SpriteBaker.Get(SpriteBaker.Shape.Block));
            SetRef(pool, "_spark", SpriteBaker.Get(SpriteBaker.Shape.Spark));
            SetInt(pool, "_sortingOrder", sortingOrder);
            SetInt(pool, "_capacity", capacity);
            return pool;
        }

        /// <summary>Top-lit gradient behind the scene plus a soft vignette, following the camera.</summary>
        public static Backdrop CreateBackdrop(Camera camera, Color color, float vignette)
        {
            var go = new GameObject("Backdrop");
            var backdrop = go.AddComponent<Backdrop>();
            var gradient = CreateSprite(go.transform, "Gradient", SpriteBaker.Get(SpriteBaker.Shape.Gradient), color, Vector2.zero, Vector2.one * 20f, -100);
            var shade = CreateSprite(go.transform, "Vignette", SpriteBaker.Get(SpriteBaker.Shape.Vignette), new Color(0.05f, 0.05f, 0.12f, vignette), Vector2.zero, Vector2.one * 20f, 95);
            SetRef(backdrop, "_camera", camera);
            SetRef(backdrop, "_gradient", gradient);
            SetRef(backdrop, "_vignette", shade);
            return backdrop;
        }

        public static TapPrompt CreateTapPrompt(Color color, int sortingOrder)
        {
            var go = new GameObject("TapPrompt");
            var prompt = go.AddComponent<TapPrompt>();
            var ring = CreateSprite(go.transform, "Ring", SpriteBaker.Get(SpriteBaker.Shape.Ring), color, Vector2.zero, Vector2.one, sortingOrder);
            var dot = CreateSprite(go.transform, "Dot", SpriteBaker.Get(SpriteBaker.Shape.Circle), color, Vector2.zero, Vector2.one * 0.3f, sortingOrder + 1);
            SetRef(prompt, "_ring", ring);
            SetRef(prompt, "_dot", dot);
            go.SetActive(false);
            return prompt;
        }

        public static SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 scale, int sortingOrder)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            r.sortingOrder = sortingOrder;
            return r;
        }

        /// <summary>Unlit sprite material (used by LineRenderers, which have no default material).</summary>
        public static Material EnsureMaterial(string name, string shaderName = UnlitShader)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(shaderName) ?? Shader.Find("Sprites/Default");
            if (material == null)
            {
                Directory.CreateDirectory(MaterialsFolder);
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c);
            return c;
        }

        // ---------------------------------------------------------------- serialized fields

        public static void SetRef(Object target, string field, Object value)
        {
            if (value == null && !ReferenceEquals(value, null))
                throw new System.InvalidOperationException($"{target.GetType().Name}.{field}: the object was destroyed (loaded before a scene change?).");
            var so = new SerializedObject(target);
            Find(so, target, field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetRefs(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = Find(so, target, field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            Find(so, target, field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            Find(so, target, field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetColor(Object target, string field, Color value)
        {
            var so = new SerializedObject(target);
            Find(so, target, field).colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetColors(Object target, string field, Color[] values)
        {
            var so = new SerializedObject(target);
            var p = Find(so, target, field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).colorValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Find(SerializedObject so, Object target, string field)
        {
            var p = so.FindProperty(field);
            if (p == null) throw new System.InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'.");
            return p;
        }
    }
}
