using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class ValidateHands {
 public static object Main(){
 var p=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Enemies/Shield.prefab");
 try{
 var parts=p.transform.Find("Visual/Body Parts");var s=parts.Find("Short Sword");var h=parts.Find("Shield");
 var right=s.Find("Right Hand");var left=h.Find("Left Hand");
 if(right==null||left==null)throw new Exception("Hands missing");
 var body=parts.Find("Body Bob");var before=body.localToWorldMatrix;
 var rightBefore=right.position; s.localPosition+=Vector3.forward*.5f;
 if(Vector3.Distance(right.position,rightBefore+parts.TransformVector(Vector3.forward*.5f))>.0001f)throw new Exception("Sword hand did not follow");
 if(body.localToWorldMatrix!=before)throw new Exception("Body moved");
 var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Enemies/ShieldBearer/ShieldBasher.fbx").GetComponentsInChildren<MeshFilter>().Single(x=>x.name=="BODY_Armored_Torso").sharedMesh;
 var meshes=new[]{body.GetComponentsInChildren<MeshFilter>().Single(x=>x.name=="BODY_Armored_Torso").sharedMesh,right.GetComponentInChildren<MeshFilter>().sharedMesh,left.GetComponentInChildren<MeshFilter>().sharedMesh};
 if(meshes.Sum(x=>x.triangles.Length)!=source.triangles.Length)throw new Exception("Triangle conservation failed");
 return new {result="PASS",triangles=source.triangles.Length/3,right=meshes[1].vertexCount,left=meshes[2].vertexCount,bodyUnaffected=true};
 }finally{PrefabUtility.UnloadPrefabContents(p);}
 }
}
