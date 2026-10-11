using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace Ataka.Editor {
public static partial class ApartmentArtBuilder {
 // Distant architecture has explicit metre UVs, no room light probes and no GI charts.
 // One persistent mesh per building/material keeps hundreds of window parts bounded.
 sealed class FacadeBatch {
  public readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
  public readonly List<Vector2> uv=new List<Vector2>(); public readonly List<int> triangles=new List<int>();
  public void Box(Mesh cube,Vector3 p,Vector3 size,bool metreUv){
   int start=vertices.Count;var vv=cube.vertices;var nn=cube.normals;var uu=cube.uv;
   for(int i=0;i<vv.Length;i++) {Vector3 v=Vector3.Scale(vv[i],size);vertices.Add(p+v);normals.Add(nn[i]);
    uv.Add(!metreUv?uu[i]:Mathf.Abs(nn[i].y)>.5f?new Vector2(v.x+p.x,v.z+p.z):Mathf.Abs(nn[i].x)>.5f?new Vector2(v.z+p.z,v.y+p.y):new Vector2(v.x+p.x,v.y+p.y));}
   foreach(int t in cube.triangles)triangles.Add(start+t);
  }
 }
 public static void RefineFacadeMaterials(){
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);mats=new Dictionary<string,Material>();FacadeMaterials();AssetDatabase.SaveAssets();Debug.Log("M1_FACADE_MATERIALS_REFINED");
 }
 static void FacadeMaterials(){
  if(mats.ContainsKey("FacadeRender"))return;
  Pbr(M("FacadeRender",C(.88f,.89f,.86f),.07f),"plastered_wall_02",.42f);
  Pbr(M("FacadePatch",C(.60f,.62f,.59f),.07f),"plastered_wall_02",.55f);
  Pbr(M("FacadePlinth",C(.22f,.255f,.25f),.09f),"plastered_wall_02",.5f);
  M("FacadeSeam",C(.20f,.22f,.215f),.04f); M("FacadeFrame",C(.78f,.79f,.73f),.14f);
  M("FacadeOldFrame",C(.26f,.23f,.18f),.09f);M("FacadeRecess",C(.035f,.045f,.049f),.12f);
  M("FacadeBalconySage",C(.49f,.55f,.49f),.12f);M("FacadeBalconyOchre",C(.59f,.53f,.40f),.12f);
  M("FacadeBalconyGrey",C(.61f,.62f,.58f),.12f);
  for(int i=0;i<8;i++){
   var mat=M("FacadeWindow"+i,Color.white,.18f);var tex=new Texture2D(128,128,TextureFormat.RGB24,true);
   Color tint=i==6?C(.32f,.40f,.43f):i==7?C(.42f,.31f,.20f):C(.72f,.49f,.27f);
   for(int y=0;y<128;y++)for(int x=0;x<128;x++){
    float u=(x+.5f)/128,v=(y+.5f)/128;
    float edge=Mathf.SmoothStep(0,1,Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v))/.08f);
    float fold=.73f+.17f*Mathf.Sin(u*(40+i*3))+.05f*Mathf.Sin(u*109);
    float light=(.48f+.52f*v)*fold*edge;
    // Distinct partly drawn curtains, lower furniture silhouettes and a blind.
    if(i%3==0 && u>.36f && u<.58f)light*=.2f;
    if(i%3==1 && u>.63f)light*=.35f;
    if(i==5 && v>.34f)light*=.45f+.28f*Mathf.Abs(Mathf.Sin(v*145));
    if(v<.15f && u>.12f && u<.74f)light*=.3f;
    if(i==3 && u>.71f && u<.83f && v<.42f)light*=.25f;
    Color pixel=i<2?C(.028f,.043f,.051f)*(.5f+light*.8f):tint*light;
    tex.SetPixel(x,y,pixel);
   }
   tex.Apply();string path=Root+"/Art/Textures/FacadeWindow"+i+".png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
   var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.wrapMode=TextureWrapMode.Clamp;imp.mipmapEnabled=true;imp.filterMode=FilterMode.Trilinear;imp.SaveAndReimport();
   mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);mat.mainTextureScale=Vector2.one;
   mat.EnableKeyword("_EMISSION");mat.SetTexture("_EmissionMap",mat.mainTexture);
   float power=i<2?.055f:i==2?.24f:i==7?.30f:1.0f;
   mat.SetColor("_EmissionColor",Color.white*power);mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
  }
 }
 static void PanelBuilding(float x,float z,float width,int floors){
  FacadeMaterials();var batches=new Dictionary<string,FacadeBatch>();
  var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=primitive.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(primitive);
  void B(string mat,float px,float py,float pz,float sx,float sy,float sz,bool uv=true){
   if(!batches.TryGetValue(mat,out var batch)){batch=new FacadeBatch();batches.Add(mat,batch);}
   batch.Box(cube,new Vector3(px,py-Floor,pz),new Vector3(sx,sy,sz),uv);
  }
  float h=floors*2.8f,front=z+4.5f;int columns=Mathf.FloorToInt((width-2.3f)/2.9f)+1;
  var rng=new System.Random(1947+(int)(x*73+z*19));
  B("FacadeRender",x,h/2,z,width,h,9); B("FacadePlinth",x,.43f,front+.013f,width,.86f,.03f);
  for(int f=0;f<=floors;f++)B("FacadeSeam",x,f*2.8f,front+.021f,width,.028f,.025f);
  for(int col=0;col<=columns;col++){
   float seam=x-width/2+col*2.9f;
   if(seam<x+width/2)B("FacadeSeam",seam,h/2,front+.023f,.027f,h,.024f);
  }
  for(int f=0;f<floors;f++)for(int col=0;col<columns;col++){
   float wx=x-width/2+1.3f+col*2.9f,y=f*2.8f+1.65f;
   int roll=rng.Next(100);int state=roll<62?rng.Next(2):roll<77?2:3+rng.Next(5);
   string frame=rng.Next(5)<2?"FacadeOldFrame":"FacadeFrame";
   if(rng.Next(9)==0)B("FacadePatch",wx,y-.85f,front+.025f,1.7f,.25f,.022f);
   // Dark surround, recessed-looking glass and projecting reveal/frame/sill.
   B("FacadeRecess",wx,y,front+.037f,1.58f,1.58f,.042f);
   B("FacadeWindow"+state,wx,y,front+.064f,1.40f,1.40f,.014f,false);
   foreach(float side in new[]{-1f,1f}){B(frame,wx+side*.745f,y,front+.107f,.06f,1.52f,.09f);B(frame,wx,y+side*.735f,front+.107f,1.5f,.06f,.09f);}
   float split=col%2==0?-.19f:.19f;
   B(frame,wx+split,y,front+.117f,.047f,1.42f,.10f);
   if((f+col)%3!=0)B(frame,wx,y+.40f,front+.117f,1.42f,.043f,.10f);
   B("FacadeFrame",wx,y-.79f,front+.16f,1.65f,.065f,.34f);
   // Balconies align vertically, but their later alterations differ by household.
   if(f>0 && col%3==1){
    B("FacadeRender",wx,y-.88f,front+.51f,2.12f,.16f,1.13f);
    string panel=new[]{"FacadeBalconySage","FacadeBalconyOchre","FacadeBalconyGrey"}[rng.Next(3)];
    B(panel,wx,y-.37f,front+1.05f,2.04f,.88f,.065f);
    foreach(float side in new[]{-1f,1f})B(panel,wx+side*1.01f,y-.37f,front+.54f,.065f,.88f,1.05f);
    B("FacadeOldFrame",wx,y+.10f,front+1.07f,2.10f,.055f,.06f);
    bool enclosed=rng.Next(100)<55;
    if(enclosed){
     B("FacadeWindow"+(state<3?1:2),wx,y+.76f,front+1.035f,2.02f,1.28f,.016f,false);
     for(int bar=0;bar<5;bar++)B(frame,wx-1.02f+bar*.51f,y+.78f,front+1.07f,.047f,1.37f,.065f);
     B(frame,wx,y+1.47f,front+1.07f,2.13f,.055f,.085f);
     B("FacadePlinth",wx,y+1.51f,front+.56f,2.16f,.065f,1.21f);
    }else for(int bar=0;bar<7;bar++)B("FacadeOldFrame",wx-.95f+bar*.317f,y-.28f,front+1.095f,.018f,.72f,.025f);
   }
  }
  B("FacadePlinth",x,h+.03f,z,width+.3f,.18f,9.3f);
  B("FacadeRender",x,h-.16f,front+.075f,width+.12f,.17f,.19f);
  foreach(float side in new[]{-1f,1f}){
   B("FacadeOldFrame",x+side*(width/2-.20f),h/2,front+.14f,.09f,h,.095f);
   for(int f=1;f<floors;f++)B("FacadeSeam",x+side*(width/2+.014f),f*2.8f,z,.025f,.032f,9);
  }
  int meshCount=0,triangles=0;
  foreach(var entry in batches){
   var b=entry.Value;string id="Facade_"+Mathf.RoundToInt(x)+"_"+entry.Key,path=Root+"/Art/M1Meshes/"+id+".asset";
   var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
   mesh.name=id;mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(b.vertices);mesh.SetNormals(b.normals);mesh.SetUVs(0,b.uv);mesh.SetTriangles(b.triangles,0);mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
   var go=new GameObject(id);go.transform.SetParent(art,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
   var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=mats[entry.Key];renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
   GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ReflectionProbeStatic);
   meshCount++;triangles+=b.triangles.Count/3;
  }
  Debug.Log($"M1_FACADE x={x} windows={columns*floors} renderers={meshCount} triangles={triangles}");
 }
}}
