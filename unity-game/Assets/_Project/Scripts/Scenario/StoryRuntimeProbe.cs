using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace Ataka.Scenario {
 // Read-only diagnostics for the explicit story-smoke run. Never moves actors or changes story state.
 public sealed class StoryRuntimeProbe:MonoBehaviour {
  string output,lastSignature;float next,lastChange;int shot;LoopDirector loop;
  static readonly FieldInfo Walking=typeof(StoryDirector).GetField("granWalking",BindingFlags.Instance|BindingFlags.NonPublic);
  [Serializable] sealed class Snapshot {
   public string utc,chapter,objective,card,scene;public string[] actions;
   public Vector3 player,grandmother;public bool playerControl,paused,grandmotherActive,grandmotherWalking;
   public float timeScale,unchangedSeconds;
  }
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){
   var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-story-smoke");if(i<0||i+1>=args.Length)return;
   var p=new GameObject("Story QA diagnostics").AddComponent<StoryRuntimeProbe>();p.output=Path.GetFullPath(args[i+1]);
  }
  string capturedScene;int captureIndex,sequence;
  static readonly float[] DeathFrames={.4f,1.1f,1.7f,2.1f,2.4f,2.6f,2.9f,3.6f,4.3f,5.1f,6.6f};
  void LateUpdate(){
   if(!loop||string.IsNullOrEmpty(output)||Application.isBatchMode)return;
   string scene=loop.scenes.PlayingScene;if(scene!=capturedScene){capturedScene=scene;captureIndex=0;sequence++;}
   if(!loop.scenes.InDeath||captureIndex>=DeathFrames.Length||loop.scenes.FrameTime<DeathFrames[captureIndex])return;
   float at=loop.scenes.FrameTime;while(captureIndex<DeathFrames.Length&&at>=DeathFrames[captureIndex])captureIndex++;
   string stamp=at.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);
   Directory.CreateDirectory(output);ScreenCapture.CaptureScreenshot(Path.Combine(output,"observed-"+sequence+"-"+scene+"-"+stamp+".png"));
  }
  void Update(){
   if(string.IsNullOrEmpty(output)||Time.unscaledTime<next)return;next=Time.unscaledTime+5;
   if(!loop)loop=FindFirstObjectByType<LoopDirector>();if(!loop||!loop.story||!loop.flat||!loop.flat.player)return;
   var story=loop.story;var player=loop.flat.player;var g=loop.flat.grandmother;
   var actions=story.OpenActs.Where(a=>!a.fired&&(a.when==null||a.when())).Select(a=>a.label).ToArray();
   string signature=story.chapter+"|"+story.Objective+"|"+story.CurrentCard+"|"+loop.scenes.PlayingScene+"|"+string.Join(";",actions);
   if(signature!=lastSignature){lastSignature=signature;lastChange=Time.unscaledTime;shot=0;}
   var state=new Snapshot{utc=DateTime.UtcNow.ToString("O"),chapter=story.chapter.ToString(),objective=story.Objective,
    card=story.CurrentCard,scene=loop.scenes.PlayingScene,actions=actions,player=player.transform.position,
    grandmother=g?g.transform.position:Vector3.zero,grandmotherActive=g&&g.activeInHierarchy,
    grandmotherWalking=Walking!=null&&(bool)Walking.GetValue(story),playerControl=player.control,paused=player.paused,
    timeScale=Time.timeScale,unchangedSeconds=Time.unscaledTime-lastChange};
   Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"runtime-status.json"),JsonUtility.ToJson(state,true));
   if(state.unchangedSeconds>=90&&shot==0&&!Application.isBatchMode){shot++;ScreenCapture.CaptureScreenshot(Path.Combine(output,"stalled-observation.png"));}
  }
 }
}
