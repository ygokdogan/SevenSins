using Player.Movement;
using UnityEditor;
using UnityEngine;

public static class GrappleAnchorMenu
{
    [MenuItem("GameObject/Western Samurai/Fly Through Grapple Anchor", false, 10)]
    private static void CreateAnchor(MenuCommand command)
    {
        var anchor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        anchor.name = "Fly Through Grapple Anchor";
        GameObjectUtility.SetParentAndAlign(anchor, command.context as GameObject);
        if (anchor.transform.parent == null && SceneView.lastActiveSceneView != null)
            anchor.transform.position = SceneView.lastActiveSceneView.pivot;
        anchor.transform.localScale = Vector3.one * 0.6f;
        anchor.GetComponent<SphereCollider>().isTrigger = true;
        anchor.AddComponent<GrappleAnchor>();
        Undo.RegisterCreatedObjectUndo(anchor, "Create Fly Through Grapple Anchor");
        Selection.activeGameObject = anchor;
    }
}
