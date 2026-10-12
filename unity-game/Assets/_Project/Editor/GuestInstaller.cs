using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Editor {
 // Idempotent: installs the ready Guest creature and the close-up claw hand prepared by tools/art/prepare_guest.py.
 // Ataka.Editor.GuestInstaller.Install  (then ScenarioBuilder.Build, or InstallAndBuild)
 // Missing files are reported and skipped; the existing proxy stays so the game still runs.
 public static class GuestInstaller {
  const string Root="Assets/_Project/Art/ReadyModels/Sketchfab/";
  [Serializable] public class Mat {public string name,texture,normal;public float[] color;public float roughness;}
  [Serializable] public class Manifest {public string id;public float size;public bool rigged;public string[] actions;public Mat[] materials;}
  static Manifest Read(string id)=>JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+id+"/manifest.json"));
  static void Materialise(string id,GameObject model,Manifest m)=>MaterialiseFolder(Root+id+"/",model,m);
  // Author textures → URP Lit materials saved next to the prepared model (folder ends with '/').
  public static void MaterialiseFolder(string folder,GameObject model,Manifest m){
   foreach(var r in model.GetComponentsInChildren<Renderer>(true)){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){
    var spec=m.materials.FirstOrDefault(x=>mats[i]&&x.name==mats[i].name)??m.materials.FirstOrDefault();if(spec==null)continue;
    string path=folder+spec.name+"_URP.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
    if(!string.IsNullOrEmpty(spec.texture))mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+spec.texture));
    if(!string.IsNullOrEmpty(spec.normal)){var normalPath=folder+spec.normal;var importer=AssetImporter.GetAtPath(normalPath) as TextureImporter;if(importer&&importer.textureType!=TextureImporterType.NormalMap){importer.textureType=TextureImporterType.NormalMap;importer.sRGBTexture=false;importer.SaveAndReimport();}var n=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);if(n){mat.SetTexture("_BumpMap",n);mat.EnableKeyword("_NORMALMAP");}}
    var c=spec.color!=null&&spec.color.Length>=3?new Color(spec.color[0],spec.color[1],spec.color[2]):Color.white;mat.SetColor("_BaseColor",c);
    mat.SetFloat("_Smoothness",Mathf.Clamp01(1-spec.roughness)*.6f);EditorUtility.SetDirty(mat);mats[i]=mat;}
    r.sharedMaterials=mats;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
  }
  // FaceTearDeath stages the claw hand by "fingers" (towards the claws) and "palm" (the side the claws hook to),
  // measured here in the model-root space from the mesh: claw faces = the untextured Claw material (prepare_guest --claws).
  static void Orient(FaceTearDeath death,GameObject model,Manifest m){
   var all=new List<Vector3>();var claws=new List<Vector3>();var toRootSpace=model.transform.worldToLocalMatrix;
   foreach(var mf in model.GetComponentsInChildren<MeshFilter>(true)){var mesh=mf.sharedMesh;if(!mesh||!mesh.isReadable)continue;var toRoot=toRootSpace*mf.transform.localToWorldMatrix;var v=mesh.vertices;var mr=mf.GetComponent<Renderer>();
    var clawIdx=new HashSet<int>();for(int sub=0;sub<mesh.subMeshCount;sub++){var mat=mr&&sub<mr.sharedMaterials.Length?mr.sharedMaterials[sub]:null;string name=mat?mat.name:"";
     if(m.materials.Any(e=>string.IsNullOrEmpty(e.texture)&&name.StartsWith(e.name)))foreach(var i in mesh.GetIndices(sub))clawIdx.Add(i);}
    for(int i=0;i<v.Length;i++){var q=toRoot.MultiplyPoint3x4(v[i]);all.Add(q);if(clawIdx.Contains(i))claws.Add(q);}}
   if(all.Count==0||claws.Count==0){Debug.LogWarning("GUEST_CLAW orientation: no readable mesh or no Claw material — FaceTearDeath falls back to clawEuler");return;}
   Vector3 ac=Vector3.zero,cc=Vector3.zero;foreach(var q in all)ac+=q;foreach(var q in claws)cc+=q;ac/=all.Count;cc/=claws.Count;
   var f=(cc-ac).normalized;var tip=claws.OrderByDescending(q=>Vector3.Dot(q-ac,f)).First();var palm=Vector3.ProjectOnPlane(tip-ac,f).normalized;
   death.clawFingers=f;death.clawPalm=palm;Debug.Log("GUEST_CLAW orientation fingers="+f.ToString("F3")+" palm="+palm.ToString("F3")+" roll="+death.clawRoll);}
  // The face axis in head-bone space must not depend on the pose the prefab happens to show in the editor (the
  // creature's Idle turns the head ±45°). Bind pose: bone-to-mesh rotation = inverse(bindpose), and in the bind pose
  // the creature faces the model's forward (its chest faces the player in every story frame). Fallback: head→Jaw pivot,
  // a child joint rigidly attached to the head (rest: straight ahead, 22° up).
  static Vector3 FaceAxis(GameObject model,Transform head,Transform jaw,Vector3 forward){
   if(!head)return Vector3.zero;
   foreach(var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
    int i=Array.IndexOf(smr.bones,head);var mesh=smr.sharedMesh;if(i<0||!mesh||i>=mesh.bindposes.Length)continue;
    var restHead=mesh.bindposes[i].inverse.rotation;var meshForward=smr.transform.InverseTransformDirection(forward);
    var axis=(Quaternion.Inverse(restHead)*meshForward).normalized;Debug.Log("GUEST_BODY face axis from bind pose "+axis.ToString("F3"));return axis;}
   if(jaw){var axis=(Quaternion.Inverse(head.rotation)*(jaw.position-head.position)).normalized;Debug.Log("GUEST_BODY face axis from head→Jaw "+axis.ToString("F3"));return axis;}
   Debug.LogWarning("GUEST_BODY no bind pose and no Jaw: head look-at disabled");return Vector3.zero;}
  static string Pick(string[] names,params string[] keys){foreach(var k in keys){var hit=names.FirstOrDefault(n=>n.ToLowerInvariant().Contains(k));if(hit!=null)return hit;}return null;}
  public static void Install(){
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   var timeline=UnityEngine.Object.FindFirstObjectByType<ScenarioTimeline>();
   var death=timeline.GetComponent<FaceTearDeath>()??timeline.gameObject.AddComponent<FaceTearDeath>();timeline.faceTear=death;
   // Close-up claws (7.4): the source object is kept disabled under the director; FaceTearDeath clones it in camera space.
   string claw=Root+"GuestClaw/GuestClaw.fbx";
   if(File.Exists(claw)){
    var cimp=(ModelImporter)AssetImporter.GetAtPath(claw);if(cimp&&!cimp.isReadable){cimp.isReadable=true;cimp.SaveAndReimport();} // orientation is measured from the mesh
    var m=Read("GuestClaw");var old=timeline.transform.Find("READY GuestClaw source");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(claw));model.name="READY GuestClaw source";model.transform.SetParent(timeline.transform,false);
    Materialise("GuestClaw",model,m);Orient(death,model,m);death.clawLength=.58f; // forearm+hand reaching from outside the frame
    model.SetActive(false);death.clawSource=model;EditorUtility.SetDirty(death);Debug.Log("GUEST_CLAW installed");
   }else Debug.LogWarning("GUEST_CLAW missing: "+claw+" — run tools/art/prepare_guest.py");
   // Body: authored creature as a child of the logical Guest; proxy surfaces hidden at runtime.
   string body=Root+"GuestBody/GuestBody.fbx";
   if(File.Exists(body)){
    var importer=(ModelImporter)AssetImporter.GetAtPath(body);if(importer.animationType!=ModelImporterAnimationType.Legacy){importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;importer.SaveAndReimport();}
    var m=Read("GuestBody");var guest=flat.guest.transform;var old=guest.Find("READY GuestBody");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
    var proxy=guest.GetComponentsInChildren<Renderer>(true);
    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(body));model.name="READY GuestBody";model.transform.SetParent(guest,false);
    Materialise("GuestBody",model,m);
    // Same height and feet as the proxy so HeadAt / chase placement stay valid.
    var pb=proxy[0].bounds;foreach(var r in proxy)pb.Encapsulate(r.bounds);var rs=model.GetComponentsInChildren<Renderer>(true);var mb=rs[0].bounds;foreach(var r in rs)mb.Encapsulate(r.bounds);
    model.transform.localScale*=pb.size.y/Mathf.Max(.01f,mb.size.y);mb=rs[0].bounds;foreach(var r in rs)mb.Encapsulate(r.bounds);
    model.transform.position+=new Vector3(pb.center.x-mb.center.x,pb.min.y-mb.min.y,pb.center.z-mb.center.z);
    var visual=guest.GetComponent<GuestVisual>()??guest.gameObject.AddComponent<GuestVisual>();visual.proxyRenderers=proxy;visual.model=model.transform;
    // Head look-at basis in the bind pose: the parent of "Jaw" (or a bone named Head) and the creature's forward in its space.
    var jaw=model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Jaw");var headBone=jaw?jaw.parent:model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.ToLowerInvariant()=="head");
    var look=guest.GetComponent<GuestLookAt>()??guest.gameObject.AddComponent<GuestLookAt>();look.headBone=headBone;look.headFaceLocal=FaceAxis(model,headBone,jaw,guest.forward);Debug.Log("GUEST_BODY head bone "+(headBone?headBone.name:"none"));
    visual.player=model.GetComponentInChildren<Animation>(true);
    var names=visual.player?visual.player.Cast<AnimationState>().Select(s=>s.name).ToArray():new string[0];
    visual.idle=Pick(names,"idle","stand")??names.FirstOrDefault();visual.walk=Pick(names,"walk");visual.run=Pick(names,"run","sprint");visual.crawl=Pick(names,"crawl","creep");
    foreach(var r in rs)if(r is SkinnedMeshRenderer s)s.updateWhenOffscreen=true;
    Debug.Log("GUEST_BODY installed clips="+string.Join(",",names)+" idle="+visual.idle+" run="+visual.run+" crawl="+visual.crawl);
   }else Debug.LogWarning("GUEST_BODY missing: "+body+" — run tools/art/prepare_guest.py");
   EditorUtility.SetDirty(flat);EditorUtility.SetDirty(timeline);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
  }
  public static void InstallAndBuild(){Install();ScenarioBuilder.Build();}
 }
}
