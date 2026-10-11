using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Cinemachine;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class KatchoGrannyBuilder {
  const string Folder="Assets/_Project/Art/Characters/KatchoGranny", Root="Assets/_Project/Scenario";
  public static void Build() {
   AssetDatabase.Refresh();
   var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/Granny.fbx");
   importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;importer.isReadable=true;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.importNormals=ModelImporterNormals.Import;importer.SaveAndReimport();
   var ti=(TextureImporter)AssetImporter.GetAtPath(Folder+"/granny.png");ti.maxTextureSize=256;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=true;ti.SaveAndReimport();
   // Point only on this author's 256px atlas. The accepted full-screen bilinear/VHS pipeline is untouched.
   var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Granny.mat");if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,Folder+"/Granny.mat");}
   mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/granny.png"));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",0);mat.SetFloat("_SpecularHighlights",0);mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");EditorUtility.SetDirty(mat);
   var scene=EditorSceneManager.OpenScene(Root+"/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();var look=flat.look;var camera=flat.player.view;
   flat.guest.SetActive(false);flat.grandmother.SetActive(false);flat.slipper.SetActive(false);flat.stool.SetActive(false);
   // Clear the portrait camera foreground only in this preview scene.
   foreach(var t in flat.apartment.GetComponentsInChildren<Transform>(true))if(t.name=="SovietCoffeeTable"||t.name=="PROXY Kenney books")t.gameObject.SetActive(false);
   foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))c.gameObject.SetActive(false);
   foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(c!=camera)c.enabled=false;
   foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(c!=look&&(c.GetType().Namespace=="Ataka.Scenario"||c is CinemachineCamera||c is CinemachineBrain))UnityEngine.Object.DestroyImmediate(c);
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Granny.fbx");var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.name="Nina Katcho original appearance";
   foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
   var body=model.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="Granny");var idleSource=AssetDatabase.LoadAllAssetsAtPath(Folder+"/Granny.fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__")&&c.name.EndsWith("Oidle"));idleSource.SampleAnimation(model,0);
   var measured=new Mesh();body.BakeMesh(measured);var points=measured.vertices.Select(v=>body.transform.TransformPoint(v)).ToArray();float lowest=points.Min(v=>v.y),highest=points.Max(v=>v.y);float heightScale=1.55f/(highest-lowest);model.transform.localScale*=heightScale;model.transform.position-=Vector3.up*lowest*heightScale;UnityEngine.Object.DestroyImmediate(measured);
   var holder=new GameObject("Nina Katcho 1.55m");model.transform.SetParent(holder.transform,true);
   var anim=model.GetComponent<Animation>();if(!anim)anim=model.AddComponent<Animation>();
   var clips=AssetDatabase.LoadAllAssetsAtPath(Folder+"/Granny.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
   foreach(var clip in clips){anim.AddClip(clip,clip.name);anim[clip.name].wrapMode=WrapMode.Loop;}
   var idle=clips.Single(c=>c.name.EndsWith("Oidle"));anim.clip=idle;anim.playAutomatically=true;
   PrefabUtility.SaveAsPrefabAsset(holder,Folder+"/NinaKatcho.prefab");
   holder.transform.position=new Vector3(4.7f,0,3.05f);
   var preview=new GameObject("Katcho appearance preview").AddComponent<GrannyAssetPreview>();preview.actor=holder.transform;preview.view=camera;preview.look=look;preview.animationPlayer=anim;preview.body=body;preview.clips=clips;
   Debug.Log("KATCHO_IMPORT_CHECK measuredIdleHeight="+((highest-lowest)*heightScale)+" clips="+clips.Length+" vertices="+body.sharedMesh.vertexCount);
   EditorSceneManager.SaveScene(scene,Root+"/GrannyReadyPreview.unity");AssetDatabase.SaveAssets();
   var old=GraphicsSettings.defaultRenderPipeline;var quality=QualitySettings.renderPipeline;var product=PlayerSettings.productName;var fs=PlayerSettings.fullScreenMode;int w=PlayerSettings.defaultScreenWidth,h=PlayerSettings.defaultScreenHeight;var apis=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);bool defaults=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
   try{GraphicsSettings.defaultRenderPipeline=look.pipeline;QualitySettings.renderPipeline=look.pipeline;PlayerSettings.productName="Nina - Katcho PSX Preview";PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});Directory.CreateDirectory("Builds/GrannyReadyPreview");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"/GrannyReadyPreview.unity"},target=BuildTarget.StandaloneWindows64,locationPathName="Builds/GrannyReadyPreview/Granny-Preview.exe",options=BuildOptions.Development});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Granny preview build failed");Debug.Log("KATCHO_PREVIEW_BUILD_OK");}
   finally{GraphicsSettings.defaultRenderPipeline=old;QualitySettings.renderPipeline=quality;PlayerSettings.productName=product;PlayerSettings.fullScreenMode=fs;PlayerSettings.defaultScreenWidth=w;PlayerSettings.defaultScreenHeight=h;PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,apis);PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,defaults);AssetDatabase.SaveAssets();}
  }
 }
}
