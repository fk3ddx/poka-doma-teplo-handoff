using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Ataka.Scenario {
 // QA only (pc28):  -face-qa <dir>
 // 11.9 and 11.10 alone (her at the stove, her back to him): the transformation second by second — her own face skin
 // (rebuilt from her Katcho triangles), one eye sliding, the temple seam, the skin sliding off the chin, the Guest's
 // grey head and its jaw under it, the skin falling — then DeathFinale's 3.2 beat with the STAND-IN face in her claw
 // and on her face. PNGs at fixed times + checks.txt (PASS/FAIL/ABSENT) + result.json; exit 0 = no FAIL.
 public sealed class FaceQA:MonoBehaviour {
  string output;readonly List<string> checks=new List<string>();int errors,failures;
  [Serializable] class Result {public bool passed;public int failures,errors,skinTris,scalpTris,guestTris,jawTris;public float eyeDrop,seamGap,slide,jawAngle,grownTop,faceToHerFace;public bool standInInstalled,leftovers;}
  readonly Result result=new Result();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-face-qa");if(i<0||i+1>=args.Length)return;
   new GameObject("Face QA").AddComponent<FaceQA>().output=Path.GetFullPath(args[i+1]);}
  void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+message.Split('\n')[0]);}}
  void Check(bool ok,string what){checks.Add((ok?"PASS ":"FAIL ")+what);if(!ok)failures++;File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));checks.Add("SHOT "+name+".png");}
  static bool InFrame(ScenarioPlayer p,Vector3 point,out Vector3 v){var cam=p.view?p.view:Camera.main;v=cam?cam.WorldToViewportPoint(point):Vector3.zero;return cam&&v.z>0&&v.x>.05f&&v.x<.95f&&v.y>.05f&&v.y<.95f;}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;Application.targetFrameRate=60;
   yield return new WaitForSecondsRealtime(2);
   var loop=FindFirstObjectByType<LoopDirector>();var story=loop.story;var flat=loop.flat;var p=flat.player;var scenes=loop.scenes;var tear=scenes.faceTear;
   loop.testing=true;p.automating=true;loop.Pause(false);
   result.standInInstalled=tear&&tear.standInFace&&tear.standInFaceMaterial;Check(result.standInInstalled,"stand-in face installed on FaceTearDeath (Ataka.Editor.FinaleFacesInstaller.Install)");
   checks.Add("ABSENT Misha's own (12-year-old) face — the 11.10 face is an ADULT Quaternius face, a labelled stand-in");
   checks.Add("ABSENT voices — her whisper and his cry are sounds/subtitles only");
   var rig=flat.grandmother.GetComponent<GranRig>();Vector3 headScale=rig&&rig.head?rig.head.localScale:Vector3.one;
   story.QaFaceFinale();float until=Time.realtimeSinceStartup+60;while(story.QaFinaleBeat!="transform"&&Time.realtimeSinceStartup<until)yield return null;
   float t0=Time.time;rig=flat.grandmother.GetComponent<GranRig>();if(rig&&rig.head)headScale=rig.head.localScale;yield return null;var fs=story.QaFaceShed;
   Check(fs&&fs.Ready,"11.9 her head rebuilt from her own triangles (face skin "+(fs?fs.SkinTris:0)+", scalp "+(fs?fs.ScalpTris:0)+") and the Guest's head "+(fs?fs.GuestTris:0)+" + jaw "+(fs?fs.JawTris:0)+" triangles");
   if(fs){result.skinTris=fs.SkinTris;result.scalpTris=fs.ScalpTris;result.guestTris=fs.GuestTris;result.jawTris=fs.JawTris;Check(fs.GuestTris>0&&fs.JawTris>0,"the Guest's head and jaw were found (Head / Jaw weights)");}
   float[] marks={.5f,.98f,1.2f,1.45f,1.7f,2f,2.3f,2.7f,3.2f};int m=0;
   while(story.QaFinaleBeat=="transform"&&m<marks.Length){if(Time.time-t0<marks[m]){yield return null;continue;}
    var mark=marks[m];yield return Shot("face-"+mark.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture));fs=story.QaFaceShed;
    if(fs){checks.Add("FACE t="+mark+" shown "+fs.Shown+" eye drop "+fs.EyeDrop.ToString("F3")+" seam "+fs.SeamGap.ToString("F3")+" slide "+fs.Slide.ToString("F2")+" jaw "+fs.JawAngle.ToString("F0")+"° her head bone scale "+(rig?rig.head.localScale.x.ToString("F4"):"?"));
     if(Mathf.Approximately(mark,1.2f)){result.eyeDrop=fs.EyeDrop;Check(fs.Shown&&fs.EyeDrop>=.03f&&rig.head.localScale.x<headScale.x*.01f,"1.0–1.3: one eye of her face slides down "+(fs.EyeDrop*100).ToString("F1")+" cm (her own skinned head collapsed, the rebuilt one shown)");}
     if(Mathf.Approximately(mark,1.45f)){result.seamGap=fs.SeamGap;Check(fs.SeamGap>=.012f,"1.3: the face skin splits at the temple ("+(fs.SeamGap*100).ToString("F1")+" cm)");}
     if(Mathf.Approximately(mark,2.3f)){result.slide=fs.Slide;result.jawAngle=fs.JawAngle;Vector3 v=Vector3.zero;bool seen=fs.QaGuestHead&&InFrame(p,fs.QaGuestHead.position,out v);
      Check(fs.Slide>.95f&&fs.JawAngle>=30,"1.5–2.25: the face skin has slid off the chin ("+fs.Slide.ToString("F2")+"), the Guest's jaw dropped "+fs.JawAngle.ToString("F0")+"°");Check(seen,"the grey head under her face is in his frame "+v.ToString("F2"));}
     if(Mathf.Approximately(mark,2.7f)&&fs.QaSkin)Check(fs.QaSkin.parent==fs.transform,"2.6: the face skin has dropped off her");}
    m++;}
   while(story.QaFinaleBeat!="tear"&&Time.realtimeSinceStartup<until)yield return null;result.grownTop=story.QaGrownTop;checks.Add("TOP rendered head top after 11.9: "+result.grownTop.ToString("F2")+" m (FinaleQA judges 2.25–2.52)");
   // 11.10: the 3.2 beat — the face in her claw, then on her face
   float[] tm={1f,2.1f,2.4f,2.8f};int k=0;bool inHand=false,onFace=false;
   while(story.QaFinaleBeat=="tear"&&Time.realtimeSinceStartup<until){
    if(scenes.PlayingScene=="DeathFinale"&&k<tm.Length&&scenes.FrameTime>=tm[k]){float ft=scenes.FrameTime;yield return Shot("tear-"+tm[k].ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));var fp=tear?tear.QaFaceProp:null;
     if(fp&&fp.gameObject.activeInHierarchy){bool vis=InFrame(p,fp.position,out var v);if(ft<2.3f)inHand|=vis;else{var local=p.head.InverseTransformPoint(fp.position);result.faceToHerFace=(local-new Vector3(0,.07f,.56f)).magnitude;onFace|=vis&&result.faceToHerFace<.06f;}checks.Add("TEAR "+ft.ToString("F2")+" stand-in face at "+fp.localPosition.ToString("F2")+" in frame "+vis+" "+v.ToString("F2"));}
     else checks.Add("TEAR "+ft.ToString("F2")+" no face prop");k++;continue;}
    yield return null;}
   Check(inHand,"11.10 3.2 beat: the stand-in face hangs in her claw, in his frame");Check(onFace,"11.10: then it is pulled over her face (≤ 6 cm from her face point, "+result.faceToHerFace.ToString("F3")+" m)");
   while(story.QaFinaleBeat!="done"&&Time.realtimeSinceStartup<until)yield return null;yield return null;
   result.leftovers=GameObject.Find("Finale face shed (11.9)")||(tear&&tear.QaFaceProp);
   Check(!result.leftovers&&(!rig||(rig.head.localScale-headScale).sqrMagnitude<1e-8f),"nothing left behind: no face skin / Guest head pieces, her skinned head restored");
   Check(errors==0,"no runtime errors ("+errors+")");
   result.errors=errors;result.failures=failures;result.passed=failures==0&&errors==0;File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));
   File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);yield return new WaitForSecondsRealtime(.5f);Application.Quit(result.passed?0:1);
  }
 }
}
