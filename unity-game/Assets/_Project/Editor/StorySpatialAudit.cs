using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
using Object=UnityEngine.Object;
namespace Ataka.Editor {
 // Read-only scene evidence. Never saves the scene or changes authored colliders.
 public static class StorySpatialAudit {
  [Serializable] class Solid {public string path,kind,door;public Vector3 min,max,position,scale;}
  [Serializable] class Node {public string name;public Vector3 at;}
  [Serializable] class Report {public string scope;public Solid[] solids;public Node[] nodes;public string[][] edges;}
  static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
  public static void Export(){
   EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-logFile");
   if(i<0||i+1>=args.Length)throw new Exception("A log folder is required");
   var folder=Path.GetDirectoryName(args[i+1]);
   var nodes=(Dictionary<string,Vector3>)typeof(StoryDirector).GetField("Nodes",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
   var edges=(string[][])typeof(StoryDirector).GetField("StoryWalks",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
   var doors=Object.FindObjectsByType<ScenarioDoor>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(d=>d.leaf).ToArray();
   var rotations=doors.Select(d=>d.leaf.localRotation).ToArray();
   try{for(int pose=0;pose<2;pose++){
    for(int j=0;j<doors.Length;j++)doors[j].leaf.localRotation=pose==0?rotations[j]:Quaternion.Euler(0,doors[j].direction*95,0);
    Physics.SyncTransforms();
    var solids=Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(c=>c.enabled&&!c.isTrigger).Select(c=>new Solid{path=PathOf(c.transform),kind=c.GetType().Name,door=c.GetComponentInParent<ScenarioDoor>()?PathOf(c.GetComponentInParent<ScenarioDoor>().transform):"",min=c.bounds.min,max=c.bounds.max,position=c.transform.position,scale=c.transform.lossyScale}).OrderBy(c=>c.path).ToArray();
    var r=new Report{scope=pose==0?"Serialized scene pose, read-only bounds":"All door leaves staged at their authored 95 degree direction; inspection only, no scene save",solids=solids,nodes=nodes.Select(n=>new Node{name=n.Key,at=n.Value}).ToArray(),edges=edges};
    File.WriteAllText(Path.Combine(folder,pose==0?"colliders-serialized.json":"colliders-open.json"),JsonUtility.ToJson(r,true));
    Debug.Log("STORY_SPATIAL_AUDIT pose="+pose+" solids="+solids.Length);
   }}finally{for(int j=0;j<doors.Length;j++)doors[j].leaf.localRotation=rotations[j];Physics.SyncTransforms();}
  }
 }
}
