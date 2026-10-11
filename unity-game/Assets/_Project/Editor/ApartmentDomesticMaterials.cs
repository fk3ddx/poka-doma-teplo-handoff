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
   const int size=1024;var pixels=new Color32[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){
    float u=(x+.5f)/size,v=(y+.5f)/size,dx=u-.5f,dy=v-.5f;
    float radius=Mathf.Sqrt(dx*dx+dy*dy),a=Mathf.Atan2(dy,dx);
    float Band(float distance,float inner,float outer)=>1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(inner,outer,distance));
    float netA=Mathf.Abs(Mathf.Repeat((u+v)*32,1)-.5f),netB=Mathf.Abs(Mathf.Repeat((u-v)*32,1)-.5f);
    float net=Band(Mathf.Min(netA,netB),.035f,.075f);
    float outline=Mathf.Min(Mathf.Abs(radius-(.23f+.065f*Mathf.Cos(a*6))),Mathf.Abs(radius-.052f));
    float flower=Band(outline,.005f,.009f);
    float stem=Band(Mathf.Abs(dx-.018f*Mathf.Sin(v*20)),.003f,.007f)*(1-Band(Mathf.Abs(dy),.28f,.31f));
    float opacity=Mathf.Max(.07f+net*.60f,Mathf.Max(flower,stem)*.82f);
    pixels[y*size+x]=new Color32(227,224,212,(byte)Mathf.RoundToInt(opacity*255));
   }
   var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);tex.SetPixels32(pixels);tex.Apply();
   string path=Root+"/Art/Textures/CrochetTulle.png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=false;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.SaveAndReimport();
   var tulle=M("Tulle",C(.94f,.94f,.91f),.01f);tulle.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);tulle.mainTextureScale=Vector2.one*4.5f;tulle.SetFloat("_Cull",0);
   tulle.SetFloat("_Surface",1);tulle.SetFloat("_Blend",0);tulle.SetFloat("_AlphaClip",0);tulle.DisableKeyword("_ALPHATEST_ON");tulle.DisableKeyword("_ALPHAPREMULTIPLY_ON");
   tulle.SetFloat("_BlendModePreserveSpecular",0);tulle.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);tulle.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
   tulle.SetFloat("_SrcBlendAlpha",(float)UnityEngine.Rendering.BlendMode.One);tulle.SetFloat("_DstBlendAlpha",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);tulle.SetFloat("_ZWrite",0);
   tulle.SetFloat("_SpecularHighlights",0);tulle.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");tulle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");tulle.SetOverrideTag("RenderType","Transparent");tulle.SetShaderPassEnabled("ShadowCaster",false);tulle.renderQueue=3000;
  }
 }
}
