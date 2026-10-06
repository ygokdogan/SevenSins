using System;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Player.Combat;
using Player.Movement;
using Player.Camera_Juice;
using Enemies.Combat;
using Combat;
public static class ValidateBlockedRecoil {
 public static object Main(){
 var p=UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
 var attack=AssetDatabase.FindAssets("t:AttackData").Select(g=>AssetDatabase.LoadAssetAtPath<AttackData>(AssetDatabase.GUIDToAssetPath(g))).First(a=>a!=null&&!string.IsNullOrEmpty(a.animationName));
 var obj=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shield.prefab"),p.transform.position+p.transform.forward*2,Quaternion.LookRotation(-p.transform.forward));
 var enemy=obj.GetComponent<EnemyShield>();enemy.enabled=false;
 var move=p.GetComponent<PlayerMovement>();var bob=p.GetComponentInChildren<WeaponBob>();
 object Field(object o,string n)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 void Require(bool c,string m){if(!c)throw new Exception(m);}
 int markers=0;Action<bool> cb=k=>markers++;p.HitConfirmed+=cb;
 try{
 Require(bob!=null,"WeaponBob missing");Require(p.AttackState.Begin(p,attack),"Attack failed to begin");p.AttackState.OnAttackWindowOpened();
 var hp=enemy.CurrentHealth;
 enemy.TakeHit(new HitInfo{attacker=p.gameObject,damage=20,impactMultiplier=1});
 var velocity=(Vector3)Field(move,"knockbackVelocity");
 Require(p.AttackState.WasBlocked&&!p.AttackState.IsAttackWindowOpen,"Attack not deflected");
 Require(velocity.magnitude>0&&velocity.magnitude<3&&Vector3.Dot(velocity,p.transform.forward)<0,"Incorrect recoil velocity");
 Require((float)Field(bob,"blockedRecoilTime")>0,"Weapon visual recoil missing");
 Require(markers==0&&enemy.CurrentHealth==hp,"Blocked hit caused damage/marker");
 move.ApplyKnockback(p.transform.right,1);
 enemy.TakeHit(new HitInfo{attacker=p.gameObject,damage=20});
 Require(Vector3.Distance((Vector3)Field(move,"knockbackVelocity"),p.transform.right)<.001f,"Repeated block reapplied recoil");
 p.AttackState.OnAttackWindowOpened();Require(!p.AttackState.IsAttackWindowOpen,"Blocked swing reopened hit window");
 return "PASS: backward impulse, weapon recoil, no damage/marker, one recoil per swing, hit window remains closed";
 }finally{p.HitConfirmed-=cb;p.ForceChangeState(p.IdleState);UnityEngine.Object.DestroyImmediate(obj);}
 }
}
