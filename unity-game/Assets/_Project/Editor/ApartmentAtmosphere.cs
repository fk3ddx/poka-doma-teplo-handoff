using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  static void MaterialRefinements(){
   foreach(string name in new[]{"Fabric","DarkFabric","Coat"}){
    if(!mats.ContainsKey(name))M(name,C(.22f,.25f,.23f),.05f);
    var m=mats[name];m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/Upholstery.png");m.mainTextureScale=Vector2.one*2;m.DisableKeyword("_METALLICSPECGLOSSMAP");m.DisableKeyword("_NORMALMAP");m.SetFloat("_Smoothness",.03f);
   }
   mats["Fabric"].color=C(.7f,.66f,.53f);
   // White undyed cotton: retain scanned weave normal/roughness, omit the yellow dyed albedo.
   foreach(string name in new[]{"Linen","Lace"}){var linen=M(name,C(.86f,.85f,.80f),.02f);Pbr(linen,"waffle_pique_cotton",3);linen.mainTexture=null;linen.SetFloat("_BumpScale",.24f);linen.SetFloat("_Smoothness",.06f);linen.SetFloat("_Cull",0);}
   foreach(string wood in new[]{"Wood","WoodLight"}){var m=mats[wood];m.mainTextureScale=Vector2.one*.68f;m.SetFloat("_BumpScale",.17f);m.SetFloat("_Smoothness",.52f);m.color=wood=="Wood"?C(.56f,.59f,.63f):C(.72f,.70f,.63f);}
   M("Leather",C(.10f,.071f,.044f),.24f);M("ShadeBinding",C(.42f,.27f,.1f),.65f,.65f);
   M("Tile",C(.62f,.67f,.60f),.28f);M("Bread",C(.27f,.15f,.075f),.06f);
   var photo=M("FamilyPhoto",Color.white,.2f);photo.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/FamilyPhoto.png");photo.SetFloat("_Cull",0);
   mats["Glass"].SetFloat("_SpecularHighlights",0);mats["Glass"].EnableKeyword("_SPECULARHIGHLIGHTS_OFF");mats["Glass"].SetFloat("_Smoothness",.25f);
   var cloth=M("Oilcloth",C(.75f,.75f,.65f),.2f);var tex=new Texture2D(256,256);var px=new Color[256*256];
   for(int y=0;y<256;y++)for(int x=0;x<256;x++){bool a=(x/32)%2==0,b=(y/32)%2==0;float tint=a&&b?.94f:a||b?.78f:.62f;float thread=((x+y)%3)*.008f;px[y*256+x]=new Color(tint-thread,tint-thread,tint*.91f);}
   tex.SetPixels(px);tex.Apply();string path=Root+"/Art/Textures/OilclothCheck.png";File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);cloth.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);cloth.mainTextureScale=Vector2.one*5;cloth.SetFloat("_Cull",0);
   BuildScreen();
  }
  static void BuildScreen(){
   int w=256,h=224;var pixels=new Color32[w*h];for(int y=0;y<h;y++)for(int x=0;x<w;x++)pixels[y*w+x]=new Color32(19,35,56,255);
   void Rect(int x,int y,int rw,int rh,Color32 c){for(int j=Mathf.Max(0,y);j<Mathf.Min(h,y+rh);j++)for(int i=Mathf.Max(0,x);i<Mathf.Min(w,x+rw);i++)pixels[j*w+i]=c;}
   var moss=new Color32(77,108,67,255);var bark=new Color32(83,57,43,255);var gold=new Color32(183,158,76,255);
   Rect(0,0,w,37,bark);Rect(0,34,w,8,moss);
   for(int i=0;i<6;i++){int x=12+i*47;Rect(x,38,8,94,bark);Rect(x-14,109,34,21,moss);Rect(x-8,129,22,10,moss);}
   Rect(66,77,50,8,moss);Rect(150,118,52,8,moss);Rect(90,42,10,17,new Color32(145,170,169,255));Rect(93,57,10,10,gold);Rect(88,40,7,5,bark);Rect(100,40,7,5,bark);
   string[] glyphs={"111101111100100","111101111101101","101101101101111","111100111001111","111100110100111"};
   for(int c=0;c<5;c++)for(int yy=0;yy<5;yy++)for(int xx=0;xx<3;xx++)if(glyphs[c][yy*3+xx]=='1')Rect(78+c*22+xx*4,180-yy*4,4,4,gold);
   var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.SetPixels32(pixels);tex.Apply();string path=Root+"/Art/Textures/EightBitPause.png";File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.filterMode=FilterMode.Point;imp.wrapMode=TextureWrapMode.Clamp;imp.SaveAndReimport();
   var screen=mats["TVScreen"];screen.shader=Shader.Find("Ataka/CRT");screen.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));screen.SetFloat("_Power",1);
  }
  static void Atmosphere(){
   var audio=new GameObject("Domestic ambience").AddComponent<Ataka.DomesticSoundscape>();audio.transform.position=Vector3.up*Floor;
   AudioClip Clip(string n)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/"+n+".wav");
   audio.radio=Clip("radio_original_waltz");audio.weather=Clip("sleet_outside");audio.tick=Clip("clock_tick");audio.steps=Enumerable.Range(0,4).Select(i=>Clip("wood_step_"+i)).ToArray();audio.player=UnityEngine.Object.FindFirstObjectByType<Ataka.GameSession>().player;audio.kitchenPower=UnityEngine.Object.FindObjectsByType<Ataka.LampSwitch>(FindObjectsSortMode.None).First(x=>x.persistentId=="lamp-kitchen");
   // Paper objects have real position, tilt and writing, rather than UI labels.
   Box("Tear off calendar",new Vector3(8.773f,1.72f,1.40f),new Vector3(.015f,.33f,.23f),"Paper",false);
   Label("НОЯБРЬ 1996\n\n15\nПЯТНИЦА",new Vector3(8.759f,1.80f,1.40f),Quaternion.Euler(0,90,0),.03f,C(.12f,.10f,.09f));
   Box("Newspaper",new Vector3(4.14f,.758f,1.94f),new Vector3(.26f,.005f,.34f),"Paper",false);
   Label("ВЕЧЕРНИЙ ГОРОД\n────────────\nНОВОСТИ РАЙОНА",new Vector3(4.14f,.762f,1.94f),Quaternion.Euler(90,0,0),.021f,C(.14f,.13f,.11f));
   foreach(var d in UnityEngine.Object.FindObjectsByType<Ataka.Door>(FindObjectsSortMode.None)){
    if(d.persistentId=="living-door")d.openAngle=95;
    float width=d.leaf.size.x+.02f;LocalBox(d.transform,"Door left casing",new Vector3(-.024f,1.01f,0),new Vector3(.067f,2.06f,.13f),"WoodLight");LocalBox(d.transform,"Door right casing",new Vector3(width+.024f,1.01f,0),new Vector3(.067f,2.06f,.13f),"WoodLight");LocalBox(d.transform,"Door top casing",new Vector3(width*.5f,2.035f,0),new Vector3(width+.1f,.075f,.13f),"WoodLight");
   }
   foreach(var p in new[]{new Vector3(2.914f,.32f,3.04f),new Vector3(6.487f,.32f,.6f),new Vector3(8.733f,1.02f,2.48f)}){
    Box("Old power socket",p,new Vector3(.033f,.085f,.09f),"Enamel",false);foreach(float z in new[]{-.016f,.016f})Box("Socket pin recess",p+new Vector3(-.018f,0,z),new Vector3(.005f,.008f,.008f),"Black",false);
   }
   WeatherParticles();
  }
  static void Label(string text,Vector3 p,Quaternion rot,float size,Color color){
   string file;Vector2 dimensions;
   if(text.StartsWith("НОЯБРЬ")){file="CalendarPrint";dimensions=new Vector2(.205f,.30f);p.y-=.08f;}
   else if(text.StartsWith("ВЕЧЕРНИЙ")){file="NewspaperPrint";dimensions=new Vector2(.255f,.33f);}
   else if(text=="ЦОЙ ЖИВ"){file="Graffiti";dimensions=new Vector2(.55f,.13f);}
   else {file="Number"+text;dimensions=new Vector2(.07f,.045f);}
   var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Printed "+file;go.transform.SetParent(art,false);go.transform.localPosition=p;go.transform.localRotation=rot;go.transform.localScale=new Vector3(dimensions.x,dimensions.y,1);UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
   var mat=M(file,Color.white,.05f);mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/"+file+".png");mat.SetFloat("_Cull",0);mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.5f);mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;go.GetComponent<MeshRenderer>().sharedMaterial=mat;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
  }
  static void LocalBox(Transform parent,string name,Vector3 p,Vector3 size,string material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mats[material];UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());}
 }
}
