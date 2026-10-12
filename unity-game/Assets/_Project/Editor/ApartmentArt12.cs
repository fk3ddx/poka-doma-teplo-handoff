using System;using System.IO;using System.Linq;using System.Collections.Generic;
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using Ataka.Scenario;using Object=UnityEngine.Object;
namespace Ataka.Editor {
 public static class ApartmentArt12 {
  const string Dir="Assets/_Project/Scenario/Apartment12/",Art="Assets/_Project/Art/";static Transform clothRoot;
  static Material Clone(string name,Material source){var path=Dir+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(source);AssetDatabase.CreateAsset(m,path);}return m;}
  static Mesh SaveMesh(string name,Mesh mesh){var path=Dir+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
  static Renderer MeshObject(string name,Mesh mesh,Material mat){var go=new GameObject(name);go.transform.SetParent(clothRoot,false);go.AddComponent<MeshFilter>().sharedMesh=SaveMesh(name,mesh);var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;return r;}
  static void Cloth(string name,float x0,float x1,float y0,float y1,float z,float folds,float depth,Material material){
   const int count=48;var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();for(int y=0;y<2;y++)for(int x=0;x<=count;x++){float u=(float)x/count;vertices.Add(new Vector3(Mathf.Lerp(x0,x1,u),y==0?y0:y1,z+Mathf.Cos(u*folds*Mathf.PI*2)*depth));uv.Add(new Vector2(u*(x1-x0),y*(y1-y0)));}
   for(int x=0;x<count;x++){int top=x+count+1;triangles.AddRange(new[]{x,top,x+1,x+1,top,top+1});}var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();MeshObject(name,mesh,material);
  }
  static void Horizontal(string name,float x0,float x1,float z0,float z1,float y,Material mat){var m=new Mesh{name=name};m.vertices=new[]{new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x1,y,z1),new Vector3(x0,y,z1)};m.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};m.triangles=new[]{0,2,1,0,3,2};m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();MeshObject(name,m,mat);}
  static bool Outside(string name)=>name.StartsWith("Facade_")||name.StartsWith("Courtyard ")||name.StartsWith("Local sodium scattering")||name.StartsWith("Entrance door ")||new[]{"Lamp arm","Sodium lamp","Soft snow edge","Weathered pavement","Kerb","Entrance warm pool","Entrance light glass","Windblown wet snow"}.Contains(name);
  public static void InstallAndBuild(){Install();ScenarioBuilder.Build();}
  public static void Install(){
   Directory.CreateDirectory(Dir);AssetDatabase.Refresh();var target=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=Object.FindFirstObjectByType<FlatBindings>();var accepted=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID).Where(l=>!l.transform.root.name.StartsWith("Apartment12")).ToArray();string before=string.Join("\n",accepted.Select(EditorJsonUtility.ToJson));string vhs=EditorJsonUtility.ToJson(flat.look.vhs);
   foreach(var previous in target.GetRootGameObjects().Where(g=>g.name.StartsWith("Apartment12")).ToArray())Object.DestroyImmediate(previous);
   var source=EditorSceneManager.OpenScene(ApartmentArtBuilder.ScenePath,OpenSceneMode.Additive);var author=source.GetRootGameObjects().Single(g=>g.name=="M1 authored apartment");var yard=Object.Instantiate(author);yard.name="Apartment12 exterior M1 architecture";UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(yard,target);yard.transform.position=Vector3.zero;
   foreach(Transform child in yard.transform.Cast<Transform>().ToArray())if(!Outside(child.name))Object.DestroyImmediate(child.gameObject);
   EditorSceneManager.CloseScene(source,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(target);
   foreach(var c in yard.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
   var shader=Shader.Find("Ataka/ExteriorAtmosphere");if(!shader)throw new Exception("Exterior shader missing");var materialMap=new Dictionary<Material,Material>();var outsideSurfaces=new List<Renderer>();
   foreach(var r in yard.GetComponentsInChildren<Renderer>()){
    r.renderingLayerMask=8;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;
    if(r is ParticleSystemRenderer||r.GetComponent<Ataka.LocalLightFog>())continue;
    var mapped=r.sharedMaterials.Select(original=>{if(materialMap.TryGetValue(original,out var existing))return existing;var m=Clone("Exterior_"+original.name,original);m.shader=shader;
     var tex=original.GetTexture("_BaseMap");m.SetTexture("_BaseMap",tex);m.SetTextureScale("_BaseMap",original.GetTextureScale("_BaseMap"));m.SetColor("_BaseColor",original.GetColor("_BaseColor"));m.SetTexture("_EmissionMap",original.GetTexture("_EmissionMap"));m.SetColor("_EmissionColor",original.IsKeywordEnabled("_EMISSION")?original.GetColor("_EmissionColor"):Color.black);m.SetColor("_FogColor",new Color(.19f,.25f,.30f));m.SetFloat("_Density",.025f);m.SetFloat("_LightLevel",.48f);m.SetFloat("_Power",1);EditorUtility.SetDirty(m);materialMap.Add(original,m);return m;}).ToArray();r.sharedMaterials=mapped;outsideSurfaces.Add(r);
   }
   var lights=yard.GetComponentsInChildren<Light>();
   foreach(var l in lights){l.GetUniversalAdditionalLightData().renderingLayers=(RenderingLayerMask)8u;l.shadows=LightShadows.None;}
   var sky=GameObject.CreatePrimitive(PrimitiveType.Sphere);sky.name="Overcast exterior sky";sky.transform.SetParent(yard.transform,false);sky.transform.localScale=Vector3.one*400;Object.DestroyImmediate(sky.GetComponent<Collider>());var skyMat=Clone("OvercastSky",materialMap.Values.First());skyMat.SetTexture("_BaseMap",null);skyMat.SetColor("_BaseColor",Color.black);skyMat.SetColor("_EmissionColor",Color.black);skyMat.SetFloat("_Density",1);skyMat.SetFloat("_LightLevel",0);sky.GetComponent<Renderer>().sharedMaterial=skyMat;sky.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;sky.GetComponent<Renderer>().renderingLayerMask=8;EditorUtility.SetDirty(skyMat);
   var exterior=yard.AddComponent<ScenarioExterior>();exterior.surfaces=outsideSurfaces.ToArray();exterior.practicals=lights;exterior.intensities=lights.Select(l=>l.intensity).ToArray();flat.look.exterior=exterior;
   // Room-scale dressing. No furniture is generated here, only permitted fabric surfaces.
   clothRoot=new GameObject("Apartment12 window textiles").transform;
   var tulle=Clone("Tulle",AssetDatabase.LoadAssetAtPath<Material>(Art+"M1Materials/Tulle.mat"));tulle.SetTextureScale("_BaseMap",Vector2.one*4.5f);tulle.SetColor("_BaseColor",new Color(.88f,.87f,.82f,.72f));tulle.SetFloat("_Cull",0);EditorUtility.SetDirty(tulle);
   var curtain=Clone("Curtains",AssetDatabase.LoadAssetAtPath<Material>(Art+"M1Materials/Curtain.mat"));curtain.SetTextureScale("_BaseMap",Vector2.one*4);curtain.SetColor("_BaseColor",new Color(.43f,.32f,.24f));curtain.SetFloat("_Cull",0);EditorUtility.SetDirty(curtain);
   Cloth("Living lace",3.27f,4.63f,.88f,1.87f,.035f,7,.016f,tulle);Cloth("Balcony lace",4.82f,5.53f,.22f,1.88f,.035f,5,.016f,tulle);
   Cloth("Living curtain left",3.03f,3.44f,.15f,2.39f,.095f,4,.035f,curtain);Cloth("Living curtain right",4.46f,4.76f,.15f,2.39f,.095f,3,.035f,curtain);Cloth("Balcony curtain right",5.53f,5.91f,.15f,2.39f,.095f,4,.035f,curtain);
   Cloth("Kitchen lace",7.12f,8.28f,.89f,1.68f,.035f,7,.018f,tulle);
   Cloth("Bedroom lace",.77f,2.03f,.89f,1.86f,.035f,7,.016f,tulle);
   var paper=Clone("HallLightWallpaper",AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Scenario/Apartment11/LivingWallpaper.mat"));paper.SetColor("_BaseColor",new Color(.74f,.77f,.70f));paper.SetTextureScale("_BaseMap",Vector2.one*(1.7f/.65f));EditorUtility.SetDirty(paper);
   foreach(var r in flat.dressing.finishedWalls)if(r&&(r.name.StartsWith("HallA")||r.name.StartsWith("HallB")))r.sharedMaterial=paper;
   var runner=Clone("HallRunner",AssetDatabase.LoadAssetAtPath<Material>(Art+"M1Materials/Carpet.mat"));runner.SetColor("_BaseColor",new Color(.61f,.54f,.47f));runner.SetTextureScale("_BaseMap",Vector2.one);EditorUtility.SetDirty(runner);Horizontal("Hall runner",7.65f,8.4f,3.15f,5.87f,.007f,runner);
   var oilcloth=Clone("KitchenOilcloth",AssetDatabase.LoadAssetAtPath<Material>(Art+"M1Materials/Oilcloth.mat"));oilcloth.SetTextureScale("_BaseMap",new Vector2(8,4));EditorUtility.SetDirty(oilcloth);Horizontal("Kitchen table oilcloth",6.85f,8.31f,.155f,.885f,.744f,oilcloth);
   var overlap=new Bounds(new Vector3(2.1725f,1.25f,5.25f),new Vector3(5.045f,3,1.49f));flat.chaseOverlapRenderers=flat.apartment.GetComponentsInChildren<Renderer>(true).Where(r=>r.bounds.Intersects(overlap)).ToArray();
   if(before!=string.Join("\n",accepted.Select(EditorJsonUtility.ToJson))||vhs!=EditorJsonUtility.ToJson(flat.look.vhs))throw new Exception("Accepted interior Light/VHS changed");
   EditorUtility.SetDirty(flat.look);EditorUtility.SetDirty(flat);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(target);EditorSceneManager.SaveScene(target);Debug.Log("APARTMENT12_INSTALLED exterior="+outsideSurfaces.Count+" existing interior lights and VHS unchanged");
  }
 }
}
