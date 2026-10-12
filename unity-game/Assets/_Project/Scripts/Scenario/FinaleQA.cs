using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Ataka.Scenario {
 // QA only:  -finale-qa <dir> -finale-branch open|key   (one entry branch per process; run both)
 // SCENARIO 11 from 22:00 through the entry branch (11.6а opened → hug; 11.6б own key → chain gap, hand, stretching
 // fingers, chain off), 11.7 at the stove, 11.8, the 3.5 s transformation, 11.10 (DeathFinale 3.5 s) into the epilogue.
 // The wait to 22:10 and the epilogue cards run at 6x, everything else at 1x. A watching player is emulated: the
 // camera is placed in the hall / kitchen and aimed at her (no key holds). PNGs per beat and at fixed times,
 // checks.txt (PASS/FAIL/ABSENT), beats.txt, result.json; exit 0 = no FAIL. ABSENT lines are parts with no mesh / rig
 // part / recording — they are reported, not passed.
 public sealed class FinaleQA:MonoBehaviour {
  string output,branch="open";readonly List<string> checks=new List<string>();int errors,failures;
  [Serializable] class Result {public string branch;public bool passed,epilogue,refusedCard;public int failures,errors,beats,cracksDuringFingers;public float transformSeconds,tearSeconds,grownTop,handScaleMax,chainGapDegrees,hugShoulderDistance;public string[] beatOrder,reachErrors;}
  readonly Result result=new Result();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-finale-qa");if(i<0||i+1>=args.Length)return;
   var qa=new GameObject("Finale QA").AddComponent<FinaleQA>();qa.output=Path.GetFullPath(args[i+1]);int b=Array.IndexOf(args,"-finale-branch");if(b>=0&&b+1<args.Length)qa.branch=args[b+1]=="key"?"key":"open";}
  void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+message.Split('\n')[0]);}}
  void Check(bool ok,string what){checks.Add((ok?"PASS ":"FAIL ")+what);if(!ok)failures++;File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot(Path.Combine(output,"finale-"+branch+"-"+name+".png"));checks.Add("SHOT finale-"+branch+"-"+name+".png");}
  static void Aim(ScenarioPlayer p,Vector3 at){var d=at-p.head.position;var flat=d;flat.y=0;if(flat.sqrMagnitude<1e-4f)return;p.transform.rotation=Quaternion.LookRotation(flat);p.pitch=Mathf.Clamp(-Mathf.Atan2(d.y,flat.magnitude)*Mathf.Rad2Deg,-80,80);p.head.localRotation=Quaternion.Euler(p.pitch,0,0);}
  bool InFrame(ScenarioPlayer p,Vector3 point,out Vector3 v){var cam=p.view?p.view:Camera.main;v=cam?cam.WorldToViewportPoint(point):Vector3.zero;return cam&&v.z>0&&v.x>=.1f&&v.x<=.9f&&v.y>=.1f&&v.y<=.9f;}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;Application.targetFrameRate=60;result.branch=branch;
   yield return new WaitForSecondsRealtime(2);
   var loop=FindFirstObjectByType<LoopDirector>();var story=loop.story;var flat=loop.flat;var p=flat.player;var scenes=loop.scenes;var sound=loop.sound;
   loop.testing=true;p.automating=true;loop.Pause(false);story.QaBegin("Finale");yield return null;yield return null;
   var rig=story.QaRig;var chain=story.QaChain;
   Check(rig&&rig.Ready,"Katcho rig layer ready (Back, Chest, Neck, Head, Arm_Upper/Lower, Hand L/R found)");
   Check(chain&&chain.Ready,"door chain made from the ready Poly Haven ball chain (pull_chain_light_socket_chain)");
   foreach(var a in new[]{"her eye sliding down (11.9): the Katcho head has no eye geometry or bones","the seam bursting at the temple (11.9): no mesh, only the wet sound","the face sliding off the chin like wet cloth (11.9): no face mesh; the skin greys (tint) instead","the jaw dropping (11.9): the rig has no jaw bone","Misha's face pulled over hers (11.10, frame 3.2): no mesh of his face; the claw hand rises into her face empty","the ring on her hand (11.6б): none on the Katcho mesh","separate stretching fingers (11.6б, 11.9): the hand is one bone; the whole hand is lengthened","sounds: her humming, the coat rustle, the chain jingle (the latch clink stands in): no recordings","voices: all lines are subtitles"})checks.Add("ABSENT "+a);
   int crackCount=0;Action<string> cue=id=>{if(id=="finger_crack")crackCount++;};sound.CueStarted+=cue;
   var hall=new Vector3(7.8f,.02f,3.55f);var gap=new Vector3(7.62f,1.43f,5.94f);var kitchen=new Vector3(7.45f,.02f,2.75f);
   string last="";int index=0;float until=Time.realtimeSinceStartup+420;float transformAt=-1,tearAt=-1,fingersAt=-1;bool inKitchen=false,fingerShot=false,sawFinaleScene=false,sawDeathFull=false;int tm=0,dm=0,hm=0;
   float[] transformMarks={.5f,1.2f,2f,2.8f,3.4f},tearMarks={.3f,1f,1.6f,2.3f,2.7f,3.2f},hugMarks={.5f,1.5f,2.5f};float hugAt=-1;
   while(story.chapter!=StoryDirector.Chapter.Ended&&Time.realtimeSinceStartup<until){
    string beat=story.QaFinaleBeat;
    Time.timeScale=beat=="22:00"||(branch=="key"&&beat=="choice")?6:1;
    if(beat!=last){last=beat;index++;
     // the watching player
     if(beat=="knock"){p.Place(branch=="open"?new Vector3(7.85f,.02f,4.75f):hall,0);Aim(p,branch=="open"?flat.entrance.transform.position+Vector3.up*.5f:gap);}
     if(beat=="choice"&&branch=="open"){yield return new WaitForSeconds(.5f);story.QaFinaleOpen();}
     if(beat=="door-locked"){p.Place(new Vector3(7.0f,.02f,5.3f),90);Aim(p,flat.grandmother.transform.position+Vector3.up*1.1f);} // off her way to the kitchen
     if(beat=="stove"&&!inKitchen){inKitchen=true;p.Place(kitchen,90);}
     if(beat=="hug")hugAt=Time.time;if(beat=="fingers")fingersAt=Time.time;if(beat=="transform")transformAt=Time.time;
     yield return Shot(index.ToString("00")+"-"+beat);
     // beat checks
     var g=flat.grandmother.transform;
     if(beat=="grope"||beat=="fingers"){float ang=Mathf.Abs(Mathf.DeltaAngle(0,flat.entrance.leaf.localEulerAngles.y));result.chainGapDegrees=ang;Check(Mathf.Abs(ang-StoryDirector.ChainGap)<1&&chain.Is==DoorChain.State.Engaged,beat+": the door stands on the chain at "+ang.ToString("F1")+"° (chain "+chain.Is+")");}
     if(beat=="chain-off")Check(rig.handScale>=2.1f,"chain-off: the hand stretched to "+rig.handScale.ToString("F2")+"× before lifting the chain");
     if(beat=="hand-normal")Check(chain.Is==DoorChain.State.Hanging&&Mathf.Abs(rig.handScale-1)<.01f,"the chain hangs from its slot and the hand is normal again ("+chain.Is+", "+rig.handScale.ToString("F2")+"×)");
     if(beat=="door-locked")Check(flat.entrance.locked&&!flat.entrance.open,"the entrance locks itself behind her");
     if(beat=="strike"){var box=story.QaProp("Matchbox");Check(box&&Vector3.Distance(box.position,rig.Tip(true))<.2f,"strike: the matchbox is in her hand ("+(box?Vector3.Distance(box.position,rig.Tip(true)).ToString("F2"):"none")+" m from the fingertip)");}
     if(beat=="gas")Check(Vector3.Distance(rig.Tip(true),new Vector3(8.48f,.95f,1.65f))<.3f,"gas: her hand at the burner ("+Vector3.Distance(rig.Tip(true),new Vector3(8.48f,.95f,1.65f)).ToString("F2")+" m)");
     if(beat=="water"){var k=story.QaProp("Kettle");Check(k&&Vector3.Distance(rig.Tip(false),StoryDirector.TapAt)<.12f&&Vector3.Distance(k.position,StoryDirector.TapAt)<.35f,"water: the kettle under the tap in her hand (kettle "+(k?Vector3.Distance(k.position,StoryDirector.TapAt).ToString("F2"):"none")+" m)");}
     if(beat=="kettle-on"){var k=story.QaProp("Kettle");Check(k&&Vector3.Distance(k.position,StoryDirector.KettleFront)<.02f,"the kettle stands on the front burner");}
     if(beat=="question"){var toP=p.transform.position-g.position;toP.y=0;Check(Vector3.Dot(g.forward,toP.normalized)<0,"11.8: she speaks with her back to him (dot "+Vector3.Dot(g.forward,toP.normalized).ToString("F2")+")");}
     if(beat=="tear"){tearAt=Time.time;if(transformAt>0){result.transformSeconds=tearAt-transformAt;Check(Mathf.Abs(result.transformSeconds-3.5f)<.15f,"11.9 transformation "+result.transformSeconds.ToString("F2")+" s (3.5)");}
      result.grownTop=story.QaGrownTop;Check(result.grownTop>=2.25f&&result.grownTop<=2.52f,"11.9 she grew to the ceiling: head top "+result.grownTop.ToString("F2")+" m (2.4 target, ceiling 2.5)");}
     if(beat=="epilogue"&&tearAt>0)result.tearSeconds=Time.time-tearAt;
     continue;}
    // per-frame: the watcher looks at her in the kitchen; timed shots inside the hug / transformation / tear
    var gr=flat.grandmother.transform;if(scenes.PlayingScene=="DeathFinale")sawFinaleScene=true;if(scenes.PlayingScene=="DeathFull")sawDeathFull=true;
    if(inKitchen&&p.control&&flat.grandmother.activeSelf)Aim(p,gr.position+Vector3.up*1.1f);
    if(hugAt>0&&hm<hugMarks.Length&&Time.time-hugAt>=hugMarks[hm]){var sh=(rig.neck.position+rig.upperL.position)*.5f;float d=Vector3.Distance(p.head.position,sh);if(hm==1){result.hugShoulderDistance=d;Check(d<.45f&&rig.weightL>.9f&&rig.weightR>.9f,"11.6а hug: camera "+d.ToString("F2")+" m from her shoulder, both arms round him (IK "+rig.weightL.ToString("F2")+"/"+rig.weightR.ToString("F2")+")");}
     yield return Shot("hug-"+hugMarks[hm].ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));hm++;continue;}
    if(fingersAt>0&&last=="fingers"){result.handScaleMax=Mathf.Max(result.handScaleMax,rig.handScale);
     if(!fingerShot&&Time.time-fingersAt>=1f){fingerShot=true;bool vis=InFrame(p,rig.Tip(true),out var v);Check(vis&&rig.Tip(true).z<6.0f,"11.6б the stretching hand is inside the gap and in his view: tip "+rig.Tip(true).ToString("F2")+", viewport "+v.ToString("F2"));yield return Shot("fingers-1.0");}}
    if(transformAt>0&&last=="transform"&&tm<transformMarks.Length&&Time.time-transformAt>=transformMarks[tm]){
     if(transformMarks[tm]>=1.2f){var toP=p.transform.position-gr.position;toP.y=0;if(transformMarks[tm]==1.2f)Check(Vector3.Dot(gr.forward,toP.normalized)>.8f,"11.9: she has turned to him by 1.0 s (dot "+Vector3.Dot(gr.forward,toP.normalized).ToString("F2")+")");}
     bool head=InFrame(p,rig.head.position,out var hv);checks.Add("TRANSFORM t="+transformMarks[tm]+" top "+rig.Top.ToString("F2")+" scale "+gr.localScale.y.ToString("F2")+" forearm "+rig.forearmScale.ToString("F2")+" hand "+rig.handScale.ToString("F2")+" bow "+rig.headBow.ToString("F0")+" head in frame "+head+" "+hv.ToString("F2"));
     if(transformMarks[tm]==3.4f)Check(head&&rig.handScale>2f&&rig.forearmScale>1.4f,"11.9 at 3.4 s: her head in his frame, forearms ×"+rig.forearmScale.ToString("F2")+", hands ×"+rig.handScale.ToString("F2"));
     yield return Shot("transform-"+transformMarks[tm].ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));tm++;continue;}
    if(scenes.PlayingScene=="DeathFinale"&&dm<tearMarks.Length&&scenes.FrameTime>=tearMarks[dm]){
     float ft=scenes.FrameTime;string extra="";
     if(ft<=1.35f&&flat.guest.activeSelf){var look=flat.guest.GetComponent<GuestLookAt>();if(look&&look.headBone){bool c=InFrame(p,look.headBone.position,out var hv);Check(c,"11.10 at "+ft.ToString("F2")+": Guest head in the central frame "+hv.ToString("F2"));}}
     if(Mathf.Abs(tearMarks[dm]-2.3f)<.01f||Mathf.Abs(tearMarks[dm]-2.7f)<.01f){var hand=p.head.Find("Claw R (death)");Check(hand&&hand.gameObject.activeInHierarchy,"11.10 at "+ft.ToString("F2")+" (the 3.2 face beat): her claw hand is in (rising into her face; Misha's face mesh ABSENT)");}
     extra=" black "+(tearMarks[dm]>=3.2f?"expected":"no");checks.Add("TEAR FrameTime "+ft.ToString("F2")+extra);
     yield return Shot("tear-"+tearMarks[dm].ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));dm++;continue;}
    if(beat=="epilogue")Time.timeScale=6;
    yield return null;}
   Time.timeScale=1;sound.CueStarted-=cue;
   var order=new List<string>();foreach(var b in story.QaFinaleBeats)order.Add(b.beat);result.beatOrder=order.ToArray();result.beats=order.Count;
   File.WriteAllLines(Path.Combine(output,"beats.txt"),story.QaFinaleBeats.ConvertAll(b=>b.at.ToString("F2")+"\t"+b.beat));
   string[] want=branch=="open"?new[]{"22:00","knock","choice","open","hug","hug-end","door-locked","stove","matches","strike","gas","kettle","water","kettle-on","whistle","question","transform","tear","epilogue"}:new[]{"22:00","knock","choice","key","gap","grope","fingers","chain-off","hand-normal","enter","door-locked","stove","matches","strike","gas","kettle","water","kettle-on","whistle","question","transform","tear","epilogue"};
   Check(string.Join(",",order)==string.Join(",",want),"beats in SCENARIO order for branch "+branch+": "+string.Join(" → ",order));
   result.epilogue=story.chapter==StoryDirector.Chapter.Ended||loop.Finished;result.refusedCard=story.QaRefusedCard;
   Check(result.epilogue,"the epilogue was reached");Check(result.refusedCard==(branch=="key"),"the «Ты не она!» card only when the door was not opened ("+result.refusedCard+")");
   Check(sawFinaleScene&&!sawDeathFull,"11.10 played as DeathFinale (short + 3.2 beat), never DeathFull");
   Check(Mathf.Abs(result.tearSeconds-3.5f)<.6f,"11.10 DeathFinale "+result.tearSeconds.ToString("F2")+" s from its start to the epilogue (3.5 s + reset), not the 7 s DeathFull");
   if(branch=="key"){result.cracksDuringFingers=crackCount;Check(result.handScaleMax>=2.1f,"11.6б fingers stretched to "+result.handScaleMax.ToString("F2")+"× over 2 s");}
   foreach(var kv in story.QaReachError)Check(kv.Value<.06f,"reach "+kv.Key+": her fingertip ended "+kv.Value.ToString("F3")+" m from the target (arm with hand ≈ 0.62 m)");
   result.reachErrors=new List<string>(story.QaReachError.Keys).ConvertAll(k=>k+"="+story.QaReachError[k].ToString("F3")).ToArray();
   Check(errors==0,"no runtime errors ("+errors+")");
   result.errors=errors;result.failures=failures;result.passed=failures==0&&errors==0;File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));
   File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);yield return new WaitForSecondsRealtime(.5f);Application.Quit(result.passed?0:1);
  }
 }
}
