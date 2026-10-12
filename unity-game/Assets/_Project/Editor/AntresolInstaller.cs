using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
using Object=UnityEngine.Object;
namespace Ataka.Editor {
 // Idempotent: the hall antresol (SCENARIO 4.4 «лампочки на антресоли») — a second instance of the already installed,
 // authored Kitchen13 WoodenWallCabinet (skw1r, CC BY 4.0), hung over the passage between hall A and hall B with its
 // back to the south wall (z 6.0), bottom 1.65 m, top 2.29 m (ceiling 2.5). The story's bulb lies on its top front edge.
 // Ataka.Editor.AntresolInstaller.Install (then ScenarioBuilder.Build). Lights/VHS are not touched.
 public static class AntresolInstaller {
  const string Folder="Assets/_Project/Art/ReadyModels/Kitchen13/WoodenWallCabinet/";
  public static readonly Vector3 At=new Vector3(6.6f,1.65f,5.835f);public const float Yaw=180; // front faces the hall (−z)
  [Serializable] class Entry {public string name;public float[] color;public string[] textures;}
  [Serializable] class Manifest {public Entry[] materials;}
  public static void Install(){
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=Object.FindFirstObjectByType<FlatBindings>();
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"WoodenWallCabinet.fbx");if(!source)throw new Exception("Missing "+Folder+"WoodenWallCabinet.fbx");
   var previous=flat.apartment.Find("READY Antresol WoodenWallCabinet");if(previous)Object.DestroyImmediate(previous.gameObject);
   var holder=new GameObject("READY Antresol WoodenWallCabinet");holder.transform.SetParent(flat.apartment,false);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(holder.transform,false);
   var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"manifest.json"));
   foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>{
    var spec=manifest.materials.Single(s=>s.name==m.name);var path=Folder+spec.name+"_URP.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(spec.color[0],spec.color[1],spec.color[2],1));material.SetFloat("_Smoothness",.23f);
     if(spec.textures.Length>0)material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+spec.textures[0]));AssetDatabase.CreateAsset(material,path);}
    return material;}).ToArray(); // the kitchen cabinet's own URP materials (KitchenArt13) are reused as they are
   var rs=model.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
   b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);var box=holder.AddComponent<BoxCollider>();box.center=b.center;box.size=b.size;
   holder.transform.SetPositionAndRotation(At,Quaternion.Euler(0,Yaw,0));
   b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);Debug.Log("ANTRESOL placed "+b.min.ToString("F3")+" … "+b.max.ToString("F3"));
   EditorUtility.SetDirty(flat);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
 }
}
