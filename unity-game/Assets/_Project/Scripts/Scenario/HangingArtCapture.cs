using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Playables;
namespace Ataka.Scenario {
 [DefaultExecutionOrder(10000)]
 public sealed class HangingArtCapture:MonoBehaviour {
  string output;Light diagnosticLight;Camera diagnosticCamera;
  void LateUpdate(){if(diagnosticCamera&&diagnosticLight){diagnosticLight.transform.SetPositionAndRotation(diagnosticCamera.transform.position,diagnosticCamera.transform.rotation);diagnosticLight.intensity=3;}}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-hanging-art-capture");if(i>=0&&i+1<args.Length)new GameObject("QA hanging art cameras").AddComponent<HangingArtCapture>().output=args[i+1];}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),tex.EncodeToPNG());Destroy(tex);}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.runInBackground=true;Application.targetFrameRate=60;yield return new WaitForSecondsRealtime(1);
   var loop=FindFirstObjectByType<LoopDirector>();var flat=loop.flat;var p=flat.player;loop.testing=true;loop.Pause(false);loop.EnterLap(5);loop.enabled=false;p.automating=true;p.control=false;p.hasFlashlight=true;flat.bedroom.open=true;
   var positions=new[]{new Vector3(2.45f,.02f,3.58f),new Vector3(2.30f,.02f,3.10f)};
   for(int i=0;i<positions.Length;i++){
    p.Place(positions[i],0);loop.scenes.PrepareHanging();var nina=flat.grandmother.GetComponent<NinaHangingPose>();p.head.rotation=Quaternion.LookRotation(nina.FeetPosition-p.head.position);loop.scenes.StartCoroutine(loop.scenes.Play("HangingVision"));
    foreach(float t in new[]{1.1f,2.6f,3.6f,4.3f,4.6f,5.1f}){while(loop.scenes.FrameTime<t)yield return null;yield return Shot("entry"+(i+1)+"-"+t.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));}
    while(loop.scenes.PlayingScene!=null)yield return null;
   }
   p.Place(positions[0],0);loop.scenes.PrepareHanging();loop.scenes.StartCoroutine(loop.scenes.Play("HangingVision"));yield return null;loop.scenes.director.playableGraph.GetRootPlayable(0).SetSpeed(0);var pose=flat.grandmother.GetComponent<NinaHangingPose>();pose.Evaluate(1.1f,p.head.position);
   var camera=p.view;camera.GetComponent<CinemachineBrain>().enabled=false;camera.orthographic=true;camera.orthographicSize=1.3f;camera.transform.SetPositionAndRotation(new Vector3(2.75f,1.1f,2.30f),Quaternion.LookRotation(Vector3.left));
   // Diagnostic only: hide occluding furniture/walls; retain actual actor, stool, rope and floor.
   foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None)){bool keep=r.transform.IsChildOf(flat.grandmother.transform)||r.transform.IsChildOf(flat.stool.transform)||r.GetComponentInParent<HangingRope>()||r.name.IndexOf("floor",StringComparison.OrdinalIgnoreCase)>=0;if(!keep)r.enabled=false;}
   loop.subtitle.enabled=false;loop.prompt.enabled=false;diagnosticLight=p.flashlight;diagnosticCamera=camera;yield return new WaitForSecondsRealtime(.2f);yield return Shot("diagnostic-side-orthographic");
   float min=float.MaxValue;foreach(var r in flat.grandmother.GetComponentsInChildren<SkinnedMeshRenderer>()){if(!r.gameObject.activeInHierarchy)continue;var mesh=new Mesh();r.BakeMesh(mesh);foreach(var v in mesh.vertices)min=Mathf.Min(min,r.transform.TransformPoint(v).y);Destroy(mesh);}
   File.WriteAllText(Path.Combine(output,"README.md"),"# Staged QA matrix — not gameplay\n\nTwo fixed entry positions: (2.45,.02,3.58), (2.30,.02,3.10). In-game timeline and lighting; no manual walk. Side orthographic camera at (2.75,1.1,2.30), direction -x, size1.3. Other furniture/walls hidden ONLY for that diagnostic, existing flashlight repositioned to camera.\nLowest visible actor vertex above floor: "+min.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+" m. This is a geometry measurement, not an artistic pass.\n");Application.Quit();
  }
 }
}
