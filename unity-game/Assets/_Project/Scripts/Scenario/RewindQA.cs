using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Ataka.Scenario {
 // QA only (~2.5 min):  -rewind-qa <dir>
 // The real night 1 (21:42 → 22:07, 6x until the death) with the player moved around the flat, the full death, then the
 // 6.1 rewind captured by its own clock: PNGs <dir>/rewind-<t>.png + checks.txt. Checks: 8–10 s, Esc pause holds the
 // rewind, per-channel sample reversal, bounded recording, and NO residue after it ends (control, body, camera, doors,
 // light enable flags, ticking, quiet, VHS, Guest, grandmother animation) — also when cancelled mid-rewind.
 // The four evening memories are STAGED here (the evening is not played in this run) and are reported as such.
 // Output: checks.txt (PASS/FAIL lines), result.json (numbers), rewind-timeline.txt (what played, rewind clock vs the
 // original night time), rewind-<t>.png. Exit 0 = all PASS. Sound goes only through the game listener; add
 // -audio-capture <wav> to record it silently with ScenarioAudioCapture.
 public sealed class RewindQA:MonoBehaviour {
  string output;readonly List<string> checks=new List<string>();int errors;
  [Serializable] class Result {public bool passed;public float rewindActiveSeconds,rewindRealSeconds,pauseRealSeconds,pauseClockDrift;public int samples,capacity,floatsPerSample,bytesReserved,bytesUsed,soundsRecorded,memoriesStaged,journalEntries,reverseOrderViolations,distinctCameraPositions,failures,errors;public string chapterAfter;}
  readonly Result result=new Result();
  static readonly float[] Marks={.5f,1.2f,2f,3f,4f,5f,5.8f,6.5f,7f,7.5f,7.9f,8.4f,9f,9.5f};
  static readonly (Vector3 at,float yaw,float pitch)[] Route={(new Vector3(7.9f,.02f,4.6f),0,0),(new Vector3(5.6f,.02f,5.2f),270,-10),(new Vector3(5.6f,.02f,3.6f),180,0),(new Vector3(4.6f,.02f,1.5f),200,15),(new Vector3(7.4f,.02f,3.5f),180,5),(new Vector3(7.0f,.02f,3.9f),20,0)};
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rewind-qa");if(i<0||i+1>=args.Length)return;
   new GameObject("Rewind QA").AddComponent<RewindQA>().output=Path.GetFullPath(args[i+1]);}
  void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+message.Split('\n')[0]);}}
  void Check(bool ok,string what){checks.Add((ok?"PASS ":"FAIL ")+what);if(!ok)result.failures++;File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);}
  List<(Light l,bool enabled,bool active)> LightStates(){var r=new List<(Light,bool,bool)>();foreach(var l in FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID))r.Add((l,l.enabled,l.gameObject.activeInHierarchy));return r;}
  static float Target(ScenarioDoor d)=>d.open?d.direction*95:d.direction*d.closedAngle;
  void Residue(string when,LoopDirector loop,List<(Light l,bool enabled,bool active)> lightsBefore){
   var flat=loop.flat;var p=flat.player;var story=loop.story;var r=story.Rewinder;var body=p.GetComponent<CharacterController>();
   Check(!r.Active,when+": rewind finished/cleaned");
   Check(p.control,when+": player control returned");
   Check(body&&body.enabled,when+": character controller enabled");
   Check(Mathf.Abs(Mathf.DeltaAngle(0,p.head.localEulerAngles.z))<.5f&&Mathf.Abs(Mathf.DeltaAngle(0,p.head.localEulerAngles.y))<.5f,when+": camera has no leftover jitter/roll");
   foreach(var d in new[]{flat.entrance,flat.kitchen,flat.bath,flat.bedroom,flat.living,flat.balconyDoor}){if(!d||!d.leaf)continue;float err=Mathf.Abs(Mathf.DeltaAngle(Mathf.DeltaAngle(0,d.leaf.localEulerAngles.y),Target(d)));
    Check(!d.scripted&&err<3,when+": door "+d.name+" "+(d.open?"open":"closed")+", not scripted, leaf at its target ("+err.ToString("F1")+"°)");}
   int changed=0;foreach(var s in lightsBefore)if(s.l&&(s.l.enabled!=s.enabled||s.l.gameObject.activeInHierarchy!=s.active))changed++;
   Check(changed==0,when+": no light object enabled/disabled ("+changed+" changed of "+lightsBefore.Count+")");
   Check(!loop.sound.RewindTickActive,when+": rewind ticking stopped");Check(!loop.sound.quiet,when+": world sound not muted");
   Check(flat.look.disruption<.01f,when+": VHS disruption back to 0");Check(!flat.guest.activeSelf,when+": Guest hidden");
   var anim=flat.grandmother.GetComponentInChildren<Animation>(true);Check(!anim||anim.enabled,when+": grandmother animation running");
  }
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;Application.targetFrameRate=60;
   yield return new WaitForSecondsRealtime(2);
   var loop=FindFirstObjectByType<LoopDirector>();var story=loop.story;var flat=loop.flat;var p=flat.player;var scenes=loop.scenes;var r=story.Rewinder;
   loop.testing=true;p.automating=true;loop.Pause(false);story.QaBegin("Night1");yield return new WaitForSecondsRealtime(1.5f);
   Check(r.Recording,"night 1 recording started at 21:42");
   story.QaRememberEvening();checks.Add("NOTE evening memories staged by QA ("+r.MemoriesRecorded+"); in play they are taken at the real evening moments");
   // The real night at 6x, the player walking a route so the rewind has something to play back.
   int k=0;float nextMove=0;float until=Time.realtimeSinceStartup+300;
   while(scenes.PlayingScene!="DeathFull"&&Time.realtimeSinceStartup<until){
    bool scene=!string.IsNullOrEmpty(scenes.PlayingScene);Time.timeScale=scene?1:6;
    if(!scene&&p.control&&Time.realtimeSinceStartup>nextMove){var w=Route[k++%Route.Length];p.Place(w.at,w.yaw,w.pitch);nextMove=Time.realtimeSinceStartup+1.5f;}
    yield return null;}
   Time.timeScale=1;Check(scenes.PlayingScene=="DeathFull","the real night 1 reached its 22:07 death");
   checks.Add("RECORDING "+r.Samples+" samples × "+r.FloatsPerSample+" floats (cap "+NightRewind.Capacity+" = "+(NightRewind.Capacity/NightRewind.Rate)+" s at "+NightRewind.Rate+" Hz), "+r.CuesRecorded+" sounds, "+r.MemoriesRecorded+" memories; memory used "+(r.BytesUsed/1024)+" KiB of "+(r.BytesReserved/1024)+" KiB reserved up front");
   result.samples=r.Samples;result.capacity=NightRewind.Capacity;result.floatsPerSample=r.FloatsPerSample;result.bytesReserved=r.BytesReserved;result.bytesUsed=r.BytesUsed;result.soundsRecorded=r.CuesRecorded;result.memoriesStaged=r.MemoriesRecorded;
   Check(r.Samples>0&&r.Samples<=NightRewind.Capacity&&!r.Recording,"recording bounded and stopped before the death");
   var lightsBefore=LightStates();
   while(!r.Active&&Time.realtimeSinceStartup<until+60)yield return null;
   float started=Time.time,startedReal=Time.realtimeSinceStartup;int m=0;bool pauseTested=false;var seen=new HashSet<string>();var positions=new HashSet<Vector3Int>();
   while(r.Active){
    seen.Add(r.Phase);if(r.Phase=="reverse")positions.Add(Vector3Int.RoundToInt(p.transform.position*10));
    if(!pauseTested&&r.Clock>4){pauseTested=true;float c0=r.Clock;var pos0=p.transform.position;loop.Pause(true);float pr=Time.realtimeSinceStartup;yield return new WaitForSecondsRealtime(.5f);float c1=r.Clock;var pos1=p.transform.position;loop.Pause(false);
     result.pauseRealSeconds=Time.realtimeSinceStartup-pr;result.pauseClockDrift=c1-c0;Check(Mathf.Abs(c1-c0)<.001f&&(pos1-pos0).sqrMagnitude<1e-6f,"pause in the middle: "+result.pauseRealSeconds.ToString("F2")+" s real, rewind clock "+c0.ToString("F2")+" → "+c1.ToString("F2")+", camera did not move");}
    if(m<Marks.Length&&r.Clock>=Marks[m]){
     yield return new WaitForEndOfFrame();string file="rewind-"+Marks[m].ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+".png";if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot(Path.Combine(output,file));
     checks.Add("SHOT "+file+" clock "+r.Clock.ToString("F2")+" phase "+r.Phase+" disruption "+flat.look.disruption.ToString("F2")+" ticking "+loop.sound.RewindTickActive+" player "+p.transform.position.ToString("F2")+" guest "+(flat.guest.activeSelf?"shown":"hidden")+" grandmother "+(flat.grandmother.activeSelf?"shown":"hidden"));m++;continue;}
    yield return null;}
   float took=r.LastActiveSeconds;result.rewindActiveSeconds=took;result.rewindRealSeconds=Time.realtimeSinceStartup-startedReal;
   Check(took>=8&&took<=10.5f,"rewind active game time "+took.ToString("F2")+" s (8–10 s incl. silence, flash, freeze, PLAY); real "+result.rewindRealSeconds.ToString("F2")+" s incl. the 0.5 s pause; Time.time span "+(Time.time-started).ToString("F2"));
   // Reverse chronology: every frame/sound shown during the reverse phase comes from an earlier night time than the one before it.
   var lines=new List<string>{"rewind_clock\tnight_time\twhat"};float last=float.MaxValue;int violations=0;
   foreach(var e in r.Journal){lines.Add(e.clock.ToString("F2")+"\t"+(e.source<0?"evening":e.source.ToString("F2"))+"\t"+e.what);if(e.source>=0){if(e.source>last+.001f)violations++;last=e.source;}}
   File.WriteAllLines(Path.Combine(output,"rewind-timeline.txt"),lines);result.journalEntries=r.Journal.Count;result.reverseOrderViolations=violations;
   Check(r.Journal.Count>10&&violations==0,"frames and sounds played in reverse chronology ("+r.Journal.Count+" entries, "+violations+" out of order) — rewind-timeline.txt");
   Check(seen.Contains("black")&&seen.Contains("reverse")&&seen.Contains("evening")&&seen.Contains("flash")&&seen.Contains("freeze")&&seen.Contains("play"),"all phases ran: "+string.Join(",",seen));
   result.distinctCameraPositions=positions.Count;Check(positions.Count>5,"the night camera was really played back ("+positions.Count+" distinct positions)");
   yield return new WaitForSeconds(.5f);
   result.chapterAfter=story.chapter.ToString();Check(story.chapter==StoryDirector.Chapter.Night2&&p.control,"rewind hands over to night 2 with control=true ("+story.chapter+", control "+p.control+")");
   Check(flat.grandmother.activeSelf&&Vector3.Distance(flat.grandmother.transform.position,StoryDirector.QaNode("Door"))<.7f,"night 2 starts with her at the door");
   Residue("after rewind",loop,lightsBefore);
   // Per-channel reversal on a synthetic stereo clip (frames reversed, channel order kept).
   var clip=AudioClip.Create("qa stereo",5,2,8000,false);clip.SetData(new float[]{.1f,-.1f,.2f,-.2f,.3f,-.3f,.4f,-.4f,.5f,-.5f},0);var rev=loop.sound.Reversed(clip);var back=new float[10];
   bool ok=rev&&rev.GetData(back,0)&&Mathf.Approximately(back[0],.5f)&&Mathf.Approximately(back[1],-.5f)&&Mathf.Approximately(back[8],.1f)&&Mathf.Approximately(back[9],-.1f);
   Check(ok,"audio reversed per channel by samples ("+(rev?string.Join(",",back):"null")+")");
   // Cancel mid-rewind (menu / new game): nothing may stay behind.
   lightsBefore=LightStates();var doorsBefore=new List<(ScenarioDoor d,bool scripted,bool locked,bool open,float angle)>();
   foreach(var d in new[]{flat.entrance,flat.kitchen,flat.bath,flat.bedroom,flat.living,flat.balconyDoor})if(d&&d.leaf)doorsBefore.Add((d,d.scripted,d.locked,d.open,Mathf.DeltaAngle(0,d.leaf.localEulerAngles.y)));
   story.QaReplayRewind();while(!(r.Active&&r.Phase=="reverse"&&r.Clock>3))yield return null;
   int scriptedDuring=0;foreach(var b in doorsBefore)if(b.d.scripted)scriptedDuring++;Check(scriptedDuring==doorsBefore.Count,"during the replay every door is scripted, so ScenarioDoor.Update does not overwrite the replayed leaves ("+scriptedDuring+"/"+doorsBefore.Count+")");
   checks.Add("CANCEL at clock "+r.Clock.ToString("F2")+" phase "+r.Phase);story.QaCancelRewind();
   foreach(var b in doorsBefore){var d=b.d;float err=Mathf.Abs(Mathf.DeltaAngle(Mathf.DeltaAngle(0,d.leaf.localEulerAngles.y),b.angle));Check(d.scripted==b.scripted&&d.locked==b.locked&&d.open==b.open&&err<.5f,"cancel restores door "+d.name+": scripted "+d.scripted+", locked "+d.locked+", open "+d.open+", angle error "+err.ToString("F2")+"°");}
   story.QaBegin("Night2");yield return new WaitForSeconds(1.5f);
   Residue("after cancel",loop,lightsBefore);
   Check(errors==0,"no runtime errors ("+errors+")");
   result.errors=errors;result.passed=result.failures==0;File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));
   File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);yield return new WaitForSecondsRealtime(.5f);Application.Quit(checks.Exists(c=>c.StartsWith("FAIL "))?1:0);
  }
 }
}
