using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  public static void InstallOpalLighting(){
   EditorSceneManager.OpenScene(ScenePath);art=GameObject.Find("M1 authored apartment").transform;
   mats=new Dictionary<string,Material>();OpalLighting();PlayerSettings.bundleVersion="0.2.7-dev";
   EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
   Debug.Log("M1_OPAL_LIGHTING_INSTALLED_REBAKE_REQUIRED");
  }
  static void OpalLighting(){
   // The JSON remains the complete Blender source. Split its shared Shade material
   // in the Unity assembly recipe so the floor-lamp fabric keeps its own appearance.
   var original=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Art/M1Meshes/Living_Shade.asset");
   if(original==null)throw new System.InvalidOperationException("Missing authored living shade mesh");
   var positions=original.vertices;var all=original.triangles;var upper=new List<int>();var lower=new List<int>();
   for(int i=0;i<all.Length;i+=3){
    bool a=positions[all[i]].y>2.03f,b=positions[all[i+1]].y>2.03f,c=positions[all[i+2]].y>2.03f;
    if(a!=b||b!=c)throw new System.InvalidOperationException("Shade separation crosses a triangle");
    (a?upper:lower).AddRange(new[]{all[i],all[i+1],all[i+2]});
   }
   if(upper.Count==0||lower.Count==0)throw new System.InvalidOperationException("Both fabric and opal shade groups are required");
   var fabric=GameObject.Find("Living_Shade");fabric.GetComponent<MeshFilter>().sharedMesh=ExtractShade(original,lower,"Living_FabricShade");
   var opal=art.Find("Living_Opal");if(opal==null){opal=new GameObject("Living_Opal",typeof(MeshFilter),typeof(MeshRenderer)).transform;opal.SetParent(art,false);}
   opal.GetComponent<MeshFilter>().sharedMesh=ExtractShade(original,upper,"Living_Opal");
   var material=M("OpalGlass",C(.20f,.19f,.18f),.24f);
   material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",C(.85f,.67f,.46f));
   material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
   material.SetFloat("_SpecularHighlights",0);material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
   opal.GetComponent<Renderer>().sharedMaterial=material;opal.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;Static(opal.gameObject);
   // Emission represents the enclosed diffuser. Existing mixed fixture supplies light;
   // do not add three realtime shadow lights just to make three bulbs look switched on.
   EditorUtility.SetDirty(material);
  }
  static Mesh ExtractShade(Mesh source,List<int> indices,string name){
   var v=source.vertices;var n=source.normals;var uv=source.uv;
   var vertices=new List<Vector3>();var normals=new List<Vector3>();var texcoords=new List<Vector2>();
   var triangles=new List<int>();var remap=new Dictionary<int,int>();
   foreach(int old in indices){
    if(!remap.TryGetValue(old,out int index)){index=vertices.Count;remap.Add(old,index);vertices.Add(v[old]);normals.Add(n[old]);texcoords.Add(uv[old]);}
    triangles.Add(index);
   }
   string path=Root+"/Art/M1Meshes/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
   mesh.name=name;mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,texcoords);mesh.SetTriangles(triangles,0);
   mesh.RecalculateTangents();mesh.RecalculateBounds();Unwrapping.GenerateSecondaryUVSet(mesh);EditorUtility.SetDirty(mesh);return mesh;
  }
 }
}
