using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Ataka.Scenario {
 // QA only (~3 min):  -evening-qa <dir>
 // 4.7 free time from 21:00 with a watching player walked to each seed (mirror, hall clock, kitchen radio, bedroom
 // lamp), 4.8 the news on the «Рубин» screen, then the 22:00 broadcast of night 1 and of night 2 (test card → blue
 // announcer screen with his words). The screen content is checked on the TV's own render texture (pixels), not only
 // by flags. PNGs + checks.txt (PASS/FAIL/ABSENT) + result.json; exit 0 = no FAIL. Sound acceptance stays with people.
 public sealed class EveningQA:MonoBehaviour {
  string output;readonly List<string> checks=new List<string>();int errors,failures;
  [Serializable] class Result {public bool passed;public int failures,errors,seeds;public string[] seedOrder;public float clockLagSeconds,lampBefore,lampAfter,newsWarmSeconds;public string testCard,news,announcer,announcer2;}
  readonly Result result=new Result();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-evening-qa");if(i<0||i+1>=args.Length)return;
   new GameObject("Evening QA").AddComponent<EveningQA>().output=Path.GetFullPath(args[i+1]);}
  void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;checks.Add("ERROR "+message.Split('\n')[0]);}}
  void Check(bool ok,string what){checks.Add((ok?"PASS ":"FAIL ")+what);if(!ok)failures++;File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();if(!Application.isBatchMode)ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));checks.Add("SHOT "+name+".png");}
  static void Aim(ScenarioPlayer p,Vector3 at){var d=at-p.head.position;var f=d;f.y=0;if(f.sqrMagnitude<1e-4f)return;p.transform.rotation=Quaternion.LookRotation(f);p.pitch=Mathf.Clamp(-Mathf.Atan2(d.y,f.magnitude)*Mathf.Rad2Deg,-80,80);p.head.localRotation=Quaternion.Euler(p.pitch,0,0);}
  // The TV picture as rendered: mean colour, brightest luma, share of saturated pixels; also saved as a PNG.
  string Feed(ScenarioTelevision tv,string name,out Color mean,out float maxLuma,out float saturated){
   mean=Color.black;maxLuma=0;saturated=0;var rt=tv.QaFeed;if(!rt)return "no feed";var was=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();RenderTexture.active=was;
   var px=tex.GetPixels();float r=0,g=0,b=0;int sat=0;foreach(var c in px){r+=c.r;g+=c.g;b+=c.b;float l=.2126f*c.r+.7152f*c.g+.0722f*c.b;maxLuma=Mathf.Max(maxLuma,l);if(Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))>.5f)sat++;}
   mean=new Color(r/px.Length,g/px.Length,b/px.Length);saturated=sat/(float)px.Length;
   if(!Application.isBatchMode)File.WriteAllBytes(Path.Combine(output,"screen-"+name+".png"),tex.EncodeToPNG());Destroy(tex);
   return "mean "+mean.r.ToString("F2")+"/"+mean.g.ToString("F2")+"/"+mean.b.ToString("F2")+", brightest "+maxLuma.ToString("F2")+", saturated "+(saturated*100).ToString("F1")+"%";}
  IEnumerator Start(){
   Directory.CreateDirectory(output);Application.logMessageReceived+=Log;Application.runInBackground=true;Application.targetFrameRate=60;
   yield return new WaitForSecondsRealtime(2);
   var loop=FindFirstObjectByType<LoopDirector>();var story=loop.story;var flat=loop.flat;var p=flat.player;var sound=loop.sound;var tv=FindFirstObjectByType<ScenarioTelevision>();
   loop.testing=true;p.automating=true;loop.Pause(false);
   foreach(var a in new[]{"4.7 the album (6 spreads), the Dendy mini-game, «Теленеделя», the radio programme and weather report","the kitchen «Янтарь» clock: none in the flat; the bedside alarm clock shows the true time instead","the announcer himself on screen (and his look into the camera): no model; the blue screen carries his words","the news titles' own jingle: no recording"})checks.Add("ABSENT "+a);
   int hiss=0;Action<string> cue=id=>{if(id=="radio_hiss")hiss++;};sound.CueStarted+=cue;
   story.QaFreeTimeAndNews();yield return new WaitForSeconds(1);
   Check(story.QaInFreeTime&&flat.look.icon&&flat.look.icon.intensity<.01f,"4.7 free time started at 21:00, the icon lamp still unlit");
   // 1 mirror
   var mirror=story.QaMirror;Check(mirror,"the dresser mirror found");
   if(mirror){var c=mirror.bounds.center;p.Place(new Vector3(c.x+.85f,.02f,c.z),270);yield return null;Aim(p,c);
    float until=Time.time+5;while(Time.time<until&&!story.QaSeeds.Exists(s=>s.seed=="mirror"))yield return null;
    bool seen=story.QaSeeds.Exists(s=>s.seed=="mirror");Check(seen,"seed 1 mirror fired when he looked into the mirror");
    if(seen){yield return Shot("seed-1-mirror");var cam=p.view?p.view:Camera.main;Check(cam&&(cam.cullingMask&(1<<31))==0,"the figure is on the TV-only layer 31: the player's camera cannot see it directly (only the probe reflection)");
     checks.Add("NOTE figure at "+story.QaMirrorFigure.ToString("F2")+"; the accepted mirror is rough probe-128 metal — judge the smear on the PNG");
     yield return new WaitForSeconds(.4f);Aim(p,story.QaMirrorFigure+Vector3.up*1.2f);yield return Shot("seed-1-mirror-turned");Check(!flat.guest.activeSelf,"turned round: nobody there");}}
   // 2 clock
   yield return new WaitForSeconds(6.2f);p.Place(new Vector3(ScenarioLayout.Clock.x,.02f,ScenarioLayout.Clock.z-1.15f),0);yield return null;Aim(p,ScenarioLayout.Clock+Vector3.up*.6f);
   {float until=Time.time+3;while(Time.time<until&&!story.QaSeeds.Exists(s=>s.seed=="clock"))yield return null;}
   var bed=story.QaBedsideClock;result.clockLagSeconds=bed?bed.seconds-flat.clock.seconds:0;
   Check(story.QaSeeds.Exists(s=>s.seed=="clock")&&bed&&Mathf.Abs(result.clockLagSeconds-StoryDirector.ClockLag)<2,"seed 2 the hall clock is "+(result.clockLagSeconds/60).ToString("F1")+" min behind the bedside clock");
   yield return Shot("seed-2-clock");
   // 3 radio
   yield return new WaitForSeconds(6.2f);p.Place(new Vector3(7.5f,.02f,1.9f),90);
   {float until=Time.time+3;while(Time.time<until&&!story.QaSeeds.Exists(s=>s.seed=="radio"))yield return null;}
   yield return null;Check(story.QaSeeds.Exists(s=>s.seed=="radio")&&hiss>0,"seed 3 the switched-off radio point hisses in the kitchen ("+hiss+" radio_hiss)");
   // 4 lamp
   yield return new WaitForSeconds(6.2f);var icon=flat.look.icon;
   if(icon){p.Place(new Vector3(1.45f,.02f,2.3f),0);yield return null;Aim(p,icon.transform.position);result.lampBefore=icon.intensity;yield return Shot("seed-4-lamp-before");
    float until=Time.time+3;while(Time.time<until&&!story.QaSeeds.Exists(s=>s.seed=="lamp"))yield return null;yield return new WaitForSeconds(1);result.lampAfter=icon.intensity;
    Check(story.QaSeeds.Exists(s=>s.seed=="lamp")&&result.lampBefore<.01f&&result.lampAfter>.13f,"seed 4 the icon lamp lit itself while he looked ("+result.lampBefore.ToString("F2")+" → "+result.lampAfter.ToString("F2")+")");yield return Shot("seed-4-lamp-after");}
   var order=story.QaSeeds.ConvertAll(s=>s.seed);result.seedOrder=order.ToArray();result.seeds=order.Count;
   Check(order.Count==4&&new HashSet<string>(order).Count==4,"four seeds, each once: "+string.Join(", ",order));
   bool spaced=true;for(int i=1;i<story.QaSeeds.Count;i++)if(story.QaSeeds[i].at-story.QaSeeds[i-1].at<5.5f)spaced=false;Check(spaced,"one at a time (≥ 6 s apart)");
   // 4.8 the news: switch the «Рубин» on through its real action
   var tvAt=flat.televisionScreen?flat.televisionScreen.bounds.center:flat.television.position;
   {float until=Time.time+90;StoryDirector.Act act=null;while(Time.time<until&&act==null){act=story.OpenActs.Find(a=>a.label=="Включить «Рубин»"&&!a.fired);if(act==null)yield return null;}
    Check(act!=null,"4.8 the news action is offered after the free time");
    if(act!=null){p.Place(new Vector3(tvAt.x-1.5f,.02f,tvAt.z),90);yield return null;Aim(p,tvAt);for(int i=0;i<30&&story.Aimed!=act;i++)yield return null;story.QaFire(act);}}
   {float t0=Time.time;while(tv&&tv.show!=ScenarioTelevision.Show.News&&Time.time-t0<3)yield return null;t0=Time.time;yield return Shot("news-0.2-warming");while(tv&&tv.Brightness<.9f&&Time.time-t0<4)yield return null;result.newsWarmSeconds=Time.time-t0;
    Check(tv&&tv.show==ScenarioTelevision.Show.News&&result.newsWarmSeconds>.8f&&result.newsWarmSeconds<2f,"the picture comes up as the tube warms ("+result.newsWarmSeconds.ToString("F2")+" s)");}
   yield return new WaitForSeconds(1.5f);yield return Shot("news-titles");
   result.news=Feed(tv,"news",out var nm,out var nl,out var ns);Check(tv.QaFeedOn&&tv.QaFeedMask==1<<31&&nl>.6f&&(nm.r+nm.g+nm.b)/3<.4f,"«Новости Заречья» on the screen: light text on a dark field ("+result.news+")");
   {float until=Time.time+40;while(Time.time<until&&!story.QaNewsDone)yield return null;}
   Check(story.QaNewsDone&&!tv.storyOn&&tv.show==ScenarioTelevision.Show.None&&flat.look.icon.intensity>.13f,"the news ends: set off, lamp lit");
   // 22:00 night 1 and night 2: test card → announcer
   foreach(int night in new[]{1,2}){
    story.QaNightEvents(night);p.Place(new Vector3(tvAt.x-1.6f,.02f,tvAt.z),90);yield return null;Aim(p,tvAt);
    {float until=Time.time+15;while(Time.time<until&&tv.show!=ScenarioTelevision.Show.TestCard)yield return null;}
    yield return new WaitForSeconds(1.5f);yield return Shot("2200-night"+night+"-testcard");
    var card=Feed(tv,"testcard-night"+night,out var cm,out var cl,out var cs);if(night==1)result.testCard=card;Check(tv.show==ScenarioTelevision.Show.TestCard&&cs>.05f&&cl>.8f,"night "+night+" 22:00 test card on the screen with colour bars ("+card+")");
    {float until=Time.time+6;while(Time.time<until&&tv.show!=ScenarioTelevision.Show.Announcer)yield return null;}
    yield return new WaitForSeconds(1.2f);yield return Shot("2200-night"+night+"-announcer");
    var an=Feed(tv,"announcer-night"+night,out var am,out var al,out var asat);if(night==1)result.announcer=an;else result.announcer2=an;
    Check(tv.show==ScenarioTelevision.Show.Announcer&&am.b>am.r+.2f&&al>.7f,"night "+night+" announcer: blue screen with his words on it ("+an+", caption «"+tv.caption+"»)");
    if(night==2){float until=Time.time+20;while(Time.time<until&&tv.caption!="Ты же помнишь, Миша?")yield return null;Check(tv.caption=="Ты же помнишь, Миша?","night 2 adds «Ты же помнишь, Миша?» on the screen");yield return new WaitForSeconds(.6f);yield return Shot("2200-night2-remember");}
    {float until=Time.time+12;while(Time.time<until&&tv.storyOn)yield return null;}
    Check(!tv.storyOn&&tv.show==ScenarioTelevision.Show.None&&!tv.QaFeedOn,"night "+night+" the screen goes out and the feed camera is handed back");}
   sound.CueStarted-=cue;
   Check(errors==0,"no runtime errors ("+errors+")");
   result.errors=errors;result.failures=failures;result.passed=failures==0&&errors==0;File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(result,true));
   File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);yield return new WaitForSecondsRealtime(.5f);Application.Quit(result.passed?0:1);
  }
 }
}
