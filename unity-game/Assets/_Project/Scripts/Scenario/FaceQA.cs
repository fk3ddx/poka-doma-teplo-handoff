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
  [Serializable] class Result {public bool passed;public int failures,errors,skinTris,scalpTris,guestTris,jawTris;public float faceBase,skinFacing,guestFacing,faceDark,seamError,ownFaceLuma,faceLuma,mouthLuma,keyOnLuma,keyOffLuma,eyeDrop,seamGap,slide,jawAngle,grownTop,faceToHerFace;public bool standInInstalled,leftovers,mirrored,guestInside,skinBelowChin;}
  readonly Result result=new Result();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-face-qa");if(i<0||i+1>=args.Length)return;
   new GameObject("Face QA").AddComponent<FaceQA>().output=Path.GetFullPath(args[i+1]);}
  void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+message.Split('\n')[0]);}}
  void Check(bool ok,string what){checks.Add((ok?"PASS ":"FAIL ")+what);if(!ok)failures++;File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);}
  // pc28d: the PNG is written synchronously and kept, so the checks below look at the very pixels Codex looks at
  Texture2D shot;
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();if(!Application.isBatchMode){if(shot)Destroy(shot);shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),shot.EncodeToPNG());}checks.Add("SHOT "+name+".png");}
  // share of near-black pixels (luma < 0.05) inside a screen rectangle of the last shot; -1 when nothing to look at
  // pc28e: the brightness of the very pixels her face covers in the PNG (face-skin triangle centres facing him)
  (float mean,float dark,int n) FaceLuma(List<Vector3> pts){if(!shot||pts.Count==0)return(-1,-1,0);float sum=0;int d=0,n=0;foreach(var s in pts){int x=(int)s.x,y=(int)s.y;if(x<0||y<0||x>=shot.width||y>=shot.height)continue;var c=shot.GetPixel(x,y);float l=.2126f*c.r+.7152f*c.g+.0722f*c.b;sum+=l;n++;if(l<.05f)d++;}return n==0?(-1,-1,0):(sum/n,d/(float)n,n);}
  float WindowLuma(Vector3 s,int r){if(!shot)return -1;float sum=0;int n=0;for(int y=(int)s.y-r;y<=(int)s.y+r;y++)for(int x=(int)s.x-r;x<=(int)s.x+r;x++){if(x<0||y<0||x>=shot.width||y>=shot.height)continue;var c=shot.GetPixel(x,y);sum+=.2126f*c.r+.7152f*c.g+.0722f*c.b;n++;}return n==0?-1:sum/n;}
  float DarkShare(Bounds r){if(!shot)return -1;int x0=Mathf.Clamp((int)r.min.x,0,shot.width-1),x1=Mathf.Clamp((int)r.max.x,0,shot.width-1),y0=Mathf.Clamp((int)r.min.y,0,shot.height-1),y1=Mathf.Clamp((int)r.max.y,0,shot.height-1);if(x1-x0<4||y1-y0<4)return -1;
   int n=0,dark=0;for(int y=y0;y<=y1;y+=2)for(int x=x0;x<=x1;x+=2){var c=shot.GetPixel(x,y);n++;if(.2126f*c.r+.7152f*c.g+.0722f*c.b<.05f)dark++;}return dark/(float)n;}
  static bool InFrame(ScenarioPlayer p,Vector3 point,out Vector3 v){var cam=p.view?p.view:Camera.main;v=cam?cam.WorldToViewportPoint(point):Vector3.zero;return cam&&v.z>0&&v.x>.05f&&v.x<.95f&&v.y>.05f&&v.y<.95f;}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;Application.targetFrameRate=60;
   yield return new WaitForSecondsRealtime(2);
   var loop=FindFirstObjectByType<LoopDirector>();var story=loop.story;var flat=loop.flat;var p=flat.player;var scenes=loop.scenes;var tear=scenes.faceTear;
   loop.testing=true;p.automating=true;loop.Pause(false);
   result.standInInstalled=tear&&tear.standInFace&&tear.standInFaceMaterial;Check(result.standInInstalled,"stand-in face installed on FaceTearDeath (Ataka.Editor.FinaleFacesInstaller.Install)");
   checks.Add("ABSENT Misha's own (15-year-old) face — the 11.10 face is an ADULT Quaternius face, a labelled stand-in");
   checks.Add("ABSENT voices — her whisper and his cry are sounds/subtitles only");
   var rig=flat.grandmother.GetComponent<GranRig>();Vector3 headScale=rig&&rig.head?rig.head.localScale:Vector3.one;
   story.QaFaceFinale();float until=Time.realtimeSinceStartup+60;while(story.QaFinaleBeat!="transform"&&Time.realtimeSinceStartup<until)yield return null;
   float t0=Time.time;rig=flat.grandmother.GetComponent<GranRig>();if(rig&&rig.head)headScale=rig.head.localScale;yield return null;var fs=story.QaFaceShed;
   Check(fs&&fs.Ready,"11.9 her head rebuilt from her own triangles (face skin "+(fs?fs.SkinTris:0)+", scalp "+(fs?fs.ScalpTris:0)+") and the Guest's head "+(fs?fs.GuestTris:0)+" + jaw "+(fs?fs.JawTris:0)+" triangles");
   if(fs){result.skinTris=fs.SkinTris;result.scalpTris=fs.ScalpTris;result.guestTris=fs.GuestTris;result.jawTris=fs.JawTris;Check(fs.GuestTris>0&&fs.JawTris>0,"the Guest's head and jaw were found (Head / Jaw weights)");}
   // pc28c: the staging must be hers standing (story28-face-c started from the hanging pose: 0.90 m → grown 3.98 m)
   yield return null;result.faceBase=story.QaFaceBase;Check(result.faceBase>1.45f&&result.faceBase<1.65f,"staging: she stands at the stove (head top "+result.faceBase.ToString("F3")+" m over her feet, 1.45–1.65)");
   var cam=p.view?p.view:Camera.main;
   float[] marks={.5f,.9f,.98f,1.2f,1.35f,1.45f,1.7f,2f,2.3f,2.7f,3.2f};int m=0;float ownFace=-1;
   while(story.QaFinaleBeat=="transform"&&m<marks.Length){if(Time.time-t0<marks[m]){yield return null;continue;}
    var mark=marks[m];yield return Shot("face-"+mark.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture));fs=story.QaFaceShed;
    if(fs){checks.Add("FACE t="+mark+" shown "+fs.Shown+" eye drop "+fs.EyeDrop.ToString("F3")+" seam "+fs.SeamGap.ToString("F3")+" slide "+fs.Slide.ToString("F2")+" jaw "+fs.JawAngle.ToString("F0")+"° her head bone scale "+(rig?rig.head.localScale.x.ToString("F4"):"?"));
     if(Mathf.Approximately(mark,1.35f)){result.eyeDrop=fs.EyeDrop;Check(fs.Shown&&fs.EyeDrop>=.03f&&fs.HeadlessBody,"1.0–1.3: one eye of her face slides down "+(fs.EyeDrop*100).ToString("F1")+" cm (her painted face cut from her body and drawn by its own skinned copy; head bone not collapsed)");}
     // pc28e: at 0.9 her OWN face (not cut yet) under the 11.9 key; at 1.2/1.45 the cut face skin must look as lit as she did
     if(Mathf.Approximately(mark,.9f)){var own=FaceLuma(fs.FaceSamples(cam));ownFace=own.mean;result.ownFaceLuma=own.mean;checks.Add("NOTE 0.9: her own face before the cut — mean luma "+own.mean.ToString("F3")+" over "+own.n+" face points, "+(own.dark*100).ToString("F0")+" % near-black; key "+(fs.QaKey?fs.QaKey.intensity.ToString("F2"):"none"));
      Check(own.n>0&&own.mean>=.1f&&own.dark<=.15f,"0.9: her face is lit in the PNG (mean luma "+own.mean.ToString("F3")+" ≥ 0.10, near-black "+(own.dark*100).ToString("F0")+" % ≤ 15 %) — not a black mask");}
     if(Mathf.Approximately(mark,1.2f)||Mathf.Approximately(mark,1.45f)){var skinL=FaceLuma(fs.FaceSamples(cam));result.faceLuma=Mathf.Max(result.faceLuma,skinL.mean);result.faceDark=Mathf.Max(result.faceDark,skinL.dark);
      Check(skinL.n>0&&skinL.dark<=.15f&&skinL.mean>=Mathf.Max(.1f,.75f*ownFace),mark.ToString("F2")+": the face skin in the PNG is lit like her own face at 0.9 (mean luma "+skinL.mean.ToString("F3")+" vs "+ownFace.ToString("F3")+", near-black "+(skinL.dark*100).ToString("F0")+" % ≤ 15 %)"+(Mathf.Approximately(mark,1.2f)?"; Guest still hidden: "+!fs.GuestShown:""));}
     // pc28e: from 2.3 the mouth must read (not a black hole): the pixels round the dropped jaw
     if(mark>=2.3f&&fs.QaJaw){var js=cam.WorldToScreenPoint(fs.QaJaw.position);float jl=WindowLuma(js,6);result.mouthLuma=Mathf.Max(result.mouthLuma,jl);Check(js.z>0&&jl>=.06f,mark.ToString("F2")+": the Guest's mouth reads in the PNG (luma "+jl.ToString("F3")+" ≥ 0.06 round the jaw)");}
     if(Mathf.Approximately(mark,1.2f)){result.mirrored=fs.Mirrored;result.guestInside=fs.GuestInside;result.skinFacing=fs.FaceSamples(cam).Count/(float)Mathf.Max(1,fs.SkinTris);
      checks.Add("NOTE her head bone matrix mirrored: "+fs.Mirrored+"; Guest head scale in hers "+fs.GuestScale.ToString("F3")+" (bind space)");
      Check(result.skinFacing>=.55f,"1.2: her rebuilt face skin faces him ("+(result.skinFacing*100).ToString("F0")+" % of its triangles front-on) — not its back");
      Check(fs.GuestInside,"before the slide the Guest's head sits inside her head (no cap over the bun, nothing through her face)");}
     // pc28g: proof that the 11.9 key reaches her face and nothing else — the same frozen frame with the key on and off
     if(Mathf.Approximately(mark,1.2f)&&fs.QaKey){Time.timeScale=0;yield return Shot("face-1.20-key-on");var pts=fs.FaceSamples(cam);var on=FaceLuma(pts);fs.QaKey.enabled=false;yield return Shot("face-1.20-key-off");var off=FaceLuma(pts);fs.QaKey.enabled=true;Time.timeScale=1;
      result.keyOnLuma=on.mean;result.keyOffLuma=off.mean;checks.Add("NOTE "+fs.KeyReport(fs.KeyTarget));
      Check(on.mean-off.mean>=.04f,"1.2: the 11.9 key reaches her face (face luma key off "+off.mean.ToString("F3")+" → on "+on.mean.ToString("F3")+")");
      bool fridgeLit=false;string fridges="";foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None)){bool f=false;for(var t=r.transform;t&&!f;t=t.parent)f=t.name.ToLowerInvariant().Contains("fridge");if(f&&r.enabled&&r.gameObject.activeInHierarchy){fridges+=r.name+" ";fridgeLit|=fs.KeyCovers(r.bounds);}}
      Check(fridges.Length>0&&!fridgeLit,"1.2: the key's cone and range do not reach the fridge ("+(fridges.Length>0?fridges.Trim():"no fridge renderer found")+")");}
     if(Mathf.Approximately(mark,1.45f)){result.seamGap=fs.SeamGap;Check(fs.SeamGap>=.012f,"1.3: the face skin splits at the temple ("+(fs.SeamGap*100).ToString("F1")+" cm)");}
     if(Mathf.Approximately(mark,2.3f)){result.slide=fs.Slide;result.jawAngle=fs.JawAngle;Vector3 v=Vector3.zero;bool seen=fs.QaGuestHead&&InFrame(p,fs.QaGuestHead.position,out v);
      Check(fs.Slide>.95f&&fs.JawAngle>=30,"1.5–2.25: the face skin has slid off the chin ("+fs.Slide.ToString("F2")+"), the Guest's jaw dropped "+fs.JawAngle.ToString("F0")+"°");Check(seen,"the grey head under her face is in his frame "+v.ToString("F2"));}
     if(Mathf.Approximately(mark,2.3f)){result.skinBelowChin=fs.SkinCentre.y<fs.HerChin.y;result.guestFacing=1;
      Check(result.skinBelowChin,"2.3: her face skin hangs below her chin ("+fs.SkinCentre.y.ToString("F2")+" < "+fs.HerChin.y.ToString("F2")+")");
      Vector3 jv=Vector3.zero;Check(fs.QaJaw&&InFrame(p,fs.QaJaw.position,out jv),"2.3: the dropped jaw / mouth is in his frame "+jv.ToString("F2"));}
     if(Mathf.Approximately(mark,2.7f))Check(fs.Fallen,"2.6: the face skin has dropped off her (a loose baked piece falls; the lining of her head stays)");}
    m++;}
   while(story.QaFinaleBeat!="tear"&&Time.realtimeSinceStartup<until)yield return null;result.grownTop=story.QaGrownTop;Check(result.grownTop>=2.25f&&result.grownTop<=2.52f,"11.9 her rendered head top "+result.grownTop.ToString("F3")+" m (2.25–2.52 under the 2.5 m ceiling; exact vertices)");
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
   var her=flat.grandmother.GetComponentInChildren<SkinnedMeshRenderer>(true);bool whole=her&&her.sharedMesh&&!her.sharedMesh.name.Contains("(no head");
   Check(!result.leftovers&&whole&&(!rig||(rig.head.localScale-headScale).sqrMagnitude<1e-8f),"nothing left behind: no face skin / Guest head pieces, her own mesh with its head is back on her ("+(her&&her.sharedMesh?her.sharedMesh.name:"?")+")");
   Check(errors==0,"no runtime errors ("+errors+")");
   result.errors=errors;result.failures=failures;result.passed=failures==0&&errors==0;File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));
   File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);yield return new WaitForSecondsRealtime(.5f);Application.Quit(result.passed?0:1);
  }
 }
}
