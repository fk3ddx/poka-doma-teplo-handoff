using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  public static void InstallWhitewashedCeilings(){
   EditorSceneManager.OpenScene(ScenePath);
   mats=new Dictionary<string,Material>();
   WhitewashedCeilings();
   EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
   AssetDatabase.SaveAssets();Debug.Log("M1_WHITEWASHED_CEILINGS_APPLIED_REBAKE_REQUIRED");
  }
  static void WhitewashedCeilings(){
   // Bare clay albedo read as a brown wooden ceiling. Limewash keeps plaster relief,
   // with the warm colour supplied by fixtures and bounce light rather than orange paint.
   var finish=M("WhitewashedCeiling",C(.79f,.79f,.765f),.045f);
   Pbr(finish,"clay_plaster",.6f);finish.mainTexture=null;
   finish.SetFloat("_BumpScale",.12f);finish.SetFloat("_OcclusionStrength",.20f);
   finish.SetFloat("_Smoothness",.045f);finish.SetFloat("_Metallic",0);
   foreach(var surface in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
    if(surface.name=="Bedroom ceiling"||surface.name=="Apartment ceiling"||surface.name=="Cornice")
     surface.sharedMaterial=finish;
   EditorUtility.SetDirty(finish);
  }
 }
}
