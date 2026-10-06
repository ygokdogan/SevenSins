using System.Linq;
using System.Text;
using Enemies.Combat;
using UnityEditor;
using UnityEngine;
using EnemyBase = Enemies.Core.EnemyBase;

public static class EnemyHierarchyAudit
{
    [MenuItem("Tools/Western Samurai/Enemies/Audit Hierarchy")]
    public static void WriteAudit()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var report = new StringBuilder();
        foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include))
        {
            report.AppendLine($"ENEMY {enemy.name} ({enemy.GetType().Name}) scene={enemy.gameObject.scene.path}");
            Visit(enemy.transform, "", report);
        }
        System.IO.File.WriteAllText("/tmp/western-enemy-hierarchy.txt", report.ToString());
    }

    private static void Visit(Transform transform, string indent, StringBuilder report)
    {
        report.AppendLine(indent + transform.name + " | " + string.Join(", ", transform.GetComponents<Component>().Select(c => c == null ? "MISSING" : c.GetType().Name)));
        foreach (var component in transform.GetComponents<MonoBehaviour>())
        {
            if (component == null) continue;
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            while (property.NextVisible(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.name != "m_Script")
                    report.AppendLine(indent + "  " + component.GetType().Name + "." + property.propertyPath + " = " + (property.objectReferenceValue != null ? property.objectReferenceValue.name : "null"));
        }
        foreach (Transform child in transform) Visit(child, indent + "  ", report);
    }
}
