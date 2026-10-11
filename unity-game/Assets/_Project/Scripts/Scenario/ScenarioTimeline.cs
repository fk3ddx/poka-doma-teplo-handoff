using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.Cinemachine;
namespace Ataka.Scenario {
 public sealed class ScenarioTimeline:MonoBehaviour {
  public FlatBindings flat;public ScenarioSound sound;public LoopDirector loop;public PlayableDirector director;public CinemachineCamera cine;
  public TimelineAsset underBed,balcony,television,hanging,peephole,death,chaseStart;
  public CloseupHand leftHand,rightHand;
  public LineRenderer rope;public Light landingLight;
  public DeathVeil veil;int deathsViewed;float skipHeld;
  public string PlayingScene{get;private set;}public float FrameTime{get;private set;}
  void HeadAt(Transform actor,Vector3 point){var animator=actor.GetComponent<Animator>();var bone=animator.GetBoneTransform(HumanBodyBones.Head);actor.position+=point-bone.position;}
  public void BalconyWaiting(){flat.guest.SetActive(true);var g=flat.guest.transform;g.localScale=guestScale;g.rotation=Quaternion.Euler(0,0,180);HeadAt(g,new Vector3(5.175f,1.8f,-.3f));}
  public float screenBlack,screenRed,screenWhite;Quaternion startHead,chaseStartRotation,chaseTargetRotation;Vector3 startGuest,startHeadPosition;bool fired;float lastTime;Vector3 guestScale;
  void Awake(){guestScale=flat.guest.transform.localScale;}
  public void Cancel(){flat.player.lookDuringScene=false;flat.player.flashlight.transform.localRotation=Quaternion.identity;if(PlayingScene=="DeathShort")sound.BeginDeathRecovery();sound.EndDeathRecovery();PlayingScene=null;FrameTime=0;director.Stop();screenBlack=screenWhite=screenRed=0;flat.look.peephole=flat.look.disruption=0;flat.player.control=true;flat.player.head.localPosition=Vector3.up*(flat.player.crouched?.94f:1.55f);if(leftHand)leftHand.gameObject.SetActive(false);if(rightHand)rightHand.gameObject.SetActive(false);flat.guest.SetActive(false);flat.grandmother.SetActive(false);}
  public void PrepareHanging(){
   flat.grandmother.SetActive(true);flat.grandmother.GetComponent<NinaHangingPose>().Evaluate(0,flat.player.head.position);
   flat.stool.transform.SetPositionAndRotation(new Vector3(1.95f,.22f,2.2f),Quaternion.Euler(0,20,90));flat.slipper.transform.position=new Vector3(1.2f,.015f,2.65f);
   if(rope){rope.enabled=true;rope.SetPosition(0,new Vector3(1.4f,2.5f,2.2f));rope.SetPosition(1,flat.grandmother.GetComponent<NinaHangingPose>().NeckPosition);}
  }
  public IEnumerator Play(string name){
   PlayingScene=name;
   if(name=="ChaseStart"){chaseStartRotation=flat.player.head.rotation;var direction=ChaseCorridor.EntryOrigin-flat.player.head.position;direction.y=0;chaseTargetRotation=Quaternion.LookRotation(direction);}
   var timeline=name=="UnderBed"?underBed:name=="BalconyPalm"?balcony:name=="Television"?television:name=="HangingVision"?hanging:name=="Peephole"?peephole:name=="DeathShort"?death:chaseStart;
   flat.player.control=false;flat.player.lookDuringScene=name=="HangingVision";startHead=flat.player.head.localRotation;startHeadPosition=flat.player.head.localPosition;fired=false;lastTime=-1;screenBlack=screenRed=screenWhite=0;
   var g=flat.guest.transform;flat.guest.GetComponent<ProxyPose>().Stand();g.position=flat.player.head.position+flat.player.head.forward*.65f-Vector3.up*1.6f;g.rotation=Quaternion.LookRotation(-flat.player.head.forward,Vector3.up);startGuest=g.position;
   flat.guest.SetActive(false);flat.grandmother.SetActive(false);
   director.playableAsset=timeline;foreach(var output in timeline.outputs)director.SetGenericBinding(output.sourceObject,this);
   director.time=name=="HangingVision"?1:0;director.Play();skipHeld=0;while(director.state==PlayState.Playing){if(name=="DeathShort"&&deathsViewed>0){skipHeld=UnityEngine.InputSystem.Keyboard.current?.eKey.isPressed==true?skipHeld+Time.deltaTime:0;if(skipHeld>=.6f)break;}yield return null;}if(name=="DeathShort"){deathsViewed++;sound.BeginDeathRecovery();}
   director.Stop();screenBlack=screenWhite=screenRed=0;if(leftHand)leftHand.gameObject.SetActive(false);if(rightHand)rightHand.gameObject.SetActive(false);flat.guest.SetActive(false);flat.grandmother.SetActive(false);if(name=="HangingVision"){
    var euler=flat.player.head.rotation.eulerAngles;flat.player.transform.rotation=Quaternion.Euler(0,euler.y,0);flat.player.pitch=Mathf.Clamp(Mathf.DeltaAngle(0,euler.x),-80,80);flat.player.head.localRotation=Quaternion.Euler(flat.player.pitch,0,0);
   }else flat.player.head.localRotation=startHead;
   flat.player.lookDuringScene=false;flat.player.flashlight.transform.localRotation=Quaternion.identity;flat.player.head.localPosition=startHeadPosition;flat.guest.transform.localScale=guestScale;
   if(name=="ChaseStart"){flat.player.transform.rotation=chaseTargetRotation;flat.player.pitch=0;flat.player.head.localRotation=Quaternion.identity;}
   var lens=cine.Lens;lens.FieldOfView=65;cine.Lens=lens;flat.player.control=true;flat.player.flashlight.enabled=true;flat.look.peephole=0;if(landingLight)landingLight.intensity=0;if(rope)rope.enabled=false;if(veil)veil.At(-1);sound.quiet=false;PlayingScene=null;FrameTime=0;
  }
  void SoundAt(float t,float at,string id,Vector3 p){if(lastTime<at&&t>=at){if(PlayingScene=="DeathShort")sound.PlayClose(id,p);else sound.Play(id,p);}}
  public void Evaluate(string name,float t){
   FrameTime=t;
   var p=flat.player;var guest=flat.guest.transform;var head=p.head;
   if(name=="UnderBed"){
    flat.guest.SetActive(true);guest.localScale=guestScale;guest.rotation=Quaternion.Euler(90,180,0);HeadAt(guest,flat.bed.position);
    if(t>=1){HeadAt(guest,Vector3.Lerp(flat.bed.position,head.position+head.forward*.25f,Mathf.Clamp01((t-1)*10)));screenWhite=Mathf.Max(0,1-(t-1)*5);SoundAt(t,1,"shriek",head.position);}
   }else if(name=="BalconyPalm"){
    BalconyWaiting();SoundAt(t,0,"balcony",flat.balcony.position);
    if(rightHand)rightHand.World(new Vector3(5.175f,1.3f,Mathf.Lerp(-.35f,-.015f,Mathf.Clamp01(t*10))),Vector3.up);
   }else if(name=="Television"){
    flat.televisionLight.intensity=0;SoundAt(t,0,"latch",flat.television.position);
   }else if(name=="HangingVision"){
    flat.bedroom.open=true;flat.grandmother.SetActive(t<5);var grandmother=flat.grandmother.transform;var nina=flat.grandmother.GetComponent<NinaHangingPose>();nina.Evaluate(t,head.position);
    flat.stool.transform.SetPositionAndRotation(new Vector3(1.95f,.22f,2.2f),Quaternion.Euler(0,20,90));flat.slipper.transform.position=new Vector3(1.2f,.015f,2.65f);
    if(rope){rope.enabled=t<5;rope.SetPosition(0,new Vector3(1.4f,2.5f,2.2f));rope.SetPosition(1,nina.NeckPosition);}
    var feet=nina.FeetPosition;var face=nina.FacePosition;
    p.lookDuringScene=t<2;
    if(t<2){p.flashlight.transform.rotation=Quaternion.RotateTowards(head.rotation,Quaternion.LookRotation(feet-p.flashlight.transform.position),12);}
    else {p.flashlight.transform.localRotation=Quaternion.identity;Vector3 target=Vector3.Lerp(feet,face,Mathf.Clamp01((t-2)/1.5f));head.rotation=Quaternion.Slerp(head.rotation,Quaternion.LookRotation(target-head.position),1-Mathf.Exp(-7.67f*Time.deltaTime));}
    p.flashlight.intensity=t>=4.5f&&t<4.9f?0:3;
    SoundAt(t,3.5f,"breath",grandmother.position);if(lastTime<4.5f&&t>=4.5f)sound.PlayClose("tinnitus",head.position);
    if(t>=5&&!fired){fired=true;sound.StopCue("rope");sound.StopCue("breath");sound.StopCue("tinnitus");loop.Thought("Нет. Нет-нет-нет…");}SoundAt(t,6,"latch",flat.carpet.transform.position);
   }else if(name=="Peephole"){
    flat.look.peephole=1;
    if(landingLight)landingLight.intensity=.7f;if(lastTime<0)sound.Grandmother(new Vector3(8.05f,1.8f,6.65f));
    flat.guest.SetActive(true);guest.localScale=guestScale;guest.position=new Vector3(8.05f,0,6.65f);guest.rotation=Quaternion.Euler(0,t<3?0:Mathf.Lerp(0,180,Mathf.Clamp01((t-3)/1.5f)),0);
    var lens=cine.Lens;lens.FieldOfView=85;cine.Lens=lens;
    head.position=new Vector3(8.05f,1.5f,6.22f);head.rotation=Quaternion.Euler(0,0,0);
    if(t>5)guest.position+=Vector3.back*Mathf.Min(.38f,(t-5)*.2f);SoundAt(t,0,"breath",guest.position);
   }else if(name=="DeathShort"){
    sound.quiet=true;if(veil)veil.At(t);
    if(leftHand)leftHand.Frame(head,t);if(rightHand)rightHand.Frame(head,t);
    flat.guest.SetActive(t<2.5f);guest.localScale=guestScale;guest.position=startGuest;HeadAt(guest,head.position+head.forward*.3f-Vector3.up*.07f);var lens=cine.Lens;lens.FieldOfView=Mathf.Lerp(70,55,Mathf.Clamp01(t/.08f));cine.Lens=lens;
    head.localRotation=startHead*Quaternion.Euler(Mathf.Sin(t*55)*1.5f,Mathf.Cos(t*41)*1,0);
    SoundAt(t,0,"breath",head.position);SoundAt(t,.3f,"finger_crack",head.position);SoundAt(t,1,"palms",head.position);SoundAt(t,1,"choked_cry",head.position);SoundAt(t,1.5f,"wet_tear",head.position);
    screenRed=t>=1.5f?Mathf.Clamp01((t-1.5f)*2):0;screenBlack=t>=2.5f?1:0;
   }else if(name=="ChaseStart"){
    var leaf=flat.entrance.leaf;
    if(landingLight){landingLight.color=new Color(1,.12f,.04f);landingLight.intensity=t>=2?.85f:0;}
    float flight=Mathf.Clamp01((t-2)/.18f);
    leaf.localRotation=t<2?Quaternion.Euler(0,Mathf.Sin(t*95)*2,0):Quaternion.Euler(90*flight,0,0);
    leaf.localPosition=new Vector3(-.45f,-1+.04f*flight,.12f*flight);
    if(t>=2){flat.guest.SetActive(true);guest.localScale=guestScale;guest.rotation=Quaternion.Euler(0,180,0);var at=new Vector3(8.05f,0,6.7f-Mathf.Clamp01((t-2.5f)/.1f)*.4f);guest.position=at;flat.guest.GetComponent<ProxyPose>().Crawl(at);}
    SoundAt(t,.8f,"knock",flat.entrance.transform.position);SoundAt(t,1.3f,"knock",flat.entrance.transform.position);SoundAt(t,1.8f,"knock",flat.entrance.transform.position);SoundAt(t,2,"door_burst",flat.entrance.transform.position);SoundAt(t,2.5f,"shriek",flat.entrance.transform.position);
    var doorLook=Quaternion.LookRotation(new Vector3(8.05f,.9f,6.7f)-head.position);
    head.rotation=t<2.6f?Quaternion.Slerp(chaseStartRotation,doorLook,Mathf.Clamp01(t/.3f)):Quaternion.Slerp(doorLook,chaseTargetRotation,Mathf.SmoothStep(0,1,Mathf.Clamp01((t-2.6f)/.4f)));

   }
   lastTime=t;
  }
  void OnGUI(){// Temporary full-frame cutscene masks; replaced by face-tear mesh before final character art approval.
   if(screenWhite>0){GUI.color=new Color(1,1,1,screenWhite);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);}
   if(screenRed>0){GUI.color=new Color(.28f,0,0,screenRed*.85f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);}
   if(screenBlack>0){GUI.color=new Color(0,0,0,screenBlack);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);}GUI.color=Color.white;
  }
 }
}
