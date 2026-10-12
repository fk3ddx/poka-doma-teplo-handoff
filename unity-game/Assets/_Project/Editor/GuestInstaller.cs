using System;
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
  [Serializable] class Mat {public string name,texture,normal;public float[] color;public float roughness;}
  [Serializable] class Manifest {public string id;public float size;public bool rigged;public string[] actions;public Mat[] materials;}
  static Manifest Read(string id)=>JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+id+"/manifest.json"));
  static void Materialise(string id,GameObject model,Manifest m){
   foreach(var r in model.GetComponentsInChildren<Renderer>(true)){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){
    var spec=m.materials.FirstOrDefault(x=>mats[i]&&x.name==mats[i].name)??m.materials.FirstOrDefault();if(spec==null)continue;
    string path=Root+id+"/"+spec.name+"_URP.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
    if(!string.IsNullOrEmpty(spec.texture))mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+id+"/"+spec.texture));
    if(!string.IsNullOrEmpty(spec.normal)){var n=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+id+"/"+spec.normal);if(n){mat.SetTexture("_BumpMap",n);mat.EnableKeyword("_NORMALMAP");}}
    var c=spec.color!=null&&spec.color.Length>=3?new Color(spec.color[0],spec.color[1],spec.color[2]):Color.white;mat.SetColor("_BaseColor",c);
    mat.SetFloat("_Smoothness",Mathf.Clamp01(1-spec.roughness)*.6f);EditorUtility.SetDirty(mat);mats[i]=mat;}
    r.sharedMaterials=mats;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
  }
  static string Pick(string[] names,params string[] keys){foreach(var k in keys){var hit=names.FirstOrDefault(n=>n.ToLowerInvariant().Contains(k));if(hit!=null)return hit;}return null;}
  public static void Install(){
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   var timeline=UnityEngine.Object.FindFirstObjectByType<ScenarioTimeline>();
   var death=timeline.GetComponent<FaceTearDeath>()??timeline.gameObject.AddComponent<FaceTearDeath>();timeline.faceTear=death;
   // Close-up claws (7.4): the source object is kept disabled under the director; FaceTearDeath clones it in camera space.
   string claw=Root+"GuestClaw/GuestClaw.fbx";
   if(File.Exists(claw)){
    var m=Read("GuestClaw");var old=timeline.transform.Find("READY GuestClaw source");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(claw));model.name="READY GuestClaw source";model.transform.SetParent(timeline.transform,false);
    Materialise("GuestClaw",model,m);model.SetActive(false);death.clawSource=model;Debug.Log("GUEST_CLAW installed");
   }else Debug.LogWarning("GUEST_CLAW missing: "+claw+" — run tools/art/prepare_guest.py");
   // Body: authored creature as a child of the logical Guest; proxy surfaces hidden at runtime.
   string body=Root+"GuestRake/GuestRake.fbx";
   if(File.Exists(body)){
    var importer=(ModelImporter)AssetImporter.GetAtPath(body);if(importer.animationType!=ModelImporterAnimationType.Legacy){importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;importer.SaveAndReimport();}
    var m=Read("GuestRake");var guest=flat.guest.transform;var old=guest.Find("READY GuestRake");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
    var proxy=guest.GetComponentsInChildren<Renderer>(true);
    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(body));model.name="READY GuestRake";model.transform.SetParent(guest,false);
    Materialise("GuestRake",model,m);
    // Same height and feet as the proxy so HeadAt / chase placement stay valid.
    var pb=proxy[0].bounds;foreach(var r in proxy)pb.Encapsulate(r.bounds);var rs=model.GetComponentsInChildren<Renderer>(true);var mb=rs[0].bounds;foreach(var r in rs)mb.Encapsulate(r.bounds);
    model.transform.localScale*=pb.size.y/Mathf.Max(.01f,mb.size.y);mb=rs[0].bounds;foreach(var r in rs)mb.Encapsulate(r.bounds);
    model.transform.position+=new Vector3(pb.center.x-mb.center.x,pb.min.y-mb.min.y,pb.center.z-mb.center.z);
    var visual=guest.GetComponent<GuestVisual>()??guest.gameObject.AddComponent<GuestVisual>();visual.proxyRenderers=proxy;visual.model=model.transform;
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
