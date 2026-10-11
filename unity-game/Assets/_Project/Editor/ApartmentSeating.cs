using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  static void Seating(){
   var previous=GameObject.Find("Living armchair interaction");if(previous!=null)Object.DestroyImmediate(previous);
   var root=new GameObject("Living armchair interaction");root.transform.SetParent(art,false);root.transform.localPosition=new Vector3(4.36f,0,.85f);
   var seat=root.AddComponent<ArmchairSeat>();seat.persistentId="living-armchair";seat.label="кресло";
   var hit=root.AddComponent<BoxCollider>();hit.center=new Vector3(0,.70f,.04f);hit.size=new Vector3(.84f,.88f,.70f);
   seat.seatedAnchor=new GameObject("Seated feet origin").transform;seat.seatedAnchor.SetParent(root.transform,false);seat.seatedAnchor.localPosition=new Vector3(0,.02f,0);
   seat.exitAnchor=new GameObject("Clear standing position").transform;seat.exitAnchor.SetParent(root.transform,false);seat.exitAnchor.localPosition=new Vector3(.76f,.02f,.65f);
   seat.lap=new GameObject("Seated knees and wool blanket");seat.lap.transform.SetParent(root.transform,false);
   foreach(float x in new[]{-.125f,.125f}){
    SeatShape(seat.lap.transform,"Trouser thigh",new Vector3(x,.57f,.25f),new Vector3(.225f,.22f,.57f),"DarkFabric");
    SeatShape(seat.lap.transform,"Bent knee",new Vector3(x,.52f,.52f),new Vector3(.21f,.24f,.23f),"DarkFabric");
    SeatShape(seat.lap.transform,"Trouser shin",new Vector3(x,.29f,.54f),new Vector3(.15f,.46f,.18f),"DarkFabric");
    SeatShape(seat.lap.transform,"House slipper",new Vector3(x,.065f,.61f),new Vector3(.17f,.11f,.30f),"Fabric");
   }
   const int nx=32,nz=32;var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
   for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){
    float u=(float)x/nx,w=(float)z/nz,px=(u-.5f)*.64f;
    float knee=Mathf.Exp(-Mathf.Pow((Mathf.Abs(px)-.125f)/.092f,2));
    float side=Mathf.Max(0,Mathf.Abs(px)-.23f)*1.8f;
    float front=Mathf.Max(0,w-.65f)*.98f;
    float y=.605f+.045f*knee-.018f*Mathf.Sin(w*5)-side-front;
    y+=.012f*Mathf.Sin(u*25+w*4)+.006f*Mathf.Sin(w*29+u*3);
    v.Add(new Vector3(px,y,-.08f+w*.80f));uv.Add(new Vector2(u*1.1f,w*1.5f));
    if(x<nx&&z<nz){int a=z*(nx+1)+x,b=a+1,c=a+nx+1,d=c+1;tris.AddRange(new[]{a,c,b,b,c,d});}
   }
   string path=Root+"/Art/M1Meshes/SeatedLapBlanket.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
   mesh.name="Draped wool over two bent knees";mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
   var cloth=new GameObject("Wool blanket on lap");cloth.transform.SetParent(seat.lap.transform,false);cloth.AddComponent<MeshFilter>().sharedMesh=mesh;
   var renderer=cloth.AddComponent<MeshRenderer>();renderer.sharedMaterial=mats["Plaid"];renderer.shadowCastingMode=ShadowCastingMode.TwoSided;
   seat.lap.SetActive(false);
  }
  static void DiagnoseSeatClearance(){
   Physics.SyncTransforms();var seat=Object.FindFirstObjectByType<ArmchairSeat>();
   var p=seat.exitAnchor.position;
   foreach(var hit in Physics.OverlapCapsule(p+Vector3.up*.27f,p+Vector3.up*1.45f,.245f,~0,QueryTriggerInteraction.Ignore))
    Debug.Log("SEAT_EXIT_OBSTACLE "+hit.name+" "+hit.bounds);
   Debug.Log("SEAT_EXIT_CLEAR "+seat.CanExit()+" at "+p);
  }
  static void SeatShape(Transform parent,string name,Vector3 p,Vector3 scale,string material){
   var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
   Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=mats[material];
  }
  // Dynamic interaction only: preserves the verified static lightmaps and reflection captures.
  public static void InstallSeating(){
   EditorSceneManager.OpenScene(ScenePath);art=GameObject.Find("M1 authored apartment").transform;mats=new Dictionary<string,Material>();
   foreach(string name in new[]{"DarkFabric","Fabric","Plaid"})mats[name]=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/M1Materials/"+name+".mat");
   Seating();WeatherParticles();DiagnoseSeatClearance();PlayerSettings.bundleVersion="0.2.3-dev";
   EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Debug.Log("M1_SEATING_INSTALLED");
  }
 }
}
