using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Editor {
 // Ataka.Editor.FinaleFacesInstaller.Install  (batch: -executeMethod Ataka.Editor.FinaleFacesInstaller.Install)
 // 11.10 "Misha's face" STAND-IN, cut from geometry already in the project: the face of Quaternius Universal Base
 // Characters "Superhero_Female_FullBody" (CC0, ScenarioProxies/Quaternius) — its Head-weighted front triangles with
 // its own UV and its own light skin texture; the eye-socket triangles inside the Eyes mesh are removed (eye holes).
 // It is an ADULT face, not Misha's (15, SCENARIO 2; 12 only on the 1993 photo): a labelled TEMPORARY placeholder for the action, to be replaced.
 // Output: Art/Characters/FinaleFaces/StandInFace_QuaterniusAdult.asset (+ .mat), assigned to FaceTearDeath in Flat.unity;
 // QA/FinaleFaces/install.txt. Accepted lights / VHS are checked unchanged.
 public static class FinaleFacesInstaller {
  const string Q="Assets/_Project/Art/ScenarioProxies/Quaternius/",Out="Assets/_Project/Art/Characters/FinaleFaces/";
  static int Dominant(BoneWeight w){int b=w.boneIndex0;float m=w.weight0;if(w.weight1>m){m=w.weight1;b=w.boneIndex1;}if(w.weight2>m){m=w.weight2;b=w.boneIndex2;}if(w.weight3>m){b=w.boneIndex3;}return b;}
  [MenuItem("Ataka/Finale/Install stand-in face (Quaternius adult)")]
  public static void InstallAndBuild(){Install();ScenarioBuilder.Build();}
  public static void Install(){
   Directory.CreateDirectory(Out);AssetDatabase.Refresh();
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=Object.FindFirstObjectByType<FlatBindings>();
   var lights=flat.apartment.GetComponentsInChildren<Light>(true);string lighting=string.Join("\n",lights.Select(l=>EditorJsonUtility.ToJson(l)));string vhs=EditorJsonUtility.ToJson(flat.look.vhs);
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Q+"Superhero_Female_FullBody.fbx");if(!prefab)throw new System.Exception("Missing "+Q+"Superhero_Female_FullBody.fbx");
   var inst=(GameObject)Object.Instantiate(prefab);inst.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
   try{
    var skins=inst.GetComponentsInChildren<SkinnedMeshRenderer>(true);var body=skins.OrderByDescending(s=>s.sharedMesh?s.sharedMesh.vertexCount:0).First();var eyes=skins.FirstOrDefault(s=>s.name.Contains("Eyes"));
    var head=body.bones.First(b=>b&&b.name=="Head");int hi=System.Array.IndexOf(body.bones,head);
    var baked=new Mesh();body.BakeMesh(baked,true);var v=baked.vertices.Select(p=>body.transform.TransformPoint(p)).ToArray();var w=body.sharedMesh.boneWeights;var uv=body.sharedMesh.uv;var tris=body.sharedMesh.triangles;
    // the two eyes (from the Eyes mesh) as boxes; the socket triangles inside them are cut out
    var eyeBoxes=new List<Bounds>();if(eyes){var em=new Mesh();eyes.BakeMesh(em,true);var ev=em.vertices.Select(p=>eyes.transform.TransformPoint(p)).ToArray();float cx=ev.Average(p=>p.x);
     foreach(var side in new[]{ev.Where(p=>p.x<cx).ToArray(),ev.Where(p=>p.x>=cx).ToArray()})if(side.Length>0){var b=new Bounds(side[0],Vector3.zero);foreach(var p in side)b.Encapsulate(p);b.Expand(.008f);eyeBoxes.Add(b);}Object.DestroyImmediate(em);}
    var headVerts=Enumerable.Range(0,v.Length).Where(i=>Dominant(w[i])==hi).ToArray();var centre=headVerts.Aggregate(Vector3.zero,(a,i)=>a+v[i])/Mathf.Max(1,headVerts.Length);
    var eyeCentre=eyeBoxes.Count>0?eyeBoxes.Aggregate(Vector3.zero,(a,b)=>a+b.center)/eyeBoxes.Count:centre+Vector3.forward;var front=eyeCentre-centre;front.y=0;front.Normalize();
    var keep=new List<int>();int holes=0;
    for(int i=0;i<tris.Length;i+=3){int a=tris[i],b=tris[i+1],c=tris[i+2];if(Dominant(w[a])!=hi||Dominant(w[b])!=hi||Dominant(w[c])!=hi)continue;var m=(v[a]+v[b]+v[c])/3;
     if(Vector3.Dot(m-centre,front)<.005f)continue;if(eyeBoxes.Any(e=>e.Contains(m))){holes++;continue;}keep.Add(a);keep.Add(b);keep.Add(c);}
    var used=keep.Distinct().ToArray();var origin=used.Aggregate(Vector3.zero,(a,i)=>a+v[i])/Mathf.Max(1,used.Length);var inv=Quaternion.Inverse(Quaternion.LookRotation(front,Vector3.up));
    var map=new Dictionary<int,int>();var nv=new List<Vector3>();var nuv=new List<Vector2>();foreach(var i in used){map[i]=nv.Count;nv.Add(inv*(v[i]-origin));nuv.Add(i<uv.Length?uv[i]:Vector2.zero);}
    var mesh=new Mesh{name="StandInFace_QuaterniusAdult"};mesh.SetVertices(nv);mesh.SetUVs(0,nuv);mesh.SetTriangles(keep.Select(i=>map[i]).ToList(),0);mesh.RecalculateNormals();mesh.RecalculateBounds();
    string meshPath=Out+"StandInFace_QuaterniusAdult.asset";AssetDatabase.DeleteAsset(meshPath);AssetDatabase.CreateAsset(mesh,meshPath);
    var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="StandInFace"};mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Q+"T_Superhero_Female_Light_BaseColor.png"));mat.SetColor("_BaseColor",new Color(1,.86f,.82f));mat.SetFloat("_Smoothness",.45f);mat.SetFloat("_Cull",0);mat.doubleSidedGI=true;
    string matPath=Out+"StandInFace.mat";AssetDatabase.DeleteAsset(matPath);AssetDatabase.CreateAsset(mat,matPath);
    var tear=Object.FindFirstObjectByType<FaceTearDeath>(FindObjectsInactive.Include);if(!tear)throw new System.Exception("FaceTearDeath not in Flat.unity");
    tear.standInFace=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);tear.standInFaceMaterial=AssetDatabase.LoadAssetAtPath<Material>(matPath);EditorUtility.SetDirty(tear);
    Directory.CreateDirectory("QA/FinaleFaces");File.WriteAllText("QA/FinaleFaces/install.txt","STAND-IN adult face (Quaternius Superhero_Female, CC0): "+keep.Count/3+" triangles, "+nv.Count+" vertices, "+holes+" eye-socket triangles cut, bounds "+mesh.bounds+"\nTEMPORARY: NOT Misha's own face (he is 15, SCENARIO 2).\n");
    Object.DestroyImmediate(baked);
   }finally{Object.DestroyImmediate(inst);}
   if(lighting!=string.Join("\n",lights.Select(l=>EditorJsonUtility.ToJson(l)))||vhs!=EditorJsonUtility.ToJson(flat.look.vhs))throw new System.Exception("Accepted lighting/VHS changed");
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("FINALE_FACES_INSTALLED "+File.ReadAllText("QA/FinaleFaces/install.txt"));
  }
 }
}
