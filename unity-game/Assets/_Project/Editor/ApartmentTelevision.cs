using UnityEditor;
using UnityEngine;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  static void CreateTelevisionMaterial(){
   var m=M("Television_01",Color.white,.65f);Pbr(m,"Television_01");
   m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/CC0/Television_01/Television_smoothness.png"));
   m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",.65f);m.SetFloat("_BumpScale",.5f);
  }
 }
}
