using System;
using System.IO;
using Player.Combat;
using UnityEditor;
using UnityEngine;

// Authoring only: these PNGs are ordinary serialized sprites in the saved scenes.
public static class HudSpriteBaker
{
    public static void Configure(PlayerCombatHud hud)
    {
        Color yellow = new SerializedObject(hud).FindProperty("grappleColor").colorValue;
        Sprite crosshair = Bake("Crosshair", 16, Vector4.zero, p =>
            Box(p, Vector2.zero, new Vector2(9, 1)) || Box(p, Vector2.zero, new Vector2(1, 9)) ? Color.white :
            Box(p, Vector2.zero, new Vector2(11, 3)) || Box(p, Vector2.zero, new Vector2(3, 11)) ? new Color(0, 0, 0, .65f) : Color.clear);
        Sprite hit = Bake("Hitmarker", 72, Vector4.zero, p =>
        {
            bool outline = false;
            for (int i = 0; i < 4; i++)
            {
                float angle = (45 + i * 90) * Mathf.Deg2Rad;
                Vector2 q = new Vector2(p.x * Mathf.Cos(angle) + p.y * Mathf.Sin(angle),
                    -p.x * Mathf.Sin(angle) + p.y * Mathf.Cos(angle));
                if (Box(q, new Vector2(22, 0), new Vector2(16, 4))) return Color.white;
                outline |= Box(q, new Vector2(22, 0), new Vector2(19, 8));
            }
            return outline ? new Color(0, 0, 0, .9f) : Color.clear;
        });
        Sprite grapple = Bake("GrappleReticle", 64, Vector4.one * 20, p =>
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y));
            if (Box(q, new Vector2(23, 28), new Vector2(14, 3)) ||
                Box(q, new Vector2(28, 23), new Vector2(3, 14))) return yellow;
            if (Box(q, new Vector2(23, 28), new Vector2(17, 7)) ||
                Box(q, new Vector2(28, 23), new Vector2(7, 17))) return new Color(.025f, .025f, .035f, .95f);
            return Color.clear;
        });
        hud.ConfigureReticleSprites(crosshair, grapple, hit);
    }

    private static bool Box(Vector2 p, Vector2 center, Vector2 size) =>
        Mathf.Abs(p.x - center.x) <= size.x * .5f && Mathf.Abs(p.y - center.y) <= size.y * .5f;

    private static Sprite Bake(string name, int size, Vector4 border, Func<Vector2, Color> sample)
    {
        const int scale = 4;
        int resolution = size * scale;
        var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        var pixels = new Color[resolution * resolution];
        for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
                pixels[y * resolution + x] = sample(new Vector2((x + .5f) / scale - size * .5f, (y + .5f) / scale - size * .5f));
        texture.SetPixels(pixels);
        texture.Apply();
        string path = "Assets/Generated/RuntimeSetup/" + name + ".png";
        byte[] bytes = texture.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(texture);
        // Do not churn importer state when baking a second scene.
        if (!File.Exists(path) || !System.Linq.Enumerable.SequenceEqual(File.ReadAllBytes(path), bytes))
        {
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100 * scale;
        importer.spriteBorder = border * scale;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
