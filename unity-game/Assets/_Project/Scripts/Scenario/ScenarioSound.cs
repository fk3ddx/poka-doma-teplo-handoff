using System;
using System.Collections.Generic;
using UnityEngine;
namespace Ataka.Scenario {
 // Recorded-source first pass. Listening/mix acceptance is separate from runtime validation.
 public sealed class ScenarioSound:MonoBehaviour {
  public Transform clock,listener;public bool paused;public float menace;
  public AudioClip phoneWarning,grandmotherKnock;public ScenarioSoundBank bank;
  public bool quiet; public bool DeathRecovery{get;private set;} public bool calibrating;AudioClip reversedPhone;LoopDirector loop;
  const int PoolSize=32;
  sealed class Voice {public AudioSource source;public AudioLowPassFilter low;public AudioHighPassFilter high;public ScenarioSoundCue cue;public Transform anchor;public bool wanted,bypassQuiet;public float baseGain,fade,occluded,obstructionTarget,tailGain=1,tailTarget=1,startedAt;}
  readonly Dictionary<string,ScenarioSoundCue> cues=new Dictionary<string,ScenarioSoundCue>();
  readonly Dictionary<ScenarioSoundCue,AnimationCurve> curves=new Dictionary<ScenarioSoundCue,AnimationCurve>();
  readonly Dictionary<string,int> lastClips=new Dictionary<string,int>();
  readonly HashSet<string> missing=new HashSet<string>();
  Voice[] voices;float obstructionTimer,worldGain=1,travel;Vector3 previousFeet;bool hadFeet;
  [Serializable] public sealed class CueDiagnostic { public string id; public int requested,started,limit,pool,replaced; }
  readonly Dictionary<string,CueDiagnostic> diagnostics=new Dictionary<string,CueDiagnostic>();
  CueDiagnostic Stats(string id){if(!diagnostics.TryGetValue(id,out var d)){d=new CueDiagnostic{id=id};diagnostics.Add(id,d);}return d;}
  public CueDiagnostic[] Diagnostics {get{var values=new List<CueDiagnostic>(diagnostics.Values);values.Sort((a,b)=>string.CompareOrdinal(a.id,b.id));return values.ToArray();}}
  public event Action<string> CueStarted;
  public int PeakVoiceCount{get;private set;}public int SuppressedOverlaps{get;private set;}
  public int ActiveVoiceCount {get{int n=0;if(voices!=null)foreach(var v in voices)if(v.cue!=null&&v.source.isPlaying)n++;return n;}}
  public float PhoneDuration=>phoneWarning?phoneWarning.length:4;
  void Awake(){
   loop=GetComponent<LoopDirector>();
   if(bank&&bank.cues!=null)foreach(var cue in bank.cues)if(cue!=null&&!string.IsNullOrEmpty(cue.id))cues[cue.id]=cue;
   voices=new Voice[PoolSize];for(int i=0;i<PoolSize;i++){
    var obj=new GameObject("Pooled sound "+i);obj.transform.SetParent(transform,false);var source=obj.AddComponent<AudioSource>();source.playOnAwake=false;source.dopplerLevel=0;source.rolloffMode=AudioRolloffMode.Custom;source.SetCustomCurve(AudioSourceCurveType.CustomRolloff,AnimationCurve.Linear(0,1,1,0));
    var low=obj.AddComponent<AudioLowPassFilter>();low.cutoffFrequency=22000;var high=obj.AddComponent<AudioHighPassFilter>();high.cutoffFrequency=10;
    voices[i]=new Voice{source=source,low=low,high=high};
   }
  }
  void Start(){if(phoneWarning){var data=new float[phoneWarning.samples*phoneWarning.channels];phoneWarning.GetData(data,0);int ch=phoneWarning.channels;for(int i=0;i<phoneWarning.samples/2;i++)for(int c=0;c<ch;c++){int a=i*ch+c,b=(phoneWarning.samples-1-i)*ch+c;float value=data[a];data[a]=data[b];data[b]=value;}reversedPhone=AudioClip.Create("Phone reversed",phoneWarning.samples,ch,phoneWarning.frequency,false);reversedPhone.SetData(data,0);}}
  public float Speak(bool reversed,Vector3 position){StopCue("phone");StopCue("phone_voice");var clip=reversed?reversedPhone:phoneWarning;if(!clip)return 0;var v=StartVoice(VoiceCue,clip,position,null,true,true);if(v!=null){v.low.cutoffFrequency=3400;v.high.cutoffFrequency=300;}return clip.length;}
  public void Grandmother(Vector3 position){if(!grandmotherKnock)return;var v=StartVoice(DoorVoiceCue,grandmotherKnock,position,null,false,false);if(v!=null)v.low.cutoffFrequency=1200;}
  readonly ScenarioSoundCue VoiceCue=new ScenarioSoundCue{id="phone_voice",bus=ScenarioBus.Voice,gainDb=-3,pitchVariation=0,spatial=false,maxInstances=1,priority=20};
  readonly ScenarioSoundCue DoorVoiceCue=new ScenarioSoundCue{id="door_voice",bus=ScenarioBus.Voice,gainDb=-6,pitchVariation=0,maxInstances=1,priority=20,minDistance=1,maxDistance=10};
  public void Play(string id,Vector3 position,Transform anchor=null){PlayInternal(id,position,anchor,false);}
  public void PlayClose(string id,Vector3 position){PlayInternal(id,position,null,true);}
  public void PlayReceiver(string id){var v=PlayInternal(id,listener.position,null,true);if(v!=null){v.low.cutoffFrequency=3400;v.high.cutoffFrequency=300;}}
  Voice PlayInternal(string id,Vector3 position,Transform anchor,bool close){
   if(!cues.TryGetValue(id,out var cue)||cue.clips==null||cue.clips.Length==0){if(missing.Add(id))Debug.LogWarning("Missing recorded sound: "+id);return null;}
   if(quiet&&!close)return null;int index=ChooseClip(cue);return StartVoice(cue,cue.clips[index],position,anchor,close,close);
  }
  int ChooseClip(ScenarioSoundCue cue){int n=cue.clips.Length,index=UnityEngine.Random.Range(0,n);if(n>1&&lastClips.TryGetValue(cue.id,out var last)&&index==last)index=(index+1+UnityEngine.Random.Range(0,n-1))%n;lastClips[cue.id]=index;return index;}
  Voice StartVoice(ScenarioSoundCue cue,AudioClip clip,Vector3 position,Transform anchor,bool force2D,bool bypassQuiet){
   if(!clip||voices==null)return null;var stats=Stats(cue.id);stats.requested++;int count=0;Voice available=null,oldest=null;
   if(cue.id=="chime")foreach(var v in voices)if(v.cue==cue&&v.source.isPlaying)v.tailTarget*=.398107f;
   foreach(var v in voices){if(v.cue==cue&&v.source.isPlaying){count++;if(oldest==null||v.startedAt<oldest.startedAt)oldest=v;}if(available==null&&(v.cue==null||!v.source.isPlaying))available=v;}
   if(count>=cue.maxInstances){if(cue.id=="chime"&&oldest!=null){available=oldest;stats.replaced++;}else{SuppressedOverlaps++;stats.limit++;return null;}}
   // Never steal an active voice/clock tail for a lower-priority event.
   if(available==null){SuppressedOverlaps++;stats.pool++;return null;}
   var voice=available;var s=voice.source;voice.cue=cue;voice.anchor=anchor;voice.bypassQuiet=bypassQuiet;voice.wanted=true;voice.fade=cue.loop?0:1;voice.occluded=0;voice.obstructionTarget=0;voice.baseGain=Mathf.Pow(10,cue.gainDb/20);voice.tailGain=voice.tailTarget=1;voice.startedAt=Time.time;stats.started++;
   s.Stop();s.transform.position=position;s.clip=clip;s.loop=cue.loop;s.spatialBlend=force2D||!cue.spatial?0:1;s.minDistance=cue.minDistance;s.maxDistance=cue.maxDistance;if(!curves.TryGetValue(cue,out var curve)){curve=new AnimationCurve(new Keyframe(0,1),new Keyframe(cue.minDistance/cue.maxDistance,1),new Keyframe(.5f,.25f),new Keyframe(1,0));curves.Add(cue,curve);}s.SetCustomCurve(AudioSourceCurveType.CustomRolloff,curve);s.priority=cue.priority;s.pitch=1+UnityEngine.Random.Range(-cue.pitchVariation,cue.pitchVariation);s.volume=0;s.ignoreListenerPause=false;
   s.outputAudioMixerGroup=bank&&bank.groups!=null&&bank.groups.Length>(int)cue.bus?bank.groups[(int)cue.bus]:null;
   voice.low.cutoffFrequency=22000;voice.high.cutoffFrequency=10;s.Play();CueStarted?.Invoke(cue.id);if(cue.loop)s.time=UnityEngine.Random.Range(0,Mathf.Max(0,clip.length-.1f));return voice;
  }
  void EnsureLoop(string id,Transform anchor){
   foreach(var v in voices)if(v.cue!=null&&v.cue.id==id&&v.anchor==anchor&&v.source.isPlaying){v.wanted=true;return;}
   if(!cues.TryGetValue(id,out var cue)||cue.clips.Length==0)return;StartVoice(cue,cue.clips[ChooseClip(cue)],anchor?anchor.position:listener.position,anchor,false,false);
  }
  public void StopCue(string id){if(voices==null)return;foreach(var v in voices)if(v.cue!=null&&v.cue.id==id){v.source.Stop();v.cue=null;}}
  // Scene-owned one-shots must not resume when its envelope is released.
  public void BeginDeathRecovery(){DeathRecovery=true;foreach(var id in new[]{"breath","palms","finger_crack","choked_cry","wet_tear"})StopCue(id);}
  public void EndDeathRecovery(){DeathRecovery=false;quiet=false;}
  public void ResetWorld(){DeathRecovery=false;if(voices!=null)foreach(var v in voices){v.source.Stop();v.cue=null;}quiet=false;worldGain=1;travel=0;hadFeet=false;}
  public void ShiftWorld(Vector3 delta){if(voices!=null)foreach(var v in voices)if(v.cue!=null&&!v.anchor&&v.source.spatialBlend>0)v.source.transform.position+=delta;}
  void Update(){
   if(paused||!loop||!bank||!listener)return;
   if(!calibrating)foreach(var v in voices)if(v.cue!=null&&v.cue.loop)v.wanted=false;
   bool chase=loop.chase&&loop.chase.Active;bool death=loop.scenes.PlayingScene=="DeathShort";bool hanging=loop.scenes.PlayingScene=="HangingVision";float sceneTime=loop.scenes.FrameTime;
   if(!calibrating&&!loop.Finished&&!death&&!DeathRecovery){
    if(chase){foreach(var module in loop.chase.pool)if(module.lamp.intensity>0&&(!loop.chase.Ending||module==loop.chase.Current))EnsureLoop("clock",module.clock.transform);if(!loop.chase.Ending){EnsureLoop("player_breath",listener);EnsureLoop("guest_breath",loop.flat.guest.transform);EnsureLoop("heartbeat",listener);}}
    else if(clock&&!hanging)EnsureLoop("clock",clock);
    if(!chase&&!hanging&&(loop.Progress.Lap==2||loop.Progress.Lap==3))EnsureLoop("wind",loop.flat.balcony);
    if(hanging&&sceneTime<4.9f)EnsureLoop("rope",loop.flat.grandmother.transform);
   }
   worldGain=Mathf.MoveTowards(worldGain,quiet?0:1,Time.deltaTime/(quiet?.05f:.3f));obstructionTimer-=Time.deltaTime;bool testObstruction=obstructionTimer<=0;if(testObstruction)obstructionTimer=.1f;
   foreach(var v in voices){
    if(v.cue==null)continue;var s=v.source;if(!s.isPlaying){v.cue=null;continue;}if(v.anchor)s.transform.position=v.anchor.position;
    if(testObstruction&&s.spatialBlend>0){var d=s.transform.position-listener.position;float length=d.magnitude;v.obstructionTarget=!calibrating&&!(v.cue.id=="chime"&&length<2)&&length>.25f&&Physics.Raycast(listener.position,d.normalized,out var hit,length-.15f,~(1<<31),QueryTriggerInteraction.Ignore)&&(!v.anchor||!hit.transform.IsChildOf(v.anchor))?1:0;}
    v.occluded=Mathf.MoveTowards(v.occluded,v.obstructionTarget,Time.deltaTime/.15f);
    if(!v.cue.id.StartsWith("phone_",StringComparison.Ordinal)&&v.cue.id!="door_voice")v.low.cutoffFrequency=Mathf.Lerp(v.low.cutoffFrequency,v.occluded>0?1800:22000,1-Mathf.Exp(-Time.deltaTime/ .15f));
    v.fade=Mathf.MoveTowards(v.fade,v.cue.loop&&!v.wanted?0:1,Time.deltaTime/.2f);
    v.tailGain=Mathf.MoveTowards(v.tailGain,v.tailTarget,Time.deltaTime/.15f);float gain=v.bypassQuiet?1:worldGain;float sceneGain=1;
    // The short death is silent from black (2.5s); the hanging vision from disappearance (5s).
    // Explicit foreground selection preserves the later latch without globally dropping Play calls.
    if(death)sceneGain=Mathf.Clamp01((2.5f-sceneTime)/.1f);
    if(hanging){bool foreground=v.cue.id=="rope"||v.cue.id=="breath"||v.cue.id=="tinnitus";sceneGain=foreground?Mathf.Clamp01((5-sceneTime)/.1f):v.cue.id=="latch"&&sceneTime>=6?1:Mathf.Clamp01(1-sceneTime/.05f);}
    if(DeathRecovery)sceneGain=0;
    s.volume=v.baseGain*v.fade*gain*v.tailGain*Mathf.Lerp(1,.5f,v.occluded)*sceneGain;
    if(v.cue.loop&&!v.wanted&&v.fade<=0){s.Stop();v.cue=null;}
   }
   PeakVoiceCount=Mathf.Max(PeakVoiceCount,ActiveVoiceCount);if(!calibrating)Steps();
  }
  void Steps(){var p=loop.flat.player;var feet=p.transform.position;if(!hadFeet){previousFeet=feet;hadFeet=true;return;}var d=feet-previousFeet;previousFeet=feet;d.y=0;if(!p.control||p.ActualSpeed<.1f||d.magnitude>1){travel=0;return;}travel+=d.magnitude;float stride=p.crouched?.7f:p.ActualSpeed>3?1.8f:.85f;if(travel<stride)return;travel-=stride;string id="step";
   if(Physics.Raycast(feet+Vector3.up*.25f,Vector3.down,out var hit,.6f,~(1<<31),QueryTriggerInteraction.Ignore)){string name=hit.collider.name.ToLowerInvariant();if(name.Contains("rug")||name.Contains("carpet"))id="carpet_step";}
   var voice=PlayInternal(id,feet,null,false);if(voice!=null)voice.baseGain*=p.crouched?.35f:p.ActualSpeed>3?1:.7f;
  }
  void OnDestroy(){if(reversedPhone)Destroy(reversedPhone);}
 }
}