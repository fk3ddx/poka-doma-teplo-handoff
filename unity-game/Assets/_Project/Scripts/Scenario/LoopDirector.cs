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
  float thoughtUntil,phoneTimer,balconyTimer,tvWatch,gateWait;bool phoneAnswered,peepholeSeen,returning,clockThought;
  string savePath;bool paused=true,initializing=true;float knockTimer;int knockIndex;
  void Start(){savePath=System.IO.Path.Combine(Application.persistentDataPath,"scenario-core-checkpoint.json");EnterLap(1);initializing=false;Pause(true);}
  public void Pause(bool value){paused=value;flat.player.Pause(value);Time.timeScale=value?0:1;AudioListener.pause=value;sound.paused=value;}
  public void Thought(string text,float seconds=4){subtitle.text=text;thoughtUntil=Time.time+seconds;}
  public void EnterLap(int n){
   Progress.RestoreCheckpoint(n);Busy=false;phoneAnswered=false;peepholeSeen=false;returning=false;clockThought=false;tvWatch=0;gateWait=0;
   flat.look.Apply(laps[n-1].condition);flat.clock.Show(Progress.ClockSeconds);flat.carpet.locked=true;flat.carpet.open=false;flat.entrance.open=false;flat.entrance.locked=true;
   if(flat.dressing)flat.dressing.Apply(n);
   flat.living.open=true;flat.living.locked=false;flat.entrance.scripted=false;flat.bedroom.open=n!=5;flat.bedroom.locked=n==5;flat.bedroom.closedAngle=n==5?Mathf.Asin(.2f/.8f)*Mathf.Rad2Deg:0;flat.kitchen.locked=flat.bath.locked=true;
   foreach(var d in flat.wardrobeLeaves)d.localRotation=Quaternion.Euler(0,n==1?95:0,0);
   for(int i=0;i<flat.conditionGroups.Length;i++)flat.conditionGroups[i].SetActive(i==n-1);
   flat.guest.SetActive(false);flat.grandmother.SetActive(false);flat.torchPickup.gameObject.SetActive(!flat.player.hasFlashlight);
   flat.stool.transform.SetPositionAndRotation(new Vector3(5.5f,0,5.8f),Quaternion.identity);flat.slipper.transform.position=new Vector3(1.85f,.01f,3.4f);
   flat.televisionLight.intensity=n==4?.1f:0;phoneTimer=0;balconyTimer=0;knockTimer=0;knockIndex=0;
   if(!initializing)SaveCheckpoint(false);
  }
  [Serializable] class Save{public int lap=1;public bool flashlight,chase;}
  public void SaveCheckpoint(bool inChase){if(!testing)System.IO.File.WriteAllText(savePath,JsonUtility.ToJson(new Save{lap=Progress.Lap,flashlight=flat.player.hasFlashlight,chase=inChase}));}
  bool Near(Transform t,float distance=1.65f)=>Vector3.Distance(flat.player.head.position,t.position)<distance;
  bool Looking(Transform t,float cosine=.86f)=>Vector3.Dot(flat.player.head.forward,(t.position-flat.player.head.position).normalized)>cosine;
  void Update(){
   if(Keyboard.current?.escapeKey.wasPressedThisFrame==true){Pause(!paused);return;}
   if(Time.time>thoughtUntil)subtitle.text="";if(paused||Finished||chase.Active)return;prompt.text="";
   if(Busy)return;var p=flat.player;bool e=Keyboard.current?.eKey.wasPressedThisFrame==true;
   if(Progress.Lap==6&&!peepholeSeen){knockTimer+=Time.deltaTime;if(knockTimer>=(knockIndex<3?.7f:5)){knockTimer=0;if(knockIndex<3){sound.Play("knock",flat.entrance.transform.position);knockIndex++;if(knockIndex==3)sound.Grandmother(flat.entrance.transform.position);}else knockIndex=0;}}
   if(!p.hasFlashlight&&Near(flat.torchPickup)&&Looking(flat.torchPickup)){prompt.text="[E] Фонарик-«жучок»";if(e){p.hasFlashlight=true;flat.torchPickup.gameObject.SetActive(false);}}
   if(returning){flat.entrance.locked=false;if(p.transform.position.z<5.6f){flat.entrance.open=false;flat.entrance.locked=true;returning=false;}}
   foreach(var door in new[]{flat.kitchen,flat.bath,flat.bedroom,flat.living})if(!(door==flat.bedroom&&Progress.Lap==5&&phoneAnswered)&&Near(door.transform)&&Looking(door.transform)){prompt.text=door.locked?"[E] Закрыто":"[E] Дверь";if(e){door.Toggle();sound.Play(door.locked?"latch":door.open?"door_open":"door_close",door.transform.position);}}
   if(Near(flat.clock.transform,2)&&Progress.Lap==2&&!clockThought){Thought("Часы идут назад. Это та же ночь. Только наоборот");clockThought=true;}
   if(Progress.Lap==1&&!Progress.EventComplete&&Vector3.Distance(p.transform.position,flat.wardrobe.position)<1.5f)StartCoroutine(Event(LapGate.Wardrobe));
   if(Progress.Lap==2&&!Progress.EventComplete){balconyTimer+=Time.deltaTime;if(balconyTimer>3){balconyTimer=0;sound.Play("breath",flat.bed.position);}if(Near(flat.bed,2)&&p.crouched&&Looking(flat.bed,.65f)){prompt.text="[E] Посмотреть под кровать";if(e)StartCoroutine(Event(LapGate.UnderBed));}}
   if(Progress.Lap==3&&!Progress.EventComplete){scenes.BalconyWaiting();balconyTimer+=Time.deltaTime;if(balconyTimer>8){balconyTimer=0;sound.Play("balcony",flat.balcony.position);}
    if(Vector3.Distance(p.transform.position,new Vector3(flat.balcony.position.x,p.transform.position.y,flat.balcony.position.z))<1.5f)StartCoroutine(Event(LapGate.BalconyPalm));}
   if(Progress.Lap==4&&!Progress.EventComplete&&Near(flat.television,3)&&Looking(flat.television,.93f)){tvWatch+=Time.deltaTime;if(tvWatch>=3)StartCoroutine(Event(LapGate.Television));}else if(Progress.Lap==4)tvWatch=0;
   if(Progress.Lap==5){if(!phoneAnswered){phoneTimer+=Time.deltaTime;if(phoneTimer>3){phoneTimer=0;sound.Play("phone",flat.phone.position);}
     if(Near(flat.phone)&&Looking(flat.phone)){prompt.text="[E] Снять трубку";if(e)StartCoroutine(Phone());}}
    else if(!Progress.EventComplete&&Near(flat.bedroom.transform,1.7f)&&Looking(flat.bedroom.transform,.7f)){prompt.text="[E] Толкнуть дверь";if(e)StartCoroutine(Event(LapGate.HangingVision));}}
   if(Near(flat.carpet.transform,1.6f)&&Looking(flat.carpet.transform,.7f)){
    prompt.text=Progress.CarpetDoorUnlocked?"[E] Открыть":"[E] Закрыто";gateWait+=Time.deltaTime;
    if(e){flat.carpet.locked=!Progress.CarpetDoorUnlocked;flat.carpet.Toggle();if(!Progress.CarpetDoorUnlocked)Thought(Progress.Lap==6?"Стучат… Она у двери. Как тогда":"Закрыто");}
    if(Progress.Lap==1&&gateWait>10&&!Progress.EventComplete){Thought("Закрыто. Что-то тут не так… шифоньер");gateWait=-1000;}
    if(Progress.Lap==2&&gateWait>20&&!Progress.EventComplete){Thought("Под кроватью… дышит");gateWait=-1000;}
   }
   var vestibuleLocal=flat.sourceVestibule.InverseTransformPoint(p.transform.position);
   if(Progress.CarpetDoorUnlocked&&Mathf.Abs(vestibuleLocal.z)<=.16f&&Mathf.Abs(vestibuleLocal.x)<=.18f)StartCoroutine(Warp());
   if(Near(flat.entrance.transform,1.8f)&&Looking(flat.entrance.transform,.6f)){
    if(returning){prompt.text="[E] Открыть";if(e)flat.entrance.Toggle();}
    else if(Progress.Lap==6){prompt.text="[E] Глазок   [R] Открыть дверь";if(e&&!peepholeSeen)StartCoroutine(Event(LapGate.Peephole));if(Keyboard.current?.rKey.wasPressedThisFrame==true)StartCoroutine(Death(6));}
   }
   if(Progress.Lap==6&&peepholeSeen)StartCoroutine(BeginChase());
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
  public void FinishCore(){Finished=true;prompt.text=subtitle.text="";flat.player.chasing=false;foreach(var group in flat.conditionGroups)group.SetActive(false);if(flat.dressing)flat.dressing.Apply(1);flat.look.WarmReturn();flat.clock.Show(22*3600);flat.player.Place(ScenarioLayout.Spawn,180);Pause(true);}
  void OnGUI(){if(!paused)return;var r=new Rect((Screen.width-620)/2,(Screen.height-420)/2,620,420);GUI.Box(r,"");GUILayout.BeginArea(new Rect(r.x+25,r.y+20,r.width-50,r.height-40));
   GUILayout.Label(Finished?"КОНЕЦ ТЕСТА ЯДРА":"АТАКА НА ЗАПОМИНАНИЕ · ПЕТЛЯ",new GUIStyle(GUI.skin.label){fontSize=24});GUILayout.Space(20);
   GUILayout.Label("Тест по SCENARIO.md. Временные модели и звуки.\nWASD / мышь · Shift — бег · Ctrl — присесть\nE — действие · F — фонарь, удерживать — подкачка\nC — оглянуться в погоне · Esc — пауза",new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true});GUILayout.Space(25);
   if(!Finished&&GUILayout.Button("Продолжить",GUILayout.Height(42)))Pause(false);
   if(GUILayout.Button("С начала петли",GUILayout.Height(36))){StopAllCoroutines();scenes.Cancel();chase.Stop();sound.ResetWorld();Finished=false;flat.player.hasFlashlight=false;EnterLap(1);flat.player.Place(ScenarioLayout.Spawn,180);Pause(false);}
   if(!Finished&&System.IO.File.Exists(savePath)&&GUILayout.Button("Продолжить с сохранённого круга",GUILayout.Height(32))){var s=JsonUtility.FromJson<Save>(System.IO.File.ReadAllText(savePath));StopAllCoroutines();scenes.Cancel();chase.Stop();sound.ResetWorld();flat.player.hasFlashlight=s.flashlight;EnterLap(Mathf.Clamp(s.lap,1,6));flat.player.Place(ScenarioLayout.Spawn,180);Pause(false);if(s.chase)StartCoroutine(BeginChase());}
   if(GUILayout.Button("Выйти",GUILayout.Height(32)))Application.Quit();GUILayout.EndArea();
  }
  void OnDestroy(){Time.timeScale=1;AudioListener.pause=false;}
 }
}
