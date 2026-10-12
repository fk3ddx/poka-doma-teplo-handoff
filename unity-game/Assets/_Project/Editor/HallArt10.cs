using System;using System.IO;using System.Linq;
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Rendering;using Ataka.Scenario;
namespace Ataka.Editor {
 public static class HallArt10 {
  const string Folder="Assets/_Project/Scenario/Hall10/";
  static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
  static Material Material(string name,Material original){var path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(original);AssetDatabase.CreateAsset(m,path);}return m;}
  public static void Install(){
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=UnityEngine.Object.FindFirstObjectByType<FlatBindings>();
   var lights=flat.apartment.GetComponentsInChildren<Light>(true);string lighting=string.Join("\n",lights.Select(l=>EditorJsonUtility.ToJson(l)));string vhs=EditorJsonUtility.ToJson(flat.look.vhs);
   var mirror=flat.hallCupboard.GetComponentsInChildren<Renderer>().Single(r=>r.name.Contains("mirror_0"));
   var planar=mirror.GetComponent<Ataka.PlanarMirror>();if(planar)UnityEngine.Object.DestroyImmediate(planar);
   var m=Material("OldMirrorProbe",AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Scenario/Hall09/MirrorFallback.mat"));m.SetColor("_BaseColor",new Color(.8f,.8f,.8f));m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",.58f);m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);mirror.sharedMaterial=m;mirror.reflectionProbeUsage=ReflectionProbeUsage.Simple;EditorUtility.SetDirty(m);
   var previous=flat.apartment.Find("Hall A reflection capture");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
   var go=new GameObject("Hall A reflection capture");go.transform.SetParent(flat.apartment,false);go.transform.position=new Vector3(7.65f,1.2f,4.55f);
   var probe=go.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.resolution=128;probe.hdr=true;probe.intensity=.8f;probe.boxProjection=true;probe.center=new Vector3(.05f,.05f,-.05f);probe.size=new Vector3(2.3f,2.55f,3.1f);probe.nearClipPlane=.04f;probe.farClipPlane=25;probe.shadowDistance=18;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=Color.black;probe.cullingMask=~((1<<30)|(1<<31));probe.importance=10;probe.blendDistance=.1f;
   var controller=go.AddComponent<HallReflections>();controller.probe=probe;controller.look=flat.look;
   var serialized=new SerializedObject(flat.look.pipeline);serialized.FindProperty("m_ReflectionProbeBoxProjection").boolValue=true;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(flat.look.pipeline);
   var torch=flat.torchPickup;var renderers=torch.GetComponentsInChildren<Renderer>();var glass=renderers.Single(r=>r.name=="signal_flashlight");var mf=glass.GetComponent<MeshFilter>();var vertices=mf.sharedMesh.vertices;var author=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/ScenarioProxies/PolyHaven/signal_flashlight/signal_flashlight.fbx").GetComponentsInChildren<Renderer>().Single(r=>r.name=="signal_flashlight");
   Debug.Log("HALL10 author material slots "+string.Join(",",author.sharedMaterials.Select(x=>x.name)));int lensSlot=Array.FindIndex(author.sharedMaterials,x=>x.name.ToLowerInvariant().Contains("glass"));if(lensSlot<0)throw new Exception("Author glass material not found");var triangles=mf.sharedMesh.GetTriangles(lensSlot);
   // Resolve Unity imported slots by AUTHOR names: FBX import may reorder Blender indices.
   Bounds LensBounds(){var b=new Bounds(mf.transform.TransformPoint(vertices[triangles[0]]),Vector3.zero);foreach(var i in triangles)b.Encapsulate(mf.transform.TransformPoint(vertices[i]));return b;}
   Vector3 lensNormal=Vector3.zero;float biggest=0;
   for(int i=0;i<triangles.Length;i+=3){var a=mf.transform.TransformPoint(vertices[triangles[i]]);var b=mf.transform.TransformPoint(vertices[triangles[i+1]]);var c=mf.transform.TransformPoint(vertices[triangles[i+2]]);var cross=Vector3.Cross(b-a,c-a);if(cross.sqrMagnitude>biggest){biggest=cross.sqrMagnitude;lensNormal=cross.normalized;}}
   torch.rotation=Quaternion.FromToRotation(lensNormal,new Vector3(.8660254f,0,.5f))*torch.rotation;
   var body=BoundsOf(torch);torch.position+=new Vector3(0,.242f-body.min.y,4.09f-body.center.z);
   torch.position+=Vector3.right*(7.10f-.035f-LensBounds().max.x);
   if(BoundsOf(torch).max.x>7.095f)torch.position+=Vector3.left*(BoundsOf(torch).max.x-7.095f);
   foreach(var r in renderers){r.reflectionProbeUsage=ReflectionProbeUsage.Simple;var materials=r.sharedMaterials;
    for(int i=0;i<materials.Length;i++){
     bool lens=r==glass&&i==lensSlot;var material=Material(lens?"TorchReflector":"TorchBody",materials[i]);material.SetFloat("_Surface",0);material.SetFloat("_ZWrite",1);material.SetFloat("_SrcBlend",1);material.SetFloat("_DstBlend",0);material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetOverrideTag("RenderType","Opaque");material.renderQueue=-1;
     if(lens)material.SetTexture("_BaseMap",null);
     material.SetColor("_BaseColor",lens?new Color(.8f,.82f,.79f):new Color(1.65f,1.65f,1.65f));material.SetFloat("_Metallic",lens?1:.2f);material.SetFloat("_Smoothness",lens?.9f:.35f);material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(material);materials[i]=material;
    }r.sharedMaterials=materials;
   }
   var phone=flat.apartment.Find("READY hallway VEF09");flat.phoneModel=phone;
   foreach(var phoneMesh in phone.GetComponentsInChildren<MeshFilter>())if(!phoneMesh.GetComponent<MeshCollider>())phoneMesh.gameObject.AddComponent<MeshCollider>().sharedMesh=phoneMesh.sharedMesh;
   if(lighting!=string.Join("\n",lights.Select(l=>EditorJsonUtility.ToJson(l)))||vhs!=EditorJsonUtility.ToJson(flat.look.vhs))throw new Exception("Accepted lighting/VHS changed");
   EditorUtility.SetDirty(flat);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Directory.CreateDirectory("QA/Hall10");File.WriteAllText("QA/Hall10/placement.txt","Mirror rough probe128 HDR, reflectance.8, no planar.\nTorch "+BoundsOf(torch)+" lens "+LensBounds()+"\nNo changed Light or VHS serialized properties.\n");Debug.Log("HALL10_INSTALLED");
  }
 }
}
