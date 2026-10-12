using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Ataka.Scenario {
 public sealed class LoopDirector:MonoBehaviour {
  public FlatBindings flat;public LapData[] laps;public ScenarioSound sound;public ScenarioTimeline scenes;public ChaseCorridor chase;
  public Text subtitle,prompt; public LoopProgress Progress{get;private set;}=new LoopProgress();
  public bool Busy{get;private set;}public bool Finished{get;private set;}
  public bool testing;public string LastEvent{get;private set;}="";
  public StoryDirector story;public bool coreTestOnly; // set by ScenarioSmoke: the chase ends in the old core-test menu instead of the finale // the whole game around the loop (prologue → evening → nights → loop → chase → finale)
  bool hangingDoorOpened;float hangingEntryTime;
  public bool HangingWaiting=>Progress.Lap==5&&hangingDoorOpened&&!Busy&&!Progress.EventComplete;
  float thoughtUntil,phoneTimer,balconyTimer,tvWatch,gateWait,balconySeen;bool phoneAnswered,peepholeSeen,returning,clockThought;
  string savePath;bool paused=true,initializing=true;float knockTimer;int knockIndex;
  void FitSubtitles(){if(!subtitle)return;subtitle.horizontalOverflow=HorizontalWrapMode.Wrap;subtitle.verticalOverflow=VerticalWrapMode.Overflow;subtitle.alignment=TextAnchor.LowerCenter;var rect=subtitle.rectTransform;rect.anchorMin=new Vector2(.07f,.035f);rect.anchorMax=new Vector2(.93f,.23f);rect.offsetMin=rect.offsetMax=Vector2.zero;}
  void Start(){FitSubtitles();savePath=System.IO.Path.Combine(Application.persistentDataPath,"scenario-core-checkpoint.json");story=GetComponent<StoryDirector>();if(!story)story=gameObject.AddComponent<StoryDirector>();story.loop=this;EnterLap(1);initializing=false;Pause(true);}
  public void Pause(bool value){paused=value;flat.player.Pause(value);Time.timeScale=value?0:1;AudioListener.pause=value;sound.paused=value;}
  public void Thought(string text,float seconds=4){subtitle.text=text;thoughtUntil=Time.time+seconds;}
  public void EnterLap(int n){
   scenes.GetComponent<HangingFloorCue>()?.ResetCue();Progress.RestoreCheckpoint(n);Busy=false;hangingDoorOpened=false;hangingEntryTime=0;phoneAnswered=false;peepholeSeen=false;returning=false;clockThought=false;tvWatch=0;gateWait=0;balconySeen=0;
   flat.look.Apply(laps[n-1].condition);flat.clock.Show(Progress.ClockSeconds);flat.carpet.locked=true;flat.carpet.open=false;flat.entrance.open=false;flat.entrance.locked=true;
   if(flat.dressing)flat.dressing.Apply(n);
   flat.living.open=true;flat.living.locked=false;flat.entrance.scripted=false;flat.bedroom.open=n!=5;flat.bedroom.locked=n==5;flat.bedroom.closedAngle=n==5?Mathf.Asin(.2f/.8f)*Mathf.Rad2Deg:0;flat.kitchen.locked=flat.bath.locked=true;flat.kitchen.open=flat.bath.open=false;
   foreach(var d in flat.wardrobeLeaves)d.localRotation=Quaternion.Euler(0,n==1?95:0,0);
   for(int i=0;i<flat.conditionGroups.Length;i++)flat.conditionGroups[i].SetActive(i==n-1);
   if(flat.hallCupboard)flat.hallCupboard.ResetForLap();
   flat.guest.SetActive(false);flat.grandmother.SetActive(false);flat.torchPickup.gameObject.SetActive(!flat.player.hasFlashlight);
   flat.stool.transform.SetPositionAndRotation(new Vector3(5.5f,0,5.8f),Quaternion.identity);flat.slipper.SetActive(true);flat.slipper.transform.SetPositionAndRotation(ScenarioLayout.WardrobeSlipper,Quaternion.identity);
   flat.televisionLight.intensity=n==4?.1f:0;phoneTimer=0;balconyTimer=0;knockTimer=0;knockIndex=0;
   if(!initializing)SaveCheckpoint(false);
   // Orientation for a first-time player: the lap's minute, as on the hall clock (SCENARIO 9.3).
   Thought(laps[n-1].hour+":"+laps[n-1].minute.ToString("00"),2.5f);
  }
  [Serializable] class Save{public int lap=1;public bool flashlight,chase;}
  public void SaveCheckpoint(bool inChase){if(!testing)System.IO.File.WriteAllText(savePath,JsonUtility.ToJson(new Save{lap=Progress.Lap,flashlight=flat.player.hasFlashlight,chase=inChase}));}
  readonly ScenarioFocus interactionFocus=new ScenarioFocus();
  bool Near(Transform t,float distance=1.65f)=>Vector3.Distance(flat.player.head.position,t.position)<distance;
  bool Looking(Transform t,float cosine=.86f)=>Vector3.Dot(flat.player.head.forward,(t.position-flat.player.head.position).normalized)>cosine;
  void Update(){
   if(Keyboard.current?.escapeKey.wasPressedThisFrame==true&&!(story&&story.EscapeTaken)){Pause(!paused);return;} // Esc leaves the console / album first (pc27)
   if(Time.time>thoughtUntil)subtitle.text="";if(paused||Finished||chase.Active)return;if(story&&story.OwnsFlat)return;prompt.text="";
   if(Busy)return;var p=flat.player;var focus=interactionFocus.Select(flat);bool e=Keyboard.current?.eKey.wasPressedThisFrame==true;
   if(Progress.Lap==6&&!peepholeSeen){knockTimer+=Time.deltaTime;if(knockTimer>=(knockIndex<3?.7f:5)){knockTimer=0;if(knockIndex<3){sound.Play("knock",flat.entrance.transform.position);knockIndex++;if(knockIndex==3)sound.Grandmother(flat.entrance.transform.position);}else knockIndex=0;}}
   if(!p.hasFlashlight&&flat.hallCupboard){
    var cupboard=flat.hallCupboard;
    if(focus==cupboard.interaction){
     prompt.text=cupboard.ReadyToTake?"[E] Взять фонарик-«жучок»":cupboard.door.open?(cupboard.door.BlockedByPlayer?"Отойди немного от дверцы":"Дверца открывается…"):"[E] Открыть тумбу";
     if(e){if(cupboard.ReadyToTake)cupboard.Take(p);else if(!cupboard.door.open)sound.Play(cupboard.door.ToggleWithCue(),cupboard.interaction.position);return;}
    }
   }
   if(!p.hasFlashlight&&!flat.hallCupboard&&Near(flat.torchPickup)&&Looking(flat.torchPickup)){prompt.text="[E] Фонарик-«жучок»";if(e){p.hasFlashlight=true;flat.torchPickup.gameObject.SetActive(false);}}
   if(returning){flat.entrance.locked=false;if(p.transform.position.z<5.6f){flat.entrance.open=false;flat.entrance.locked=true;returning=false;}}
   foreach(var door in new[]{flat.kitchen,flat.bath,flat.bedroom,flat.living})if(!(door==flat.bedroom&&Progress.Lap==5&&phoneAnswered)&&focus==door.transform){prompt.text=door.locked?"[E] Закрыто":"[E] Дверь";if(e){sound.Play(door.ToggleWithCue(),door.transform.position);}}
   if(Near(flat.clock.transform,2)&&Progress.Lap==2&&!clockThought){Thought("Часы идут назад. Это та же ночь. Только наоборот");clockThought=true;}
   if(Progress.Lap==1&&!Progress.EventComplete&&p.head.position.x<2.6f&&Vector3.Distance(p.transform.position,flat.wardrobe.position)<1.5f&&ScenarioSight.Clear(p.transform,p.head.position,flat.wardrobe.position+Vector3.up))StartCoroutine(Event(LapGate.Wardrobe));
   if(Progress.Lap==2&&!Progress.EventComplete){balconyTimer+=Time.deltaTime;if(balconyTimer>3){balconyTimer=0;sound.Play("breath",flat.bed.position);}if(Near(flat.bed,2)&&Looking(flat.bed,.65f)){if(!p.crouched)prompt.text="[Ctrl] Присесть";else{prompt.text="[E] Посмотреть под кровать";if(e)StartCoroutine(Event(LapGate.UnderBed));}}}
   if(Progress.Lap==3&&!Progress.EventComplete){scenes.BalconyWaiting();balconyTimer+=Time.deltaTime;if(balconyTimer>8){balconyTimer=0;sound.Play("balcony",flat.balcony.position);}
    // The lap route crosses the living room ~3.5 m from the glass, so a clear look must also fire the gate, not only the 1.5 m approach.
    var glass=flat.balcony.position+new Vector3(0,.4f,.25f);
    bool seen=Vector3.Distance(p.head.position,glass)<4.6f&&Looking(flat.balcony,.93f)&&ScenarioSight.Clear(p.transform,p.head.position,glass);
    balconySeen=seen?balconySeen+Time.deltaTime:0;
    if(balconySeen>=.6f||Vector3.Distance(p.transform.position,new Vector3(flat.balcony.position.x,p.transform.position.y,flat.balcony.position.z))<1.5f)StartCoroutine(Event(LapGate.BalconyPalm));}
   if(Progress.Lap==4&&!Progress.EventComplete&&Near(flat.television,3)&&Looking(flat.television,.93f)){tvWatch+=Time.deltaTime;if(tvWatch>=3)StartCoroutine(Event(LapGate.Television));}else if(Progress.Lap==4)tvWatch=0;
   if(Progress.Lap==5){if(!phoneAnswered){phoneTimer+=Time.deltaTime;if(phoneTimer>3){phoneTimer=0;sound.Play("phone",flat.phone.position);}
     if(focus==flat.phone){prompt.text="[E] Снять трубку";if(e)StartCoroutine(Phone());}}
    else if(!Progress.EventComplete){
     if(!hangingDoorOpened&&Near(flat.bedroom.transform,1.7f)&&Looking(flat.bedroom.transform,.7f)){prompt.text="[E] Толкнуть дверь";if(e){hangingDoorOpened=true;flat.bedroom.open=true;scenes.PrepareHanging();sound.Play("door_open",flat.bedroom.transform.position);}}
     if(hangingDoorOpened){
      bool inside=p.head.position.x<2.70f&&p.head.position.z>0&&p.head.position.z<4.4f;
      var nina=flat.grandmother.GetComponent<NinaHangingPose>();
      bool visible=inside&&ScenarioSight.Clear(p.transform,p.head.position,nina.FeetPosition)&&ScenarioSight.Clear(p.transform,p.head.position,nina.FacePosition);
      hangingEntryTime=visible?hangingEntryTime+Time.deltaTime:0;
      bool feetSeen=Vector3.Angle(p.head.forward,nina.FeetPosition-p.head.position)<35;
      if(visible&&(feetSeen||hangingEntryTime>=1.5f))StartCoroutine(Event(LapGate.HangingVision));
     }
    }}
   if(focus==flat.carpet.transform){
    prompt.text=Progress.CarpetDoorUnlocked?"[E] Открыть":"[E] Закрыто";gateWait+=Time.deltaTime;
    if(e){flat.carpet.locked=!Progress.CarpetDoorUnlocked;sound.Play(flat.carpet.ToggleWithCue(),flat.carpet.transform.position);if(!Progress.CarpetDoorUnlocked)Thought(Progress.Lap==6?"Стучат… Она у двери. Как тогда":"Закрыто");}
    if(Progress.Lap==1&&gateWait>10&&!Progress.EventComplete){Thought("Закрыто. Что-то тут не так… шифоньер");gateWait=-1000;}
    if(Progress.Lap==2&&gateWait>20&&!Progress.EventComplete){Thought("Под кроватью… дышит");gateWait=-1000;}
    if(Progress.Lap==3&&gateWait>10&&!Progress.EventComplete){Thought("Балкон… За стеклом кто-то есть");gateWait=-1000;}
    if(Progress.Lap==4&&gateWait>10&&!Progress.EventComplete){Thought("Телевизор в зале… он включён");gateWait=-1000;}
   }
   var vestibuleLocal=flat.sourceVestibule.InverseTransformPoint(p.transform.position);
   if(Progress.CarpetDoorUnlocked&&Mathf.Abs(vestibuleLocal.z)<=.16f&&Mathf.Abs(vestibuleLocal.x)<=.18f)StartCoroutine(Warp());
   if(focus==flat.entrance.transform){
    if(returning){prompt.text="[E] Открыть";if(e)sound.Play(flat.entrance.ToggleWithCue(),flat.entrance.transform.position);}
    else if(Progress.Lap==6){prompt.text="[E] Глазок   [R] Открыть дверь";if(e&&!peepholeSeen)StartCoroutine(Event(LapGate.Peephole));if(Keyboard.current?.rKey.wasPressedThisFrame==true)StartCoroutine(Death(6));}
   }
   if(Progress.Lap==6&&peepholeSeen)StartCoroutine(BeginChase());
   if(prompt.text==""&&p.TorchNeedsPump)prompt.text="[F] удерживать — подкачать фонарик";
  }
  IEnumerator Phone(){Busy=true;yield return new WaitForSeconds(sound.Speak(true,flat.phone.position));Thought("…Это не я за дверью! Не открывай!..",sound.PhoneDuration);yield return new WaitForSeconds(sound.Speak(false,flat.phone.position));sound.PlayReceiver("phone_shriek");yield return new WaitForSeconds(1.2f);sound.PlayReceiver("phone_breath");sound.Play("breath",flat.player.transform.position-flat.player.transform.forward-flat.player.transform.right*.5f);phoneAnswered=true;Busy=false;}
  public IEnumerator Event(LapGate gate){
   if(Busy)yield break;Busy=true;LastEvent=gate.ToString();
   if(gate==LapGate.Wardrobe){foreach(var leaf in flat.wardrobeLeaves)leaf.localRotation=Quaternion.identity;sound.Play("wardrobe",flat.wardrobe.position);Thought("Здесь. Здесь она меня…");yield return new WaitForSeconds(.4f);}
   else {yield return scenes.Play(gate.ToString());}
   Progress.Complete(gate);flat.carpet.locked=!Progress.CarpetDoorUnlocked;if(gate==LapGate.Peephole)peepholeSeen=true;else sound.Play("latch",flat.carpet.transform.position);
   Busy=false;
  }
  IEnumerator Warp(){
   Busy=true;var p=flat.player;p.control=false;
   var pose=VestibuleMapping.Map(new Pose(p.transform.position,p.transform.rotation),flat.sourceVestibule,flat.destinationVestibule);
   flat.carpet.open=false;flat.vestibuleInner.open=false;
   yield return new WaitUntil(()=>flat.carpet.IsClosed&&flat.vestibuleInner.IsClosed);
   yield return new WaitForSeconds(.12f); // Render the fully sealed chamber before mapping it.
   if(!Progress.CrossVestibule(flat.carpet.IsClosed,flat.vestibuleInner.IsClosed)){Busy=false;p.control=true;yield break;}
   int n=Progress.Lap;EnterLap(n);p.Place(pose.position,pose.rotation.eulerAngles.y,p.pitch);p.control=true;returning=true;flat.entrance.locked=false;Busy=false;
  }
  public IEnumerator Death(int lap){Busy=true;yield return scenes.Play("DeathShort");flat.look.disruption=1;yield return new WaitForSeconds(1);flat.look.disruption=0;EnterLap(lap);flat.player.Place(ScenarioLayout.Spawn,180);sound.EndDeathRecovery();Busy=false;}
  IEnumerator BeginChase(){Busy=true;chase.PrepareEntry();yield return scenes.Play("ChaseStart");chase.Begin();Busy=false;}
  // After the chase: the story finale (11), unless an automated core test is running.
  public void FinishCore(){if(story&&!coreTestOnly&&(story.chapter==StoryDirector.Chapter.Loop||story.chapter==StoryDirector.Chapter.None)){Finished=false;prompt.text=subtitle.text="";flat.player.chasing=false;foreach(var group in flat.conditionGroups)group.SetActive(false);if(flat.dressing)flat.dressing.Apply(1);story.ChaseFinished();return;}FinishCoreTest();}
  public void EndStory(){story.chapter=StoryDirector.Chapter.None;Finished=true;prompt.text=subtitle.text="";Pause(true);}
  void FinishCoreTest(){Finished=true;prompt.text=subtitle.text="";flat.player.chasing=false;foreach(var group in flat.conditionGroups)group.SetActive(false);if(flat.dressing)flat.dressing.Apply(1);flat.look.WarmReturn();flat.clock.Show(22*3600);flat.player.Place(ScenarioLayout.Spawn,180);Pause(true);}
  public void ResumeSavedLap(){StopAllCoroutines();scenes.Cancel();chase.Stop();sound.ResetWorld();Finished=false;
   if(!System.IO.File.Exists(savePath)){EnterLap(1);flat.player.Place(ScenarioLayout.Spawn,180);return;}
   var s=JsonUtility.FromJson<Save>(System.IO.File.ReadAllText(savePath));flat.player.hasFlashlight=s.flashlight;EnterLap(Mathf.Clamp(s.lap,1,6));flat.player.Place(ScenarioLayout.Spawn,180);if(s.chase)StartCoroutine(BeginChase());}
  // Core-loop test buttons only with an explicit -qa argument (the Development preview build for Dima must not offer them).
  static readonly bool QaMenu=Array.IndexOf(Environment.GetCommandLineArgs(),"-qa")>=0;public static bool QaMenuShown=>QaMenu;
  void OnGUI(){if(!paused)return;var r=new Rect((Screen.width-620)/2,(Screen.height-560)/2,620,560);GUI.Box(r,"");GUILayout.BeginArea(new Rect(r.x+25,r.y+20,r.width-50,r.height-40));
   GUILayout.Label(Finished?"КОНЕЦ":"АТАКА НА ЗАПОМИНАНИЕ",new GUIStyle(GUI.skin.label){fontSize=24});GUILayout.Space(20);
   GUILayout.Label((QaMenu?"QA: тест по SCENARIO.md.":"Сквозная preview-сборка: часть сцен пока титры, модели и звуки временные.")+"\nWASD / мышь · Shift — бег · Ctrl — присесть\nE — действие · F — фонарь, удерживать — подкачка\nC — оглянуться в погоне · Esc — пауза",new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true});GUILayout.Space(25);
   if(!Finished&&story&&story.Active&&GUILayout.Button("Продолжить",GUILayout.Height(42)))Pause(false);
   if(GUILayout.Button("Новая игра",GUILayout.Height(42))){StopAllCoroutines();scenes.Cancel();chase.Stop();sound.ResetWorld();Finished=false;flat.player.hasFlashlight=false;Pause(false);story.NewGame();}
   if(story&&story.HasSave&&!story.Active&&GUILayout.Button("Продолжить с сохранения",GUILayout.Height(36))){StopAllCoroutines();scenes.Cancel();chase.Stop();sound.ResetWorld();Finished=false;Pause(false);story.Continue();}
   if(QaMenu&&!Finished&&(!story||!story.Active)&&GUILayout.Button("Продолжить (тест петли)",GUILayout.Height(30)))Pause(false);
   if(QaMenu&&GUILayout.Button("Петля с начала (тест ядра)",GUILayout.Height(30))){if(story)story.Abandon();StopAllCoroutines();scenes.Cancel();chase.Stop();sound.ResetWorld();Finished=false;flat.player.hasFlashlight=false;EnterLap(1);flat.player.Place(ScenarioLayout.Spawn,180);Pause(false);}
   if(QaMenu&&!Finished&&System.IO.File.Exists(savePath)&&GUILayout.Button("Продолжить с сохранённого круга",GUILayout.Height(32))){if(story)story.Abandon();ResumeSavedLap();Pause(false);}
   if(GUILayout.Button("Выйти",GUILayout.Height(32)))Application.Quit();GUILayout.EndArea();
  }
  void OnDestroy(){Time.timeScale=1;AudioListener.pause=false;}
 }
}
