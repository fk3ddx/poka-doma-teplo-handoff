using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.LightLab;
namespace Ataka.Tests {
 public class LightLabTests {
  const string Root="Assets/_Project/LightLab/";
  [Test] public void ShippingPipelineUsesLinearUpscaleAndNoMsaa(){
   var p=AssetDatabase.LoadMainAssetAtPath(Root+"LightLab_URP.asset");Assert.NotNull(p);var s=new SerializedObject(p);
   Assert.AreEqual(2f/3,s.FindProperty("m_RenderScale").floatValue,.0001f);
   Assert.AreEqual(1,s.FindProperty("m_MSAA").intValue);
   // UpscalingFilterSelection.Linear = 1 in URP17; Point = 2.
   Assert.AreEqual(1,s.FindProperty("m_UpscalingFilter").intValue);
  }
  [Test] public void VhsShaderAndFourProfilesArePresent(){
   var shader=Shader.Find("Ataka/LightLabVHS");Assert.NotNull(shader);Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
   var normal=AssetDatabase.LoadAssetAtPath<VhsProfile>(Root+"VhsProfile_Normal.asset");var tension=AssetDatabase.LoadAssetAtPath<VhsProfile>(Root+"VhsProfile_Tension.asset");
   Assert.Greater(tension.ghostWeight,normal.ghostWeight);Assert.Greater(normal.grain,0);
   foreach(var n in new[]{"Scare","Rewind"})Assert.Greater(AssetDatabase.LoadAssetAtPath<VhsProfile>(Root+"VhsProfile_"+n+".asset").disruption,0);
  }
  [Test] public void SceneHasSixViewsAndUprightTexturedFreeProps(){
   var prior=EditorSceneManager.GetSceneManagerSetup();
   try{EditorSceneManager.OpenScene(Root+"LightLab.unity");var lab=Object.FindFirstObjectByType<LightLabSession>();Assert.NotNull(lab);Assert.AreEqual(6,lab.viewpoints.Length);
    int count=0;foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())if(go.name.StartsWith("CC0 ")){
     count++;var rs=go.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs){b.Encapsulate(r.bounds);Assert.NotNull(r.sharedMaterial.GetTexture("_BaseMap"),go.name);}
     if(go.name.Contains("chair")){Assert.AreEqual(.85f,b.size.y,.01f);Assert.AreEqual(0,b.min.y,.005f);}
     if(go.name.Contains("vase"))Assert.Greater(b.size.y,b.size.x);
    }Assert.AreEqual(5,count);
   }finally{if(System.Array.Exists(prior,s=>s.isActive && s.isLoaded))EditorSceneManager.RestoreSceneManagerSetup(prior);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
  }
 }
}
