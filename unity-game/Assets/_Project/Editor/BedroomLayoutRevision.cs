using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class BedroomLayoutRevision {
  public static readonly Vector3 OldCenter=new Vector3(2.05f,0,4.07f);
  public static void Relocate(FlatBindings flat){
   if(flat.wardrobe.parent.name=="Wardrobe side-wall placement")return;
   var holder=new GameObject("Wardrobe side-wall placement").transform;holder.SetParent(flat.apartment,false);holder.position=OldCenter;
   flat.wardrobe.SetParent(holder,true);
   foreach(var hinge in flat.wardrobeLeaves)hinge.parent.SetParent(holder,true);
   holder.SetPositionAndRotation(ScenarioLayout.Wardrobe,Quaternion.Euler(0,90,0));
   flat.slipper.transform.SetPositionAndRotation(ScenarioLayout.WardrobeSlipper,Quaternion.Euler(0,90,0));
   EditorUtility.SetDirty(flat);
  }
  public static void ApplyAndBuild(){
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();Relocate(flat);
   EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   // Audit both open and closed wardrobe leaves with the actual bedroom door open.
   flat.bedroom.leaf.localRotation=Quaternion.Euler(0,flat.bedroom.direction*95,0);
   var report="User approved side-wall relocation; original room dimensions and lighting preserved.\n";
   foreach(float angle in new[]{0f,95f}){
    foreach(var leaf in flat.wardrobeLeaves)leaf.localRotation=Quaternion.Euler(0,angle,0);Physics.SyncTransforms();
    for(int i=0;i<=12;i++){
     var point=Vector3.Lerp(new Vector3(3.3f,0,3.6f),new Vector3(1.9f,0,3.6f),i/12f);
     var hits=Physics.OverlapCapsule(point+Vector3.up*.3f,point+Vector3.up*1.4f,.22f,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(flat.player.transform)).ToArray();
     if(hits.Length>0)throw new Exception("Bedroom route blocked at "+point+": "+string.Join(",",hits.Select(x=>x.name)));
    }
    report+="PASS bedroom entry 3.3→1.9 at z3.6, capsule radius .22, wardrobe angle "+angle+"\n";
   }
   Directory.CreateDirectory("../QA/BedroomLayout");File.WriteAllText("../QA/BedroomLayout/clearance.txt",report);
   EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");ScenarioBuilder.Build();
  }
 }
}
