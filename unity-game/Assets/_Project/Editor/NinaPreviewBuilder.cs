using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class NinaPreviewBuilder {
  const string Folder="Assets/_Project/Art/Characters/NinaPreview",Root="Assets/_Project/Scenario";
  [Serializable] class Entry{public string name,texture;public bool alphaClip;public float roughness;}
  [Serializable] class Manifest{public float height;public Entry[] materials;}
  public static void Build(){
   AssetDatabase.Refresh();var scene=EditorSceneManager.OpenScene(Root+"/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();var look=flat.look;var camera=flat.player.view;
   flat.guest.SetActive(false);flat.grandmother.SetActive(false);
   foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))c.gameObject.SetActive(false);
   foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(c!=camera)c.enabled=false;
   foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(c!=look&&(c.GetType().Namespace=="Ataka.Scenario"||c is CinemachineCamera||c is CinemachineBrain))UnityEngine.Object.DestroyImmediate(c);
   var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"/manifest.json"));
   var root=new GameObject("Nina appearance fitting only");root.transform.position=new Vector3(4.7f,0,3.05f);
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Nina-preview.fbx");if(!source)throw new Exception("Missing Nina FBX");
   var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
   foreach(var r in model.GetComponentsInChildren<Renderer>()){
    var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){
     var entry=manifest.materials.First(e=>e.name==mats[i].name);string path=Folder+"/"+entry.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
     string tp=Folder+"/"+entry.texture;var ti=(TextureImporter)AssetImporter.GetAtPath(tp);if(ti){ti.maxTextureSize=entry.name.Contains("body")||entry.name.Contains("lips")||entry.name.Contains("ears")?512:256;ti.filterMode=FilterMode.Bilinear;ti.mipmapEnabled=true;ti.alphaIsTransparency=entry.alphaClip;ti.SaveAndReimport();}
     mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(tp));mat.SetColor("_BaseColor",entry.name.Contains("kimono")?new Color(.41f,.48f,.40f):entry.name.Contains("glasses")?new Color(.14f,.105f,.075f):Color.white);mat.SetFloat("_Smoothness",.03f);mat.SetFloat("_SpecularHighlights",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");mat.SetFloat("_Cull",0);
     mat.SetFloat("_AlphaClip",entry.alphaClip?1:0);mat.SetFloat("_Cutoff",.25f);if(entry.alphaClip){mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;mat.SetOverrideTag("RenderType","TransparentCutout");}EditorUtility.SetDirty(mat);mats[i]=mat;
    }r.sharedMaterials=mats;
   }
   var preview=new GameObject("Nina Art Preview").AddComponent<NinaArtPreview>();preview.actor=root.transform;preview.view=camera;preview.look=look;
   EditorSceneManager.SaveScene(scene,Root+"/NinaPreview.unity");AssetDatabase.SaveAssets();
   var old=GraphicsSettings.defaultRenderPipeline;var quality=QualitySettings.renderPipeline;var product=PlayerSettings.productName;var fs=PlayerSettings.fullScreenMode;int w=PlayerSettings.defaultScreenWidth,h=PlayerSettings.defaultScreenHeight;var apis=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);bool defaults=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
   try{GraphicsSettings.defaultRenderPipeline=look.pipeline;QualitySettings.renderPipeline=look.pipeline;PlayerSettings.productName="Нина — примерка внешности";PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});Directory.CreateDirectory("Builds/NinaPreview");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"/NinaPreview.unity"},target=BuildTarget.StandaloneWindows64,locationPathName="Builds/NinaPreview/Nina-Preview.exe",options=BuildOptions.Development});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Nina preview build failed");Debug.Log("NINA_PREVIEW_BUILD_OK");}
   finally{GraphicsSettings.defaultRenderPipeline=old;QualitySettings.renderPipeline=quality;PlayerSettings.productName=product;PlayerSettings.fullScreenMode=fs;PlayerSettings.defaultScreenWidth=w;PlayerSettings.defaultScreenHeight=h;PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,apis);PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,defaults);AssetDatabase.SaveAssets();}
  }
 }
}
