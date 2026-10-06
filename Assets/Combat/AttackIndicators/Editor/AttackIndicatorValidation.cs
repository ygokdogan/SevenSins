using System;
using Combat.AttackIndicators;
using UnityEditor;
using UnityEngine;

public static class AttackIndicatorValidation
{
    [MenuItem("Tools/Western Samurai/Attack Indicators/Validate Indicators and Defense Rules")]
    private static void Validate()
    {
        try
        {
            var expectedMatrix = new[]
            {
                new[] { GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Block, GuardResponse.Hit, GuardResponse.Parry },
                new[] { GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Parry },
                new[] { GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit, GuardResponse.Hit }
            };
            foreach (AttackDefense type in Enum.GetValues(typeof(AttackDefense)))
            {
                int caseIndex = 0;
                foreach (bool guarding in new[] { false, true })
                foreach (bool parrying in new[] { false, true })
                foreach (bool facing in new[] { false, true })
                {
                    Require(AttackDefenseRules.Resolve(type, guarding, parrying, facing) == expectedMatrix[(int)type][caseIndex++],
                        "Defense matrix: " + type + " guarding=" + guarding + " parrying=" + parrying + " facing=" + facing);
                }
                var prefab = Resources.Load<AttackIndicatorView>("AttackIndicators/" + type);
                Require(prefab != null, "Missing prefab " + type);
                Require(prefab.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace, "Canvas mode " + type);
                foreach (var image in prefab.GetComponentsInChildren<UnityEngine.UI.Image>())
                    Require(image.sprite != null && !image.raycastTarget && image.rectTransform.sizeDelta.x > 0f, "Noninteractive visible image " + type);
            }
            var arc = AssetDatabase.LoadAssetAtPath<Enemies.Combat.RangedAttackData>("Assets/Resources/Attacks/Enemy/Archer Arc Attack.asset");
            Require(arc != null && arc.defense == AttackDefense.DodgeOnly, "Arc projectile must be dodge-only");
            Debug.Log("ATTACK_INDICATOR_VALIDATION_PASS: 24 guard/parry/facing combinations, 3 prefab assets, sprite references, world canvases and dodge-only arc attack.");
        }
        catch (Exception exception) { Debug.LogException(exception); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
