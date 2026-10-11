using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Ataka.Scenario {
 public sealed class NinaArtPreview:MonoBehaviour {
  public Transform actor;public Camera view;public ScenarioLook look;
  int angle;bool fullBody,cold;string output;int errors;
  IEnumerator Start(){
   Application.runInBackground=true;Application.targetFrameRate=60;Time.timeScale=1;AudioListener.pause=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   Application.logMessageReceived+=OnLog;
   yield return null;look.WarmReturn();SetView();
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-nina-capture");if(i<0||i+1>=args.Length)yield break;output=args[i+1];Directory.CreateDirectory(output);
   foreach(bool isCold in new[]{false,true}){cold=isCold;Lighting();foreach(int a in new[]{0,45,90,180}){angle=a;SetView();yield return new WaitForSecondsRealtime(1);yield return Shot((cold?"cold":"warm")+"-"+angle);}}
   cold=false;Lighting();fullBody=true;angle=0;SetView();yield return new WaitForSecondsRealtime(1);yield return Shot("warm-full-body");
   File.WriteAllText(Path.Combine(output,"result.json"),"{\"errors\":"+errors+",\"captures\":9,\"height\":1.55,\"status\":\"unapproved appearance preview\"}");Application.Quit(errors==0?0:1);
  }
  void Update(){var k=Keyboard.current;if(k==null)return;if(k.escapeKey.wasPressedThisFrame)Application.Quit();if(k.digit1Key.wasPressedThisFrame){angle=0;fullBody=false;SetView();}if(k.digit2Key.wasPressedThisFrame){angle=45;fullBody=false;SetView();}if(k.digit3Key.wasPressedThisFrame){angle=90;fullBody=false;SetView();}if(k.digit4Key.wasPressedThisFrame){fullBody=!fullBody;SetView();}if(k.digit5Key.wasPressedThisFrame){angle=180;fullBody=false;SetView();}if(k.lKey.wasPressedThisFrame){cold=!cold;Lighting();}}
  void Lighting(){look.WarmReturn();if(cold)foreach(var l in look.warm)l.intensity=0;}
  void SetView(){actor.rotation=Quaternion.Euler(0,180+angle,0);var target=actor.position+Vector3.up*(fullBody?.79f:1.36f);view.transform.position=actor.position+new Vector3(0,fullBody?1.05f:1.39f,fullBody?-2.1f:-1.1f);view.transform.LookAt(target);view.fieldOfView=fullBody?47:34;}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());Destroy(t);}
  void OnLog(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)errors++;}
  void OnDestroy(){Application.logMessageReceived-=OnLog;}
  void OnGUI(){GUI.color=Color.white;GUI.Label(new Rect(18,16,900,30),"НИНА ПЕТРОВНА — ПРИМЕРКА ВНЕШНОСТИ / НЕ ФИНАЛЬНЫЙ ПЕРСОНАЖ");GUI.Label(new Rect(18,Screen.height-52,1000,26),"1 — лицо   2 — три четверти   3 — профиль   4 — рост   5 — спина   L — тёплый / холодный свет   Esc — выйти");GUI.Label(new Rect(18,Screen.height-28,1100,25),"Готовая ткань на голове ещё без узла; застёжка халата и цепочка не готовы. Примерка PS1-образа.");}
 }
}
