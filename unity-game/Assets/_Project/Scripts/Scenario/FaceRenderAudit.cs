using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Ataka.Scenario {
 // Diagnostic only: compare original and reconstructed head at the same frozen pose, camera and light.
 // No scene/asset writes, no gameplay acceptance. Enabled only by the explicit command line switch.
 public sealed class FaceRenderAudit:MonoBehaviour {
  string output;float mark=1.2f;int errors;readonly List<string> notes=new List<string>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-face-render-audit");if(i>=0&&i+1<a.Length)new GameObject("Face render audit").AddComponent<FaceRenderAudit>().output=Path.GetFullPath(a[i+1]);}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());Destroy(t);notes.Add("SHOT "+name);}
  void Error(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;notes.Add("ERROR "+message);}}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Error;Application.runInBackground=true;Application.targetFrameRate=60;yield return new WaitForSecondsRealtime(2);
   var loop=FindFirstObjectByType<LoopDirector>();var flat=loop.flat;var story=loop.story;loop.testing=true;flat.player.automating=true;loop.Pause(false);
   var body=flat.grandmother.GetComponentInChildren<SkinnedMeshRenderer>(true);var original=body.sharedMesh;var mats=body.sharedMaterials;
   story.QaFaceFinale();float deadline=Time.realtimeSinceStartup+30;while(story.QaFinaleBeat!="transform"&&Time.realtimeSinceStartup<deadline)yield return null;
   var args=Environment.GetCommandLineArgs();int mi=Array.IndexOf(args,"-audit-mark");if(mi>=0&&mi+1<args.Length)float.TryParse(args[mi+1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out mark);notes.Add("MARK "+mark);float start=Time.time;while(Time.time-start<mark&&Time.realtimeSinceStartup<deadline)yield return null;
   var fs=story.QaFaceShed;if(!fs||!fs.Shown){notes.Add("FAIL no reconstructed head");Finish(1);yield break;}
   Time.timeScale=0;yield return new WaitForEndOfFrame();var headless=body.sharedMesh;var parts=fs.GetComponentsInChildren<MeshRenderer>(true);var enabled=new bool[parts.Length];var partMats=new Material[parts.Length][];
   var rig=flat.grandmother.GetComponent<GranRig>();int hi=Array.IndexOf(body.bones,rig.head);
   notes.Add("ORIGINAL "+original.name+" triangles="+original.triangles.Length/3+" body="+body.transform.localToWorldMatrix+" head="+rig.head.localToWorldMatrix+" bind="+original.bindposes[hi]);
   notes.Add("BODY_MATERIAL "+mats[0].name+" shader="+mats[0].shader.name+" texture="+(mats[0].mainTexture?mats[0].mainTexture.name:"none")+" color="+mats[0].color);
   for(int i=0;i<parts.Length;i++){enabled[i]=parts[i].enabled;partMats[i]=parts[i].sharedMaterials;notes.Add("PART "+parts[i].name+" active="+parts[i].gameObject.activeInHierarchy+" bounds="+parts[i].bounds+" mat="+parts[i].sharedMaterial.name);}
   yield return Shot("01-rebuilt-lit");
   foreach(var r in parts)r.enabled=false;body.sharedMesh=original;yield return Shot("02-original-lit");
   var shader=Shader.Find("Universal Render Pipeline/Unlit");var created=new List<Material>();
   Material[] Unlit(Material[] source){var result=new Material[source.Length];for(int i=0;i<source.Length;i++){var m=new Material(shader);m.SetTexture("_BaseMap",source[i].mainTexture);m.SetColor("_BaseColor",Color.white);if(source[i].HasProperty("_BaseMap")){m.SetTextureScale("_BaseMap",source[i].GetTextureScale("_BaseMap"));m.SetTextureOffset("_BaseMap",source[i].GetTextureOffset("_BaseMap"));}result[i]=m;created.Add(m);}return result;}
   if(shader){body.sharedMaterials=Unlit(mats);yield return Shot("03-original-unlit");body.sharedMesh=headless;for(int i=0;i<parts.Length;i++){parts[i].sharedMaterials=Unlit(partMats[i]);parts[i].enabled=enabled[i];}yield return Shot("04-rebuilt-unlit");}else{notes.Add("FAIL missing unlit shader");errors++;}
   body.sharedMaterials=mats;for(int i=0;i<parts.Length;i++){parts[i].sharedMaterials=partMats[i];parts[i].enabled=enabled[i];}
   // Lighting probes are diagnostic only, never saved in the scene or retained in a build's gameplay.
   for(int i=0;i<parts.Length;i++){var probe=new Material[partMats[i].Length];for(int j=0;j<probe.Length;j++){probe[j]=new Material(partMats[i][j]);probe[j].SetFloat("_ReceiveShadows",0);probe[j].EnableKeyword("_RECEIVE_SHADOWS_OFF");created.Add(probe[j]);}parts[i].sharedMaterials=probe;}
   yield return Shot("05-rebuilt-no-received-shadows");for(int i=0;i<parts.Length;i++)parts[i].sharedMaterials=partMats[i];
   var cam=flat.player.view?flat.player.view:Camera.main;var fill=new GameObject("DIAGNOSTIC camera-side reflected light").AddComponent<Light>();fill.type=LightType.Point;fill.transform.position=cam.transform.position+cam.transform.forward*.3f;fill.color=new Color(1,.8f,.6f);fill.intensity=.8f;fill.range=3;fill.shadows=LightShadows.None;yield return Shot("06-rebuilt-diagnostic-fill");Destroy(fill.gameObject);
   foreach(var m in created)Destroy(m);
   notes.Add("COMPARE only: screenshots diagnose light vs texture/geometry, not art approval");Finish(errors==0?0:1);
  }
  void Finish(int code){Time.timeScale=1;File.WriteAllLines(Path.Combine(output,"audit.txt"),notes);File.WriteAllText(Path.Combine(output,"result.json"),"{\"errors\":"+errors+",\"completed\":"+(code==0?"true":"false")+"}");Application.Quit(code);}
 }
}
