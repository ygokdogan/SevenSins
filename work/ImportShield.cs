using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class ImportShield {
 public static object Main() {
  Directory.CreateDirectory("Assets/Art/Enemies/ShieldBearer");
  const string p="Assets/Art/Enemies/ShieldBearer/ShieldBasher.fbx";
  File.Copy("/Users/ygokdogan/Documents/Codex/2026-10-04/blender-plugin-computer-use-openai-bundled/outputs/shield_basher.fbx",p,true);
  AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);
  var imp=(ModelImporter)AssetImporter.GetAtPath(p); imp.importAnimation=false; imp.animationType=ModelImporterAnimationType.None; imp.SaveAndReimport();
  var m=AssetDatabase.LoadAssetAtPath<GameObject>(p);
  return new { model=m.GetComponentsInChildren<Transform>(true).Select(t=>new { t.name,pos=t.position.ToString(),rot=t.eulerAngles.ToString(),scale=t.lossyScale.ToString() }).ToArray(), materials=m.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().Select(x=> new { x.name,color=x.color.ToString(),shader=x.shader.name }).ToArray(), enemies=UnityEngine.Object.FindObjectsByType<Enemies.Combat.EnemyShield>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(e=>new { e.name,prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(e),pos=e.transform.position.ToString() }).ToArray() };
 }
}
