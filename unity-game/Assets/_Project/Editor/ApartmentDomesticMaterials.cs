using System.IO;
using UnityEditor;
using UnityEngine;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  static void DomesticCorner(){
   // A small household reproduction, not a claim that the original museum work is in this flat.
   Box("Icon shelf",new Vector3(2.66f,1.54f,.40f),new Vector3(.26f,.025f,.49f),"WoodLight",false);
   Box("Devotional picture frame",new Vector3(2.765f,1.76f,.38f),new Vector3(.025f,.39f,.29f),"WoodLight",false);
   var picture=GameObject.CreatePrimitive(PrimitiveType.Quad);picture.name="Household icon reproduction";picture.transform.SetParent(art,false);picture.transform.localPosition=new Vector3(2.749f,1.765f,.38f);picture.transform.localRotation=Quaternion.Euler(0,90,0);picture.transform.localScale=new Vector3(.255f,.345f,1);Object.DestroyImmediate(picture.GetComponent<Collider>());
   var icon=M("DevotionalPrint",Color.white,.10f);icon.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/OpenAccessMet/Icon831188.jpg");picture.GetComponent<Renderer>().sharedMaterial=icon;
   var glass=GameObject.CreatePrimitive(PrimitiveType.Sphere);glass.name="Red votive glass";glass.transform.SetParent(art,false);glass.transform.localPosition=new Vector3(2.57f,1.62f,.40f);glass.transform.localScale=new Vector3(.08f,.115f,.08f);glass.GetComponent<Renderer>().sharedMaterial=mats["BookRed"];Object.DestroyImmediate(glass.GetComponent<Collider>());
   Rod("Votive brass base",new Vector3(2.57f,1.553f,.40f),new Vector3(2.57f,1.569f,.40f),.045f,"Brass");
   Rod("Votive candle wick",new Vector3(2.57f,1.676f,.40f),new Vector3(2.57f,1.69f,.40f),.002f,"Black");
  }
  static void DomesticMaterials(){
   M("WasherBlue",C(.30f,.48f,.49f),.30f);
   Pbr(M("YardPaint",C(.18f,.27f,.24f),.18f),"plastered_wall_02",2);
   Pbr(M("GaragePaint",C(.16f,.21f,.21f),.20f),"plastered_wall_02",.8f);
   M("StackBand",C(.22f,.13f,.10f),.06f);
   // Original procedural crochet pattern: open net, six-petal medallion and seams.
   const int size=256;var pixels=new Color32[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    float u=(x+.5f)/size,v=(y+.5f)/size,dx=u-.5f,dy=v-.5f;
    float radius=Mathf.Sqrt(dx*dx+dy*dy),a=Mathf.Atan2(dy,dx);
    bool mesh=Mathf.Abs(Mathf.Repeat((u+v)*12,1)-.5f)<.035f||Mathf.Abs(Mathf.Repeat((u-v)*12,1)-.5f)<.035f;
    bool flower=Mathf.Abs(radius-(.23f+.065f*Mathf.Cos(a*6)))<.011f||Mathf.Abs(radius-.052f)<.011f;
    bool stem=Mathf.Abs(dx-.018f*Mathf.Sin(v*20))<.008f && Mathf.Abs(dy)>.29f;
    pixels[y*size+x]=new Color32(227,224,212,(byte)(mesh||flower||stem?255:0));
   }
   var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);tex.SetPixels32(pixels);tex.Apply();
   string path=Root+"/Art/Textures/CrochetTulle.png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Repeat;importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.35f;importer.SaveAndReimport();
   var tulle=M("Tulle",C(.84f,.83f,.77f),.01f);tulle.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);tulle.mainTextureScale=Vector2.one*2.5f;tulle.SetFloat("_Cull",0);tulle.SetFloat("_AlphaClip",1);tulle.SetFloat("_Cutoff",.35f);tulle.EnableKeyword("_ALPHATEST_ON");tulle.renderQueue=2450;
  }
 }
}
