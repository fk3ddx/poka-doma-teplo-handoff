using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using Ataka.Scenario;using Object=UnityEngine.Object;
namespace Ataka.Editor {
 public static class KitchenArt13 {
  const string Dir="Assets/_Project/Art/ReadyModels/Kitchen13/";
  [Serializable] class Entry {public string name;public float[] color;public string[] textures;}
  [Serializable] class Manifest {public Entry[] materials;}
  static Bounds BoundsOf(GameObject g){var rr=g.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
  static void Place(FlatBindings flat,string id,string oldName,Vector3 at,float yaw){
   var folder=Dir+id+"/";var source=AssetDatabase.LoadAssetAtPath<GameObject>(folder+id+".fbx");if(!source)throw new Exception("Missing "+id);
   var old=flat.apartment.Find(oldName);if(old)old.gameObject.SetActive(false);
   var previous=flat.apartment.Find("READY Kitchen13 "+id);if(previous)Object.DestroyImmediate(previous.gameObject);
   var holder=new GameObject("READY Kitchen13 "+id);holder.transform.SetParent(flat.apartment,false);var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(holder.transform,false);
   var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(folder+"manifest.json"));
   foreach(var renderer in model.GetComponentsInChildren<Renderer>()){
    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
     var spec=manifest.materials.Single(s=>s.name==m.name);var path=folder+spec.name+"_URP.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
     if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
     material.SetColor("_BaseColor",new Color(spec.color[0],spec.color[1],spec.color[2],1));material.SetFloat("_Smoothness",id=="SovietSink"?.32f:.23f);material.SetFloat("_Metallic",0);
     if(spec.textures.Length>0)material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+spec.textures[0]));
     else if(id=="SovietSink"&&spec.name.EndsWith("_2")){ // white enamel: subtle photographed surface, not featureless color
      material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/CC0/painted_plaster_wall/Diffuse.jpg"));
     }
     EditorUtility.SetDirty(material);return material;
    }).ToArray();
   }
   var b=BoundsOf(model);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);b=BoundsOf(model);
   if(b.size.x>1.0f||b.size.y>1.1f||b.size.z>.6f)throw new Exception("Invalid prepared dimensions "+id+" "+b);
   var collider=holder.AddComponent<BoxCollider>();collider.center=b.center;collider.size=b.size;
   holder.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));Debug.Log("KITCHEN13_PLACED "+id+" "+BoundsOf(holder));
  }
  public static void InstallAndBuild(){
   AssetDatabase.Refresh();var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=Object.FindFirstObjectByType<FlatBindings>();
   var lights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID);string before=string.Join("\n",lights.Select(EditorJsonUtility.ToJson));string vhs=EditorJsonUtility.ToJson(flat.look.vhs);
   Place(flat,"SovietSink","PROXY Kenney kitchenSink",new Vector3(6.824f,.30f,1.6f),-90);
   Place(flat,"WoodenWallCabinet","PROXY Kenney kitchenCabinetUpper",new Vector3(6.769f,1.55f,1.6f),90);
   if(before!=string.Join("\n",lights.Select(EditorJsonUtility.ToJson))||vhs!=EditorJsonUtility.ToJson(flat.look.vhs))throw new Exception("Accepted light/VHS changed");
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("KITCHEN13_SAVED_LIGHT_VHS_UNCHANGED");ScenarioBuilder.Build();
  }
 }
}
