using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class BedroomReadyProps {
  const string Folder="Assets/_Project/Art/CC0/painted_wooden_stool";
  static Bounds BoundsOf(GameObject root){var rs=root.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new Exception("Stool has no renderers");var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
  public static GameObject Create(Transform parent,Vector3 position){
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/painted_wooden_stool.fbx");if(!source)throw new Exception("Missing Poly Haven stool");
   foreach(var file in new[]{"diffuse.jpg","normal.png"}){var ti=(TextureImporter)AssetImporter.GetAtPath(Folder+"/"+file);ti.maxTextureSize=512;ti.filterMode=FilterMode.Bilinear;ti.textureType=file=="normal.png"?TextureImporterType.NormalMap:TextureImporterType.Default;ti.SaveAndReimport();}
   var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/StoolURP.mat");if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,Folder+"/StoolURP.mat");}
   material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/diffuse.jpg"));material.SetColor("_BaseColor",Color.white);material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/normal.png"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.7f);material.SetFloat("_Smoothness",.18f);EditorUtility.SetDirty(material);
   var root=new GameObject("READY CC0 painted wooden stool");root.transform.SetParent(parent,false);var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
   foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>material).ToArray();
   var b=BoundsOf(root);model.transform.localScale*=.45f/b.size.y;b=BoundsOf(root);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
   b=BoundsOf(root);var collider=root.AddComponent<BoxCollider>();collider.center=b.center;collider.size=b.size;root.transform.position=position;
   Debug.Log("READY_STOOL_METRES "+b.size.ToString("F4"));return root;
  }
  public static void InstallAndBuild(){
   EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   var old=flat.stool;var position=old.transform.position;flat.stool=Create(old.transform.parent,position);UnityEngine.Object.DestroyImmediate(old);EditorUtility.SetDirty(flat);
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
   var upright=BoundsOf(flat.stool);flat.stool.transform.SetPositionAndRotation(new Vector3(1.25f,0,2.55f),Quaternion.Euler(0,20,90));var b=BoundsOf(flat.stool);flat.stool.transform.position+=Vector3.up*(.004f-b.min.y);b=BoundsOf(flat.stool);
   Directory.CreateDirectory("../QA/BedroomLayout");File.WriteAllText("../QA/BedroomLayout/stool.txt","Ready asset: Poly Haven painted_wooden_stool / Kirill Sannikov / CC0\nUpright metres="+upright.size.ToString("F4")+"\nOn side floor min y="+b.min.y.ToString("F4")+"\n");
   if(Mathf.Abs(upright.size.y-.45f)>.001f||Mathf.Abs(b.min.y-.004f)>.001f)throw new Exception("Stool scale/floor validation failed");
   EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");ScenarioBuilder.Build();
  }
 }
}
