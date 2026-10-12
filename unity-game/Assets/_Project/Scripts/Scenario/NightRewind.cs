using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Ataka.Scenario {
 // SCENARIO 6.1 — the single rewind of the game, 8–10 s, from the actual first night played backwards.
 // Recording: night 1 from 21:42 to the 22:07 death at 10 Hz into a preallocated ring buffer (camera, door leaves,
 // light intensities, the "Rubin", the Guest) plus the one-shot sounds that played, and four evening memories taken at
 // the real evening moments (tea with jam, the belt in the wardrobe, her "tap-tap… tap", the bulb).
 // Playback: 1 s black silence → the night in reverse with reversed sound and ticking that grows → evening flashes →
 // white flash → a frozen VHS pause frame with jitter → PLAY into the start of night 2.
 // Lights are only re-dimmed (never enabled/disabled); doors are driven through their leaves and handed back;
 // Cleanup() leaves no state behind on a normal end or when the story is cancelled mid-rewind.
 public sealed class NightRewind:MonoBehaviour {
  public LoopDirector loop;
  public const float Rate=10;public const int Capacity=4000; // 400 s of night at 10 Hz
  public const float Black=1f,Reverse=5.2f,Evening=1.6f,Flash=.2f,Freeze=1f,PlayTag=.6f;
  public static float Duration=>Black+Reverse+Evening+Flash+Freeze+PlayTag;
  FlatBindings flat=>loop.flat;ScenarioSound sound=>loop.sound;ScenarioTimeline scenes=>loop.scenes;
  ScenarioDoor[] doors;Light[] lights;ScenarioTelevision tv;int stride;float[] data;int count,first;float nextSample,startedAt;
  struct Cue {public AudioClip clip;public float t;public string id;}
  readonly List<Cue> cues=new List<Cue>();const int CueCapacity=256;
  public sealed class Memory {public string id,cue;public Vector3 feet;public float yaw,pitch;public bool gran;public Vector3 granAt;public Quaternion granRot;public float[] leaves,light;}
  readonly List<Memory> memories=new List<Memory>();
  public bool Recording{get;private set;}public bool Active{get;private set;}public float Time01{get;private set;}public float Clock{get;private set;}public string Phase{get;private set;}="";
  public int Samples=>count;public int CuesRecorded=>cues.Count;public int MemoriesRecorded=>memories.Count;
  public int BytesReserved=>(data!=null?data.Length*4:0)+CueCapacity*16;
  public int BytesUsed=>count*stride*4+cues.Count*16;public int FloatsPerSample=>stride;
  // QA journal of one playback: what was shown/heard at which rewind time, with the ORIGINAL night time it came from.
  public struct Played {public float clock,source;public string what;}
  public readonly List<Played> Journal=new List<Played>();const int JournalCapacity=600;
  public float LastActiveSeconds{get;private set;}
  void Note(float clock,float source,string what){if(Journal.Count<JournalCapacity)Journal.Add(new Played{clock=clock,source=source,what=what});}
  string osd="";float black,white,band;bool[] scriptedBefore,lockedBefore,openBefore;float[] lightsBefore,leavesBefore,doorAngleBefore;bool restored;

  void Setup(){
   if(data!=null)return;
   var d=new List<ScenarioDoor>();foreach(var x in new[]{flat.entrance,flat.kitchen,flat.bath,flat.bedroom,flat.living,flat.balconyDoor})if(x&&x.leaf)d.Add(x);doors=d.ToArray();
   var l=new List<Light>();void Add(Light x){if(x&&!l.Contains(x))l.Add(x);}foreach(var x in flat.look.warm)Add(x);foreach(var x in flat.look.cold)Add(x);Add(flat.look.icon);Add(flat.televisionLight);Add(scenes.landingLight);lights=l.ToArray();
   tv=FindFirstObjectByType<ScenarioTelevision>();
   stride=6+doors.Length+lights.Length+7;data=new float[Capacity*stride];
  }
  // ---------- recording ----------
  public void Begin(){Setup();count=0;first=0;cues.Clear();Recording=true;nextSample=0;startedAt=Time.time;sound.VoiceClip-=OnVoice;sound.VoiceClip+=OnVoice;}
  public void Stop(){Recording=false;if(sound)sound.VoiceClip-=OnVoice;}
  public void ForgetEvening(){memories.Clear();}
  void OnVoice(string id,AudioClip clip){if(!Recording||!clip||id=="step"||id=="carpet_step"||id=="rewind")return; // footsteps would crowd out the night's events
   if(cues.Count>=CueCapacity)cues.RemoveAt(0);cues.Add(new Cue{clip=clip,t=Time.time-startedAt,id=id});}
  void Update(){
   if(!Recording||flat.player.paused||Time.time<nextSample)return;nextSample=Time.time+1/Rate;
   int slot;if(count<Capacity){slot=(first+count)%Capacity;count++;}else{slot=first;first=(first+1)%Capacity;}
   Write(slot*stride);
  }
  void Write(int o){
   var p=flat.player;var at=p.transform.position;data[o]=at.x;data[o+1]=at.y;data[o+2]=at.z;data[o+3]=p.transform.eulerAngles.y;data[o+4]=Mathf.DeltaAngle(0,p.head.localEulerAngles.x);data[o+5]=Time.time-startedAt;o+=6;
   foreach(var d in doors)data[o++]=Mathf.DeltaAngle(0,d.leaf.localEulerAngles.y);
   foreach(var l in lights)data[o++]=l.intensity;
   var g=flat.guest.transform;data[o++]=tv&&tv.storyOn?1:0;data[o++]=flat.guest.activeSelf?1:0;data[o++]=g.position.x;data[o++]=g.position.y;data[o++]=g.position.z;data[o++]=g.eulerAngles.y;data[o++]=g.up.y;
  }
  // An evening moment, taken where it really happens (StoryDirector) — or staged by RewindQA.
  public void Remember(string id,string cue){
   Setup();memories.RemoveAll(m=>m.id==id);var p=flat.player;var gr=flat.grandmother;
   var m=new Memory{id=id,cue=cue,feet=p.transform.position,yaw=p.transform.eulerAngles.y,pitch=Mathf.DeltaAngle(0,p.head.localEulerAngles.x),gran=gr.activeSelf,granAt=gr.transform.position,granRot=gr.transform.rotation,leaves=new float[flat.wardrobeLeaves.Length],light=new float[lights.Length]};
   for(int i=0;i<m.leaves.Length;i++)m.leaves[i]=Mathf.DeltaAngle(0,flat.wardrobeLeaves[i].localEulerAngles.y);for(int i=0;i<lights.Length;i++)m.light[i]=lights[i].intensity;
   memories.Add(m);
  }
  // ---------- playback ----------
  float S(int sample,int k)=>data[((first+sample)%Capacity)*stride+k];
  Action<bool> freeze;
  public IEnumerator Play(Action prepareNight2,Action<bool> freezeGrandmother){
   Setup();Stop();Active=true;freeze=freezeGrandmother;Phase="black";var p=flat.player;var body=p.GetComponent<CharacterController>();
   Journal.Clear();LastActiveSeconds=0;
   // ScenarioDoor.Advance returns while scripted, so the replayed leaf poses are not overwritten by the door's own Update.
   scriptedBefore=new bool[doors.Length];lockedBefore=new bool[doors.Length];openBefore=new bool[doors.Length];doorAngleBefore=new float[doors.Length];
   for(int i=0;i<doors.Length;i++){var d=doors[i];scriptedBefore[i]=d.scripted;lockedBefore[i]=d.locked;openBefore[i]=d.open;doorAngleBefore[i]=Mathf.DeltaAngle(0,d.leaf.localEulerAngles.y);d.scripted=true;}
   lightsBefore=new float[lights.Length];for(int i=0;i<lights.Length;i++)lightsBefore[i]=lights[i].intensity;
   restored=false;if(body)body.enabled=false;p.control=false;sound.quiet=true;loop.prompt.text="";
   leavesBefore=new float[flat.wardrobeLeaves.Length];for(int i=0;i<leavesBefore.Length;i++)leavesBefore[i]=Mathf.DeltaAngle(0,flat.wardrobeLeaves[i].localEulerAngles.y);
   float t=0,total=Duration;bool guestHanging=false;int lastSample=count;var order=memories.ToArray();Array.Reverse(order); // backwards in time: the knock, the belt, the bulb, tea with jam
   int shownMemory=-1;bool prepared=false;
   while(t<total){
    Clock=t;Time01=t/total;
    if(t<Black){Phase="black";black=1;osd="";}
    else if(t<Black+Reverse){
     Phase="reverse";float u=(t-Black)/Reverse;black=Mathf.Clamp01(1-(t-Black)/.15f);osd="◄◄";band=u;flat.look.disruption=.55f+.25f*Mathf.PerlinNoise(t*7,.3f);
     sound.RewindTick(Mathf.Lerp(.15f,1,u));
     if(count>1){
      float f=(1-u)*(count-1);int a=Mathf.FloorToInt(f);int b=Mathf.Min(count-1,a+1);float w=f-a;
      ApplySample(a,b,w,ref guestHanging);
      // sounds of the samples just passed, played backwards
      float tA=S(a,5),tB=lastSample<count?S(lastSample,5):float.MaxValue;
      for(int i=cues.Count-1;i>=0;i--)if(cues[i].t>=tA&&cues[i].t<tB){sound.PlayReversed(cues[i].clip,1.6f);Note(t,cues[i].t,"sound "+cues[i].id+" ("+cues[i].clip.name+")");}
      if(a!=lastSample)Note(t,tA,"frame "+a);
      lastSample=a;
     }
     flat.clock.Show(Mathf.Lerp(22*3600+7*60,21*3600+42*60,u));
    }
    else if(t<Black+Reverse+Evening){
     Phase="evening";float e=(t-Black-Reverse)/Evening;sound.RewindTick(1);flat.look.disruption=.75f+.2f*Mathf.PerlinNoise(t*9,.7f);band=1-e;
     if(order.Length>0){int k=Mathf.Min(order.Length-1,Mathf.FloorToInt(e*order.Length));if(k!=shownMemory){shownMemory=k;ApplyMemory(order[k]);Note(t,-1,"evening "+order[k].id);var clip=sound.FirstClip(order[k].cue);if(clip)sound.PlayReversed(clip,1.4f);}}
     flat.clock.Show(Mathf.Lerp(21*3600+42*60,21*3600+40*60,e));
    }
    else if(t<Black+Reverse+Evening+Flash){
     Phase="flash";white=1-(t-Black-Reverse-Evening)/Flash;sound.RewindTick(0);
     if(!prepared){prepared=true;Restore();prepareNight2?.Invoke();SnapDoors();p.control=false;if(body)body.enabled=false;freezeGrandmother?.Invoke(true);flat.look.disruption=.35f;}
    }
    else if(t<total-PlayTag){Phase="freeze";white=0;osd="II";band=0;var j=Mathf.Sin(t*71)*.25f;p.head.localRotation=Quaternion.Euler(j,Mathf.Sin(t*53)*.2f,0);flat.look.disruption=.3f+.05f*Mathf.Sin(t*40);}
    else{Phase="play";osd="► PLAY";p.head.localRotation=Quaternion.identity;freezeGrandmother?.Invoke(false);flat.look.disruption=Mathf.Lerp(.3f,0,(t-(total-PlayTag))/PlayTag);}
    yield return null;t+=Time.deltaTime;LastActiveSeconds=t; // game time only: Time.deltaTime is 0 while the game is paused
   }
   if(!prepared){Restore();prepareNight2?.Invoke();SnapDoors();}
   Cleanup();
  }
  void ApplySample(int a,int b,float w,ref bool guestHanging){
   var p=flat.player;var at=Vector3.Lerp(new Vector3(S(a,0),S(a,1),S(a,2)),new Vector3(S(b,0),S(b,1),S(b,2)),w);
   p.transform.SetPositionAndRotation(at,Quaternion.Euler(0,Mathf.LerpAngle(S(a,3),S(b,3),w),0));p.head.localRotation=Quaternion.Euler(Mathf.Lerp(S(a,4),S(b,4),w),0,0);
   int o=6;foreach(var d in doors){d.leaf.localRotation=Quaternion.Euler(0,Mathf.LerpAngle(S(a,o),S(b,o),w),0);o++;}
   foreach(var l in lights){l.intensity=Mathf.Lerp(S(a,o),S(b,o),w);o++;}
   if(tv)tv.storyOn=S(a,o)>.5f;o++;
   bool guest=S(a,o)>.5f;o++;var g=flat.guest;
   if(guest){
    var gp=new Vector3(S(a,o),S(a,o+1),S(a,o+2));bool hanging=S(a,o+4)<0;
    if(hanging){if(!guestHanging||!g.activeSelf)scenes.BalconyWaiting(0);guestHanging=true;}
    else{guestHanging=false;g.SetActive(true);g.transform.rotation=Quaternion.Euler(0,S(a,o+3),0);var pose=g.GetComponent<ProxyPose>();if(pose)pose.Crawl(new Vector3(gp.x,0,gp.z));}
   }else if(g.activeSelf){g.SetActive(false);guestHanging=false;}
  }
  void ApplyMemory(Memory m){
   var p=flat.player;p.transform.SetPositionAndRotation(m.feet,Quaternion.Euler(0,m.yaw,0));p.head.localRotation=Quaternion.Euler(m.pitch,0,0);
   flat.guest.SetActive(false);var gr=flat.grandmother;gr.SetActive(m.gran);if(m.gran)gr.transform.SetPositionAndRotation(m.granAt,m.granRot);
   for(int i=0;i<m.leaves.Length&&i<flat.wardrobeLeaves.Length;i++)flat.wardrobeLeaves[i].localRotation=Quaternion.Euler(0,m.leaves[i],0);
   for(int i=0;i<m.light.Length&&i<lights.Length;i++)lights[i].intensity=m.light[i];
   if(tv)tv.storyOn=false;
  }
  // Back to what the night left (intensities, wardrobe, door ownership) before the story sets night 2 up.
  void Restore(){
   restored=true;scenes.Cancel();
   if(lightsBefore!=null)for(int i=0;i<lights.Length;i++)lights[i].intensity=lightsBefore[i];
   if(leavesBefore!=null)for(int i=0;i<leavesBefore.Length&&i<flat.wardrobeLeaves.Length;i++)flat.wardrobeLeaves[i].localRotation=Quaternion.Euler(0,leavesBefore[i],0);
   if(scriptedBefore!=null)for(int i=0;i<doors.Length;i++){var d=doors[i];d.scripted=scriptedBefore[i];d.locked=lockedBefore[i];d.open=openBefore[i];d.leaf.localRotation=Quaternion.Euler(0,doorAngleBefore[i],0);}
   Physics.SyncTransforms();
   if(tv)tv.storyOn=false;
  }
  // Leaves exactly where ScenarioDoor would settle for the open/closed state night 2 asked for.
  void SnapDoors(){foreach(var d in doors){d.leaf.localRotation=Quaternion.Euler(0,d.open?d.direction*95:d.direction*d.closedAngle,0);}Physics.SyncTransforms();}
  public void Cleanup(){
   if(!Active&&!Recording)return;
   if(Active){
    if(!restored)Restore(); // cancelled mid-playback: put back the night's own lights, wardrobe, TV and door ownership
    freeze?.Invoke(false);
    var p=flat.player;var body=p.GetComponent<CharacterController>();if(body)body.enabled=true;p.control=true;p.head.localRotation=Quaternion.Euler(p.pitch,0,0);
    sound.RewindTick(0);sound.quiet=false;flat.look.disruption=0;
   }
   Stop();Active=false;freeze=null;Phase="";osd="";black=white=band=0;scriptedBefore=lockedBefore=openBefore=null;lightsBefore=leavesBefore=doorAngleBefore=null;
  }
  Texture2D noise;
  void OnGUI(){
   if(!Active)return;GUI.depth=-10;float w=Screen.width,h=Screen.height;
   if(black>0){GUI.color=new Color(0,0,0,black);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);}
   if(band>0&&black<1){
    if(!noise){noise=new Texture2D(256,8,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Point};var r=new System.Random(7);for(int y=0;y<8;y++)for(int x=0;x<256;x++){float v=(float)r.NextDouble();noise.SetPixel(x,y,new Color(v,v,v,.55f*v));}noise.Apply();}
    // VHS tracking band creeping up from the bottom
    float y0=h*(1-band)-h*.06f;GUI.color=new Color(1,1,1,.7f);GUI.DrawTextureWithTexCoords(new Rect(0,y0,w,h*.07f),noise,new Rect(UnityEngine.Random.value,UnityEngine.Random.value,3,1));
   }
   if(white>0){GUI.color=new Color(1,1,1,white);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);}
   GUI.color=Color.white;
   if(osd!=""){var st=new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(h*.05f),fontStyle=FontStyle.Bold};GUI.color=new Color(0,0,0,.6f);GUI.Label(new Rect(w*.06f+3,h*.07f+3,w*.5f,h*.1f),osd,st);GUI.color=Color.white;GUI.Label(new Rect(w*.06f,h*.07f,w*.5f,h*.1f),osd,st);}
  }
  void OnDestroy(){if(noise)Destroy(noise);if(sound)sound.VoiceClip-=OnVoice;}
 }
}
