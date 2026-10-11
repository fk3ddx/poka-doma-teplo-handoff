using UnityEditor;
using UnityEngine;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  static void PersonalMaterials(){
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/QuietWalnutVeneer.png");
   void Veneer(string name,Color tint,float scale,float smooth){
    var m=M(name,tint,smooth);m.mainTexture=texture;m.mainTextureScale=Vector2.one*scale;
    m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");m.SetTexture("_MetallicGlossMap",null);m.DisableKeyword("_METALLICSPECGLOSSMAP");
    m.SetTexture("_OcclusionMap",null);m.DisableKeyword("_OCCLUSIONMAP");m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",smooth);
   }
   Veneer("Wood",C(.64f,.60f,.53f),1.1f,.24f);
   Veneer("WoodLight",C(.85f,.79f,.68f),1.2f,.28f);
   Veneer("WoodWalnut",C(.78f,.72f,.64f),1.0f,.38f);
   Veneer("WoodWalnutTrim",C(.52f,.46f,.38f),1.6f,.30f);
   Veneer("WoodAged",C(.66f,.56f,.43f),1.25f,.20f);
   Veneer("WoodBeech",C(.91f,.78f,.57f),2.0f,.27f);
   var teak=M("WoodTeak",C(.40f,.43f,.46f),.35f);Pbr(teak,"dark_wood",1.3f);teak.SetFloat("_BumpScale",.08f);teak.SetFloat("_Smoothness",.28f);
  }
  static void PersonalPhotos(){
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/FamilySnapshotsAtlas.png");
   var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=1024;importer.SaveAndReimport();
   for(int i=0;i<2;i++){
    var photo=GameObject.CreatePrimitive(PrimitiveType.Quad);photo.name=i==0?"Wedding print in cabinet":"Dacha print in cabinet";
    photo.transform.SetParent(art,false);photo.transform.localPosition=new Vector3(6.0545f,i==0?1.38f:1.37f,i==0?1.57f:2.21f);
    photo.transform.localRotation=Quaternion.Euler(0,90,0);photo.transform.localScale=new Vector3(.145f,.205f,1);Object.DestroyImmediate(photo.GetComponent<Collider>());
    var m=M(i==0?"WeddingPrint":"DachaPrint",new Color(.89f,.88f,.85f),.12f);m.mainTexture=texture;m.mainTextureScale=new Vector2(.5f,1);m.mainTextureOffset=new Vector2(i*.5f,0);m.SetFloat("_Cull",0);
    photo.GetComponent<Renderer>().sharedMaterial=m;Static(photo);
   }
  }
 }
}
