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
 // Camera height: crouched stops on the route and crouched from 22:05 to the death; replay/PLAY/cancel heights checked.
 // The (crouched) death: death-<pose>-<t>.png at 0.3/1.3/3.4/4.6 s with the -death-qa Guest head check.
 // Pause: no CueStarted / rewind_tick request while paused.
 // The four evening memories are STAGED here (the evening is not played in this run) and are reported as such.
 // Output: checks.txt (PASS/FAIL lines), result.json (numbers), rewind-timeline.txt (what played, rewind clock vs the
 // original night time), rewind-<t>.png. Exit 0 = all PASS. Sound goes only through the game listener; add
 // -audio-capture <wav> to record it silently with ScenarioAudioCapture.
 public sealed class RewindQA:MonoBehaviour {
  string output;readonly List<string> checks=new List<string>();int errors;
  [Serializable] class Result {public string tvLive,tvReplayed;public int tvBlueFrames;public bool passed;public float rewindActiveSeconds,rewindRealSeconds,pauseRealSeconds,pauseClockDrift;public int darkFrames,deathShots;public float knockFacing,knockPoseError,beltLuma,beltFill;public int memoriesWithPose;public int pauseCueStarts,pauseTickRequests,tickRequested,tickStarted,tickLimited,reversedRequested,reversedStarted,reversedLimited,crouchedSamples,standingSamples,playbackCrouchedFrames,playbackStandingFrames,eyeMismatches;public bool diedCrouched;public float playEye;public int bytesRecordingReserved,bytesReversedCache,reversedPrepared,reversedSkipped,memoriesRestoredFromSave,visibilityFailures;public int samples,capacity,floatsPerSample,bytesReserved,bytesUsed,soundsRecorded,memoriesStaged,journalEntries,reverseOrderViolations,distinctCameraPositions,failures,errors;public string chapterAfter;}
  readonly Result result=new Result();
  static readonly float[] Marks={.5f,1.2f,2f,3f,4f,5f,5.8f,6.5f,7f,7.5f,7.9f,8.4f,9f,9.5f};
  // Two crouched stops (Ctrl, the ordinary hiding pose) and, from 22:05, crouched until the death — the rewind must replay both heights.
  static readonly (Vector3 at,float yaw,float pitch,bool crouch)[] Route={(new Vector3(7.9f,.02f,4.6f),0,0,false),(new Vector3(5.6f,.02f,5.2f),270,-10,false),(new Vector3(5.6f,.02f,3.6f),180,0,true),(new Vector3(4.6f,.02f,1.5f),200,15,false),(new Vector3(7.4f,.02f,3.5f),180,5,true),(new Vector3(7.0f,.02f,3.9f),20,0,false)};
  static int[] Cue(ScenarioSound sound,string id){foreach(var d in sound.Diagnostics)if(d.id==id)return new[]{d.requested,d.started,d.limit};return new[]{0,0,0};}
  // The live camera height at a night time (recording clock); `edge` when the stance changed within ±0.35 s (interpolated there).
  static float LiveEye(List<(float t,float eye)> log,float t,out bool edge){edge=false;if(log.Count==0)return 0;int lo=0,hi=log.Count-1;while(lo<hi){int mid=(lo+hi)/2;if(log[mid].t<t)lo=mid+1;else hi=mid;}
   float e=log[lo].eye;for(int i=lo;i>=0&&log[i].t>t-.35f;i--)if(Mathf.Abs(log[i].eye-e)>.05f)edge=true;for(int i=lo;i<log.Count&&log[i].t<t+.35f;i++)if(Mathf.Abs(log[i].eye-e)>.05f)edge=true;return e;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rewind-qa");if(i<0||i+1>=args.Length)return;
   new GameObject("Rewind QA").AddComponent<RewindQA>().output=Path.GetFullPath(args[i+1]);}
  void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+message.Split('\n')[0]);}}
  void Check(bool ok,string what){checks.Add((ok?"PASS ":"FAIL ")+what);if(!ok)result.failures++;File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);}
  List<(Light l,bool enabled,bool active)> LightStates(){var r=new List<(Light,bool,bool)>();foreach(var l in FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID))r.Add((l,l.enabled,l.gameObject.activeInHierarchy));return r;}
  static float body0(ScenarioPlayer p){var b=p.GetComponent<CharacterController>();return b?b.height:0;}
  static float Target(ScenarioDoor d)=>d.open?d.direction*95:d.direction*d.closedAngle;
  void Residue(string when,LoopDirector loop,List<(Light l,bool enabled,bool active)> lightsBefore){
   var flat=loop.flat;var p=flat.player;var story=loop.story;var r=story.Rewinder;var body=p.GetComponent<CharacterController>();
   Check(!r.Active,when+": rewind finished/cleaned");
   Check(p.control,when+": player control returned");
   Check(body&&body.enabled,when+": character controller enabled");
   Check(Mathf.Abs(Mathf.DeltaAngle(0,p.head.localEulerAngles.z))<.5f&&Mathf.Abs(Mathf.DeltaAngle(0,p.head.localEulerAngles.y))<.5f,when+": camera has no leftover jitter/roll");
   foreach(var d in new[]{flat.entrance,flat.kitchen,flat.bath,flat.bedroom,flat.living,flat.balconyDoor}){if(!d||!d.leaf)continue;float err=Mathf.Abs(Mathf.DeltaAngle(Mathf.DeltaAngle(0,d.leaf.localEulerAngles.y),Target(d)));
    Check(!d.scripted&&err<3,when+": door "+d.name+" "+(d.open?"open":"closed")+", not scripted, leaf at its target ("+err.ToString("F1")+"°)");}
   var changed=new List<string>();foreach(var s in lightsBefore)if(s.l&&(s.l.enabled!=s.enabled||s.l.gameObject.activeInHierarchy!=s.active))changed.Add(GranNav.PathOf(s.l.transform)+" enabled "+s.enabled+"→"+s.l.enabled+", active "+s.active+"→"+s.l.gameObject.activeInHierarchy);
   Check(changed.Count==0,when+": no light object enabled/disabled since the rewind started ("+changed.Count+" of "+lightsBefore.Count+(changed.Count>0?": "+string.Join("; ",changed):"")+")");
   Check(!loop.sound.RewindTickActive,when+": rewind ticking stopped");Check(!loop.sound.quiet,when+": world sound not muted");
   Check(flat.look.disruption<.01f,when+": VHS disruption back to 0");Check(!flat.guest.activeSelf,when+": Guest hidden");
   var anim=flat.grandmother.GetComponentInChildren<Animation>(true);Check(!anim||anim.enabled,when+": grandmother animation running");
  }
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;Application.targetFrameRate=60;
   yield return new WaitForSecondsRealtime(2);
   var loop=FindFirstObjectByType<LoopDirector>();var story=loop.story;var flat=loop.flat;var p=flat.player;var scenes=loop.scenes;var r=story.Rewinder;
   {var args=Environment.GetCommandLineArgs();int f=Array.IndexOf(args,"-rewind-belt-fill");if(f>=0&&f+1<args.Length&&float.TryParse(args[f+1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var v))r.beltFill=v;result.beltFill=r.beltFill;checks.Add("NOTE belt memory light intensity "+r.beltFill.ToString(System.Globalization.CultureInfo.InvariantCulture));}
   loop.testing=true;p.automating=true;loop.Pause(false);story.QaBegin("Night1");yield return new WaitForSecondsRealtime(1.5f);
   Check(r.Recording,"night 1 recording started at 21:42");
   story.QaRememberEvening();checks.Add("NOTE evening memories staged by QA ("+r.MemoriesRecorded+"); in play they are taken at the real evening moments (StoryAutopilot run)");
   // Saved with the night-1 checkpoint, forgotten, then restored by Continue (the path a player takes after quitting).
   var staged=r.Export();story.qaAllowSave=true;story.QaSavePath(Path.Combine(output,"story-checkpoint.json"));story.QaCheckpoint("Night1");r.ForgetEvening();
   story.Continue();yield return new WaitForSecondsRealtime(1.5f);int same=0;
   foreach(var a in staged)foreach(var b in r.Memories)if(a.id==b.id&&(a.cam-b.cam).sqrMagnitude<1e-4f&&a.subject==b.subject)same++;
   int poses=0;
   foreach(var a in staged)if(a.gran&&a.granPose!=null&&a.granPose.Length>0){var b=new List<NightRewind.Memory>(r.Memories).Find(x=>x.id==a.id);bool poseSaved=b!=null&&b.granPose!=null&&b.granPose.Length==a.granPose.Length;
    if(poseSaved)for(int n=0;n<a.granPose.Length;n++){var x=a.granPose[n];var y=b.granPose[n];if(x.path!=y.path||Quaternion.Angle(x.rotation,y.rotation)>.05f||(x.position-y.position).sqrMagnitude>1e-8f||(x.scale-y.scale).sqrMagnitude>1e-8f)poseSaved=false;}
    Check(poseSaved,"memory "+a.id+": saved rig pose survives Continue ("+a.granPose.Length+" transforms)");if(poseSaved)poses++;}
   result.memoriesWithPose=poses;Check(poses==2,"tea and knock contain their original rig poses, not the later hanging pose");
   result.memoriesRestoredFromSave=same;Check(staged.Length==4&&same==4&&story.chapter==StoryDirector.Chapter.Night1&&r.Recording,"the four memories survive a save + Continue at night 1 ("+same+"/"+staged.Length+", "+story.chapter+")");
   // The real night at 6x, the player walking a route so the rewind has something to play back.
   int k=0;float nextMove=0;float until=Time.realtimeSinceStartup+300;var liveEye=new List<(float t,float eye)>();
   // pc27: what the «Рубин» showed live (22:00 test card → announcer captions) and, later, what the rewind shows
   var tv=FindFirstObjectByType<ScenarioTelevision>();var liveTv=new List<string>();var playTv=new List<string>();int blueFrames=0,announcerFrames=0;
   string TvKey()=>tv&&tv.storyOn&&tv.show!=ScenarioTelevision.Show.None?tv.show+(tv.show==ScenarioTelevision.Show.Announcer?":"+tv.caption:""):"";
   void Note(List<string> list){var key=TvKey();if(key!=""&&(list.Count==0||list[list.Count-1]!=key))list.Add(key);}
   while(scenes.PlayingScene!="DeathFull"&&Time.realtimeSinceStartup<until){
    bool scene=!string.IsNullOrEmpty(scenes.PlayingScene);Time.timeScale=scene?1:6;
    if(!scene&&p.control&&Time.realtimeSinceStartup>nextMove){var w=Route[k++%Route.Length];p.crouched=w.crouch||story.QaNightSeconds>=22*3600+5*60;p.Place(w.at,w.yaw,w.pitch);nextMove=Time.realtimeSinceStartup+1.5f;}
    yield return null;if(r.Recording){liveEye.Add((r.RecordingTime,p.head.localPosition.y));Note(liveTv);}}
   Time.timeScale=1;Check(scenes.PlayingScene=="DeathFull","the real night 1 reached its 22:07 death");result.diedCrouched=p.crouched;
   checks.Add("RECORDING "+r.Samples+" samples × "+r.FloatsPerSample+" floats (cap "+NightRewind.Capacity+" = "+(NightRewind.Capacity/NightRewind.Rate)+" s at "+NightRewind.Rate+" Hz), "+r.CuesRecorded+" sounds, "+r.MemoriesRecorded+" memories; memory used "+(r.BytesUsed/1024)+" KiB of "+(r.BytesReserved/1024)+" KiB reserved up front");
   result.samples=r.Samples;result.capacity=NightRewind.Capacity;result.floatsPerSample=r.FloatsPerSample;result.bytesReserved=r.BytesReserved;result.bytesRecordingReserved=r.BytesRecordingReserved;result.soundsRecorded=r.CuesRecorded;result.memoriesStaged=r.MemoriesRecorded;
   Check(r.Samples>0&&r.Samples<=NightRewind.Capacity&&!r.Recording,"recording bounded and stopped before the death");
   for(int i=0;i<r.Samples;i++){float e=r.EyeAt(i);if(e<1)result.crouchedSamples++;else if(e>1.5f)result.standingSamples++;}
   Check(result.crouchedSamples>0&&result.standingSamples>0,"camera height recorded: "+result.crouchedSamples+" crouched samples (eye < 1 m), "+result.standingSamples+" standing; died "+(result.diedCrouched?"crouched":"standing"));
   // The death itself (crouched when the route left the player crouched): frames + the Guest's head check as in -death-qa,
   // so a death that runs but is not seen is not taken on trust.
   var deathMarks=new[]{.3f,1.3f,3.4f,4.6f};int dm=0,headsInFrame=0;
   while(scenes.PlayingScene=="DeathFull"&&Time.realtimeSinceStartup<until+60){
    if(dm<deathMarks.Length&&scenes.FrameTime>=deathMarks[dm]){
     yield return new WaitForEndOfFrame();string file="death-"+(result.diedCrouched?"crouched":"standing")+"-"+deathMarks[dm].ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+".png";if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot(Path.Combine(output,file));
     var g=flat.guest.transform;checks.Add("SHOT "+file+" FrameTime "+scenes.FrameTime.ToString("F2")+" eye "+p.head.localPosition.y.ToString("F2")+" m (world "+p.head.position.y.ToString("F2")+") pitch "+Mathf.DeltaAngle(0,p.head.localEulerAngles.x).ToString("F0")+" torch "+(p.flashlight.enabled?"on":"off")+" guest "+(flat.guest.activeSelf?g.position.ToString("F2")+" tilt "+Vector3.Angle(g.up,Vector3.up).ToString("F0"):"hidden"));
     if(flat.guest.activeSelf&&Vector3.Angle(g.up,Vector3.up)>5)Check(false,file+": Guest tilted "+Vector3.Angle(g.up,Vector3.up).ToString("F0")+"°");
     if(flat.guest.activeSelf&&scenes.FrameTime>=.3f&&scenes.FrameTime<=1.35f){var look=flat.guest.GetComponent<GuestLookAt>();
      if(!look||!look.headBone)Check(false,file+": ready head bone missing");
      else{var at=p.view.WorldToViewportPoint(look.headBone.position);bool central=at.z>0&&at.x>=.1f&&at.x<=.9f&&at.y>=.1f&&at.y<=.9f;if(central)headsInFrame++;Check(central,file+": Guest head in the central frame "+at.ToString("F3"));}}
     dm++;continue;}
    yield return null;}
   result.deathShots=dm;Check(dm==deathMarks.Length&&headsInFrame>0,"death "+(result.diedCrouched?"crouched":"standing")+" captured: "+dm+"/"+deathMarks.Length+" frames, Guest head in frame in "+headsInFrame+" of the early marks (0.3, 1.3)");
   while(!r.Active&&Time.realtimeSinceStartup<until+60)yield return null;
   var lightsBefore=LightStates(); // baseline at the rewind's own start (after DeathFull has re-enabled the torch)
   string lastKey="";int peakUsed=0,peakCache=0;
   int pauseCues=0;Action<string> countCue=id=>pauseCues++;var tick0=Cue(loop.sound,"rewind_tick");var rev0=Cue(loop.sound,"rewind");
   float started=Time.time,startedReal=Time.realtimeSinceStartup;int m=0;bool pauseTested=false;bool playChecked=false;var seen=new HashSet<string>();var positions=new HashSet<Vector3Int>();
   while(r.Active){
    if(r.Phase=="reverse"){Note(playTv);if(tv&&tv.storyOn&&tv.show==ScenarioTelevision.Show.Announcer){announcerFrames++;var rt=tv.QaFeed;if(rt&&tv.Brightness>.9f){var was=RenderTexture.active;RenderTexture.active=rt;var px=new Texture2D(8,8,TextureFormat.RGBA32,false);px.ReadPixels(new Rect(4,4,8,8),0,0);px.Apply();RenderTexture.active=was;var c=px.GetPixel(4,4);Destroy(px);if(c.b>c.r+.2f)blueFrames++;}}}
    if(r.Phase=="reverse"&&r.Samples>1){float eye=p.head.localPosition.y;if(eye<1)result.playbackCrouchedFrames++;else if(eye>1.5f)result.playbackStandingFrames++;float live=LiveEye(liveEye,r.CurrentSource,out bool edge);if(!edge&&Mathf.Abs(live-eye)>.05f){if(result.eyeMismatches++<5)checks.Add("NOTE camera height at night time "+r.CurrentSource.ToString("F2")+": replayed "+eye.ToString("F2")+" m, live "+live.ToString("F2")+" m");}}
    if(r.Phase=="play"&&!playChecked){playChecked=true;result.playEye=p.head.localPosition.y;Check(!p.crouched&&Mathf.Abs(p.head.localPosition.y-1.55f)<.01f,"PLAY: night 2 starts standing (crouched "+p.crouched+", eye "+p.head.localPosition.y.ToString("F2")+" m; died "+(result.diedCrouched?"crouched":"standing")+")");}
    seen.Add(r.Phase);peakUsed=Mathf.Max(peakUsed,r.BytesUsed);peakCache=Mathf.Max(peakCache,r.BytesReversedCache);if(r.Phase=="reverse")positions.Add(Vector3Int.RoundToInt(p.transform.position*10));
    // Every evening memory and PLAY: the subject (prop bounds / her head bounds) must really be in the frame and unobstructed.
    string key=r.Phase=="evening"?"evening-"+r.CurrentMemory:r.Phase=="play"?"play":"";
    if(key!=""&&key!=lastKey&&r.Phase=="evening"&&!r.CurrentSubject.HasValue&&r.CurrentMemory!=""){lastKey=key;result.visibilityFailures++;Check(false,"memory "+r.CurrentMemory+" has no subject in the scene (prop missing or grandmother hidden) — it would show an arbitrary view");}
    if(key!=""&&key!=lastKey&&(r.Phase!="evening"||r.CurrentSubject.HasValue)){lastKey=key;yield return new WaitForEndOfFrame();Texture2D frame=null;if(!Application.isBatchMode){ScreenCapture.CaptureScreenshot(Path.Combine(output,"rewind-"+key+".png"));frame=ScreenCapture.CaptureScreenshotAsTexture();}
     // Not art acceptance: the subject region must not be near-black (the belt was "visible" by raycast yet unreadable).
     var lit=r.Phase=="evening"?r.CurrentSubject:r.GranFacePoint()??r.GranHead();if(lit.HasValue&&frame){bool bright=r.Readable(frame,lit.Value,out var lw);if(key=="evening-belt")result.beltLuma=r.LastLuma;if(!bright)result.darkFrames++;Check(bright,key+": subject region not dark — "+lw);}if(frame)Destroy(frame);
     if(r.Phase=="evening"&&r.CurrentMemory=="knock"){float poseError=r.MemoryPoseError();result.knockPoseError=poseError;Check(poseError>=0&&poseError<.1f,"memory knock restores the recorded bone pose after the hanging night (max error "+poseError.ToString("F3")+" degrees)");float facing=r.GranFacingCamera();result.knockFacing=facing;Check(facing>=.5f,"memory knock: her face turned to the camera (cos "+facing.ToString("F2")+", ≥ 0.5)");}
     if(r.Phase=="evening"){var mem=new List<NightRewind.Memory>(r.Memories).Find(x=>x.id==r.CurrentMemory);bool g=mem!=null&&mem.subject=="gran";bool vis=r.Visible(r.CurrentSubject.Value,g,out var why);if(!vis)result.visibilityFailures++;Check(vis,"memory "+r.CurrentMemory+": subject "+(mem!=null?mem.subject:"?")+" visible — "+why);}
     else{var head=r.GranHead();bool vis=head.HasValue&&r.Visible(head.Value,true,out var why)&&Vector3.Distance(p.head.position,head.Value)>=1f;string w2=head.HasValue?(r.Visible(head.Value,true,out var w)?w:w)+", head "+Vector3.Distance(p.head.position,head.Value).ToString("F2")+" m from the camera":"grandmother hidden";if(!vis)result.visibilityFailures++;Check(vis,"PLAY: her head in frame and ≥ 1 m away — "+w2);}}
    if(!pauseTested&&r.Clock>4){pauseTested=true;float c0=r.Clock;var pos0=p.transform.position;var head0=p.head.localRotation;var pt0=Cue(loop.sound,"rewind_tick");loop.Pause(true);loop.sound.CueStarted+=countCue;float pr=Time.realtimeSinceStartup;yield return new WaitForEndOfFrame();if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot(Path.Combine(output,"rewind-paused.png"));yield return new WaitForSecondsRealtime(.5f);float c1=r.Clock;var pos1=p.transform.position;var head1=p.head.localRotation;loop.sound.CueStarted-=countCue;var pt1=Cue(loop.sound,"rewind_tick");loop.Pause(false);
     result.pauseRealSeconds=Time.realtimeSinceStartup-pr;result.pauseClockDrift=c1-c0;result.pauseCueStarts=pauseCues;result.pauseTickRequests=pt1[0]-pt0[0];
     Check(Mathf.Abs(c1-c0)<.001f&&(pos1-pos0).sqrMagnitude<1e-6f&&Quaternion.Angle(head0,head1)<.01f,"pause in the middle: "+result.pauseRealSeconds.ToString("F2")+" s real, rewind clock "+c0.ToString("F2")+" → "+c1.ToString("F2")+", camera did not move");
     Check(pauseCues==0&&result.pauseTickRequests==0,"pause has no playback side effects: "+pauseCues+" new CueStarted, "+result.pauseTickRequests+" rewind_tick requests in "+result.pauseRealSeconds.ToString("F2")+" s");}
    if(m<Marks.Length&&r.Clock>=Marks[m]){
     yield return new WaitForEndOfFrame();string file="rewind-"+Marks[m].ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+".png";if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot(Path.Combine(output,file));
     checks.Add("SHOT "+file+" clock "+r.Clock.ToString("F2")+" phase "+r.Phase+" disruption "+flat.look.disruption.ToString("F2")+" ticking "+loop.sound.RewindTickActive+" player "+p.transform.position.ToString("F2")+" guest "+(flat.guest.activeSelf?"shown":"hidden")+" grandmother "+(flat.grandmother.activeSelf?"shown":"hidden"));m++;continue;}
    yield return null;}
   {var t1=Cue(loop.sound,"rewind_tick");var v1=Cue(loop.sound,"rewind");result.tickRequested=t1[0]-tick0[0];result.tickStarted=t1[1]-tick0[1];result.tickLimited=t1[2]-tick0[2];result.reversedRequested=v1[0]-rev0[0];result.reversedStarted=v1[1]-rev0[1];result.reversedLimited=v1[2]-rev0[2];
    Check(result.tickStarted==1&&result.tickLimited==0,"rewind ticking is ONE voice for the whole rewind: rewind_tick requested "+result.tickRequested+", started "+result.tickStarted+", limited "+result.tickLimited);
    checks.Add("SOUND reversed one-shots requested "+result.reversedRequested+", started "+result.reversedStarted+", limited "+result.reversedLimited+" (8 at a time)");}
   Check(result.playbackCrouchedFrames>0&&result.playbackStandingFrames>0&&result.eyeMismatches==0,"camera height replayed as recorded: "+result.playbackCrouchedFrames+" crouched / "+result.playbackStandingFrames+" standing frames, "+result.eyeMismatches+" frames off the live height by > 5 cm");
   // pc27: the replayed set must show the recorded PICTURE (announcer screen with his words, test card), in reverse order
   {var liveCaps=liveTv.FindAll(x=>x.StartsWith("Announcer:"));var playCaps=playTv.FindAll(x=>x.StartsWith("Announcer:"));var reversed=new List<string>(liveCaps);reversed.Reverse();
    int matched=0,j=0;foreach(var c in playCaps){while(j<reversed.Count&&reversed[j]!=c)j++;if(j<reversed.Count){matched++;j++;}}
    int cardLive=liveTv.IndexOf("TestCard"),cardPlay=playTv.IndexOf("TestCard"),lastAnnPlay=playTv.FindLastIndex(x=>x.StartsWith("Announcer:"));
    result.tvLive=string.Join(" | ",liveTv);result.tvReplayed=string.Join(" | ",playTv);result.tvBlueFrames=blueFrames;
    Check(liveCaps.Count>=4&&cardLive>=0,"live night: the 22:00 test card and "+liveCaps.Count+" announcer captions were on the screen");
    Check(playCaps.Count>=2&&matched==playCaps.Count&&cardPlay>lastAnnPlay,"rewind replays the screen backwards: "+playCaps.Count+" announcer captions, all in reverse recorded order ("+matched+"), the test card after them");
    Check(announcerFrames==0||blueFrames>0,"replayed announcer frames show the blue screen on the CRT at full brightness ("+blueFrames+" of "+announcerFrames+" frames)");}
   result.bytesUsed=peakUsed;result.bytesReversedCache=peakCache;result.reversedPrepared=r.ReversedPrepared;result.reversedSkipped=r.ReversedSkipped;
   checks.Add("MEMORY peak used "+(peakUsed/1024)+" KiB (recording "+(r.Samples*r.FloatsPerSample*4/1024)+", reversed clips "+(peakCache/1024)+" for "+r.ReversedPrepared+" clips, "+r.ReversedSkipped+" over budget) of "+(r.BytesReserved/1024)+" KiB ceiling ("+(r.BytesRecordingReserved/1024)+" recording/cues/journal + "+(NightRewind.ReversedBudgetBytes/1024)+" reversed-audio budget); after the rewind the cache holds "+(r.BytesReversedCache/1024)+" KiB");
   Check(r.BytesReversedCache==0&&peakCache<=NightRewind.ReversedBudgetBytes,"reversed-audio cache within budget during the rewind and freed after it");
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
   Check(!p.crouched&&Mathf.Abs(p.head.localPosition.y-1.55f)<.01f&&body0(p)>1.6f,"after rewind: standing in night 2 (eye "+p.head.localPosition.y.ToString("F2")+" m, body "+body0(p).ToString("F2")+" m)");
   // Per-channel reversal on a synthetic stereo clip (frames reversed, channel order kept).
   var clip=AudioClip.Create("qa stereo",5,2,8000,false);clip.SetData(new float[]{.1f,-.1f,.2f,-.2f,.3f,-.3f,.4f,-.4f,.5f,-.5f},0);var rev=loop.sound.Reversed(clip);var back=new float[10];
   bool ok=rev&&rev.GetData(back,0)&&Mathf.Approximately(back[0],.5f)&&Mathf.Approximately(back[1],-.5f)&&Mathf.Approximately(back[8],.1f)&&Mathf.Approximately(back[9],-.1f);
   Check(ok,"audio reversed per channel by samples ("+(rev?string.Join(",",back):"null")+")");
   // Cancel mid-rewind (menu / new game): nothing may stay behind.
   lightsBefore=LightStates();var doorsBefore=new List<(ScenarioDoor d,bool scripted,bool locked,bool open,float angle)>();
   foreach(var d in new[]{flat.entrance,flat.kitchen,flat.bath,flat.bedroom,flat.living,flat.balconyDoor})if(d&&d.leaf)doorsBefore.Add((d,d.scripted,d.locked,d.open,Mathf.DeltaAngle(0,d.leaf.localEulerAngles.y)));
   p.crouched=true;yield return null;yield return null;float eyeCancel=p.head.localPosition.y; // replay started crouched: cancel must give the crouch back
   story.QaReplayRewind();while(!(r.Active&&r.Phase=="reverse"&&r.Clock>3))yield return null;
   int scriptedDuring=0;foreach(var b in doorsBefore)if(b.d.scripted)scriptedDuring++;Check(scriptedDuring==doorsBefore.Count,"during the replay every door is scripted, so ScenarioDoor.Update does not overwrite the replayed leaves ("+scriptedDuring+"/"+doorsBefore.Count+")");
   checks.Add("CANCEL at clock "+r.Clock.ToString("F2")+" phase "+r.Phase);story.QaCancelRewind();
   foreach(var b in doorsBefore){var d=b.d;float err=Mathf.Abs(Mathf.DeltaAngle(Mathf.DeltaAngle(0,d.leaf.localEulerAngles.y),b.angle));Check(d.scripted==b.scripted&&d.locked==b.locked&&d.open==b.open&&err<.5f,"cancel restores door "+d.name+": scripted "+d.scripted+", locked "+d.locked+", open "+d.open+", angle error "+err.ToString("F2")+"°");}
   Check(p.crouched&&Mathf.Abs(p.head.localPosition.y-eyeCancel)<.01f,"cancel restores the crouch: crouched "+p.crouched+", eye "+p.head.localPosition.y.ToString("F2")+" m (before "+eyeCancel.ToString("F2")+")");
   story.QaBegin("Night2");yield return new WaitForSeconds(1.5f);
   Check(!p.crouched&&Mathf.Abs(p.head.localPosition.y-1.55f)<.01f,"night 2 after cancel starts standing (eye "+p.head.localPosition.y.ToString("F2")+" m)");
   Residue("after cancel",loop,lightsBefore);
   Check(errors==0,"no runtime errors ("+errors+")");
   result.errors=errors;result.passed=result.failures==0;File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));
   File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);yield return new WaitForSecondsRealtime(.5f);Application.Quit(checks.Exists(c=>c.StartsWith("FAIL "))?1:0);
  }
 }
}
