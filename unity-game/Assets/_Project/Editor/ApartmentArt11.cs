using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class ApartmentArt11 {
  const string Dir="Assets/_Project/Scenario/Apartment11/", Art="Assets/_Project/Art/";
  static Transform root;static int index;static List<Renderer> finishes;
  struct Room {public string name;public float x0,x1,z0,z1;public Material lower,upper;public float repeat;public Room(string n,float a,float b,float c,float d,Material lo,Material hi,float tile){name=n;x0=a;x1=b;z0=c;z1=d;lower=lo;upper=hi;repeat=tile;}}
  static Texture2D Texture(string path,bool normal=false){var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)throw new Exception("Missing texture "+path);bool change=importer.wrapMode!=TextureWrapMode.Repeat||importer.filterMode!=FilterMode.Bilinear||(normal&&importer.textureType!=TextureImporterType.NormalMap);if(change){importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Bilinear;if(normal)importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();}return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
  static Material Mat(string name,string texture,Color color,float smooth,string bump=null,float strength=.15f){var path=Dir+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetTexture("_BaseMap",Texture(texture));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",0);m.SetTextureScale("_BaseMap",Vector2.one);if(bump!=null){m.SetTexture("_BumpMap",Texture(bump,true));m.SetFloat("_BumpScale",strength);m.EnableKeyword("_NORMALMAP");}else{m.DisableKeyword("_NORMALMAP");m.SetTexture("_BumpMap",null);}EditorUtility.SetDirty(m);return m;}
  static Renderer Quad(string name,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Material mat,float repeat,bool decay=true){
   var mesh=new Mesh{name=name};mesh.vertices=new[]{a,b,c,d};bool reverse=Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0;mesh.triangles=reverse?new[]{0,2,1,0,3,2}:new[]{0,1,2,0,2,3};
   // World metre UVs keep texture density independent of wall segment dimensions and openings.
   Vector2 UV(Vector3 v)=>Mathf.Abs(normal.y)>.5f?new Vector2(v.x,v.z)/repeat:Mathf.Abs(normal.x)>.5f?new Vector2(v.z,v.y)/repeat:new Vector2(v.x,v.y)/repeat;
   mesh.uv=new[]{UV(a),UV(b),UV(c),UV(d)};mesh.normals=Enumerable.Repeat(normal,4).ToArray();mesh.RecalculateTangents();mesh.RecalculateBounds();string path=Dir+"Surface"+(index++).ToString("D3")+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
   var g=new GameObject(name);g.transform.SetParent(root,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;if(decay)finishes.Add(r);return r;
  }
  static void Face(Room room,Bounds bounds,bool xAxis,float plane,Vector3 normal){
   float start=Mathf.Max(xAxis?bounds.min.z:bounds.min.x,xAxis?room.z0:room.x0),end=Mathf.Min(xAxis?bounds.max.z:bounds.max.x,xAxis?room.z1:room.x1);if(end-start<.005f)return;
   void Band(float low,float high,Material material,float repeat){float y0=Mathf.Max(low,bounds.min.y),y1=Mathf.Min(high,bounds.max.y);if(y1-y0<.005f)return;Vector3 V(float u,float y)=> (xAxis?new Vector3(plane,y,u):new Vector3(u,y,plane))+normal*.002f;Quad(room.name+" wall finish",V(start,y0),V(end,y0),V(end,y1),V(start,y1),normal,material,repeat);}
   if(room.upper!=room.lower){Band(0,1.5f,room.lower,room.repeat);Band(1.5f,2.5f,room.upper,3);}else Band(0,2.5f,room.lower,room.repeat);
  }
  static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
  static void Ready(FlatBindings flat,string oldName,string id,Vector3 position,float yaw){var old=flat.apartment.Find(oldName);if(old)old.gameObject.SetActive(false);var previous=flat.apartment.Find("READY CC0 "+id);if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);{string key=id=="SovietShelf"?"readyShelf":id=="SovietTV"?"readyTV":"readyDiningChair";ScenarioReadyArt.TryPlace(key,position,Vector3.one,yaw,flat.apartment);}}
  [Serializable] class AuthorMaterial {public string name,texture;public bool glass;public float[] color;}
  [Serializable] class AuthorManifest {public AuthorMaterial[] materials;}
  static GameObject PostSoviet(FlatBindings flat,string oldName,string id,Vector3 position,float yaw){
   const string folder=Art+"ReadyModels/PostSoviet/";var old=flat.apartment.Find(oldName);if(old)old.gameObject.SetActive(false);var existing=flat.apartment.Find("READY PostSoviet "+id);if(existing)UnityEngine.Object.DestroyImmediate(existing.gameObject);
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(folder+id+".fbx");if(!source)throw new Exception("Missing prepared "+id);var g=new GameObject("READY PostSoviet "+id);g.transform.SetParent(flat.apartment,false);var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(g.transform,false);var manifest=JsonUtility.FromJson<AuthorManifest>(File.ReadAllText(folder+"manifest.json"));
   foreach(var r in model.GetComponentsInChildren<Renderer>()){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){var spec=manifest.materials.Single(m=>m.name==mats[i].name);var path=folder+spec.name+"_URP.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}if(!string.IsNullOrEmpty(spec.texture))mat.SetTexture("_BaseMap",Texture(folder+"Textures/"+spec.texture));mat.SetColor("_BaseColor",new Color(spec.color[0],spec.color[1],spec.color[2],spec.glass?.17f:1));mat.SetFloat("_Smoothness",spec.glass?.8f:.24f);mat.SetFloat("_Surface",0);mat.SetFloat("_ZWrite",1);mat.SetFloat("_SrcBlend",1);mat.SetFloat("_DstBlend",0);mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetShaderPassEnabled("ShadowCaster",true);mat.renderQueue=-1;mat.SetFloat("_Cull",0);if(spec.glass){mat.SetFloat("_Surface",1);mat.SetFloat("_ZWrite",0);mat.SetFloat("_SrcBlend",5);mat.SetFloat("_DstBlend",10);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetShaderPassEnabled("ShadowCaster",false);mat.renderQueue=3000;}mats[i]=mat;EditorUtility.SetDirty(mat);}r.sharedMaterials=mats;}
   var bounds=BoundsOf(model.transform);model.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);bounds=BoundsOf(model.transform);var collider=g.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size;g.transform.position=position;g.transform.rotation=Quaternion.Euler(0,yaw,0);Debug.Log("APARTMENT11_READY "+id+" "+BoundsOf(g.transform));return g;
  }
  public static void Install(){
   Directory.CreateDirectory(Dir);AssetDatabase.Refresh();var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID);string lightState=string.Join("\n",lights.Select(l=>EditorJsonUtility.ToJson(l)));string vhs=EditorJsonUtility.ToJson(flat.look.vhs);
   var prior=flat.apartment.Find("Apartment11 room surfaces");if(prior)UnityEngine.Object.DestroyImmediate(prior.gameObject);root=new GameObject("Apartment11 room surfaces").transform;root.SetParent(flat.apartment,false);index=0;finishes=new List<Renderer>();
   string plaster=Art+"CC0/painted_plaster_wall/Diffuse.jpg",tile=Art+"CC0/Tiles107/Tiles107_1K-JPG_Color.jpg",tileNormal=Art+"CC0/Tiles107/Tiles107_1K-JPG_NormalGL.jpg";
   var white=Mat("Whitewash",plaster,new Color(.91f,.90f,.86f),.05f);
   var enamel=Mat("PaintedWindowFrames",plaster,new Color(.88f,.88f,.83f),.28f);
   var green=Mat("KitchenGreenPaint",plaster,new Color(.48f,.66f,.47f),.18f);
   var wallpaper=Mat("LivingWallpaper",Art+"Textures/GrandmaWallpaper.png",new Color(.87f,.82f,.7f),.04f);
   var woodPaper=Mat("HallWoodPaper",Art+"CC0/dark_wood/Diffuse.jpg",new Color(.85f,.8f,.7f),.06f);
   var blue=Mat("BathBlueTiles",tile,new Color(.45f,.68f,.74f),.42f,tileNormal,.2f);
   var tileWhite=Mat("KitchenWhiteTiles",tile,new Color(.83f,.83f,.77f),.38f,tileNormal,.16f);
   var lino=Mat("BrownLinoleum",Art+"CC0/linoleum_brown/Diffuse.jpg",new Color(.72f,.66f,.59f),.16f);
   var kitchenFloor=Mat("KitchenTilePrintLinoleum",Art+"CC0/square_tiled_wall/Diffuse.jpg",new Color(.84f,.87f,.8f),.09f);
   var bathFloor=Mat("BathFloorTiles",tile,new Color(.54f,.57f,.57f),.22f,tileNormal,.18f);
   var rooms=new[]{new Room("Living",2.9f,6.5f,0,4.4f,wallpaper,wallpaper,.65f),new Room("Kitchen",6.6f,8.8f,0,2.9f,green,white,3),new Room("HallA",6.6f,8.8f,3,6,woodPaper,woodPaper,1.7f),new Room("HallB",4.7f,6.6f,4.5f,6,woodPaper,woodPaper,1.7f),new Room("Bath",2.9f,4.6f,4.5f,6,blue,blue,1.2f)};
   var walls=flat.dressing.walls.Where(r=>r&&r.name!="Ceiling section 2.5m").ToArray();
   foreach(var r in walls){var b=r.bounds;foreach(var room in rooms){if(b.size.x<.4f){if(Mathf.Abs(b.max.x-room.x0)<.012f)Face(room,b,true,b.max.x,Vector3.right);if(Mathf.Abs(b.min.x-room.x1)<.012f)Face(room,b,true,b.min.x,Vector3.left);}if(b.size.z<.4f){if(Mathf.Abs(b.max.z-room.z0)<.012f)Face(room,b,false,b.max.z,Vector3.forward);if(Mathf.Abs(b.min.z-room.z1)<.012f)Face(room,b,false,b.min.z,Vector3.back);}}}
   foreach(var r in flat.dressing.walls.Where(r=>r&&r.name=="Ceiling section 2.5m")){var b=r.bounds;Quad("Whitewashed ceiling",new Vector3(b.min.x,2.498f,b.min.z),new Vector3(b.max.x,2.498f,b.min.z),new Vector3(b.max.x,2.498f,b.max.z),new Vector3(b.min.x,2.498f,b.max.z),Vector3.down,white,3,false);}
   foreach(var room in rooms.Where(r=>r.name!="Living")){Material material=room.name=="Bath"?bathFloor:room.name=="Kitchen"?kitchenFloor:lino;Quad(room.name+" floor finish",new Vector3(room.x0,.003f,room.z0),new Vector3(room.x1,.003f,room.z0),new Vector3(room.x1,.003f,room.z1),new Vector3(room.x0,.003f,room.z1),Vector3.up,material,room.name=="Bath"?1.2f:room.name=="Kitchen"?8:2,false);}
   // Backsplash uses the downloaded 8x8 tile map: repeat1.2m gives 15cm tiles.
   Quad("Kitchen sink tiled backsplash",new Vector3(6.604f,.85f,.95f),new Vector3(6.604f,.85f,2.2f),new Vector3(6.604f,1.5f,2.2f),new Vector3(6.604f,1.5f,.95f),Vector3.right,tileWhite,1.2f);
   Quad("Kitchen stove tiled backsplash",new Vector3(8.796f,.85f,1.25f),new Vector3(8.796f,.85f,2.2f),new Vector3(8.796f,1.5f,2.2f),new Vector3(8.796f,1.5f,1.25f),Vector3.left,tileWhite,1.2f);
   foreach(var r in flat.apartment.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Window frame"||r.name=="Window transom"||r.name=="Window sill"))if(r.bounds.center.x>2.9f)r.sharedMaterial=enamel;
   flat.dressing.finishedWalls=finishes.ToArray();
   foreach(var name in new[]{"PROXY Kenney bookcaseOpen","READY CC0 SovietShelf"}){var item=flat.apartment.Find(name);if(item)item.gameObject.SetActive(false);}
   Ready(flat,"PROXY Kenney televisionVintage","SovietTV",new Vector3(6.12f,.65f,2.5f),-90);
   PostSoviet(flat,"PROXY Kenney kitchenStove","Stove",new Vector3(8.48f,0,1.65f),-90);
   PostSoviet(flat,"PROXY Kenney kitchenFridge","Fridge",new Vector3(8.24f,0,2.43f),180);
   PostSoviet(flat,"PROXY Kenney table","DiningTable",new Vector3(7.58f,0,.52f),0);
   PostSoviet(flat,"PROXY Kenney bookcaseClosedDoors","Sideboard",new Vector3(6.09f,0,1.02f),-90);
   // Existing ready Kenney sink and upper cabinet remain temporary; remove wood-as-enamel fallback.
   foreach(var name in new[]{"PROXY Kenney kitchenSink","PROXY Kenney kitchenCabinetUpper","PROXY Kenney bathtub","PROXY Kenney bathroomSink","PROXY Kenney washer"}){var item=flat.apartment.Find(name);if(item)foreach(var r in item.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>enamel).ToArray();}
   foreach(var item in new[]{"READY CC0 SovietTV","READY PostSoviet Stove","READY PostSoviet Fridge","READY PostSoviet Sideboard"}){var t=flat.apartment.Find(item);var size=BoundsOf(t).size;if(size.x>2.5f||size.z>2.5f||size.y>2.1f)throw new Exception("Invalid furniture scale "+item+" "+size);}
   var coffee=flat.apartment.Find("READY CC0 SovietCoffeeTable");if(coffee){coffee.localScale=new Vector3(.66f,1,.66f);Debug.Log("APARTMENT11_COFFEE "+BoundsOf(coffee));}
   // Large textile is a textured plane, not new procedural furniture.
   var carpet=AssetDatabase.LoadAssetAtPath<Material>(Art+"M1Materials/Carpet.mat");if(carpet){var r=Quad("Living wall carpet",new Vector3(2.915f,.7f,.9f),new Vector3(2.915f,.7f,2.9f),new Vector3(2.915f,2.1f,2.9f),new Vector3(2.915f,2.1f,.9f),Vector3.right,carpet,1,false);var mesh=r.GetComponent<MeshFilter>().sharedMesh;mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};EditorUtility.SetDirty(mesh);}
   var overlap=new Bounds(new Vector3(2.1725f,1.25f,5.25f),new Vector3(5.045f,3,1.49f));flat.chaseOverlapRenderers=flat.apartment.GetComponentsInChildren<Renderer>(true).Where(r=>r.bounds.Intersects(overlap)).ToArray();flat.chaseOverlapColliders=flat.apartment.GetComponentsInChildren<Collider>(true).Where(r=>r.bounds.Intersects(overlap)).ToArray();
   if(lightState!=string.Join("\n",lights.Select(l=>EditorJsonUtility.ToJson(l)))||vhs!=EditorJsonUtility.ToJson(flat.look.vhs))throw new Exception("Accepted light/VHS changed");
   EditorUtility.SetDirty(flat.dressing);EditorUtility.SetDirty(flat);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("APARTMENT11_INSTALLED surfaces="+index+" original lights and VHS unchanged");
  }
 }
}
