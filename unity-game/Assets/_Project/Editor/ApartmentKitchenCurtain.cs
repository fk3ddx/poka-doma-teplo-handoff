using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  // Non-colliding decoration: probe-lit, without invalidating the apartment's baked GI.
  const string KitchenCurtainRoot="Kitchen cafe curtain mounting";
  public static void InstallKitchenCurtainSupport(){
   EditorSceneManager.OpenScene(ScenePath);
   art=GameObject.Find("M1 authored apartment").transform;
   mats=AssetDatabase.FindAssets("t:Material",new[]{Root+"/Art/M1Materials"})
    .Select(id=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id)))
    .ToDictionary(m=>m.name,m=>m);
   KitchenCurtainSupport();
   PlayerSettings.bundleVersion="0.2.6-dev";
   EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
   AssetDatabase.SaveAssets();Debug.Log("M1_KITCHEN_CURTAIN_MOUNTED");
  }
  static void KitchenCurtainSupport(){
   var previous=art.Find(KitchenCurtainRoot);if(previous!=null)Object.DestroyImmediate(previous.gameObject);
   var parent=art;art=new GameObject(KitchenCurtainRoot).transform;art.SetParent(parent,false);
   M("KitchenHeadingTape",C(.72f,.70f,.62f),.035f).SetFloat("_Cull",0);
   Rod("Painted cafe curtain rod",new Vector3(7.085f,1.704f,-.015f),new Vector3(8.315f,1.704f,-.015f),.006f,"Enamel");
   foreach(float x in new[]{7.10f,8.30f}){
    var plate=Box("Rod bracket base",new Vector3(x,1.704f,-.124f),new Vector3(.019f,.043f,.009f),"Enamel",false);
    GameObjectUtility.SetStaticEditorFlags(plate,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ReflectionProbeStatic);
    Rod("Rod bracket arm",new Vector3(x,1.704f,-.123f),new Vector3(x,1.704f,-.015f),.004f,"Enamel");
    foreach(float y in new[]{1.69f,1.718f})Rod("Bracket screw",new Vector3(x,y,-.118f),new Vector3(x,y,-.116f),.003f,"Brass");
    Rod("Curtain rod end collar",new Vector3(x-.009f,1.704f,-.015f),new Vector3(x+.009f,1.704f,-.015f),.009f,"Enamel");
   }
   var vertices=new List<Vector3>();var triangles=new List<int>();
   // Cloth follows the Blender curtain's exact pleated upper edge (v=1).
   float Fold(float u,float v)=>-.07f+.045f*Mathf.Cos(u*Mathf.PI*12+.12f*Mathf.Sin(v*4))+.007f*Mathf.Sin(v*7+u*13);
   const int segments=96;
   for(int i=0;i<=segments;i++){
    float u=(float)i/segments,x=7.13f+1.14f*u;
    vertices.Add(new Vector3(x,1.672f,Fold(u,1)+.0015f));
    vertices.Add(new Vector3(x,1.653f,Fold(u,1-.019f/.67f)+.0015f));
    if(i<segments){int a=i*2;triangles.AddRange(new[]{a,a+1,a+2,a+2,a+1,a+3});}
   }
   SaveCurtainMesh("Kitchen curtain heading tape",vertices,triangles,"KitchenHeadingTape");
   vertices.Clear();triangles.Clear();
   for(int i=0;i<13;i++){
    float x=7.15f+i*1.10f/12,u=(x-7.13f)/1.14f;
    var points=new List<Vector3>();
    for(int j=0;j<=20;j++){float a=j*Mathf.PI*2/20;points.Add(new Vector3(x,1.704f+.012f*Mathf.Cos(a),-.015f+.012f*Mathf.Sin(a)));}
    CurtainWire(points,.0014f,vertices,triangles);
    CurtainWire(new List<Vector3>{new Vector3(x,1.692f,-.015f),new Vector3(x,1.679f,Fold(u,1)+.004f),new Vector3(x,1.664f,Fold(u,1)+.004f)},.0014f,vertices,triangles);
   }
   SaveCurtainMesh("Kitchen curtain rings and clips",vertices,triangles,"Brass");
   art=parent;
  }
  static void CurtainWire(List<Vector3> points,float radius,List<Vector3> vertices,List<int> triangles){
   int start=vertices.Count;const int sides=6;
   for(int i=0;i<points.Count;i++){
    var tangent=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(0,i-1)]).normalized;
    var side=Vector3.right;var up=Vector3.Cross(tangent,side).normalized;
    for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;vertices.Add(points[i]+radius*(side*Mathf.Cos(a)+up*Mathf.Sin(a)));}
    if(i==points.Count-1)continue;
    for(int j=0;j<sides;j++){int a=start+i*sides+j,b=start+i*sides+(j+1)%sides;triangles.AddRange(new[]{a,b,a+sides,b,b+sides,a+sides});}
   }
  }
  static void SaveCurtainMesh(string name,List<Vector3> vertices,List<int> triangles,string material){
   string path=Root+"/Art/M1Meshes/"+name.Replace(' ','_')+".asset";
   var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
   mesh.name=name;mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
   var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(art,false);
   go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mats[material];
   GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ReflectionProbeStatic);
  }
 }
}
