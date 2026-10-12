using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Ataka.Scenario {
 // QA only (pc27):
 //  -leisure-qa <dir>                      4.7: the console on the «Рубин» (started through its real action, played by
 //                                         a simple autopilot, game-menu pause, the game's own pause, leaving = Esc),
 //                                         the album with her (6 spreads), «Теленеделя»; camera / control / FOV /
 //                                         Esc / TV handed back after each.
 //  -epilogue-qa <dir> [-epilogue-refused] 11.1: the VHS news on the real CRT, the refused line only in that branch,
 //                                         credits over the ticking clock, the last album spread, the ticking cut off.
 // PNGs + checks.txt (PASS/FAIL/ABSENT) + result.json; exit 0 = no FAIL. Missing album/newspaper images are ABSENT.
 public sealed class LeisureQA:MonoBehaviour {
  string output;bool epilogue,refused;readonly List<string> checks=new List<string>();int errors,failures;
  [Serializable] class Result {public bool passed;public int failures,errors,score,kills,shots,albumImages,epilogueCaptions,clockStarts;public float consoleFov,fovAfter,pauseDrift,gamePauseDrift;public string screen,epilogueScreen;public string[] absent;}
  readonly Result result=new Result();readonly List<string> absent=new List<string>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-leisure-qa"),e=Array.IndexOf(args,"-epilogue-qa");int at=i>=0?i:e;if(at<0||at+1>=args.Length)return;
   var qa=new GameObject("Leisure QA").AddComponent<LeisureQA>();qa.output=Path.GetFullPath(args[at+1]);qa.epilogue=at==e;qa.refused=Array.IndexOf(args,"-epilogue-refused")>=0;}
  void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+message.Split('\n')[0]);}}
  void Check(bool ok,string what){checks.Add((ok?"PASS ":"FAIL ")+what);if(!ok)failures++;File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));checks.Add("SHOT "+name+".png");}
  static void Aim(ScenarioPlayer p,Vector3 at){var d=at-p.head.position;var f=d;f.y=0;if(f.sqrMagnitude<1e-4f)return;p.transform.rotation=Quaternion.LookRotation(f);p.pitch=Mathf.Clamp(-Mathf.Atan2(d.y,f.magnitude)*Mathf.Rad2Deg,-80,80);p.head.localRotation=Quaternion.Euler(p.pitch,0,0);}
  // The CRT picture itself: share of strongly coloured pixels and brightest luma; saved as PNG.
  string Screen(ScenarioTelevision tv,string name,out float saturated,out float bright){
   saturated=0;bright=0;var rt=tv?tv.QaFeed:null;if(!rt)return "no feed";var was=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();RenderTexture.active=was;
   int sat=0;foreach(var c in t.GetPixels()){float l=.2126f*c.r+.7152f*c.g+.0722f*c.b;bright=Mathf.Max(bright,l);if(Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))>.45f)sat++;}saturated=sat/(float)(rt.width*rt.height);
   if(!Application.isBatchMode)File.WriteAllBytes(Path.Combine(output,"screen-"+name+".png"),t.EncodeToPNG());Destroy(t);return "saturated "+(saturated*100).ToString("F1")+"%, brightest "+bright.ToString("F2");}
  IEnumerator Fire(StoryDirector story,ScenarioPlayer p,string label,Func<Vector3> at,float stand){
   StoryDirector.Act act=null;float until=Time.time+20;while(Time.time<until&&act==null){act=story.OpenActs.Find(a=>a.label==label&&!a.fired&&(a.when==null||a.when()));if(act==null)yield return null;}
   Check(act!=null,"action «"+label+"» offered");if(act==null)yield break;var point=at();var feet=point+(p.transform.position-point).normalized*stand;
   var d=p.transform.position-point;d.y=0;feet=point+(d.sqrMagnitude>.01f?d.normalized:Vector3.left)*stand;feet.y=.02f;p.Place(feet,0);yield return null;Aim(p,point);for(int i=0;i<40&&story.Aimed!=act;i++){Aim(p,point);yield return null;}Check(story.Aimed==act,"action actually selected by aim/LOS: "+label+"; blocker "+story.QaLosBlocker(p.head.position,point));if(story.Aimed!=act){yield return Shot("FAILED-action");yield break;}story.QaFire(act);yield return null;}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;Application.targetFrameRate=60;
   yield return new WaitForSecondsRealtime(2);
   var loop=FindFirstObjectByType<LoopDirector>();var story=loop.story;var flat=loop.flat;var p=flat.player;var tv=FindFirstObjectByType<ScenarioTelevision>();
   loop.testing=true;p.automating=true;loop.Pause(false);
   if(epilogue)yield return Epilogue(loop,story,flat,p,tv);else yield return Leisure(loop,story,flat,p,tv);
   result.absent=absent.ToArray();foreach(var a in absent)checks.Add("ABSENT "+a);
   Check(errors==0,"no runtime errors ("+errors+")");result.errors=errors;result.failures=failures;result.passed=failures==0&&errors==0;
   File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);yield return new WaitForSecondsRealtime(.5f);Application.Quit(result.passed?0:1);
  }
  IEnumerator Leisure(LoopDirector loop,StoryDirector story,FlatBindings flat,ScenarioPlayer p,ScenarioTelevision tv){
   story.QaFreeTimeAndNews();yield return new WaitForSeconds(1);float fov0=story.QaFov;
   // Approach the actual screen from the living-room side, as in EveningQA; default spawn is behind its wall.
   var tvAt=flat.televisionScreen?flat.televisionScreen.bounds.center:flat.television.position;p.Place(new Vector3(tvAt.x-1.6f,.02f,tvAt.z),90);yield return null;
   // the console
   yield return Fire(story,p,"Поиграть в приставку",()=>flat.televisionScreen?flat.televisionScreen.bounds.center:flat.television.position,1.6f);
   yield return new WaitForSeconds(1);var game=story.QaConsole;result.consoleFov=story.QaFov;
   Check(story.QaLeisure&&game&&!p.control&&tv.show==ScenarioTelevision.Show.Game&&story.QaFov<fov0-10,"console: he sits at the set, control off, the CRT shows the game, the view narrows (FOV "+fov0.ToString("F0")+" → "+story.QaFov.ToString("F0")+")");
   if(!game)yield break;yield return Shot("console-1-title");
   game.qaDrive=true;game.qaStart=true;yield return null;yield return null;Check(game.State==ConsoleGame.Mode.Play,"console: Enter starts a round ("+game.State+")");
   for(float t=0;t<12;t+=Time.deltaTime){game.qaFire=true;game.qaMove=Mathf.Sin(Time.time*1.3f);yield return null;}
   yield return Shot("console-2-play");var shot=Screen(tv,"console",out var sat,out var br);result.screen=shot;Check(sat>.002f&&br>.6f,"console picture on the CRT: coloured sprites and bright text ("+shot+")");
   result.score=game.Score;result.kills=game.Kills;result.shots=game.Shots;Check(game.Shots>10&&game.Kills>0&&game.Score>0,"console: shots "+game.Shots+", hits "+game.Kills+", score "+game.Score);
   // the game menu (Esc is taken by the console, the menu is reached otherwise) and the game's own pause: time stands
   {float e0=game.Elapsed;loop.Pause(true);yield return new WaitForSecondsRealtime(.6f);float e1=game.Elapsed;loop.Pause(false);result.pauseDrift=e1-e0;Check(Mathf.Abs(e1-e0)<1e-4f,"console: the game menu freezes the round (drift "+(e1-e0).ToString("F4")+" s)");}
   {game.qaStart=true;yield return null;yield return null;float e0=game.Elapsed;yield return new WaitForSeconds(.5f);float e1=game.Elapsed;result.gamePauseDrift=e1-e0;yield return Shot("console-3-paused");Check(game.State==ConsoleGame.Mode.Paused&&Mathf.Abs(e1-e0)<1e-4f,"console: Enter pauses the round («ПАУЗА», drift "+(e1-e0).ToString("F4")+")");game.qaStart=true;yield return null;}
   Check(story.EscapeTaken,"console: Esc belongs to the console while it is on (it does not open the game menu)");
   story.QaLeaveLeisure();yield return new WaitForSeconds(.8f);result.fovAfter=story.QaFov;var body=p.GetComponent<CharacterController>();
   Check(!story.QaLeisure&&story.QaConsole==null&&p.control&&body&&body.enabled&&Mathf.Abs(story.QaFov-fov0)<.5f&&!tv.storyOn&&tv.show==ScenarioTelevision.Show.None&&!tv.QaFeedOn&&!story.EscapeTaken&&!p.paused,"console left (Esc): control, body, FOV "+story.QaFov.ToString("F0")+", set off and feed handed back, Esc opens the menu again");
   yield return Shot("console-4-left");
   // the album with her
   yield return new WaitForSeconds(.5f);
   yield return Fire(story,p,"Посмотреть альбом с бабушкой",()=>flat.grandmother.transform.position+Vector3.up*1.1f,1.2f);yield return new WaitForSeconds(.5f);
   var v=story.QaViewer;Check(v&&v.IsOpen&&v.pages.Count==6&&!p.control,"album: open, 6 spreads, control off");
   if(v&&v.IsOpen){for(int i=0;i<v.pages.Count;i++){var page=v.Current;if(page.loaded)result.albumImages++;else absent.Add("album image Resources/"+page.resource+".png (2:1 spread) — paper placeholder shown");yield return Shot("album-"+(i+1).ToString("00"));v.Next();yield return new WaitForSeconds(.4f);}
    story.QaLeaveLeisure();yield return new WaitForSeconds(.3f);}
   Check(v&&!v.IsOpen&&p.control&&!story.QaLeisure,"album closed: control back");
   // «Теленеделя»
   Check(story.QaPaper,"«Теленеделя» lies on the coffee table (paper sheet)");
   if(story.QaPaper){yield return Fire(story,p,"Почитать «Теленеделю»",()=>story.QaPaper.position,1.0f);yield return new WaitForSeconds(.4f);
    Check(v.IsOpen,"«Теленеделя» open");if(!v.Current.loaded)absent.Add("newspaper spread Resources/"+StoryDirector.PaperSpread+".png — programme and crossword drawn in code");yield return Shot("teleweek");story.QaLeaveLeisure();yield return new WaitForSeconds(.3f);Check(!v.IsOpen&&p.control,"«Теленеделя» closed: control back");}
   if(!Resources.Load<Texture2D>(StoryDirector.PaperCover))absent.Add("newspaper cover Resources/"+StoryDirector.PaperCover+".png — procedural newsprint on the sheet");
  }
  IEnumerator Epilogue(LoopDirector loop,StoryDirector story,FlatBindings flat,ScenarioPlayer p,ScenarioTelevision tv){
   int clocks=0;Action<string> cue=id=>{if(id=="clock")clocks++;};loop.sound.CueStarted+=cue;float fov0=story.QaFov;
   story.QaEpilogue(refused);float until=Time.realtimeSinceStartup+90;int shot=0;string lastCaption="";bool screenChecked=false,albumShot=false,creditsShot=false;
   while(!loop.Finished&&Time.realtimeSinceStartup<until){
    if(story.QaEpiloguePart=="news"&&tv.caption!=lastCaption&&!string.IsNullOrEmpty(tv.caption)){lastCaption=tv.caption;yield return new WaitForSeconds(.6f);yield return Shot("epilogue-news-"+(++shot));
     if(!screenChecked){screenChecked=true;var s=Screen(tv,"epilogue-news",out var sat,out var br);result.epilogueScreen=s;Check(loop.subtitle.horizontalOverflow==UnityEngine.HorizontalWrapMode.Wrap&&loop.subtitle.preferredHeight<=loop.subtitle.rectTransform.rect.height,"long news subtitles wrap inside the safe area (height "+loop.subtitle.preferredHeight.ToString("F0")+" / "+loop.subtitle.rectTransform.rect.height.ToString("F0")+")");Check(tv.storyOn&&tv.show==ScenarioTelevision.Show.News&&br>.6f,"epilogue: «Новости Заречья» on the real CRT ("+s+")");}continue;}
    if(story.QaEpiloguePart=="credits"&&!creditsShot){creditsShot=true;yield return new WaitForSeconds(1);yield return Shot("epilogue-credits");continue;}
    if(story.QaEpiloguePart=="album"&&!albumShot){albumShot=true;yield return new WaitForSeconds(.5f);yield return Shot("epilogue-album");var v=story.QaViewer;if(v&&v.Current!=null&&!v.Current.loaded)absent.Add("epilogue album image Resources/"+StoryDirector.AlbumEpilogue+".png — paper placeholder shown");result.clockStarts=clocks;Check(clocks>0,"credits and album over the ticking clock ("+clocks+" clock starts)");continue;}
    yield return null;}
   loop.sound.CueStarted-=cue;var caps=story.QaEpilogueCaptions;result.epilogueCaptions=caps.Count;bool hasRefused=caps.Exists(c=>c.Contains("Ты не она"));
   Check(caps.Count==(refused?4:3)&&hasRefused==refused,"epilogue news: "+caps.Count+" lines, the «Ты не она!» line "+(hasRefused?"shown":"absent")+" ("+(refused?"door not opened":"door opened")+")");
   Check(loop.Finished&&Mathf.Abs(story.QaFov-fov0)<.5f&&!tv.storyOn&&loop.flat.look.disruption<.01f,"epilogue ended: story finished, FOV back, set off, no VHS left");
  }
 }
}
