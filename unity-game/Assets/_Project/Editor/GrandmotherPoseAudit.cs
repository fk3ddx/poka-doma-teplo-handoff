using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class GrandmotherPoseAudit {
  public static void Run(){
   EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");
   var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   var g=flat.grandmother;g.SetActive(true);g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   var anim=g.GetComponentInChildren<Animation>(true);var lines=new List<string>();
   string idle=null,walk=null;foreach(AnimationState s in anim){lines.Add("CLIP "+s.name+" len="+s.length);if(s.name.EndsWith("Oidle"))idle=s.name;if(s.name.EndsWith("Owalk"))walk=s.name;}
   var transforms=g.GetComponentsInChildren<Transform>(true);var paths=transforms.ToDictionary(t=>AnimationUtility.CalculateTransformPath(t,g.transform),t=>t);
   void Dump(string label){foreach(var n in new[]{"Armature","Back","Chest","Neck","Head"}){var t=transforms.FirstOrDefault(t=>t.name==n);if(t)lines.Add(label+" "+n+" p="+t.position.ToString("F4")+" localP="+t.localPosition.ToString("F4")+" localR="+t.localEulerAngles.ToString("F2")+" scale="+t.localScale.ToString("F4"));}}
   Dump("scene");anim[idle].clip.SampleAnimation(anim.gameObject,0);Dump("idle");anim[walk].clip.SampleAnimation(anim.gameObject,0);Dump("walk");anim[idle].clip.SampleAnimation(anim.gameObject,0);Dump("walk_then_idle");
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Characters/KatchoGranny/NinaKatcho.prefab");
   foreach(var t in prefab.GetComponentsInChildren<Transform>(true)){var path=AnimationUtility.CalculateTransformPath(t,prefab.transform);if(path.Length>0&&paths.TryGetValue(path,out var dest)){dest.localPosition=t.localPosition;dest.localRotation=t.localRotation;dest.localScale=t.localScale;}}
   Dump("prefab_restored");anim[idle].clip.SampleAnimation(anim.gameObject,0);Dump("prefab_then_idle");
   foreach(var binding in AnimationUtility.GetCurveBindings(anim[idle].clip))lines.Add("IDLE_CURVE "+binding.path+" "+binding.propertyName);
   Directory.CreateDirectory("QA/GrandmotherPoseAudit");File.WriteAllLines("QA/GrandmotherPoseAudit/result.txt",lines);Debug.Log("POSE_AUDIT written; scene NOT saved");
  }
 }
}
