using System;
using System.Linq;
using Combat.AttackIndicators;
using Core;
using Enemies;
using Enemies.Combat;
using Enemies.Combat.Utilities;
using Enemies.Combat.Visual_Feedback;
using Enemies.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using EnemyBase = Enemies.Core.EnemyBase;
using Object = UnityEngine.Object;

// All hierarchy changes go through Unity APIs; GUIDs, serialized references and authored rigs survive.
public static class EnemySetupTools
{
    [MenuItem("Tools/Western Samurai/Enemies/Normalize Scene and Prefabs")]
    public static void NormalizeAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        MoveAssetIfNeeded("Assets/Prefabs/Enemies/ShieldBearer.prefab", "Assets/Prefabs/Enemies/Shield.prefab");
        MoveAssetIfNeeded("Assets/Resources/EnemyConfigs/ShieldBearer.asset", "Assets/Resources/EnemyConfigs/Shield.asset");
        var report = new System.Text.StringBuilder();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponent<EnemyBase>() == null) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Normalize(root.GetComponent<EnemyBase>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
                report.AppendLine("Prefab: " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include))
        {
            Undo.RegisterFullObjectHierarchyUndo(enemy.gameObject, "Normalize enemy hierarchy");
            Normalize(enemy);
            PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
            EditorSceneManager.MarkSceneDirty(enemy.gameObject.scene);
            report.AppendLine("Scene: " + enemy.name);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        EnemyHierarchyAudit.WriteAudit();
        System.IO.File.WriteAllText("/tmp/western-enemy-migration.txt", report.ToString());
        Debug.Log("ENEMY_NORMALIZATION_PASS\n" + report);
    }

    public static void Normalize(EnemyBase enemy)
    {
        GameObject root = enemy.gameObject;
        if (enemy is EnemyShield) root.name = "Shield";
        RefreshTypeName(enemy);
        RefreshTypeName(root.GetComponent<EnemyAttackStyle>());
        Transform visual = Child(root.transform, "Visual");
        Transform body = Child(visual, "Body Parts");
        Transform effects = Child(visual, "Effects");
        Ensure<HitJuice>(visual.gameObject).enabled = true;
        Ensure<HitFlash>(body.gameObject);
        Ensure<Animator>(body.gameObject);
        var audio = Ensure<AudioSource>(visual.gameObject);
        var fx = Ensure<HitFx>(visual.gameObject);
        var oldFx = root.GetComponent<HitFx>();
        var oldAudio = root.GetComponent<AudioSource>();
        if (oldAudio != null) EditorUtility.CopySerialized(oldAudio, audio);
        if (oldFx != null) EditorUtility.CopySerialized(oldFx, fx);
        SetReference(fx, "audioSource", audio);
        var fxData = new SerializedObject(fx);
        foreach (string field in new[] { "hitParticles", "deathParticles" })
        {
            var particles = fxData.FindProperty(field).objectReferenceValue as ParticleSystem;
            if (particles != null && particles.transform.parent != visual)
                particles.transform.SetParent(visual, true);
        }
        if (oldFx != null) Object.DestroyImmediate(oldFx);
        if (oldAudio != null) Object.DestroyImmediate(oldAudio);
        foreach (var stun in root.GetComponentsInChildren<EnemyStunVisual>(true))
            if (stun.transform.parent != effects) stun.transform.SetParent(effects, true);
        // Preserve the existing generated hand as authored geometry; shield hiding must not hide it.
        var shieldHand = body.Find("Shield/Left Hand");
        if (shieldHand != null) shieldHand.SetParent(body, true);
        var locomotion = Ensure<EnemyLocomotionAnimation>(root);
        SetReference(locomotion, "capsuleVisual", body);
        locomotion.enabled = true;
        var animation = root.GetComponentInChildren<EnemyAnimationManager>(true);
        if (animation != null) SetReference(enemy, "animationManager", animation);
        Ensure<EnemyMovement>(root);
        Ensure<EnemyAttackIndicator>(root);
        // Match the reference Archer's Inspector order, followed by the shared attack indicator.
        Order(root, root.GetComponent<Collider>(), enemy, root.GetComponent<NavMeshAgent>(),
            locomotion, root.GetComponent<EnemyMovement>(), root.GetComponent<EnemyAttackStyle>(),
            root.GetComponent<EnemyAttackIndicator>());
        Order(visual.gameObject, visual.GetComponent<HitJuice>(), fx, audio);
        Order(body.gameObject, body.GetComponent<HitFlash>(), body.GetComponent<Animator>());
        EditorUtility.SetDirty(enemy);
        EditorUtility.SetDirty(root);
    }

    [MenuItem("GameObject/Western Samurai/Enemies/Samurai", false, 11)]
    private static void CreateSamurai(MenuCommand command) => Create(typeof(EnemySamurai), "Samurai", command);
    [MenuItem("GameObject/Western Samurai/Enemies/Archer", false, 12)]
    private static void CreateArcher(MenuCommand command) => Create(typeof(EnemyArcher), "Archer", command);
    [MenuItem("GameObject/Western Samurai/Enemies/Shield", false, 13)]
    private static void CreateShield(MenuCommand command) => Create(typeof(EnemyShield), "Shield", command);

    public static GameObject Create(Type enemyType, string configName, MenuCommand command = null)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>($"Assets/Resources/EnemyConfigs/{configName}.asset");
        if (config == null) throw new InvalidOperationException("Missing EnemyConfig: " + configName);
        var root = new GameObject(enemyType.Name);
        root.SetActive(false);
        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = Vector3.up; collider.height = 2f; collider.radius = 0.4f;
        var enemy = (EnemyBase)root.AddComponent(enemyType);
        SetReference(enemy, "config", config);
        var agent = root.GetComponent<NavMeshAgent>();
        agent.radius = 0.4f; agent.height = 2f;
        var visual = Child(root.transform, "Visual");
        var body = Child(visual, "Body Parts");
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Body Placeholder";
        capsule.transform.SetParent(body, false);
        capsule.transform.localPosition = Vector3.up;
        capsule.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
        Object.DestroyImmediate(capsule.GetComponent<Collider>());
        // Empty manual-authoring space. This tool never creates hands, weapons or hand animations.
        Child(body, "Equipment");
        var origin = Child(body, "Attack Origin");
        origin.localPosition = new Vector3(0f, 1.3f, 0.7f);
        var style = root.GetComponent<EnemyAttackStyle>();
        if (style is ArcherRangedAttackStyle)
        {
            SetReference(style, "muzzle", origin);
            SetReference(style, "attackData", AssetDatabase.LoadAssetAtPath<RangedAttackData>("Assets/Resources/Attacks/Enemy/Archer Arc Attack.asset"));
        }
        if (style is SamuraiMeleeAttackStyle)
        {
            var hitBox = Child(root.transform, "Hit Collider").gameObject.AddComponent<BoxCollider>();
            hitBox.center = new Vector3(0f, 1f, 1.1f); hitBox.size = new Vector3(1.2f, 1.8f, 2f);
            hitBox.isTrigger = true; hitBox.enabled = false;
            SetReference(style, "hitCollider", hitBox);
            var dataGuid = AssetDatabase.FindAssets("Basic Attack t:AttackData", new[] { "Assets/Resources" }).FirstOrDefault();
            if (dataGuid != null) SetReference(style, "attackData", AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(dataGuid)));
            var so = new SerializedObject(style);
            so.FindProperty("hittableLayers").intValue = ~0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        // Copy only feedback assets from the reference Archer, never its body/equipment.
        var reference = Object.FindObjectsByType<EnemyArcher>(FindObjectsInactive.Include).FirstOrDefault(e => e.gameObject != root);
        if (reference != null)
        {
            var sourceFx = reference.GetComponentInChildren<HitFx>(true);
            var targetFx = visual.gameObject.AddComponent<HitFx>();
            if (sourceFx != null)
            {
                EditorUtility.CopySerialized(sourceFx, targetFx);
                var sourceData = new SerializedObject(sourceFx);
                foreach (string field in new[] { "hitParticles", "deathParticles" })
                {
                    var source = sourceData.FindProperty(field).objectReferenceValue as ParticleSystem;
                    if (source != null) SetReference(targetFx, field, Object.Instantiate(source, visual));
                }
            }
            var stun = reference.GetComponentInChildren<EnemyStunVisual>(true);
            if (stun != null) Object.Instantiate(stun.gameObject, Child(visual, "Effects"));
        }
        Normalize(enemy);
        root.GetComponentInChildren<HitFx>(true).BakePersistentObjects();
        int layer = LayerMask.NameToLayer("Hittable");
        if (layer >= 0) foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
        GameObjectUtility.SetParentAndAlign(root, command?.context as GameObject);
        root.SetActive(true);
        Undo.RegisterCreatedObjectUndo(root, "Create enemy");
        Selection.activeGameObject = root;
        return root;
    }

    private static void MoveAssetIfNeeded(string from, string to)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(from) == null) return;
        string error = AssetDatabase.MoveAsset(from, to);
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
    }

    private static void RefreshTypeName(MonoBehaviour component)
    {
        if (component == null) return;
        var data = new SerializedObject(component);
        var property = data.FindProperty("m_EditorClassIdentifier");
        if (property == null) return;
        property.stringValue = component.GetType().Assembly.GetName().Name + "::" + component.GetType().FullName;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T Ensure<T>(GameObject root) where T : Component
    {
        var existing = root.GetComponent<T>();
        return existing != null ? existing : root.AddComponent<T>();
    }
    private static Transform Child(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) return child;
        child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }
    private static void SetReference(Object target, string field, Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Order(GameObject root, params Component[] ordered)
    {
        int index = 1;
        foreach (var component in ordered)
        {
            if (component == null) continue;
            int safety = 30;
            while (Array.IndexOf(root.GetComponents<Component>(), component) > index && safety-- > 0)
                if (!UnityEditorInternal.ComponentUtility.MoveComponentUp(component)) break;
            index++;
        }
    }
}
