using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Ataka.Editor {
 public static class ScenarioReadyArt {
  const string Folder="Assets/_Project/Art/ReadyModels/CommieBlock";
  [Serializable] class TextureLink {public string name;public string texture;}
  [Serializable] class AssetEntry {public string id;public float height;public float[] dimensions;public TextureLink[] materials;}
  [Serializable] class Manifest {public AssetEntry[] assets;}
  static Manifest manifest;
  public static GameObject TryPlace(string proxy,Vector3 position,Vector3 maximumSize,float yaw,Transform parent){
   string id=proxy switch {"loungeSofa"=>"SovietSofa","loungeChair"=>"SovietArmchair","tableCoffee"=>"SovietCoffeeTable",_=>null};
   if(id==null)return null;
   manifest??=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"/manifest.json"));
   var entry=manifest.assets.First(x=>x.id==id);
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+id+".fbx");
   if(!source)throw new Exception("Missing prepared CC0 asset: "+id);
   var root=new GameObject("READY CC0 "+id);root.transform.SetParent(parent,false);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);model.transform.localRotation=Quaternion.identity;
   var renderers=model.GetComponentsInChildren<Renderer>();
   foreach(var r in renderers){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){
    var link=entry.materials.FirstOrDefault(x=>x.name==mats[i].name);
    if(link==null)throw new Exception("Unmapped author material "+mats[i].name+" on "+id);
    string path=Folder+"/"+link.name+"_URP.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
    var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+link.texture);if(!tex)throw new Exception("Missing author texture "+link.texture);
    mat.SetTexture("_BaseMap",tex);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.22f);EditorUtility.SetDirty(mat);mats[i]=mat;
   }r.sharedMaterials=mats;}
   Bounds Bounds(){var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
   var bounds=Bounds();float scale=entry.height/bounds.size.y;
   // Height in metres is authoritative; fitting a rotated AABB shrinks seats and makes table props float.
   model.transform.localScale*=scale;bounds=Bounds();model.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
   // Compute collision in the unrotated root, then rotate the whole object: no enlarged world-AABB around angled chairs.
   bounds=Bounds();var solid=root.AddComponent<BoxCollider>();solid.center=root.transform.InverseTransformPoint(bounds.center);solid.size=bounds.size;root.transform.localPosition=position;root.transform.localRotation=Quaternion.Euler(0,yaw,0);
   bounds=Bounds();Debug.Log("READY_ART_SCALE "+id+" metres="+bounds.size.ToString("F3")+" position="+position.ToString("F3"));
   return root;
  }
 }
}
