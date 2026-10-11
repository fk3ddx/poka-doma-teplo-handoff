using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class BedroomBedInstaller {
  const string Folder="Assets/_Project/Art/CC0/old_bed_frame";
  static Material Material(string name,string diffuse,string normal,Color color,float smooth){
   string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   foreach(string file in new[]{diffuse,normal})if(!string.IsNullOrEmpty(file)){var ti=(TextureImporter)AssetImporter.GetAtPath(file);ti.maxTextureSize=512;ti.filterMode=FilterMode.Bilinear;ti.textureType=file==normal?TextureImporterType.NormalMap:TextureImporterType.Default;if(file.EndsWith("LinenDiffuse.jpg")||file.EndsWith("CoverDiffuse.jpg")){ti.swizzleR=TextureImporterSwizzle.R;ti.swizzleG=TextureImporterSwizzle.R;ti.swizzleB=TextureImporterSwizzle.R;}ti.SaveAndReimport();}
   m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(diffuse));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);
   if(!string.IsNullOrEmpty(normal)){m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));m.SetFloat("_BumpScale",.6f);m.EnableKeyword("_NORMALMAP");}EditorUtility.SetDirty(m);return m;
  }
  public static GameObject Create(Transform parent){
   var root=new GameObject("READY Poly Haven iron bed with textured linen");root.transform.SetParent(parent,false);
   var frame=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/old_bed_frame.fbx"));frame.transform.SetParent(root.transform,false);
   var frameMat=Material("FrameURP",Folder+"/Textures/old_bed_frame_diff.png",Folder+"/Textures/old_bed_frame_nor_gl.png",Color.white,.22f);
   foreach(var rr in frame.GetComponentsInChildren<Renderer>())rr.sharedMaterials=rr.sharedMaterials.Select(_=>frameMat).ToArray();
   var linen=Material("LinenURP",Folder+"/LinenDiffuse.jpg",Folder+"/LinenNormal.jpg",new Color(.83f,.79f,.70f),.03f);
   var cover=Material("CoverURP",Folder+"/CoverDiffuse.jpg",Folder+"/CoverNormal.jpg",new Color(.32f,.39f,.34f),.04f);
   var textile=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/BedTextiles.fbx"));textile.transform.SetParent(root.transform,false);textile.transform.localRotation=Quaternion.Euler(0,180,0);
   foreach(var rr in textile.GetComponentsInChildren<Renderer>())rr.sharedMaterials=rr.sharedMaterials.Select(m=>m.name.Contains("Cover")?cover:linen).ToArray();
   var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,.58f,0);collider.size=new Vector3(.90f,.16f,2);root.transform.position=new Vector3(.50f,0,1.80f);
   Debug.Log("READY_BED footprint .904 x 2.000 m; height 1.200 m; mattress .86 x .14 x 1.88; three pillows .60/.50/.40. Lace pending.");return root;
  }
  public static void InstallAndBuild(){
   EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   foreach(var t in flat.apartment.GetComponentsInChildren<Transform>(true).ToArray())if(t&&(t.name=="PROXY Kenney bedSingle"||t.name=="READY Poly Haven iron bed with textured linen"||(t.name=="PROXY Kenney pillow"&&t.position.x<1&&t.position.z<2)))UnityEngine.Object.DestroyImmediate(t.gameObject);
   Create(flat.apartment);flat.look.icon.transform.position=new Vector3(.22f,1.6f,.22f);
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());ScenarioBuilder.Build();
  }
 }
}
