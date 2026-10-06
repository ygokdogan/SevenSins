using Combat.AttackIndicators;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AttackIndicatorBuilder
{
    public const string Root = "Assets/Combat/AttackIndicators";
    static AttackIndicatorBuilder() => EditorApplication.delayCall += EnsureAssets;
    private static void EnsureAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Resources/AttackIndicators/DodgeOnly.prefab") != null) return;
        Build();
    }
    [MenuItem("Tools/Western Samurai/Attack Indicators/Build Indicator Assets")]
    public static void Build()
    {
        System.IO.Directory.CreateDirectory(Root + "/Sprites");
        System.IO.Directory.CreateDirectory(Root + "/Resources/AttackIndicators");
        var ring = MakeSprite("CountdownRing", (x, y) =>
        {
            float radius = Mathf.Sqrt(x * x + y * y);
            return radius >= 33f && radius <= 36f;
        });
        foreach (AttackDefense type in System.Enum.GetValues(typeof(AttackDefense)))
        {
            var icon = MakeSprite(type.ToString(), (x, y) => Icon(type, x, y));
            var plate = MakeSprite(type + "Plate", (x, y) => Plate(type, x, y));
            Color color = type == AttackDefense.Basic ? new Color(1f, 0.78f, 0.13f) :
                type == AttackDefense.ParryOnly ? new Color(0.22f, 0.93f, 1f) : new Color(1f, 0.24f, 0.35f);
            var root = new GameObject(type + " Attack Indicator", typeof(RectTransform), typeof(Canvas), typeof(AttackIndicatorView));
            var rect = root.GetComponent<RectTransform>(); rect.sizeDelta = Vector2.one * 80f;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 80;
            Image(root.transform, "Dark Silhouette", plate, new Color(0.025f, 0.03f, 0.055f, 0.96f), 1.13f);
            Image(root.transform, "Accent Plate", plate, color, 1f);
            Image(root.transform, "Inset", plate, new Color(0.065f, 0.075f, 0.11f), 0.81f);
            var symbol = Image(root.transform, "Distinct Symbol", icon, color, 1f);
            Image(root.transform, "Ring Track", ring, new Color(0.1f, 0.13f, 0.19f, 0.85f), 1f);
            var countdown = Image(root.transform, "Windup Countdown", ring, color, 1f);
            countdown.type = UnityEngine.UI.Image.Type.Filled;
            countdown.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            countdown.fillOrigin = 2; countdown.fillClockwise = true; countdown.fillAmount = 0f;
            var serialized = new SerializedObject(root.GetComponent<AttackIndicatorView>());
            serialized.FindProperty("countdown").objectReferenceValue = countdown;
            serialized.FindProperty("icon").objectReferenceValue = symbol;
            serialized.FindProperty("accent").colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.transform.localScale = Vector3.one * 0.009f;
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Resources/AttackIndicators/" + type + ".prefab");
            Object.DestroyImmediate(root);
        }
        // Give the existing high arc attack a dodge cue and matching hit rules.
        var arc = AssetDatabase.LoadAssetAtPath<Enemies.Combat.RangedAttackData>("Assets/Resources/Attacks/Enemy/Archer Arc Attack.asset");
        if (arc != null) { arc.defense = AttackDefense.DodgeOnly; EditorUtility.SetDirty(arc); }
        BuildPreview();
        AssetDatabase.SaveAssets();
        Debug.Log("ATTACK_INDICATORS_BUILT: Basic / ParryOnly / DodgeOnly in " + Root);
    }
    private static void BuildPreview()
    {
        const int width = 600, height = 200;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            int slot = x / 200;
            var type = (AttackDefense)slot;
            Color accent = slot == 0 ? new Color(1f, 0.78f, 0.13f) : slot == 1 ? new Color(0.22f, 0.93f, 1f) : new Color(1f, 0.24f, 0.35f);
            int px = (x % 200 - 100) / 2, py = (y - 100) / 2;
            Color pixel = new Color(0.035f, 0.045f, 0.07f);
            if (Plate(type, Mathf.RoundToInt(px / 1.13f), Mathf.RoundToInt(py / 1.13f))) pixel = Color.black;
            if (Plate(type, px, py)) pixel = accent;
            if (Plate(type, Mathf.RoundToInt(px / 0.81f), Mathf.RoundToInt(py / 0.81f))) pixel = new Color(0.065f, 0.075f, 0.11f);
            if (Icon(type, px, py)) pixel = accent;
            float radius = Mathf.Sqrt(px * px + py * py);
            if (radius >= 33f && radius <= 36f) pixel = accent;
            pixels[y * width + x] = pixel;
        }
        texture.SetPixels(pixels); texture.Apply();
        System.IO.File.WriteAllBytes(Root + "/Preview.png", texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(Root + "/Preview.png", ImportAssetOptions.ForceSynchronousImport);
    }
    private static bool Plate(AttackDefense type, int x, int y)
    {
        if (type == AttackDefense.DodgeOnly) return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) <= 27 && Mathf.Abs(x) + Mathf.Abs(y) <= 39;
        return Mathf.Abs(x) + Mathf.Abs(y) <= (type == AttackDefense.Basic ? 30 : 32);
    }
    private static bool Icon(AttackDefense type, int x, int y)
    {
        if (type == AttackDefense.Basic)
            return (Mathf.Abs(x) <= 3 && y >= -3 && y <= 17) || (Mathf.Abs(x) <= 3 && y >= -15 && y <= -10);
        if (type == AttackDefense.ParryOnly)
            return (Mathf.Abs(x) <= 2 && y >= -7 && y <= 17) ||
                (Mathf.Abs(x) <= 9 && y >= -8 && y <= -5) || (Mathf.Abs(x) <= 3 && y >= -17 && y <= -10) ||
                (Mathf.Abs(x - 13) + Mathf.Abs(y - 10) <= 5) || (Mathf.Abs(x + 13) + Mathf.Abs(y + 1) <= 4);
        return (Mathf.Abs(x - y) <= 3 || Mathf.Abs(x + y) <= 3) && Mathf.Abs(x) <= 15 && Mathf.Abs(y) <= 15;
    }
    private static Sprite MakeSprite(string name, System.Func<int, int, bool> mask)
    {
        const int size = 80;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            pixels[y * size + x] = mask(x - size / 2, y - size / 2) ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
        texture.SetPixels32(pixels); texture.Apply();
        string path = Root + "/Sprites/" + name + ".png";
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 80f;
        importer.spriteImportMode = SpriteImportMode.Single; importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static UnityEngine.UI.Image Image(Transform parent, string name, Sprite sprite, Color color, float scale)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var image = go.GetComponent<UnityEngine.UI.Image>(); image.rectTransform.SetParent(parent, false);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = Vector2.one * 0.5f;
        image.rectTransform.sizeDelta = Vector2.one * 80f * scale;
        image.sprite = sprite; image.color = color; image.raycastTarget = false;
        return image;
    }
}
