using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  static void CreateDistantWindowCurtain(Material material){
   const int size=256;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    float u=(x+.5f)/size,v=(y+.5f)/size;
    float edge=Mathf.SmoothStep(.16f,1,Mathf.Clamp01(Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v))/.12f));
    float folds=.58f+.20f*Mathf.Sin(u*52)+.07f*Mathf.Sin(u*97);
    float light=Mathf.Lerp(.54f,1,v)*folds*edge;
    // Curtain silhouette and a darker upper transom; authored, not a copied interior photo.
    if(v>.81f)light*=.65f;
    if(u>.43f&&u<.48f)light*=.42f;
    pixels[y*size+x]=new Color(light,light*.95f,light*.83f,1);
   }
   tex.SetPixels(pixels);tex.Apply();string path=Root+"/Art/Textures/DistantWindowCurtain.png";
   File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);material.mainTexture=texture;material.SetTexture("_EmissionMap",texture);
  }
  // Material-only iteration preserves geometry, drawer state wiring and baked map assignments.
  public static void PolishMaterials(){
   EditorSceneManager.OpenScene(ScenePath);
   var crystal=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/M1Materials/Crystal.mat");
   crystal.SetFloat("_Blend",0);crystal.SetFloat("_BlendModePreserveSpecular",1);crystal.EnableKeyword("_ALPHAPREMULTIPLY_ON");
   crystal.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.One);EditorUtility.SetDirty(crystal);
   var window=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/M1Materials/WindowWarm.mat");
   window.SetColor("_EmissionColor",C(.90f,.54f,.24f));CreateDistantWindowCurtain(window);EditorUtility.SetDirty(window);
   AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());Debug.Log("M1_MATERIAL_POLISH_COMPLETE");
  }
 }
}
