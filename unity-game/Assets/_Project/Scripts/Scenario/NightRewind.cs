using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Ataka.Scenario {
 // SCENARIO 6.1 — the single rewind of the game, 8–10 s, from the actual first night played backwards.
 // Recording: night 1 from 21:42 to the 22:07 death at 10 Hz into a preallocated ring buffer (camera incl. its real
 // height, standing or crouched in a hiding place; the player's torch, door leaves, light intensities, the "Rubin",
 // the Guest) plus the one-shot sounds that played.
 // Evening memories: the world state of four real evening moments (tea with jam, the belt in the wardrobe, her
 // "tap-tap… tap", the new bulb) with a DIRECTED camera aimed at the subject's real bounds (props / her head), pulled
 // back no further than the walls allow (her memory: into her face from the head bone). The belt inside the wardrobe gets
 // the rewind's own memory light (intensity only). They are saved with the night-1 checkpoint and restored on Continue.
 // Playback (frozen, no side effects, while the game is paused): 1 s black silence (reversed sounds prepared here,
 // bounded cache) → the night in reverse with reversed sound and growing ticking → evening flashes (her animation
 // frozen) → white flash → frozen VHS pause frame → PLAY into night 2, standing.
 // Lights are only re-dimmed (never enabled/disabled); doors are driven through their leaves and handed back;
 // Cleanup() leaves no state behind on a normal end or when the story is cancelled mid-rewind.
 public sealed class NightRewind:MonoBehaviour {
  public LoopDirector loop;public Func<string,Transform> prop; // story props by id (StoryDirector)
  public const float Rate=10;public const int Capacity=4000; // 400 s of night at 10 Hz
  public const float Black=1f,Reverse=5.2f,Evening=1.6f,Flash=.2f,Freeze=1f,PlayTag=.6f;
  public const int ReversedBudgetBytes=16*1024*1024,CueCapacity=256,JournalCapacity=600;
  public static float Duration=>Black+Reverse+Evening+Flash+Freeze+PlayTag;
  FlatBindings flat=>loop.flat;ScenarioSound sound=>loop.sound;ScenarioTimeline scenes=>loop.scenes;
  ScenarioDoor[] doors;Light[] lights;ScenarioTelevision tv;int stride;float[] data;int count,first;float nextSample,startedAt;
  struct Cue {public AudioClip clip;public float t;public string id;}
  readonly List<Cue> cues=new List<Cue>();
  [Serializable] public sealed class BonePose {public string path;public Vector3 position,scale;public Quaternion rotation;}
  [Serializable] public sealed class Memory {public BonePose[] granPose;public string id,cue,subject;public Vector3 cam;public Quaternion camRot;public bool gran;public Vector3 granAt;public Quaternion granRot;public float[] leaves,light;}
  readonly List<Memory> memories=new List<Memory>();
  public bool Recording{get;private set;}public bool Active{get;private set;}public float Clock{get;private set;}public string Phase{get;private set;}="";
  public int Samples=>count;public int CuesRecorded=>cues.Count;public int MemoriesRecorded=>memories.Count;public int FloatsPerSample=>stride;
  public IReadOnlyList<Memory> Memories=>memories;public string CurrentMemory{get;private set;}="";public Vector3? CurrentSubject{get;private set;}
  public float RecordingTime=>Time.time-startedAt;public float CurrentSource{get;private set;} // QA: night time of the frame now shown
  public int ReversedPrepared{get;private set;}public int ReversedSkipped{get;private set;}
  // Honest memory accounting: recording buffer + cue list + journal + memories + the reversed-clip cache (actual PCM).
  public int BytesRecordingReserved=>(data!=null?data.Length*4:0)+CueCapacity*24+JournalCapacity*48;
  public int BytesMemories {get {int bytes=0;foreach(var m in memories){bytes+=64+4*(m.leaves?.Length??0)+4*(m.light?.Length??0);if(m.granPose!=null)foreach(var b in m.granPose)bytes+=40+2*(b.path?.Length??0);}return bytes;}}
  public int BytesReversedCache=>sound?sound.ReversedBytes:0;
  public int BytesReserved=>BytesRecordingReserved+ReversedBudgetBytes;
  public int BytesUsed=>count*stride*4+cues.Count*24+Journal.Count*48+BytesMemories+BytesReversedCache;
  // QA journal of one playback: what was shown/heard at which rewind time, with the ORIGINAL night time it came from.
  public struct Played {public float clock,source;public string what;}
  public readonly List<Played> Journal=new List<Played>();
  public float LastActiveSeconds{get;private set;}
  void Note(float clock,float source,string what){if(Journal.Count<JournalCapacity)Journal.Add(new Played{clock=clock,source=source,what=what});}
  string osd="";float black,white,band;bool[] scriptedBefore,lockedBefore,openBefore;float[] lightsBefore,leavesBefore,doorAngleBefore;float torchBefore,eyeBefore;bool restored,crouchedBefore;

  void Setup(){
   if(data!=null)return;
   var d=new List<ScenarioDoor>();foreach(var x in new[]{flat.entrance,flat.kitchen,flat.bath,flat.bedroom,flat.living,flat.balconyDoor})if(x&&x.leaf)d.Add(x);doors=d.ToArray();
   var l=new List<Light>();void Add(Light x){if(x&&!l.Contains(x))l.Add(x);}foreach(var x in flat.look.warm)Add(x);foreach(var x in flat.look.cold)Add(x);Add(flat.look.icon);Add(flat.televisionLight);Add(scenes.landingLight);lights=l.ToArray();
   tv=FindFirstObjectByType<ScenarioTelevision>();
   // The memory's own light: always enabled, intensity 0 except while a memory that needs it is on screen (the belt
   // inside the wardrobe). The flat's accepted lights are not touched for it.
   memoryLight=new GameObject("Rewind memory light").AddComponent<Light>();memoryLight.transform.SetParent(transform,false);memoryLight.type=LightType.Spot;memoryLight.spotAngle=60;memoryLight.innerSpotAngle=30;memoryLight.color=new Color(1,.8f,.6f);memoryLight.range=2.5f;memoryLight.shadows=LightShadows.None;memoryLight.intensity=0;
   stride=8+doors.Length+lights.Length+7;data=new float[Capacity*stride];
  }
  // ---------- recording ----------
  public void Begin(){Setup();count=0;first=0;cues.Clear();Recording=true;nextSample=0;startedAt=Time.time;sound.VoiceClip-=OnVoice;sound.VoiceClip+=OnVoice;}
  public void Stop(){Recording=false;if(sound)sound.VoiceClip-=OnVoice;}
  public void ForgetEvening(){memories.Clear();}
  public Memory[] Export()=>memories.ToArray();
  public void Import(Memory[] saved){memories.Clear();if(saved!=null)foreach(var m in saved)if(m!=null&&!string.IsNullOrEmpty(m.id))memories.Add(m);}
  void OnVoice(string id,AudioClip clip){if(!Recording||!clip||id=="step"||id=="carpet_step"||id=="rewind")return; // footsteps would crowd out the night's events
   if(cues.Count>=CueCapacity)cues.RemoveAt(0);cues.Add(new Cue{clip=clip,t=Time.time-startedAt,id=id});}
  void Update(){
   if(!Recording||flat.player.paused||Time.time<nextSample)return;nextSample=Time.time+1/Rate;
   int slot;if(count<Capacity){slot=(first+count)%Capacity;count++;}else{slot=first;first=(first+1)%Capacity;}
   Write(slot*stride);
  }
  void Write(int o){
   var p=flat.player;var at=p.transform.position;data[o]=at.x;data[o+1]=at.y;data[o+2]=at.z;data[o+3]=p.transform.eulerAngles.y;data[o+4]=Mathf.DeltaAngle(0,p.head.localEulerAngles.x);data[o+5]=Time.time-startedAt;
   data[o+6]=p.flashlight&&p.flashlight.enabled?p.flashlight.intensity:0;data[o+7]=p.head.localPosition.y;o+=8;
   foreach(var d in doors)data[o++]=Mathf.DeltaAngle(0,d.leaf.localEulerAngles.y);
   foreach(var l in lights)data[o++]=l.intensity;
   var g=flat.guest.transform;data[o++]=tv&&tv.storyOn?1:0;data[o++]=flat.guest.activeSelf?1:0;data[o++]=g.position.x;data[o++]=g.position.y;data[o++]=g.position.z;data[o++]=g.eulerAngles.y;data[o++]=g.up.y;
  }
  // ---------- evening memories: the real state, a directed camera ----------
  public Vector3? GranHead(){var g=flat.grandmother;if(!g.activeInHierarchy)return null;var rs=g.GetComponentsInChildren<Renderer>();if(rs.Length==0)return null;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return new Vector3(b.center.x,b.max.y-.12f,b.center.z);}
  public Vector3? PropCenter(string id){var t=prop?.Invoke(id);if(!t||!t.gameObject.activeInHierarchy)return null;var rs=t.GetComponentsInChildren<Renderer>();if(rs.Length==0)return t.position;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b.center;}
  public Vector3? SubjectPoint(string subject)=>subject=="gran"?GranFacePoint()??GranHead():PropCenter(subject);
  // Her face direction from the bind pose of the Katcho head bone (the frozen idle frame may have the head turned).
  Transform granHeadBone;Vector3 granFaceLocal;
  public Vector3? GranFace(){
   var g=flat.grandmother;if(!g.activeInHierarchy)return null;
   if(!granHeadBone){var pose=g.GetComponent<NinaHangingPose>();var skin=g.GetComponentInChildren<SkinnedMeshRenderer>(true);if(!pose||!pose.head||!skin||!skin.sharedMesh)return null;int i=Array.IndexOf(skin.bones,pose.head);var binds=skin.sharedMesh.bindposes;if(i<0||i>=binds.Length)return null;
    granFaceLocal=binds[i].MultiplyVector(skin.transform.InverseTransformDirection(g.transform.forward)).normalized;granHeadBone=pose.head;}
   return granHeadBone.TransformDirection(granFaceLocal);}
  public Vector3? GranFacePoint(){var f=GranFace();return f.HasValue?granHeadBone.position+f.Value*.07f+Vector3.up*.03f:(Vector3?)null;}
  // QA: cosine between her face direction and the direction to the camera (1 = looking straight into it).
  public float GranFacingCamera(){var f=GranFace();var at=GranFacePoint();if(!f.HasValue||!at.HasValue)return -1;return Vector3.Dot(f.Value.normalized,(flat.player.head.position-at.Value).normalized);}
  // QA: mean luma (sRGB, 0..1) and 10–90 % spread of a screen capture around a world point — a darkness floor, not art acceptance.
  public bool Readable(Texture2D frame,Vector3 point,out string why){
   var cam=flat.player.view?flat.player.view:Camera.main;if(!frame||!cam){why="no frame";return false;}
   var v=cam.WorldToViewportPoint(point);int w=frame.width,h=frame.height,x0=Mathf.Clamp(Mathf.RoundToInt((v.x-.1f)*w),0,w-1),x1=Mathf.Clamp(Mathf.RoundToInt((v.x+.1f)*w),x0+1,w),y0=Mathf.Clamp(Mathf.RoundToInt((v.y-.12f)*h),0,h-1),y1=Mathf.Clamp(Mathf.RoundToInt((v.y+.12f)*h),y0+1,h);
   var px=frame.GetPixels(x0,y0,x1-x0,y1-y0);var luma=new float[px.Length];double sum=0;for(int i=0;i<px.Length;i++){luma[i]=.2126f*px[i].r+.7152f*px[i].g+.0722f*px[i].b;sum+=luma[i];}Array.Sort(luma);
   float mean=(float)(sum/Mathf.Max(1,luma.Length)),spread=luma.Length>0?luma[luma.Length*9/10]-luma[luma.Length/10]:0;
   LastLuma=mean;why="luma "+mean.ToString("F2")+", spread "+spread.ToString("F2")+" around the subject (floor .15 / .08)";return mean>=.15f&&spread>=.08f;}
  public float LastLuma{get;private set;}Light memoryLight;
  // Fill for a memory whose subject sits inside furniture: a soft spot from just above the camera onto the subject, so
  // the open wardrobe's interior and the belt are lit and the leaves at the frame edges are not. Calibrate in QA with
  // -rewind-belt-fill <intensity>.
  public float beltFill=2.5f;
  float MemoryFill(string id)=>id=="belt"?beltFill:id=="knock"?.65f:0;
  bool Ignored(Transform t)=>t.IsChildOf(flat.player.transform)||t.IsChildOf(flat.grandmother.transform)||t.IsChildOf(flat.guest.transform);
  // From the subject towards `from`, stopping 0.18 m before the first wall/furniture (never closer than 0.55 m).
  Vector3 Boom(Vector3 target,Vector3 from,float distance){
   var dir=(from).normalized;float d=distance;
   foreach(var h in Physics.RaycastAll(target,dir,distance,~(1<<31),QueryTriggerInteraction.Ignore))if(!Ignored(h.transform)&&h.distance>.05f)d=Mathf.Min(d,Mathf.Max(.55f,h.distance-.18f));
   return target+dir*d;}
  // A root transform is insufficient: night 1 leaves the hanging pose in this same rig. Each memory must
  // restore its own local bone pose BEFORE computing the face/camera, including after save + Continue.
  BonePose[] granPoseBefore;
  BonePose[] CaptureGranPose(){
   var root=flat.grandmother.transform;var result=new List<BonePose>();
   foreach(var t in root.GetComponentsInChildren<Transform>(true)){if(t==root)continue;string path=t.name;var a=t.parent;while(a!=root&&a){path=a.name+"/"+path;a=a.parent;}
    result.Add(new BonePose{path=path,position=t.localPosition,rotation=t.localRotation,scale=t.localScale});}return result.ToArray();
  }
  void ApplyGranPose(BonePose[] pose){if(pose==null)return;var root=flat.grandmother.transform;foreach(var b in pose){var t=root.Find(b.path);if(t){t.localPosition=b.position;t.localRotation=b.rotation;t.localScale=b.scale;}}}
  void SampleGranIdle(){var anim=flat.grandmother.GetComponentInChildren<Animation>(true);if(!anim)return;foreach(AnimationState state in anim)if(state.name.EndsWith("Oidle")){state.clip.SampleAnimation(anim.gameObject,0);return;}}
  public float MemoryPoseError(){foreach(var m in memories)if(m.id==CurrentMemory&&m.granPose!=null){float error=0;foreach(var b in m.granPose){var t=flat.grandmother.transform.Find(b.path);if(!t)return 180;error=Mathf.Max(error,Quaternion.Angle(t.localRotation,b.rotation));}return error;}return -1;}
  public void Remember(string id,string cue,bool canonicalPose=false){
   Setup();memories.RemoveAll(m=>m.id==id);var gr=flat.grandmother;
   var savedPose=canonicalPose&&gr.activeSelf?CaptureGranPose():null;if(savedPose!=null)SampleGranIdle();
   string subject=id=="tea"?(PropCenter("TeaSet")!=null?"TeaSet":"JamJar"):id=="belt"?"Belt":id=="bulb"?"Bulb":"gran";
   var target=SubjectPoint(subject);Vector3 cam;Quaternion rot;
   if(target.HasValue){
    Vector3 from=id=="tea"?new Vector3(-.35f,.6f,.75f):id=="belt"?new Vector3(-1,.3f,0):id=="bulb"?new Vector3(0,-.35f,-1):(GranFace()??gr.transform.forward)+Vector3.up*.12f;
    float dist=id=="tea"?1.15f:id=="belt"?.95f:id=="bulb"?1.05f:1.5f;
    cam=Boom(target.Value,from,dist);rot=Quaternion.LookRotation(target.Value-cam);
   }else{cam=flat.player.head.position;rot=flat.player.head.rotation;subject="player view";}
   var m=new Memory{id=id,cue=cue,subject=subject,cam=cam,camRot=rot,granPose=gr.activeSelf?CaptureGranPose():null,gran=gr.activeSelf,granAt=gr.transform.position,granRot=gr.transform.rotation,leaves=new float[flat.wardrobeLeaves.Length],light=new float[lights.Length]};
   for(int i=0;i<m.leaves.Length;i++)m.leaves[i]=Mathf.DeltaAngle(0,flat.wardrobeLeaves[i].localEulerAngles.y);for(int i=0;i<lights.Length;i++)m.light[i]=lights[i].intensity;
   memories.Add(m);if(savedPose!=null)ApplyGranPose(savedPose);
  }
  // QA: is a world point really in the rendered frame (viewport 0.1–0.9, in front) and not hidden behind a wall/furniture?
  public bool Visible(Vector3 point,bool subjectIsGran,out string why){
   var cam=flat.player.view?flat.player.view:Camera.main;if(!cam){why="no camera";return false;}
   var v=cam.WorldToViewportPoint(point);var from=cam.transform.position;float dist=Vector3.Distance(from,point);bool inView=v.z>.3f&&v.x>.1f&&v.x<.9f&&v.y>.1f&&v.y<.9f;string by="";
   foreach(var h in Physics.RaycastAll(from,(point-from).normalized,dist,~(1<<31),QueryTriggerInteraction.Ignore)){if(h.transform.IsChildOf(flat.player.transform)||(subjectIsGran&&h.transform.IsChildOf(flat.grandmother.transform)))continue;if(h.distance<dist-.25f&&(by==""||h.distance<dist)){by=GranNav.PathOf(h.transform)+" at "+h.distance.ToString("F2")+" m";break;}}
   why="viewport ("+v.x.ToString("F2")+", "+v.y.ToString("F2")+"), "+dist.ToString("F2")+" m, "+(by==""?"line of sight clear":"blocked by "+by);return inView&&by=="";}
  // ---------- playback ----------
  float S(int sample,int k)=>data[((first+sample)%Capacity)*stride+k];
  public float EyeAt(int sample)=>sample>=0&&sample<count?S(sample,7):0; // QA: recorded camera height
  Action<bool> freeze;
  public IEnumerator Play(Action prepareNight2,Action<bool> freezeGrandmother){
   Setup();Stop();Active=true;freeze=freezeGrandmother;Phase="black";var p=flat.player;var body=p.GetComponent<CharacterController>();
   Journal.Clear();LastActiveSeconds=0;CurrentMemory="";CurrentSubject=null;
   // ScenarioDoor.Advance returns while scripted, so the replayed leaf poses are not overwritten by the door's own Update.
   scriptedBefore=new bool[doors.Length];lockedBefore=new bool[doors.Length];openBefore=new bool[doors.Length];doorAngleBefore=new float[doors.Length];
   for(int i=0;i<doors.Length;i++){var d=doors[i];scriptedBefore[i]=d.scripted;lockedBefore[i]=d.locked;openBefore[i]=d.open;doorAngleBefore[i]=Mathf.DeltaAngle(0,d.leaf.localEulerAngles.y);d.scripted=true;}
   lightsBefore=new float[lights.Length];for(int i=0;i<lights.Length;i++)lightsBefore[i]=lights[i].intensity;torchBefore=p.flashlight?p.flashlight.intensity:0;crouchedBefore=p.crouched;eyeBefore=p.head.localPosition.y;
   granPoseBefore=CaptureGranPose();
   restored=false;if(body)body.enabled=false;p.control=false;sound.quiet=true;loop.prompt.text="";
   leavesBefore=new float[flat.wardrobeLeaves.Length];for(int i=0;i<leavesBefore.Length;i++)leavesBefore[i]=Mathf.DeltaAngle(0,flat.wardrobeLeaves[i].localEulerAngles.y);
   // Reverse every clip the rewind can play during the silent black second — never mid-rewind (no hitch), within budget.
   var needed=new List<AudioClip>();foreach(var c in cues)if(c.clip&&!needed.Contains(c.clip))needed.Add(c.clip);foreach(var m in memories){var c=sound.FirstClip(m.cue);if(c&&!needed.Contains(c))needed.Add(c);}
   needed.Sort((a,b)=>(a.samples*a.channels).CompareTo(b.samples*b.channels));ReversedPrepared=0;ReversedSkipped=0;
   foreach(var c in needed){if(sound.PrepareReversed(c,ReversedBudgetBytes))ReversedPrepared++;else ReversedSkipped++;}
   float t=0,total=Duration;bool guestHanging=false;int lastSample=count;var order=memories.ToArray();Array.Reverse(order); // backwards in time: the knock, the belt, the bulb, tea with jam
   int shownMemory=-1;bool prepared=false,eveningFrozen=false;
   while(t<total){
    if(p.paused||sound.paused){yield return null;continue;} // paused: no sample, no sound request, no jitter — nothing
    Clock=t;
    if(t<Black){Phase="black";black=1;osd="";}
    else if(t<Black+Reverse){
     Phase="reverse";float u=(t-Black)/Reverse;black=Mathf.Clamp01(1-(t-Black)/.15f);osd="◄◄";band=u;flat.look.disruption=.55f+.25f*Mathf.PerlinNoise(t*7,.3f);
     sound.RewindTick(Mathf.Lerp(.15f,1,u));
     if(count>1){
      float f=(1-u)*(count-1);int a=Mathf.FloorToInt(f);int b=Mathf.Min(count-1,a+1);float w=f-a;
      ApplySample(a,b,w,ref guestHanging);
      // sounds of the samples just passed, played backwards (later night time first), then the frame itself
      float tA=S(a,5),tB=lastSample<count?S(lastSample,5):float.MaxValue;
      for(int i=cues.Count-1;i>=0;i--)if(cues[i].t>=tA&&cues[i].t<tB){sound.PlayReversed(cues[i].clip,1.6f);Note(t,cues[i].t,"sound "+cues[i].id+" ("+cues[i].clip.name+")");}
      if(a!=lastSample)Note(t,tA,"frame "+a);
      lastSample=a;
     }
     flat.clock.Show(Mathf.Lerp(22*3600+7*60,21*3600+42*60,u));
    }
    else if(t<Black+Reverse+Evening){
     Phase="evening";float e=(t-Black-Reverse)/Evening;sound.RewindTick(1);flat.look.disruption=.75f+.2f*Mathf.PerlinNoise(t*9,.7f);band=1-e;
     if(!eveningFrozen){eveningFrozen=true;flat.guest.SetActive(false);if(p.flashlight)p.flashlight.intensity=0;freeze?.Invoke(true);}
     if(order.Length>0){int k=Mathf.Min(order.Length-1,Mathf.FloorToInt(e*order.Length));if(k!=shownMemory){shownMemory=k;ApplyMemory(order[k]);Note(t,-1,"evening "+order[k].id+" → "+order[k].subject);sound.PlayReversed(sound.FirstClip(order[k].cue),1.4f);}}
     flat.clock.Show(Mathf.Lerp(21*3600+42*60,21*3600+40*60,e));
    }
    else if(t<Black+Reverse+Evening+Flash){
     Phase="flash";white=1-(t-Black-Reverse-Evening)/Flash;sound.RewindTick(0);CurrentMemory="";CurrentSubject=null;
     if(!prepared){prepared=true;Restore();prepareNight2?.Invoke();SnapDoors();p.control=false;if(body)body.enabled=false;freeze?.Invoke(true);flat.look.disruption=.35f;}
    }
    else if(t<total-PlayTag){Phase="freeze";white=0;osd="II";band=0;var j=Mathf.Sin(t*71)*.25f;p.head.localRotation=Quaternion.Euler(j,Mathf.Sin(t*53)*.2f,0);flat.look.disruption=.3f+.05f*Mathf.Sin(t*40);}
    else{Phase="play";osd="► PLAY";p.head.localRotation=Quaternion.identity;freeze?.Invoke(false);flat.look.disruption=Mathf.Lerp(.3f,0,(t-(total-PlayTag))/PlayTag);}
    yield return null;t+=Time.deltaTime;LastActiveSeconds=t; // game time only: Time.deltaTime is 0 while the game is paused
   }
   if(!prepared){Restore();prepareNight2?.Invoke();SnapDoors();}
   Cleanup();
  }
  void ApplySample(int a,int b,float w,ref bool guestHanging){
   CurrentSource=Mathf.Lerp(S(a,5),S(b,5),w);var p=flat.player;var at=Vector3.Lerp(new Vector3(S(a,0),S(a,1),S(a,2)),new Vector3(S(b,0),S(b,1),S(b,2)),w);
   p.transform.SetPositionAndRotation(at,Quaternion.Euler(0,Mathf.LerpAngle(S(a,3),S(b,3),w),0));p.head.localRotation=Quaternion.Euler(Mathf.Lerp(S(a,4),S(b,4),w),0,0);p.head.localPosition=Vector3.up*Mathf.Lerp(S(a,7),S(b,7),w); // eye height as recorded (.94 crouched, 1.55 standing)
   if(p.flashlight)p.flashlight.intensity=Mathf.Lerp(S(a,6),S(b,6),w); // the torch as the player had it (intensity only)
   int o=8;foreach(var d in doors){d.leaf.localRotation=Quaternion.Euler(0,Mathf.LerpAngle(S(a,o),S(b,o),w),0);o++;}
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
   var p=flat.player;var e=m.camRot.eulerAngles;p.head.localPosition=Vector3.up*1.55f;
   p.transform.SetPositionAndRotation(m.cam-Vector3.up*1.55f,Quaternion.Euler(0,e.y,0));p.head.localRotation=Quaternion.Euler(Mathf.DeltaAngle(0,e.x),0,0);
   flat.guest.SetActive(false);var gr=flat.grandmother;gr.SetActive(m.gran);if(m.gran){gr.transform.SetPositionAndRotation(m.granAt,m.granRot);if(m.granPose!=null&&m.granPose.Length>0)ApplyGranPose(m.granPose);else SampleGranIdle();}
   for(int i=0;i<m.leaves.Length&&i<flat.wardrobeLeaves.Length;i++)flat.wardrobeLeaves[i].localRotation=Quaternion.Euler(0,m.leaves[i],0);
   for(int i=0;i<m.light.Length&&i<lights.Length;i++)lights[i].intensity=m.light[i];
   if(m.subject=="gran"){var at=GranFacePoint();var f=GranFace();if(at.HasValue&&f.HasValue){var d=f.Value;d.y=0;if(d.sqrMagnitude<.01f)d=gr.transform.forward;var cam=Boom(at.Value,d.normalized+Vector3.up*.12f,1.5f);var look=Quaternion.LookRotation(at.Value-cam).eulerAngles; // into her face, wherever the frozen frame turned her head
    p.transform.SetPositionAndRotation(cam-Vector3.up*1.55f,Quaternion.Euler(0,look.y,0));p.head.localRotation=Quaternion.Euler(Mathf.DeltaAngle(0,look.x),0,0);}}
   var subject=SubjectPoint(m.subject);float fill=MemoryFill(m.id);
   if(memoryLight){memoryLight.intensity=subject.HasValue?fill:0;if(subject.HasValue){var from=p.head.position+Vector3.up*.15f;memoryLight.transform.SetPositionAndRotation(from,Quaternion.LookRotation(subject.Value-from));}}
   if(tv)tv.storyOn=false;CurrentMemory=m.id;CurrentSubject=subject;
  }
  // Back to what the night left (intensities, torch, wardrobe, TV, door ownership/state) before night 2 is set up.
  void Restore(){
   restored=true;if(memoryLight)memoryLight.intensity=0;scenes.Cancel();ApplyGranPose(granPoseBefore);granPoseBefore=null;
   if(lightsBefore!=null)for(int i=0;i<lights.Length;i++)lights[i].intensity=lightsBefore[i];
   var p=flat.player;if(p.flashlight)p.flashlight.intensity=torchBefore;p.crouched=crouchedBefore;p.head.localPosition=Vector3.up*eyeBefore; // night 2 setup stands the player up itself
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
    if(!restored)Restore(); // cancelled mid-playback: put back the night's own lights, torch, wardrobe, TV and doors
    freeze?.Invoke(false);
    var p=flat.player;var body=p.GetComponent<CharacterController>();if(body)body.enabled=true;p.control=true;p.head.localRotation=Quaternion.Euler(p.pitch,0,0);p.head.localPosition=Vector3.up*(p.crouched?.94f:1.55f);
    sound.RewindTick(0);sound.quiet=false;sound.ClearReversed();flat.look.disruption=0;if(memoryLight)memoryLight.intensity=0;
   }
   Stop();Active=false;freeze=null;Phase="";osd="";black=white=band=0;CurrentMemory="";CurrentSubject=null;scriptedBefore=lockedBefore=openBefore=null;lightsBefore=leavesBefore=doorAngleBefore=null;
  }
  Texture2D noise;
  // Drawn BELOW the pause menu (GUI.depth 1 > the menu's 0) and not at all while paused, so Esc always shows the menu.
  void OnGUI(){
   if(!Active||flat.player.paused)return;GUI.depth=1;float w=Screen.width,h=Screen.height;
   if(black>0){GUI.color=new Color(0,0,0,black);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);}
   if(band>0&&black<1){
    if(!noise){noise=new Texture2D(256,8,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Point};var r=new System.Random(7);for(int y=0;y<8;y++)for(int x=0;x<256;x++){float v=(float)r.NextDouble();noise.SetPixel(x,y,new Color(v,v,v,.55f*v));}noise.Apply();}
    float y0=h*(1-band)-h*.06f;GUI.color=new Color(1,1,1,.7f);GUI.DrawTextureWithTexCoords(new Rect(0,y0,w,h*.07f),noise,new Rect(UnityEngine.Random.value,UnityEngine.Random.value,3,1));
   }
   if(white>0){GUI.color=new Color(1,1,1,white);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);}
   GUI.color=Color.white;
   if(osd!=""){var st=new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(h*.05f),fontStyle=FontStyle.Bold};GUI.color=new Color(0,0,0,.6f);GUI.Label(new Rect(w*.06f+3,h*.07f+3,w*.5f,h*.1f),osd,st);GUI.color=Color.white;GUI.Label(new Rect(w*.06f,h*.07f,w*.5f,h*.1f),osd,st);}
  }
  void OnDestroy(){if(noise)Destroy(noise);if(sound)sound.VoiceClip-=OnVoice;}
 }
}
