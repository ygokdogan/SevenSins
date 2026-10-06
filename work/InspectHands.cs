using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class InspectHands {
 public static object Main(){
 var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Enemies/ShieldBearer/ShieldBasher.fbx"); var f=p.GetComponentsInChildren<MeshFilter>().Single(x=>x.name=="BODY_Armored_Torso"); var m=f.sharedMesh; var v=m.vertices; var par=Enumerable.Range(0,v.Length).ToArray();
 int Find(int i){while(par[i]!=i){par[i]=par[par[i]];i=par[i];}return i;} void Join(int a,int b){par[Find(a)]=Find(b);}
 var weld=new Dictionary<Vector3Int,int>(); for(int i=0;i<v.Length;i++){var k=Vector3Int.RoundToInt(v[i]*1000000);if(weld.TryGetValue(k,out int j))Join(i,j);else weld[k]=i;}
 var tr=m.triangles;for(int i=0;i<tr.Length;i+=3){Join(tr[i],tr[i+1]);Join(tr[i],tr[i+2]);}
 return Enumerable.Range(0,v.Length).GroupBy(Find).Select(g=>{var b=new Bounds(f.transform.TransformPoint(v[g.First()]),Vector3.zero);foreach(int i in g)b.Encapsulate(f.transform.TransformPoint(v[i]));return new {n=g.Count(),center=b.center.ToString("F4"),size=b.size.ToString("F4")};}).ToArray();
 }
}
