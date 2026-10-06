using System;
using System.Collections.Generic;
using System.Linq;
using Combat;
using Enemies;
using Enemies.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using EnemyBase = Enemies.Core.EnemyBase;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class EnemySystemValidation
{
    private const string Pending = "WesternSamurai.EnemyValidation";
    static EnemySystemValidation() => EditorApplication.playModeStateChanged += OnPlayMode;

    [MenuItem("Tools/Western Samurai/Enemies/Run Refactor Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        try
        {
            foreach (var pair in new[] { (typeof(EnemySamurai), "Samurai"), (typeof(EnemyArcher), "Archer"), (typeof(EnemyShield), "Shield") })
            {
                GameObject created = EnemySetupTools.Create(pair.Item1, pair.Item2);
                try
                {
                    Require(created.GetComponents<EnemyBase>().Length == 1, "One enemy type per root");
                    Require(created.GetComponents<EnemyAttackStyle>().Length == 1, "One attack style per root");
                    Require(created.transform.Find("Visual/Body Parts/Equipment").childCount == 0, "Hands and equipment must remain manual");
                    Require(created.GetComponent<EnemyLocomotionAnimation>().enabled, "Default locomotion must be enabled");
                    string[] expected = { "Transform", "CapsuleCollider", pair.Item1.Name, "NavMeshAgent", "EnemyLocomotionAnimation", "EnemyMovement" };
                    Require(created.GetComponents<Component>().Take(6).Select(c => c.GetType().Name).SequenceEqual(expected), "Archer core component order");
                    foreach (var transform in created.GetComponentsInChildren<Transform>(true))
                        Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script in new enemy");
                }
                finally { Object.DestroyImmediate(created); }
            }
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }
        catch (Exception error) { Report(error.ToString()); Debug.LogException(error); }
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        EditorApplication.isPaused = true;
        EditorApplication.delayCall += RunRuntime;
    }

    private static void RunRuntime()
    {
        try
        {
            EnemyCombatValidation.Validate();
            Require(System.IO.File.ReadAllText("/tmp/western-enemy-combat-test.txt") == "PASS", "Shield combat regression suite");
            foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include))
            {
                if (!enemy.IsAlive) continue;
                Require(enemy.States.Attack != null && enemy.States.Grapple != null && enemy.States.Escape != null, "Complete shared state registration");
                Require(enemy.Locomotion.enabled, "Locomotion enabled for all types");
            }
            TestSamurai();
            TestArcher();
            Report("PASS: three type constructors/core order/manual equipment, shared states, default locomotion, shield guard/combo/parry, grapple damage/release/death, Samurai without Animator, Archer projectile/recovery.");
        }
        catch (Exception error) { Report(error.ToString()); Debug.LogException(error); }
        finally
        {
            EditorApplication.isPaused = false;
            EditorApplication.isPlaying = false;
        }
    }

    private static EnemyBase Clone<T>() where T : EnemyBase
    {
        var source = Object.FindAnyObjectByType<T>();
        Require(source != null, "Scene reference for " + typeof(T).Name);
        Vector3 position = source.transform.position;
        Require(NavMesh.SamplePosition(position, out var hit, 30f, NavMesh.AllAreas), "NavMesh for combat checks");
        var clone = Object.Instantiate(source.gameObject, hit.position, source.transform.rotation).GetComponent<T>();
        clone.enabled = false;
        Require(clone.IsAlive, "Enemy initialized");
        return clone;
    }

    private static void TestSamurai()
    {
        var enemy = Clone<EnemySamurai>();
        var target = new GameObject("Samurai validation target");
        try
        {
            var style = (SamuraiMeleeAttackStyle)enemy.AttackStyle;
            // Exercise a newly authored enemy before the hand/weapon Animator has been assigned.
            var serialized = new SerializedObject(enemy);
            serialized.FindProperty("animationManager").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var collider = target.AddComponent<CapsuleCollider>(); collider.height = 2f; collider.center = Vector3.up;
            var probe = target.AddComponent<ShieldBearerHitProbe>();
            int mask = style.HittableLayers.value;
            for (int i = 0; i < 32; i++) if ((mask & (1 << i)) != 0) { target.layer = i; break; }
            target.transform.position = style.HitCollider.transform.TransformPoint(style.HitCollider.center) - Vector3.up;
            enemy.Perception.Remember(target.transform, collider);
            Physics.SyncTransforms();
            enemy.RequestTransition(enemy.States.Attack);
            enemy.States.Attack.Tick(enemy, style.AttackData.telegraphDuration + 0.01f);
            enemy.States.Attack.Tick(enemy, style.FallbackHitStart + 0.01f);
            Require(probe.Count == 1, "Samurai fallback must hit once");
            enemy.States.Attack.Tick(enemy, style.FallbackDuration + 0.01f);
            Require(enemy.CurrentState != enemy.States.Attack, "Samurai without Animator must finish attack");
        }
        finally { Object.DestroyImmediate(target); Object.DestroyImmediate(enemy.gameObject); }
    }

    private static void TestArcher()
    {
        var enemy = Clone<EnemyArcher>();
        var target = new GameObject("Archer validation target");
        var existing = new HashSet<EnemyProjectile>(Object.FindObjectsByType<EnemyProjectile>(FindObjectsInactive.Include));
        try
        {
            var collider = target.AddComponent<CapsuleCollider>(); collider.height = 2f; collider.center = Vector3.up;
            target.transform.position = enemy.transform.position + enemy.transform.forward * 8f;
            enemy.Perception.Remember(target.transform, collider);
            var style = (ArcherRangedAttackStyle)enemy.AttackStyle;
            var data = style.SelectAttack();
            enemy.RequestTransition(enemy.States.Attack);
            enemy.States.Attack.Tick(enemy, data.telegraphDuration + 0.01f);
            Require(Object.FindObjectsByType<EnemyProjectile>(FindObjectsInactive.Include).Any(p => !existing.Contains(p)), "Archer must fire a projectile");
            enemy.States.Attack.Tick(enemy, data.recoveryDuration + 0.01f);
            Require(enemy.CurrentState == enemy.States.Engage, "Archer must resume engagement after recovery");
        }
        finally
        {
            foreach (var projectile in Object.FindObjectsByType<EnemyProjectile>(FindObjectsInactive.Include))
                if (!existing.Contains(projectile)) Object.DestroyImmediate(projectile.gameObject);
            Object.DestroyImmediate(target); Object.DestroyImmediate(enemy.gameObject);
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Report(string text)
    {
        System.IO.File.WriteAllText("/tmp/western-enemy-refactor-tests.txt", text);
        Debug.Log("ENEMY_REFACTOR_CHECKS: " + text);
    }
}
