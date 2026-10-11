using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Ataka.Scenario {
 public sealed class GrannyAssetPreview:MonoBehaviour {
  public Transform actor;public Camera view;public ScenarioLook look;public Animation animationPlayer;public SkinnedMeshRenderer body;public AnimationClip[] clips;
  float measuredHeight;int angle,clipIndex,errors,captures;bool fullBody=true,cold;string output;
  [Serializable] class ClipCheck {public string name;public float seconds,maxVertexMovement;}
  [Serializable] class Result {public int errors,captures;public float standingHeight;public ClipCheck[] animations;public string status="Ready Katcho model; appearance approved from author preview; gameplay not tested";}
  IEnumerator Start(){
   Application.runInBackground=true;Application.targetFrameRate=60;Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;Application.logMessageReceived+=OnLog;
   yield return null;look.WarmReturn();PlayIdle();animationPlayer[clips[clipIndex].name].time=0;animationPlayer.Sample();var initial=WorldVertices();measuredHeight=initial.Max(v=>v.y)-initial.Min(v=>v.y);if(Mathf.Abs(measuredHeight-1.55f)>.005f)Debug.LogError("Incorrect measured height: "+measuredHeight);SetView();
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-granny-capture");if(i<0||i+1>=args.Length)yield break;output=args[i+1];Directory.CreateDirectory(output);
   fullBody=false;
   foreach(bool c in new[]{false,true}){cold=c;Lighting();foreach(int a in new[]{0,45,90,180}){angle=a;SetView();yield return new WaitForSecondsRealtime(.8f);yield return Shot((cold?"cold":"warm")+"-"+angle);}}
   cold=false;Lighting();fullBody=true;angle=0;SetView();yield return new WaitForSecondsRealtime(.8f);yield return Shot("warm-full-body");
   var checks=new List<ClipCheck>();
   foreach(var clip in clips){clipIndex=Array.IndexOf(clips,clip);animationPlayer.Play(clip.name);var state=animationPlayer[clip.name];state.speed=0;state.normalizedTime=0;animationPlayer.Sample();var first=BakedVertices();state.normalizedTime=.45f;animationPlayer.Sample();var second=BakedVertices();float movement=0;for(int v=0;v<first.Length;v++)movement=Mathf.Max(movement,Vector3.Distance(first[v],second[v]));checks.Add(new ClipCheck{name=clip.name,seconds=clip.length,maxVertexMovement=movement});if(movement<.0001f){Debug.LogError("Animation has no deformation: "+clip.name);}yield return Shot("animation-"+clip.name.Replace('|','-'));state.speed=1;}
   File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Result{errors=errors,captures=captures,standingHeight=measuredHeight,animations=checks.ToArray()},true));Application.Quit(errors==0?0:1);
  }
  // World-space QA uses bind poses directly instead of depending on BakeMesh scale conventions.
  // The builder measures before resizing; this check runs after the final uniform scale is applied.
  Vector3[] WorldVertices(){var mesh=body.sharedMesh;var points=mesh.vertices;var weights=mesh.boneWeights;var binds=mesh.bindposes;var matrices=body.bones.Select((b,i)=>b.localToWorldMatrix*binds[i]).ToArray();for(int i=0;i<points.Length;i++){var p=points[i];var w=weights[i];points[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;}return points;}
  Vector3[] BakedVertices(){var mesh=new Mesh();body.BakeMesh(mesh);var points=mesh.vertices;Destroy(mesh);return points;}
  void PlayIdle(){clipIndex=Array.FindIndex(clips,c=>c.name.EndsWith("Oidle"));animationPlayer.Play(clips[clipIndex].name);}
  void Update(){var k=Keyboard.current;if(k==null)return;if(k.escapeKey.wasPressedThisFrame)Application.Quit();if(k.digit1Key.wasPressedThisFrame){angle=0;fullBody=false;SetView();}if(k.digit2Key.wasPressedThisFrame){angle=45;fullBody=false;SetView();}if(k.digit3Key.wasPressedThisFrame){angle=90;fullBody=false;SetView();}if(k.digit4Key.wasPressedThisFrame){fullBody=!fullBody;SetView();}if(k.digit5Key.wasPressedThisFrame){angle=180;fullBody=false;SetView();}if(k.lKey.wasPressedThisFrame){cold=!cold;Lighting();}if(k.spaceKey.wasPressedThisFrame){clipIndex=(clipIndex+1)%clips.Length;animationPlayer.CrossFade(clips[clipIndex].name,.15f);}}
  void Lighting(){look.WarmReturn();if(cold)foreach(var l in look.warm)l.intensity=0;}
  void SetView(){actor.rotation=Quaternion.Euler(0,180+angle,0);var target=actor.position+Vector3.up*(fullBody?.74f:1.35f);view.transform.position=actor.position+new Vector3(0,fullBody?1.05f:1.38f,fullBody?-2.65f:-1.1f);view.transform.LookAt(target);view.fieldOfView=fullBody?47:34;}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());Destroy(t);captures++;}
  void OnLog(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)errors++;}
  void OnDestroy(){Application.logMessageReceived-=OnLog;}
  void OnGUI(){GUI.Label(new Rect(18,16,1000,30),"НИНА / ГОТОВАЯ МОДЕЛЬ KATCHO / ПРОВЕРКА В НАШЕМ СВЕТЕ");GUI.Label(new Rect(18,Screen.height-54,1400,26),"1 — лицо   2 — три четверти   3 — профиль   4 — рост   5 — спина   L — свет   Пробел — анимация   Esc — выйти");GUI.Label(new Rect(18,Screen.height-28,1200,26),"Авторская внешность и текстура без переделок. Анимация: "+(clips.Length>0?clips[clipIndex].name:"—"));}
 }
}
