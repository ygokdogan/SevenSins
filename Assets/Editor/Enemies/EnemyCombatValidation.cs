using System;
using Combat;
using Combat.AttackIndicators;
using Enemies.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using EnemyBase = Enemies.Core.EnemyBase;

public static class EnemyCombatValidation
{
    [MenuItem("Tools/Western Samurai/Enemies/Validate Shield Combat (Play Mode)")]
    public static void Validate()
    {
        if (!EditorApplication.isPlaying) { Debug.LogError("Run Shield Bearer validation in Play Mode."); return; }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shield.prefab");
        GameObject instance = null;
        GameObject attacker = null;
        Transform droppedShield = null;
        try
        {
            Vector3 spawn = Vector3.zero;
            var placed = UnityEngine.Object.FindAnyObjectByType<EnemyShield>();
            if (placed != null) spawn = placed.transform.position;
            if (NavMesh.SamplePosition(spawn, out NavMeshHit navHit, 30f, NavMesh.AllAreas)) spawn = navHit.position;
            instance = UnityEngine.Object.Instantiate(prefab, spawn, Quaternion.identity);
            var enemy = instance.GetComponent<EnemyShield>();
            enemy.enabled = false;
            attacker = new GameObject("Shield validation attacker");
            float initialHealth = enemy.CurrentHealth;
            attacker.transform.position = enemy.transform.position + enemy.transform.forward * 2f;
            enemy.TakeHit(new HitInfo { attacker = attacker, damage = 20f, poiseDamage = 10f });
            Require(Mathf.Approximately(enemy.CurrentHealth, initialHealth), "Front strike must block health damage");
            Require(Mathf.Approximately(enemy.PostureDamage, 0.5f), "Front posture damage must be 5 percent");
            Require(enemy.BlocksDirection(enemy.transform.forward), "Forward direction must block");
            Require(!enemy.BlocksDirection(enemy.transform.right), "Side direction must be exposed");
            Require(!enemy.BlocksDirection(-enemy.transform.forward), "Back direction must be exposed");
            attacker.transform.position = enemy.transform.position - enemy.transform.forward * 2f;
            enemy.TakeHit(new HitInfo { attacker = attacker, damage = 20f });
            Require(Mathf.Approximately(enemy.CurrentHealth, initialHealth - 20f), "Back strike must deal full damage");
            attacker.transform.position = enemy.transform.position + enemy.transform.right * 2f;
            enemy.TakeHit(new HitInfo { attacker = attacker, damage = 10f });
            Require(Mathf.Approximately(enemy.CurrentHealth, initialHealth - 30f), "Side strike must deal full damage");
            enemy.Parried();
            Require(!enemy.ShieldBroken, "Sword parry must preserve shield");
            enemy.OnGrapplePulled();
            Require(!enemy.BlocksDirection(enemy.transform.forward), "Grapple pull must expose front");
            enemy.IsBashing = true;
            droppedShield = instance.transform.Find("Visual/Body Parts/Shield");
            var shieldHand = instance.transform.Find("Visual/Body Parts/Left Hand");
            enemy.Parried();
            Require(shieldHand != null && shieldHand.IsChildOf(instance.transform), "Shield hand must remain on enemy when shield breaks");
            Require(enemy.ShieldBroken, "Bash parry must permanently break shield");
            Require(droppedShield != null && !droppedShield.gameObject.activeSelf, "Broken shield placeholder must be hidden");
            Require(!enemy.BlocksDirection(enemy.transform.forward), "Broken shield must never block");
            UnityEngine.Object.DestroyImmediate(droppedShield.gameObject); droppedShield = null;
            UnityEngine.Object.DestroyImmediate(instance);
            instance = UnityEngine.Object.Instantiate(prefab, spawn, Quaternion.identity);
            enemy = instance.GetComponent<EnemyShield>(); enemy.enabled = false;
            attacker.transform.position = spawn + Vector3.forward * 1.1f;
            var targetCollider = attacker.AddComponent<CapsuleCollider>(); targetCollider.center = Vector3.up;
            targetCollider.height = 2f; targetCollider.radius = 0.3f;
            var probe = attacker.AddComponent<ShieldBearerHitProbe>();
            Physics.SyncTransforms();
            SetTarget(enemy, attacker.transform, targetCollider);
            var attack = enemy.AttackStyle.CreateState(); enemy.RequestTransition(attack);
            Require(enemy.AttackIndicator.IsVisible && enemy.AttackIndicator.CurrentType == AttackDefense.ParryOnly, "Bash must show parry cue");
            attack.Tick(enemy, 0.49f);
            Require(probe.Count == 0, "Bash must wait for 0.5 second telegraph");
            attack.Tick(enemy, 0.02f); attack.Tick(enemy, 0.01f);
            Require(probe.LastDefense == AttackDefense.ParryOnly, "Bash must bypass block and allow parry");
            Require(probe.Count == 1 && probe.TotalDamage == 12f && probe.LastPush == 12f, "Bash must hit once and push");
            attack.Tick(enemy, 0.28f);
            Require(enemy.AttackIndicator.CurrentType == AttackDefense.Basic, "Sword must switch to basic cue");
            attack.Tick(enemy, 0.15f); attack.Tick(enemy, 0.24f);
            attack.Tick(enemy, 0.15f); attack.Tick(enemy, 0.24f);
            Require(probe.LastDefense == AttackDefense.Basic, "Sword combo must use basic defense");
            Require(probe.Count == 3 && probe.TotalDamage == 28f, "Bash must chain two weak sword hits");
            SetTarget(enemy, attacker.transform, targetCollider);
            attack = enemy.AttackStyle.CreateState(); enemy.RequestTransition(attack);
            probe.ParryEnemy = enemy;
            droppedShield = instance.transform.Find("Visual/Body Parts/Shield");
            attack.Tick(enemy, 0.51f); attack.Tick(enemy, 0.01f);
            Require(!enemy.AttackIndicator.IsVisible, "Parry interruption must hide indicator");
            Require(enemy.ShieldBroken && !enemy.IsBashing,
                "Synchronous bash parry must interrupt combo and stun");
            Require(enemy.BeginGrappleLock(), "Grapple must acquire control");
            Require(!enemy.BeginGrappleLock(), "Repeated grapple must not re-enter the lock");
            var lockedState = enemy.CurrentState;
            float beforeLockedHit = enemy.CurrentHealth;
            enemy.TakeHit(new HitInfo { damage = 1f, knockbackForce = 10f });
            Require(Mathf.Approximately(enemy.CurrentHealth, beforeLockedHit - 1f), "Locked enemy must take damage without an attacker reference");
            Require(enemy.IsGrappleLocked && enemy.CurrentState == lockedState, "Hit must preserve grapple state");
            enemy.Parried();
            enemy.RequestTransition(enemy.States.Attack);
            Require(enemy.CurrentState == lockedState, "Parry, AI transitions and Tick must preserve lock");
            Require(enemy.Movement.BeginGrapplePull(), "Validation enemy must be on NavMesh for pull cleanup");
            enemy.EndGrappleLock();
            Require(!enemy.IsGrappleLocked && !enemy.Movement.IsGrapplePulled && enemy.CurrentState == enemy.States.Idle,
                "Release must exit lock and clean up pull movement");
            enemy.EndGrappleLock();
            Require(enemy.CurrentState == enemy.States.Idle, "Repeated release must be harmless");
            Require(enemy.BeginGrappleLock(), "Released enemy must support another grapple");
            Require(enemy.Movement.BeginGrapplePull(), "Second grapple must start pull");
            enemy.TakeHit(new HitInfo { damage = enemy.CurrentHealth + 1f });
            Require(!enemy.IsGrappleLocked && !enemy.Movement.IsGrapplePulled && enemy.CurrentState == null,
                "Death must exit grapple and clean up movement immediately");
            Require(!enemy.BeginGrappleLock(), "Dead enemy must reject grapple");
            System.IO.File.WriteAllText("/tmp/western-enemy-combat-test.txt", "PASS");
            Debug.Log("SHIELD_BEARER_VALIDATION_PASS: front/side/back damage, 5% posture, grapple opening, sword parry, bash break, hidden broken shield, telegraph timing, bash push, two-hit combo synchronous parry interruption, grapple damage/control, release, reacquisition and death cleanup.");
        }
        catch (Exception exception) { System.IO.File.WriteAllText("/tmp/western-enemy-combat-test.txt", exception.ToString()); Debug.LogException(exception); }
        finally
        {
            if (droppedShield != null) UnityEngine.Object.DestroyImmediate(droppedShield.gameObject);
            if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            if (attacker != null) UnityEngine.Object.DestroyImmediate(attacker);
        }
    }
    // Feed an explicit test target through the same perception object used by runtime AI.
    private static void SetTarget(EnemyBase enemy, Transform target, Collider collider)
    {
        enemy.Perception.Remember(target, collider);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception("Shield Bearer validation: " + message); }
}

public sealed class ShieldBearerHitProbe : MonoBehaviour, IDamageable
{
    public int Count;
    public float TotalDamage;
    public float LastPush;
    public AttackDefense LastDefense;
    public EnemyShield ParryEnemy;
    public void TakeHit(HitInfo hit)
    {
        LastDefense = hit.defense;
        Count++; TotalDamage += hit.damage; LastPush = hit.knockbackForce;
        ParryEnemy?.Parried();
    }
}
