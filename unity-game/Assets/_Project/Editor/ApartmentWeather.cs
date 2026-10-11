using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Ataka.Editor {
 public static partial class ApartmentArtBuilder {
  static void WeatherParticles(){
   var previous=GameObject.Find("Windblown wet snow");if(previous!=null)Object.DestroyImmediate(previous);
   var snow=new GameObject("Windblown wet snow");snow.transform.SetParent(art,false);snow.transform.localPosition=new Vector3(4,8,-16);snow.transform.localRotation=Quaternion.Euler(90,0,0);
   var ps=snow.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=ps.main;main.startLifetime=7;main.startSpeed=3;main.startSize=new ParticleSystem.MinMaxCurve(.016f,.04f);main.startColor=new Color(.56f,.62f,.65f,.32f);main.maxParticles=1000;main.simulationSpace=ParticleSystemSimulationSpace.World;main.prewarm=true;
   var emission=ps.emission;emission.rateOverTime=130;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(40,26,1);
   var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=.8f;velocity.y=-.5f;
   var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/M1Materials/Sleet.mat");
   if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(mat,Root+"/Art/M1Materials/Sleet.mat");}
   mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",0);mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);mat.SetFloat("_ZWrite",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetOverrideTag("RenderType","Transparent");mat.renderQueue=3000;
   const int size=32;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);var colors=new Color[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++){float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;float a=Mathf.Clamp01(1-u*u-v*v);colors[y*size+x]=new Color(1,1,1,a*a);}
   tex.SetPixels(colors);tex.Apply();string path=Root+"/Art/Textures/SleetParticle.png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);EditorUtility.SetDirty(mat);
   var renderer=snow.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=mat;renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=1.3f;renderer.velocityScale=.02f;renderer.shadowCastingMode=ShadowCastingMode.Off;
  }
 }
}
