using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class NinaScenarioInstaller {
  const string Folder="Assets/_Project/Art/Characters/KatchoGranny";
  const string Prefab=Folder+"/NinaKatcho.prefab", PosePath=Folder+"/Nina_Hang.anim";
  static Transform Bone(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
  static void Aim(Transform bone,Transform child,Vector3 direction){bone.rotation=Quaternion.FromToRotation(child.position-bone.position,direction)*bone.rotation;}
  static void Curve(AnimationClip clip,string path,string property,float value){clip.SetCurve(path,typeof(Transform),property,AnimationCurve.Constant(0,1,value));}
  static AnimationClip MakePose(GameObject actor){
   var animation=actor.GetComponentInChildren<Animation>(true);animation.Stop();animation.playAutomatically=false;animation.enabled=false;
   animation.clip.SampleAnimation(animation.gameObject,0);
   foreach(string side in new[]{"L","R"}){
    var upper=Bone(actor,"Arm_Upper."+side);var lower=Bone(actor,"Arm_Lower."+side);var hand=Bone(actor,"Hand."+side);
    Aim(upper,lower,new Vector3(side=="L"?-.08f:.08f,-1,.02f));Aim(lower,hand,new Vector3(0,-1,.05f));
   }
   var head=Bone(actor,"Head");head.rotation=Quaternion.AngleAxis(24,Vector3.forward)*head.rotation;
   var clip=new AnimationClip{legacy=true,name="Nina_Hang",wrapMode=WrapMode.ClampForever};
   foreach(var bone in animation.GetComponentsInChildren<Transform>(true)){
    if(bone==animation.transform)continue;
    string path=AnimationUtility.CalculateTransformPath(bone,animation.transform);var p=bone.localPosition;var q=bone.localRotation;
    Curve(clip,path,"localPosition.x",p.x);Curve(clip,path,"localPosition.y",p.y);Curve(clip,path,"localPosition.z",p.z);
    Curve(clip,path,"localRotation.x",q.x);Curve(clip,path,"localRotation.y",q.y);Curve(clip,path,"localRotation.z",q.z);Curve(clip,path,"localRotation.w",q.w);
   }
   var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(PosePath);
   if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);EditorUtility.SetDirty(existing);clip=existing;}else AssetDatabase.CreateAsset(clip,PosePath);
   return clip;
  }
  public static GameObject CreateActor(){
   var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));actor.name="Nina — approved Katcho";
   var clip=MakePose(actor);var animation=actor.GetComponentInChildren<Animation>(true);
   Bone(actor,"CaneDummy").gameObject.SetActive(false); // §7.5: free hanging hands; only stool and slipper remain.
   var pose=actor.AddComponent<NinaHangingPose>();pose.animationPlayer=animation;pose.hangingPose=clip;pose.neck=Bone(actor,"Neck");pose.head=Bone(actor,"Head");pose.leftFoot=Bone(actor,"Foot.L");pose.rightFoot=Bone(actor,"Foot.R");
   foreach(var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.updateWhenOffscreen=true;
   actor.transform.position=new Vector3(0,-20,0);actor.SetActive(false);return actor;
  }
  public static void InspectSight(){
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   flat.grandmother.SetActive(true);var pose=flat.grandmother.GetComponent<NinaHangingPose>();pose.Evaluate(0,new Vector3(3.35f,1.56f,3.59f));
   flat.bedroom.leaf.localRotation=Quaternion.Euler(0,flat.bedroom.direction*95,0);flat.stool.transform.SetPositionAndRotation(new Vector3(1.95f,.22f,2.2f),Quaternion.Euler(0,20,90));flat.slipper.transform.position=new Vector3(1.2f,.015f,2.65f);Physics.SyncTransforms();
   foreach(var from in new[]{new Vector3(2.54f,1.56f,3.53f),new Vector3(2.25f,1.56f,3.4f),new Vector3(2.4f,1.56f,3.7f)})
    foreach(var to in new[]{pose.FeetPosition,pose.FacePosition}){
     bool hit=Physics.Linecast(from,to,out var info,~(1<<31),QueryTriggerInteraction.Ignore);
     Debug.Log("NINA_SIGHT from="+from+" to="+to+" hit="+(hit?info.collider.name+" at "+info.point:"clear"));
    }
  }
  public static void InstallAndBuild(){
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   var previous=flat.grandmother;flat.grandmother=CreateActor();UnityEngine.Object.DestroyImmediate(previous);EditorUtility.SetDirty(flat);
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
   Directory.CreateDirectory("../QA/NinaIntegration");
   var pose=flat.grandmother.GetComponent<NinaHangingPose>();pose.Evaluate(3.6f,new Vector3(3.25f,1.55f,3.6f));
   File.WriteAllText("../QA/NinaIntegration/bindings.txt","prefab="+Prefab+"\npose="+PosePath+"\nneck="+pose.NeckPosition.ToString("F4")+"\nfeet="+pose.FeetPosition.ToString("F4")+"\nNo character mesh/texture or accepted lighting changed.\n");
   EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");ScenarioBuilder.Build();
  }
 }
}
