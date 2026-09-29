using System;
using System.Collections.Generic;
using System.IO;
using Core;
using Enemies.Combat.Utilities;
using Player;
using Player.Combat;
using Player.Combat.States;
using Player.Movement;
using Player.Movement.States;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PersistentObjectBaker
{
    [MenuItem("Tools/Western Samurai/Bake Persistent Objects")]
    public static void Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before baking persistent objects.");
        string folder = "Assets/Generated/RuntimeSetup";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        int players = 0;
        var opened = new List<Scene>();
        Scene active = SceneManager.GetActiveScene();
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Scene scene = SceneManager.GetSceneByPath(path);
                if (!scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    opened.Add(scene);
                }
                // Keep a disk snapshot before persisting the authoring migration.
                string backup = Path.Combine(Path.GetTempPath(), "WesternSamurai-PersistentBackup");
                Directory.CreateDirectory(backup);
                string backupFile = Path.Combine(backup, Path.GetFileName(path));
                if (!File.Exists(backupFile)) File.Copy(path, backupFile);
                foreach (GameObject root in scene.GetRootGameObjects())
                    players += BakeHierarchy(root);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset.GetComponentInChildren<PlayerManager>(true) == null &&
                    asset.GetComponentInChildren<HitFx>(true) == null &&
                    asset.GetComponentInChildren<HitFlash>(true) == null &&
                    asset.GetComponentInChildren<PlayerHitFeedback>(true) == null) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    players += BakeHierarchy(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/PersistentObjectBakeResult.txt",
                "SUCCESS: Persistent objects baked and validated. Players: " + players);
            Debug.Log("Persistent objects baked and validated. Players: " + players);
        }
        catch (Exception error)
        {
            File.WriteAllText("Temp/PersistentObjectBakeResult.txt", error.ToString());
            Debug.LogException(error);
            throw;
        }
        finally
        {
            foreach (Scene scene in opened) EditorSceneManager.CloseScene(scene, true);
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        }
    }

    private static int BakeHierarchy(GameObject root)
    {
        var players = root.GetComponentsInChildren<PlayerManager>(true);
        foreach (PlayerManager player in players)
        {
            var serialized = new SerializedObject(player);
            Transform movementRoot = EnsureRoot(player, serialized, "movementStatesRoot", "Movement States");
            Transform combatRoot = EnsureRoot(player, serialized, "combatStatesRoot", "Combat States");
            Ensure<GroundedState>(movementRoot.gameObject);
            Ensure<AirborneState>(movementRoot.gameObject);
            Ensure<WallRunState>(movementRoot.gameObject);
            Ensure<PlayerGrappleState>(movementRoot.gameObject);
            Ensure<DashState>(movementRoot.gameObject);
            Ensure<CombatIdleState>(combatRoot.gameObject);
            Ensure<CombatAttackState>(combatRoot.gameObject);
            Ensure<CombatBlockState>(combatRoot.gameObject);
            Ensure<CombatHitState>(combatRoot.gameObject);
            Ensure<CombatSheatheState>(combatRoot.gameObject);
            DashParticleBaker.Bake(Ensure<DashVisualEffects>(player.gameObject));
            var hud = Ensure<PlayerCombatHud>(player.gameObject);
            hud.BakePersistentObjects();
            HudSpriteBaker.Configure(hud);
            EditorUtility.SetDirty(hud);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(player);
        }
        foreach (GrappleController grapple in root.GetComponentsInChildren<GrappleController>(true))
        {
            grapple.BakePersistentObjects();
            EditorUtility.SetDirty(grapple);
        }
        foreach (PlayerHitFeedback feedback in root.GetComponentsInChildren<PlayerHitFeedback>(true))
        {
            feedback.BakePersistentObjects();
            EditorUtility.SetDirty(feedback);
        }
        foreach (HitFx fx in root.GetComponentsInChildren<HitFx>(true))
        {
            fx.BakePersistentObjects();
            EditorUtility.SetDirty(fx);
        }
        foreach (PlayerManager player in players)
        {
            var hud = new SerializedObject(player.GetComponent<PlayerCombatHud>());
            foreach (string field in new[] { "canvas", "root", "grappleRoot", "hitRoot", "grappleGroup", "hitGroup", "crosshairImage", "grappleImage", "hitImage" })
                if (hud.FindProperty(field).objectReferenceValue == null)
                    throw new InvalidOperationException(player.name + ": Missing HUD field " + field);
        }
        return players.Length;
    }

    private static T Ensure<T>(GameObject go) where T : Component =>
        go.GetComponent<T>() ?? go.AddComponent<T>();

    private static Transform EnsureRoot(PlayerManager player, SerializedObject serialized, string field, string name)
    {
        SerializedProperty property = serialized.FindProperty(field);
        if (property.objectReferenceValue is Transform existing) return existing;
        Transform root = player.transform.Find(name);
        if (root == null)
        {
            root = new GameObject(name).transform;
            root.SetParent(player.transform, false);
        }
        property.objectReferenceValue = root;
        return root;
    }
}
