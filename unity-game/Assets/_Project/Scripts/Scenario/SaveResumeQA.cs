using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Ataka.Scenario {
 // QA only:  -save-resume-qa <out> -save-resume-case lap2|chase|finale -save-resume-phase stage|resume -qa-save-dir <dir>
 // Proves saving and continuing across a FULL restart of the Windows process, with the player's own saves untouched
 // (both checkpoint paths come from -qa-save-dir before anything is read or written; see ScenarioSaves).
 //  stage  (process 1): the state is staged, then written by the game's own methods — StoryDirector.Checkpoint through
 //         the real EnterLoop / Finale, LoopDirector.SaveCheckpoint through the real EnterLap / ChaseCorridor.Begin —
 //         and the process quits. Nothing writes JSON by hand.
 //    lap2:   real EnterLoop (chapter Loop), the flashlight, real EnterLap(2)
 //    chase:  real EnterLoop, the flashlight, real EnterLap(6), the chase begun at the hall door (as -scenario-chase-only)
 //    finale: the story's Finale chapter begun (its own Checkpoint("Finale"))
 //  resume (process 2, fresh): the menu's own «Продолжить с сохранения» — LoopDirector.ContinueFromSave → StoryDirector.
 //         Continue — and then the real game:
 //    lap2:   chapter Loop, lap 2, flashlight, door swing; ScenarioSmoke walks lap 2, returns through the tambour, lap 3
 //    chase:  the chase runs again; ScenarioSmoke runs it to the clock strikes, the live finale, FinaleQA to the epilogue
 //    finale: chapter Finale from its start; FinaleQA follows the same running finale to the epilogue
 // Output: <out>/stage|resume/{checks.txt,result.json,*.png}, then ScenarioSmoke / FinaleQA output under resume/.
 public sealed class SaveResumeQA:MonoBehaviour {
  string output,kase,phase;readonly List<string> checks=new List<string>();int errors,failures;
  [Serializable] class Result {public string kase,phase,saveDirectory,storyPath,loopPath,storyJson,loopJson,chapter,storyHashBoot,loopHashBoot,storyHashBeforeContinue,loopHashBeforeContinue,storyHashStaged,loopHashStaged;public bool passed,offeredContinue,flashlight,chase,isolated,unchangedUntilContinue,storySaveDeletedAfterEpilogue,epilogueReached;public int failures,errors,lap;public float doorDirection;}
  // SHA-256 of a QA checkpoint as it lies on disk ("(none)" when absent); only ever called on files inside -qa-save-dir
  static string Hash(string path){if(!File.Exists(path))return "(none)";using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
  string bootStoryHash,bootLoopHash;
  readonly Result result=new Result();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-save-resume-qa");if(i<0||i+1>=a.Length)return;
   string Arg(string k){int j=Array.IndexOf(a,k);return j>=0&&j+1<a.Length?a[j+1]:"";}
   var qa=new GameObject("Save/resume QA").AddComponent<SaveResumeQA>();qa.kase=Arg("-save-resume-case");qa.phase=Arg("-save-resume-phase");qa.output=Path.Combine(Path.GetFullPath(a[i+1]),qa.phase);
   // the earliest moment in the process (after Awake, before any Start): what the checkpoints hold when the game boots
   if(ScenarioSaves.QaDirectory!=null){qa.bootStoryHash=Hash(ScenarioSaves.File("story-checkpoint.json"));qa.bootLoopHash=Hash(ScenarioSaves.File("scenario-core-checkpoint.json"));}}
  void Log(string m,string t,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+m.Split('\n')[0]);}}
  void Check(bool ok,string what){checks.Add((ok?"PASS ":"FAIL ")+what);if(!ok)failures++;File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();if(Application.isBatchMode)yield break;var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),t.EncodeToPNG());Destroy(t);checks.Add("SHOT "+name+".png");}
  static string Read(string p)=>File.Exists(p)?File.ReadAllText(p):"(none)";
  IEnumerator Wait(Func<bool> until,float seconds){float end=Time.realtimeSinceStartup+seconds;while(!until()&&Time.realtimeSinceStartup<end)yield return null;}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;Application.targetFrameRate=60;
   // no usable -qa-save-dir: fail before anything is read, written or deleted (there is no fallback to the player's folder)
   if(!ScenarioSaves.QaRequested||ScenarioSaves.QaRejected||ScenarioSaves.QaDirectory==null){result.kase=kase;result.phase=phase;Check(false,"-qa-save-dir "+(!ScenarioSaves.QaRequested?"missing":"rejected: "+ScenarioSaves.QaProblem)+" — nothing read, written or deleted");Finish(true);yield break;}
   yield return new WaitForSecondsRealtime(2);
   var loop=FindFirstObjectByType<LoopDirector>();var story=loop.story;var flat=loop.flat;var p=flat.player;
   result.kase=kase;result.phase=phase;result.saveDirectory=ScenarioSaves.QaDirectory;result.storyPath=story.SavePath;result.loopPath=loop.SavePath;
   // isolation before anything else: both paths in the QA folder, which is not the player's own folder
   var own=Path.GetFullPath(Application.persistentDataPath).TrimEnd('\\','/');var dir=result.saveDirectory;
   result.isolated=!string.IsNullOrEmpty(dir)&&!string.Equals(Path.GetFullPath(dir).TrimEnd('\\','/'),own,StringComparison.OrdinalIgnoreCase)&&Path.GetDirectoryName(story.SavePath)==dir&&Path.GetDirectoryName(loop.SavePath)==dir;
   Check(result.isolated,"both checkpoints redirected to the QA folder "+dir+" (story "+story.SavePath+", loop "+loop.SavePath+"; player's folder "+own+" untouched)");
   if(!result.isolated||(kase!="lap2"&&kase!="chase"&&kase!="finale")||(phase!="stage"&&phase!="resume")){Check(false,"usage: -save-resume-case lap2|chase|finale -save-resume-phase stage|resume -qa-save-dir <dir>");Finish(true);yield break;}
   if(phase=="stage")yield return Stage(loop,story,flat,p);else yield return Resume(loop,story,flat,p);
  }
  IEnumerator Stage(LoopDirector loop,StoryDirector story,FlatBindings flat,ScenarioPlayer p){
   // a clean QA folder (only our two files in it), then the real saving code with saving enabled
   foreach(var f in new[]{story.SavePath,loop.SavePath})if(File.Exists(f))File.Delete(f);
   loop.testing=false;p.automating=true;loop.Pause(false);
   if(kase=="finale"){story.QaBegin("Finale");yield return Wait(()=>story.chapter==StoryDirector.Chapter.Finale&&File.Exists(story.SavePath),10);}
   else{
    story.QaEnterLoop();yield return Wait(()=>story.chapter==StoryDirector.Chapter.Loop&&File.Exists(story.SavePath),10);
    p.hasFlashlight=true;
    if(kase=="lap2"){loop.EnterLap(2);yield return null;}
    else{loop.EnterLap(6);p.Place(new Vector3(8.05f,.02f,5.35f),-90);loop.chase.Begin();yield return Wait(()=>loop.chase.Active,5);yield return null;}
   }
   yield return Shot("stage-"+kase);
   result.storyJson=Read(story.SavePath);result.loopJson=Read(loop.SavePath);
   Check(File.Exists(story.SavePath),"story checkpoint written by StoryDirector.Checkpoint: "+result.storyJson);
   if(kase!="finale")Check(File.Exists(loop.SavePath),"loop checkpoint written by LoopDirector.SaveCheckpoint: "+result.loopJson);
   Check(result.storyJson.Contains("\"chapter\":\""+(kase=="finale"?"Finale":"Loop")+"\""),"saved chapter "+(kase=="finale"?"Finale":"Loop"));
   if(kase=="lap2")Check(result.loopJson.Contains("\"lap\":2")&&result.loopJson.Contains("\"flashlight\":true")&&result.loopJson.Contains("\"chase\":false"),"saved lap 2 with the flashlight, no chase");
   if(kase=="chase")Check(result.loopJson.Contains("\"lap\":6")&&result.loopJson.Contains("\"flashlight\":true")&&result.loopJson.Contains("\"chase\":true"),"saved lap 6 in the chase, with the flashlight");
   result.storyHashStaged=Hash(story.SavePath);result.loopHashStaged=Hash(loop.SavePath);checks.Add("HASH staged story "+result.storyHashStaged+" loop "+result.loopHashStaged);
   checks.Add("STAGED — this process now quits; run the same case with -save-resume-phase resume in a NEW process");Finish(true);
  }
  IEnumerator Resume(LoopDirector loop,StoryDirector story,FlatBindings flat,ScenarioPlayer p){
   result.storyJson=Read(story.SavePath);result.loopJson=Read(loop.SavePath);checks.Add("FOUND story "+result.storyJson);checks.Add("FOUND loop "+result.loopJson);
   result.storyHashBoot=bootStoryHash;result.loopHashBoot=bootLoopHash;
   result.offeredContinue=loop.OffersContinue;Check(result.offeredContinue,"a fresh process opens on the menu with «Продолжить с сохранения» offered (save present, no story running)");
   yield return Shot("resume-menu");
   // right before Continue: both paths still in the QA folder, and nothing has rewritten the checkpoints since boot
   bool inQa=Path.GetDirectoryName(story.SavePath)==ScenarioSaves.QaDirectory&&Path.GetDirectoryName(loop.SavePath)==ScenarioSaves.QaDirectory;
   Check(inQa,"before Continue both SavePaths are in the QA folder (story "+story.SavePath+", loop "+loop.SavePath+")");if(!inQa){Finish(true);yield break;}
   result.storyHashBeforeContinue=Hash(story.SavePath);result.loopHashBeforeContinue=Hash(loop.SavePath);
   result.unchangedUntilContinue=result.storyHashBeforeContinue==bootStoryHash&&result.loopHashBeforeContinue==bootLoopHash&&bootStoryHash!="(none)";
   checks.Add("HASH boot story "+bootStoryHash+" loop "+bootLoopHash+" / before Continue story "+result.storyHashBeforeContinue+" loop "+result.loopHashBeforeContinue);
   Check(result.unchangedUntilContinue,"the checkpoints were not rewritten between process start and Continue");
   if(kase!="lap2"){story.qaAllowSave=true;StartCoroutine(WatchEpilogue(loop,story));} // the epilogue's own deletion of the story save is then allowed (QA folder only)
   p.automating=true;loop.ContinueFromSave(); // exactly the menu button
   yield return Wait(()=>story.Active,5);yield return new WaitForSeconds(1);
   result.chapter=story.chapter.ToString();result.lap=loop.Progress.Lap;result.flashlight=p.hasFlashlight;result.chase=loop.chase.Active;result.doorDirection=flat.entrance.direction;
   checks.Add("CONTINUED chapter "+result.chapter+" lap "+result.lap+" flashlight "+result.flashlight+" chase "+result.chase+" door direction "+result.doorDirection+" at "+p.transform.position.ToString("F2"));
   if(kase=="lap2"){
    Check(story.chapter==StoryDirector.Chapter.Loop&&loop.Progress.Lap==2&&p.hasFlashlight&&!loop.chase.Active,"Continue restored the loop at lap 2 with the flashlight, no chase");
    Check(flat.entrance.direction<0,"the flat door swings into the hall (the tambour return stays open)");
    Check(Vector3.Distance(p.transform.position,ScenarioLayout.Spawn)<.5f,"he stands at the lap's start");
    yield return Shot("resume-lap2");Finish(false);ScenarioSmoke.ResumeAfterContinue(Path.Combine(output,"walk"),2,false);
   }else if(kase=="chase"){
    yield return Wait(()=>loop.chase.Active,8);result.chase=loop.chase.Active;
    Check(story.chapter==StoryDirector.Chapter.Loop&&loop.Progress.Lap==6&&p.hasFlashlight&&loop.chase.Active,"Continue restored lap 6 and began the chase again, with the flashlight");
    yield return Shot("resume-chase");Finish(false);ScenarioSmoke.ResumeAfterContinue(Path.Combine(output,"chase"),6,true);
   }else{
    Check(story.chapter==StoryDirector.Chapter.Finale&&story.Objective=="Дождаться бабушку","Continue restored the finale from its start (22:00, «Дождаться бабушку»)");
    yield return Shot("resume-finale");Finish(false);FinaleQA.ContinueCurrent(Path.Combine(output,"finale"),"key");
   }
  }
  // after the finale: the story's own end deletes its checkpoint (qaAllowSave lets it under loop.testing, in the QA folder)
  IEnumerator WatchEpilogue(LoopDirector loop,StoryDirector story){float end=Time.realtimeSinceStartup+1200;while(!loop.Finished&&Time.realtimeSinceStartup<end)yield return null;
   result.epilogueReached=loop.Finished;result.storySaveDeletedAfterEpilogue=loop.Finished&&!File.Exists(story.SavePath);
   Check(result.epilogueReached,"the continued story reached its end (epilogue)");Check(result.storySaveDeletedAfterEpilogue,"the epilogue deleted the story checkpoint "+story.SavePath+" (no Continue after the end)");
   File.WriteAllText(Path.Combine(output,"epilogue-save.json"),JsonUtility.ToJson(new EpilogueSave{reached=result.epilogueReached,storySaveDeleted=result.storySaveDeletedAfterEpilogue,path=story.SavePath},true));Finish(false);}
  [Serializable] class EpilogueSave {public bool reached,storySaveDeleted;public string path;}
  // quit only after staging; after a resume the game goes on under ScenarioSmoke / FinaleQA, which quit themselves
  void Finish(bool quit){Check(errors==0,"no runtime errors ("+errors+")");result.failures=failures;result.errors=errors;result.passed=failures==0&&errors==0;
   File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);
   if(quit)Application.Quit(result.passed?0:1);}
 }
}
