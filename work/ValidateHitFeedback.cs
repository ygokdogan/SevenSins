using System;
using System.Reflection;
using Combat;
using Enemies.Combat;
using Enemies.Combat.Utilities;
using Player.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
public static class ValidateHitFeedback {
 public static object Main(){
 if(!EditorApplication.isPlaying)throw new Exception("Play mode required");
 var player=UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
 var hud=player.GetComponent<PlayerCombatHud>();
 var image=(UnityEngine.UI.Image)typeof(PlayerCombatHud).GetField("hitImage",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shield.prefab");
 var pos=player.transform.position+player.transform.forward*4;
 if(NavMesh.SamplePosition(pos,out var hit,20,NavMesh.AllAreas))pos=hit.position;
 var obj=UnityEngine.Object.Instantiate(prefab,pos,Quaternion.identity);
 var enemy=obj.GetComponent<EnemyShield>();enemy.enabled=false;
 var fx=new SerializedObject(obj.GetComponent<HitFx>());
 var blood=(ParticleSystem)fx.FindProperty("hitParticles").objectReferenceValue;
 var death=(ParticleSystem)fx.FindProperty("deathParticles").objectReferenceValue;
 int count=0;bool killed=false;Action<bool> callback=k=>{count++;killed=k;};player.HitConfirmed+=callback;
 void Require(bool c,string m){if(!c)throw new Exception(m);}
 try{
 var toward=player.transform.position-pos;toward.y=0;obj.transform.rotation=Quaternion.LookRotation(toward);
 var info=new HitInfo{attacker=player.gameObject,damage=5,impactMultiplier=1,hitPoint=pos+Vector3.up,hitDirection=-toward.normalized};
 enemy.TakeHit(info);Require(count==0,"Blocked hit showed marker");Require(!blood.isPlaying,"Blocked hit emitted blood");
 obj.transform.rotation=Quaternion.LookRotation(-toward);
 enemy.TakeHit(info);Require(count==1&&!killed,"Damage confirmation missing");Require(image.color==Color.white,"Damage marker not white");Require(blood.isPlaying,"Hit blood not playing");
 info.damage=10000;enemy.TakeHit(info);Require(count==2&&killed,"Kill confirmation missing");Require(image.color==Color.red,"Kill marker not red");Require(death.transform.parent==null&&death.isPlaying,"Death VFX not detached/playing");
 enemy.TakeHit(info);Require(count==2,"Dead enemy generated duplicate marker");
 return "PASS: block has no marker/blood; damage white + blood; kill red + detached death particles; duplicate death suppressed";
 }finally{player.HitConfirmed-=callback;if(death!=null)UnityEngine.Object.DestroyImmediate(death.gameObject);if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);}
 }
}
