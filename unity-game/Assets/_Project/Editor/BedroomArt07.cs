using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Editor {
 // Review06 from Claude. Explicit operation; never rebuild the accepted lighting scene.
 public static class BedroomArt07 {
  const string Folder="Assets/_Project/Scenario/Bedroom07/";
  static Bounds Measure(Transform t){var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new Exception("No geometry "+t.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
  static void Corner(FlatBindings flat,bool review07=false){
   var root=flat.apartment.Find("Bedroom05 icon corner");if(!root)throw new Exception("Bedroom06 must already be installed");
   var shelf=root.Find("Corner icon shelf");
   // Small architectural corner shelf, explicitly specified by art director; existing walnut material/UV.
   var mesh=new Mesh{name="Review06 triangular corner shelf"};
   float reach=review07?.37f:.65f;
   var p=new[]{new Vector3(.02f,0,.02f),new Vector3(reach,0,.02f),new Vector3(.02f,0,reach)};
   var verts=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var tris=new System.Collections.Generic.List<int>();
   void Face(params Vector3[] points){int start=verts.Count;foreach(var v in points){verts.Add(v);uv.Add(new Vector2(v.x*2,v.z*2+v.y*2));}for(int i=1;i<points.Length-1;i++){tris.Add(start);tris.Add(start+i);tris.Add(start+i+1);}}
   Vector3 down=Vector3.down*.025f;
   Face(p[0],p[2],p[1]);Face(p[0]+down,p[1]+down,p[2]+down);
   for(int i=0;i<3;i++){int j=(i+1)%3;Face(p[i],p[j],p[j]+down,p[i]+down);}
   mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
   string path=Folder+"CornerShelf.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
   shelf.GetComponent<MeshFilter>().sharedMesh=saved;shelf.localScale=Vector3.one;shelf.rotation=Quaternion.identity;shelf.position=new Vector3(0,1.62f,0);
   var icons=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Ready framed Met icon ")).OrderBy(t=>t.name).ToArray();if(icons.Length!=2)throw new Exception("Expected two existing ready icons");
   for(int i=0;i<2;i++){
    var t=icons[i];t.rotation=Quaternion.Euler(0,45,90);t.localScale*=.24f/Measure(t).size.y;
    var b=Measure(t);var center=i==0?new Vector3(.205f,0,.395f):new Vector3(.395f,0,.205f);
    if(review07)center=i==0?new Vector3(.103f,0,.247f):new Vector3(.247f,0,.103f);
    t.position+=new Vector3(center.x-b.center.x,1.622f-b.min.y,center.z-b.center.z);
    if(Mathf.Abs(Measure(t).size.y-.24f)>.001f)throw new Exception("Icon height is not24cm");
   }
   var halo=root.GetComponentsInChildren<Light>().Single(l=>l.name=="Icon glass halo");halo.color=new Color(1,.28f,.10f);halo.range=.4f;halo.intensity=review07?.006f:.012f;
   if(review07){
    var lamp=root.Find("Approved lampada06");if(!lamp)throw new Exception("Missing existing lampada");
    lamp.position=new Vector3(.235f,1.75f,.235f);
    PositionRod(lamp.Find("Lampada wall bracket"),new Vector3(.235f,1.95f,.012f),new Vector3(.235f,1.95f,.235f),.003f);
    PositionRod(lamp.Find("Lampada bracket wall plate"),new Vector3(.235f,1.91f,.012f),new Vector3(.235f,1.99f,.012f),.005f);
    halo.transform.position=lamp.Find("Lampada flame 1 x 2 cm").position;
   }
   var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Scenario/Bedroom06/LampadaRedGlass.mat");glass.EnableKeyword("_EMISSION");glass.SetColor("_EmissionColor",new Color(.6f,.006f,.001f));EditorUtility.SetDirty(glass);
  }
  static void PositionRod(Transform rod,Vector3 a,Vector3 b,float radius){
   if(!rod)throw new Exception("Missing existing lamp bracket");
   rod.position=(a+b)*.5f;rod.up=(b-a).normalized;
   var scale=rod.parent.lossyScale;rod.localScale=new Vector3(radius*2/scale.x,(b-a).magnitude*.5f/scale.y,radius*2/scale.z);
  }
  [MenuItem("Ataka/Bedroom07/Apply Claude review07 compact icon corner")]
  public static void ApplyReview07(){
   AssetDatabase.Refresh();var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   string spotBefore=EditorJsonUtility.ToJson(flat.look.icon);Corner(flat,true);
   if(spotBefore!=EditorJsonUtility.ToJson(flat.look.icon))throw new Exception("Accepted icon spot changed unexpectedly");
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("BEDROOM07 review07: shelf legs35cm, icons24cm, lamp bracket reaches wall, halo006. Main Spot unchanged. Render verification pending.");
  }
  static void Glass(){
   var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Scenario/Bedroom06/ReadyDrinkingGlass.mat");glass.SetColor("_BaseColor",new Color(.95f,.98f,1,.025f));glass.SetFloat("_Smoothness",.95f);EditorUtility.SetDirty(glass);
   var water=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Scenario/Bedroom06/ReadyDrinkingWater.mat");water.SetColor("_BaseColor",new Color(.9f,.95f,1,.025f));water.SetFloat("_Smoothness",.95f);EditorUtility.SetDirty(water);
  }
  [MenuItem("Ataka/Bedroom07/Apply Claude review06 corner and glass")]
  public static void ApplyReview06(){
   System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   string spotBefore=EditorJsonUtility.ToJson(flat.look.icon);Corner(flat);Glass();
   if(spotBefore!=EditorJsonUtility.ToJson(flat.look.icon))throw new Exception("Accepted icon spot changed unexpectedly");
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("BEDROOM07 review06 corner/glass applied. Accepted Spot unchanged. Pill box not yet sourced; render and route verification pending.");
  }
 }
}
