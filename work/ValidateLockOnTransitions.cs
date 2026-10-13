using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Enemies.Core;
using Player.CameraFeel;
public static class ValidateLockOnTransitions
{
    static FieldInfo Field(Type t,string n)=>t.GetField(n,BindingFlags.Instance|BindingFlags.NonPublic);
    static void Set(object o,string n,object v)=>Field(o.GetType(),n).SetValue(o,v);
    public static object Main()
    {
        if(Application.isPlaying) throw new Exception("Run in Edit mode");
        var enemies=UnityEngine.Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Take(3).ToArray();
        if(enemies.Length<3) throw new Exception("Need three scene enemies");
        var positions=enemies.Select(e=>e.transform.position).ToArray();
        var points=enemies.Select(e=>e.lockPoint).ToArray();
        var state=Field(typeof(EnemyBase),"stateMachine"); var dead=Field(typeof(EnemyBase),"isDead");
        var states=enemies.Select(e=>state.GetValue(e)).ToArray(); var deaths=enemies.Select(e=>dead.GetValue(e)).ToArray();
        var created=new List<GameObject>(); var passed=new List<string>();
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);passed.Add(name);};
        var origin=new Vector3(5000,5000,5000);
        try
        {
            var root=new GameObject("LockOn validation");created.Add(root);root.transform.position=origin;
            var pivot=new GameObject("Pitch");pivot.transform.SetParent(root.transform,false);
            var l=root.AddComponent<LockOn>();Set(l,"camPivot",pivot.transform);Set(l,"enemyMask",(LayerMask)(1<<7));Set(l,"obstacleMask",(LayerMask)1);Set(l,"targetSwitchDelay",0f);
            for(int i=0;i<3;i++)
            {
                enemies[i].transform.position=origin+new Vector3(i==0?0:i==1?4:-4,0,8);
                var point=new GameObject("Test aim");created.Add(point);point.transform.position=enemies[i].transform.position;
                enemies[i].lockPoint=point.transform;state.SetValue(enemies[i],new Core.StateMachine<EnemyBase>(enemies[i]));dead.SetValue(enemies[i],false);
            }
            var self=new GameObject("Self collider");self.transform.SetParent(root.transform,false);self.transform.localPosition=Vector3.forward;self.AddComponent<BoxCollider>();
            Physics.SyncTransforms();l.Toggle();check(l.Target==enemies[0],"Acquire nearest screen center and ignore own solid collider");
            l.HandleSwitchInput(1,true);check(l.Target==enemies[0],"Held input on acquisition cannot switch");
            l.HandleSwitchInput(0,true);l.HandleSwitchInput(1,true);check(l.Target==enemies[1],"Right flick selects right neighbor");
            l.HandleSwitchInput(-1,true);check(l.Target==enemies[1],"No repeat without neutral");
            l.HandleSwitchInput(0,true);l.HandleSwitchInput(-1,true);check(l.Target==enemies[0],"Neutral re-arms left switch");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);created.Add(wall);wall.transform.position=origin+Vector3.forward*4;wall.transform.localScale=new Vector3(20,20,1);Physics.SyncTransforms();
            l.Tick(.1f,-80,80);check(l.IsLocked,"Short occlusion preserves lock");
            l.Tick(.3f,-80,80);check(!l.IsLocked,"Sustained occlusion releases lock");
            wall.SetActive(false);Physics.SyncTransforms();l.Toggle();check(l.IsLocked,"Visible target reacquires");
            enemies[0].lockPoint.position=origin+Vector3.forward*30;l.Tick(.01f,-80,80);check(!l.IsLocked,"Out of range releases without jumping to another enemy");
            enemies[0].lockPoint.position=enemies[0].transform.position;l.Toggle();dead.SetValue(enemies[0],true);l.Tick(.01f,-80,80);check(l.IsLocked&&l.Target!=enemies[0],"Dead target hands off to visible neighbor");
            l.Release();dead.SetValue(enemies[0],false);enemies[0].lockPoint=null;root.transform.rotation=Quaternion.identity;pivot.transform.localRotation=Quaternion.identity;l.Toggle();check(l.Target==enemies[0],"Missing lock point uses body fallback");
            l.enabled=false;check(!l.IsLocked,"Disabled component clears lock");
            var camera=UnityEngine.Object.FindFirstObjectByType<CameraManager>();
            var cameraPivot=(Transform)Field(typeof(CameraManager),"cameraPitchPivot").GetValue(camera);
            var rotation=cameraPivot.localRotation;var pitch=Field(typeof(CameraManager),"currentPitch").GetValue(camera);
            try {cameraPivot.localRotation=Quaternion.Euler(30,0,0);camera.LookPitch(0);check(Mathf.Abs(Mathf.DeltaAngle(cameraPivot.localEulerAngles.x,30))<.01f,"Free look preserves last locked pitch");}
            finally {cameraPivot.localRotation=rotation;Field(typeof(CameraManager),"currentPitch").SetValue(camera,pitch);}
            return new {passed=passed.Count, checks=passed};
        }
        finally
        {
            for(int i=0;i<enemies.Length;i++){enemies[i].transform.position=positions[i];enemies[i].lockPoint=points[i];state.SetValue(enemies[i],states[i]);dead.SetValue(enemies[i],deaths[i]);}
            foreach(var go in created)if(go)UnityEngine.Object.DestroyImmediate(go);
            Physics.SyncTransforms();
        }
    }
}
