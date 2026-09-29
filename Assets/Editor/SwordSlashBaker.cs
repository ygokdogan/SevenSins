using System.IO;
using System.Text;
using Player.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class SwordSlashBaker
{
    [MenuItem("Tools/Western Samurai/Bake Sword Slash")]
    public static void Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Exit Play Mode before baking sword slash.");
        const string folder = "Assets/Generated/SwordSlash";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var shader = Shader.Find("WesternSamurai/SwordSlash");
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new System.Exception("Slash shader failed to compile.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(folder + "/SwordSlash.mat");
        if (!material)
        {
            material = new Material(shader) { name = "Sword Slash" };
            AssetDatabase.CreateAsset(material, folder + "/SwordSlash.mat");
        }
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(folder + "/SwordSlashMesh.asset");
        if (!mesh)
        {
            mesh = new Mesh { name = "Sword Slash Mesh" };
            AssetDatabase.CreateAsset(mesh, folder + "/SwordSlashMesh.asset");
        }
        int count = 0;
        foreach (string path in new[] { "Assets/Scenes/SampleScene.unity", "Assets/Scenes/CombatTestLevel.unity" })
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                string backup = Path.Combine(Path.GetTempPath(), "WesternSamurai-SlashBackup");
                Directory.CreateDirectory(backup);
                string backupPath = Path.Combine(backup, Path.GetFileName(path));
                if (!File.Exists(backupPath)) File.Copy(path, backupPath);
                foreach (var root in scene.GetRootGameObjects())
                foreach (var combat in root.GetComponentsInChildren<PlayerCombat>(true))
                {
                    MeshFilter blade = null;
                    foreach (var filter in combat.GetComponentsInChildren<MeshFilter>(true))
                        if (filter.name == "lpp_KATANA") { blade = filter; break; }
                    if (!blade) throw new System.Exception("No katana found under " + combat.name);
                    Transform bottom = EnsureChild(blade.transform, "Slash Blade Base");
                    Transform tip = EnsureChild(blade.transform, "Slash Blade Tip");
                    // This model's guard is at local z=0; the cutting edge runs toward -Z.
                    if (!combat.GetComponentInChildren<SwordSlashTrail>(true))
                    {
                        bottom.localPosition = new Vector3(0, 0, -.04f);
                        tip.localPosition = new Vector3(0, 0, blade.sharedMesh.bounds.min.z + .025f);
                    }
                    Transform effect = EnsureChild(blade.transform, "Sword Slash");
                    effect.gameObject.layer = blade.gameObject.layer;
                    var filterComponent = Ensure<MeshFilter>(effect.gameObject);
                    var renderer = Ensure<MeshRenderer>(effect.gameObject);
                    var trail = Ensure<SwordSlashTrail>(effect.gameObject);
                    filterComponent.sharedMesh = mesh;
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    renderer.enabled = false;
                    var serialized = new SerializedObject(trail);
                    serialized.FindProperty("combat").objectReferenceValue = combat;
                    serialized.FindProperty("bladeBase").objectReferenceValue = bottom;
                    serialized.FindProperty("bladeTip").objectReferenceValue = tip;
                    serialized.FindProperty("meshFilter").objectReferenceValue = filterComponent;
                    serialized.FindProperty("meshRenderer").objectReferenceValue = renderer;
                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(trail);
                    count++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("Could not save " + path);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/SwordSlashBakeResult.txt", "SUCCESS: Baked " + count + " sword slash effects in SampleScene and CombatTestLevel.");
        Debug.Log("Baked " + count + " sword slash effects. Scene objects and materials are persisted.");
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component ? component : Undo.AddComponent<T>(go);
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child) return child;
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Bake Sword Slash");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    [MenuItem("Tools/Western Samurai/Inspect Sword Slash Setup")]
    public static void Inspect()
    {
        var report = new StringBuilder();
        foreach (var combat in Object.FindObjectsByType<PlayerCombat>(FindObjectsInactive.Include))
        {
            report.AppendLine("PLAYER " + combat.name + " scene=" + combat.gameObject.scene.path);
            foreach (var filter in combat.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh) report.AppendLine("MESH " + AnimationUtility.CalculateTransformPath(filter.transform, combat.transform) +
                    " bounds=" + filter.sharedMesh.bounds + " scale=" + filter.transform.lossyScale);
        }
        File.WriteAllText("Temp/SwordSlashInspection.txt", report.ToString());
        Debug.Log(report.ToString());
    }
}
