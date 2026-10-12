using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Editor {
 public static class TelevisionSurfaceInstaller {
  public static void InstallAndBuild(){
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");
   var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();var root=flat.apartment.Find("READY CC0 SovietTV");if(!root)throw new Exception("Authored SovietTV missing");
   // UV island 15 in the original CC0 TV: the flat centre of its glass. Copy the author's triangles exactly;
   // the old display box belonged to the previous Kenney TV and floated ahead of this replacement's bezel.
   var verts=new List<Vector3>();var ids=new List<int>();
   foreach(var mf in root.GetComponentsInChildren<MeshFilter>()){
    var mesh=mf.sharedMesh;if(!mesh)continue;var uv=mesh.uv;var vs=mesh.vertices;var tr=mesh.triangles;
    bool Glass(int i)=>uv[i].x>=.493f&&uv[i].x<=.755f&&uv[i].y>=.505f&&uv[i].y<=.728f;
    for(int i=0;i<tr.Length;i+=3){if(!Glass(tr[i])||!Glass(tr[i+1])||!Glass(tr[i+2]))continue;
     var a=mf.transform.TransformPoint(vs[tr[i]]);var b=mf.transform.TransformPoint(vs[tr[i+1]]);var c=mf.transform.TransformPoint(vs[tr[i+2]]);
     if(Mathf.Abs(Vector3.Dot(Vector3.Cross(b-a,c-a).normalized,Vector3.left))<.99f)continue;
     foreach(int k in new[]{tr[i],tr[i+1],tr[i+2]}){ids.Add(verts.Count);verts.Add(mf.transform.TransformPoint(vs[k])+Vector3.left*.001f);}
    }
   }
   if(verts.Count!=6)throw new Exception("Expected author's 2 centre-screen triangles, found "+verts.Count/3);
   var bounds=new Bounds(verts[0],Vector3.zero);foreach(var v in verts)bounds.Encapsulate(v);
   if(bounds.size.y<.15f||bounds.size.y>.4f||bounds.size.z<.25f||bounds.size.z>.5f)throw new Exception("Invalid glass dimensions "+bounds);
   var screen=flat.televisionScreen;screen.transform.SetParent(root,false);screen.transform.localPosition=Vector3.zero;screen.transform.localRotation=Quaternion.identity;screen.transform.localScale=Vector3.one;
   var tex=new List<Vector2>();for(int i=0;i<verts.Count;i++){var v=verts[i];tex.Add(new Vector2((bounds.max.z-v.z)/bounds.size.z,(v.y-bounds.min.y)/bounds.size.y));verts[i]=screen.transform.InverseTransformPoint(v);}
   var display=new Mesh{name="Authored SovietTV centre glass display"};display.SetVertices(verts);display.SetTriangles(ids,0);display.SetUVs(0,tex);display.RecalculateNormals();display.RecalculateBounds();
   const string path="Assets/_Project/Scenario/SovietTVDisplay.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(display,existing);UnityEngine.Object.DestroyImmediate(display);display=existing;}else AssetDatabase.CreateAsset(display,path);
   screen.GetComponent<MeshFilter>().sharedMesh=display;screen.name="Authored TV glass live picture";foreach(var c in screen.GetComponents<Collider>())c.enabled=false;screen.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   EditorUtility.SetDirty(flat);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
   Debug.Log("TV_AUTHORED_SURFACE triangles=2 world="+bounds+"; original author mesh/texture and lighting unchanged");ScenarioBuilder.Build();
  }
 }
}
