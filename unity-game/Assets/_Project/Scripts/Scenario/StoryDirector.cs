using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Ataka.Scenario {
 // The whole game around the loop core: prologue at the door (3.8–3.9), evening (4), first night (6) with the full
 // face-tear death, the single rewind (6.1), second night (8), hand-off to the existing loop (9) and chase (10),
 // finale and epilogue (11). Lines and timings are SCENARIO.md; nothing here invents story.
 // Environments that do not exist in the flat (yard, stairwell, Valya on the landing) are presented as title text
 // and off-screen voice — listed as temporary in docs/reviews/2026-10-12/claude-playable-complete/README.md.
 public sealed class StoryDirector:MonoBehaviour {
  public enum Chapter {None,Evening,Night1,Night2,Loop,Finale,Ended}
  public LoopDirector loop;public Chapter chapter=Chapter.None;
  public bool OwnsFlat=>chapter==Chapter.Evening||chapter==Chapter.Night1||chapter==Chapter.Night2||chapter==Chapter.Finale||chapter==Chapter.Ended;
  public bool Active=>chapter!=Chapter.None;
  FlatBindings flat=>loop.flat;ScenarioSound sound=>loop.sound;ScenarioTimeline scenes=>loop.scenes;ScenarioPlayer player=>loop.flat.player;
  ScenarioTelevision tv;string objective="";string card;float fade;bool belt,beltTied,openedDoorFinale;int night;
  Coroutine running;LineRenderer strap;
  string savePath;
  [Serializable] class Save {public string chapter;public bool flashlight,belt;}

  // ---------- interaction (all story actions go through one aimed-target list) ----------
  public sealed class Act {public Func<Vector3> at;public string label;public float range=1.8f,hold,cos=.86f;public Key key=Key.E;public Func<bool> when;public bool fired;public Action done;public Action<bool> holding;}
  readonly List<Act> acts=new List<Act>();Act aimed;float held;
  Act Offer(Func<Vector3> at,string label,float hold=0,float range=1.8f,Key key=Key.E,Func<bool> when=null,Action done=null){var a=new Act{at=at,label=label,hold=hold,range=range,key=key,when=when,done=done};acts.Add(a);return a;}
  IEnumerator Use(Vector3 point,string label,float hold=0,float range=1.8f){var a=Offer(()=>point,label,hold,range);while(!a.fired)yield return null;}
  IEnumerator Use(Func<Vector3> at,string label,float hold=0,float range=1.8f){var a=Offer(at,label,hold,range);while(!a.fired)yield return null;}
  // A held action with its own sound while the key is down (tap, clock key, bulb); the sound fades if let go.
  IEnumerator UseHeld(Vector3 point,string label,float hold,float range,string cue,Vector3 soundAt){var a=Offer(()=>point,label,hold,range);a.holding=on=>{if(on)sound.Play(cue,soundAt);else sound.ReleaseCue(cue);};while(!a.fired)yield return null;}
  Act holdingAct;void Holding(Act a){if(holdingAct==a)return;var was=holdingAct;holdingAct=a;was?.holding?.Invoke(false);a?.holding?.Invoke(true);}
  void Withdraw(Act a){if(a!=null)acts.Remove(a);}
  void TickActs(){
   aimed=null;float best=-1;var eye=player.head.position;var forward=player.head.forward;
   foreach(var a in acts){if(a.fired||(a.when!=null&&!a.when()))continue;var point=a.at();var d=point-eye;if(d.magnitude>a.range)continue;float dot=Vector3.Dot(forward,d.normalized);if(dot>a.cos&&dot>best&&Reachable(eye,point)){best=dot;aimed=a;}}
   string text="";
   if(aimed!=null){
    var k=Keyboard.current;bool pressed=k!=null&&k[aimed.key].wasPressedThisFrame,down=k!=null&&k[aimed.key].isPressed;string key=aimed.key==Key.E?"E":aimed.key.ToString();
    if(aimed.hold<=0){Holding(null);text="["+key+"] "+aimed.label;if(pressed)Fire(aimed);}
    else{held=down?held+Time.deltaTime:0;Holding(held>0?aimed:null);text="["+key+" удерживать] "+aimed.label+(held>0?"  "+Mathf.RoundToInt(100*Mathf.Clamp01(held/aimed.hold))+"%":"");if(held>=aimed.hold){held=0;Holding(null);Fire(aimed);}}
   }else{held=0;Holding(null);}
   // Two prompts on one door (peephole / open) are shown together.
   foreach(var a in acts.ToArray())if(a!=aimed&&!a.fired&&(a.when==null||a.when())&&aimed!=null&&(a.at()-aimed.at()).sqrMagnitude<.05f){var k=Keyboard.current;text+="   ["+a.key+"] "+a.label;if(k!=null&&k[a.key].wasPressedThisFrame)Fire(a);}
   loop.prompt.text=text;
  }
  float lastProgress;
  // No action through walls: the first thing on the line from the eye must be the target's own surface
  // (within 0.4 m of the target point), never a wall, door or furniture in between.
  bool Reachable(Vector3 eye,Vector3 point){var d=point-eye;float length=d.magnitude;if(length<.05f)return true;
   foreach(var hit in Physics.RaycastAll(eye,d/length,length,~(1<<31),QueryTriggerInteraction.Ignore)){
    if(hit.transform.IsChildOf(player.transform)||hit.transform.IsChildOf(flat.grandmother.transform))continue;if(hit.distance<length-.4f)return false;}
   return true;}
  // QA: the same line-of-sight rule as the prompt, its first blocker as a full path with bounds, and a walkable route
  // for the player (her grid, closed room doors may be opened — as the player would).
  public bool QaReachable(Vector3 eye,Vector3 point)=>Reachable(eye,point);
  public string QaLosBlocker(Vector3 eye,Vector3 point){var d=point-eye;float length=d.magnitude;if(length<.05f)return "none";RaycastHit first=default;bool any=false;
   foreach(var hit in Physics.RaycastAll(eye,d/length,length,~(1<<31),QueryTriggerInteraction.Ignore)){if(hit.transform.IsChildOf(player.transform)||hit.transform.IsChildOf(flat.grandmother.transform))continue;if(hit.distance<length-.4f&&(!any||hit.distance<first.distance)){first=hit;any=true;}}
   return any?GranNav.PathOf(first.collider.transform)+" ["+first.collider.GetType().Name+"] at "+first.point.ToString("F2")+" bounds c="+first.collider.bounds.center.ToString("F2")+" size="+first.collider.bounds.size.ToString("F2"):"none";}
  public List<ScenarioDoor> QaPlayerRoute(Vector3 from,Vector3 to){Nav.Ensure();var r=Nav.Find(from,to,QaCanOpen);return r.steps!=null?r.crossed:null;}
  void Fire(Act a){a.fired=true;acts.Remove(a);lastProgress=Time.time;a.done?.Invoke();}
  // QA surface for StoryAutopilot (-story-smoke): it may only fire an act after the real aim selection picked it.
  public List<Act> OpenActs=>acts;public Act Aimed=>aimed;public void QaFire(Act a){if(a==aimed)Fire(a);}
  // A second key on the same aimed spot (peephole E / open R) is reachable whenever its partner is aimed.
  public void QaFireCompanion(Act a){if(aimed!=null&&(a==aimed||(a.at()-aimed.at()).sqrMagnitude<.05f))Fire(a);}
  public bool Choosing=>choiceA!=null;public void QaChoose(int c){choice=c;}public string CurrentCard=>card;public string Objective=>objective;
  public void QaBegin(string at){Begin(at);}
  public bool QaBelt=>beltTied;public int QaRestarts{get;private set;}public bool qaAllowSave;
  public void QaSavePath(string path){savePath=path;}public string SavePath=>savePath;

  // ---------- presentation ----------
  void Say(string who,string line,float seconds=0){loop.Thought(string.IsNullOrEmpty(who)?line:who+": «"+line+"»",seconds>0?seconds:Mathf.Clamp(line.Length*.07f,2.5f,7f));}
  IEnumerator Line(string who,string line,float seconds=0){float s=seconds>0?seconds:Mathf.Clamp(line.Length*.07f,2.5f,7f);Say(who,line,s);yield return new WaitForSeconds(s);}
  void Task(string text){objective=text;lastProgress=Time.time;}
  void Clock(int h,int m,float s=0){flat.clock.Show(h*3600+m*60+s);}
  IEnumerator Fade(float to,float seconds){float from=fade;for(float t=0;t<seconds;t+=Time.deltaTime){fade=Mathf.Lerp(from,to,t/seconds);yield return null;}fade=to;}
  IEnumerator Card(string text,float seconds){card=text;yield return new WaitForSeconds(seconds);card=null;}
  int choice;
  IEnumerator Choose(string a,string b,float timeout=0){choice=0;card=null;choiceA=a;choiceB=b;float until=timeout>0?Time.time+timeout:float.MaxValue;
   while(choice==0&&Time.time<until){var k=Keyboard.current;if(k!=null){if(k.digit1Key.wasPressedThisFrame)choice=1;else if(k.digit2Key.wasPressedThisFrame)choice=2;}yield return null;}choiceA=choiceB=null;}
  string choiceA,choiceB;

  // ---------- grandmother (approved Katcho, author clips Oidle/Owalk) ----------
  Animation granAnim;string granIdle,granWalk;
  static readonly Dictionary<string,Vector3> Nodes=new Dictionary<string,Vector3>{
   {"Door",new Vector3(7.85f,0,5.25f)},{"HallA",new Vector3(7.75f,0,4.4f)},{"KitchenDoor",new Vector3(7.38f,0,3.25f)},{"Kitchen",new Vector3(7.5f,0,2.1f)},{"Stove",new Vector3(7.9f,0,1.57f)},
   {"HallJoint",new Vector3(6.9f,0,5.25f)},{"HallB",new Vector3(5.65f,0,5.3f)},{"Clock",new Vector3(5.55f,0,5.3f)},{"LivingDoor",new Vector3(5.65f,0,4.0f)},{"LivingMid",new Vector3(4.4f,0,3.5f)},
   {"Aisle",new Vector3(4.15f,0,2.8f)},{"Armchair",new Vector3(4.3f,0,1.28f)},{"BedroomDoor",new Vector3(3.2f,0,3.75f)},{"Bedroom",new Vector3(2.0f,0,3.5f)},{"Landing",new Vector3(8.05f,0,6.62f)}};
  string granNode="Door";
  // Walks the story actually orders (SCENARIO 3.9, 4.1–4.9, 8, 11.7), validated in the real door states.
  static readonly string[][] StoryWalks={new[]{"Door","Kitchen"},new[]{"Kitchen","HallA"},new[]{"HallA","Door"},new[]{"Door","HallB"},new[]{"HallB","Clock"},new[]{"Clock","Armchair"},new[]{"Armchair","Door"},new[]{"Door","Landing"},new[]{"Door","Stove"}};
  GranNav nav;
  GranNav Nav=>nav??=new GranNav(()=>new[]{player.transform,flat.grandmother.transform,flat.guest.transform},NavState);
  string NavState(){var sb=new System.Text.StringBuilder();foreach(var d in new[]{flat.entrance,flat.kitchen,flat.bath,flat.bedroom,flat.living,flat.carpet,flat.vestibuleInner,flat.balconyDoor})if(d&&d.leaf)sb.Append(d.open?'o':'c').Append(Mathf.RoundToInt(d.leaf.localEulerAngles.y/5)).Append(',');
   var st=flat.stool;sb.Append(st&&st.activeSelf?st.transform.position.ToString("F1"):"-");return sb.ToString();}
  // She opens only unlocked doors; the flat door is opened by the story itself before she walks through it.
  bool RoomDoor(ScenarioDoor d)=>d&&(d==flat.kitchen||d==flat.bath||d==flat.bedroom||d==flat.living); // never the dresser cupboard
  bool CanOpen(ScenarioDoor d)=>RoomDoor(d)&&!d.locked;
  bool QaCanOpen(ScenarioDoor d)=>CanOpen(d)||d==flat.entrance;
  Vector3 NodeAt(string node){var raw=Nodes[node];Nav.Ensure();return Nav.Snap(raw,.6f,out var q)?q:raw;}
  // QA: every story walk planned with her capsule (r=GranNav.Radius, h=GranNav.Height) in the CURRENT door states;
  // each crossed door is then opened to its real 95° pose and the route re-planned with that leaf as an obstacle.
  public List<string> QaValidateRoutes(List<string> report){
   var bad=new List<string>();var nav=Nav;nav.Invalidate();nav.Ensure();
   report.Add("ROUTE capsule r="+GranNav.Radius+" h="+GranNav.Height+" lift="+GranNav.Lift+" grid="+GranNav.Cell+" m; runtime collider world r="+(granBody?granBody.radius*Mathf.Max(granBody.transform.lossyScale.x,granBody.transform.lossyScale.z):0).ToString("F3")+" h="+(granBody?granBody.height*granBody.transform.lossyScale.y:0).ToString("F3")+"; doors "+NavState());
   foreach(var kv in Nodes){if(!nav.Snap(kv.Value,.6f,out var q)){bad.Add("node "+kv.Key+" "+kv.Value.ToString("F2")+": no free floor within 0.6 m; blockers: "+nav.Blockers(kv.Value,.1f));continue;}
    float moved=Vector3.Distance(q,kv.Value);if(moved>.06f)report.Add("ROUTE node "+kv.Key+" "+kv.Value.ToString("F2")+" → free floor "+q.ToString("F2")+" ("+moved.ToString("F2")+" m); at the authored spot: "+nav.Blockers(kv.Value,0));}
   foreach(var w in StoryWalks){
    var r=nav.Find(Nodes[w[0]],Nodes[w[1]],QaCanOpen);string name=w[0]+"→"+w[1];
    if(r.steps==null){bad.Add(name+": no route; closest reachable "+r.closest.ToString("F2")+" ("+r.closestToGoal.ToString("F2")+" m short); blockers there: "+nav.Blockers(r.closest,.12f));continue;}
    report.Add("ROUTE "+name+" "+Length(r.steps).ToString("F1")+" m"+(r.crossed.Count>0?" opening "+string.Join(", ",r.crossed.ConvertAll(d=>d.name)):""));
    if(r.crossed.Count==0)continue;
    // The opened leaf is a real obstacle: open it to its pose and plan again without crossing.
    var saved=new List<(ScenarioDoor d,bool open,Quaternion rot)>();foreach(var d in r.crossed){saved.Add((d,d.open,d.leaf.localRotation));d.open=true;d.leaf.localRotation=Quaternion.Euler(0,d.direction*95,0);}
    nav.Invalidate();nav.Ensure();var again=nav.Find(Nodes[w[0]],Nodes[w[1]],null);
    if(again.steps==null)bad.Add(name+" with the opened leaf in place: no route; closest "+again.closest.ToString("F2")+"; blockers: "+nav.Blockers(again.closest,.12f));
    else report.Add("ROUTE "+name+" with opened leaf "+Length(again.steps).ToString("F1")+" m");
    foreach(var x in saved){x.d.open=x.open;x.d.leaf.localRotation=x.rot;}nav.Invalidate();nav.Ensure();}
   return bad;}
  static float Length(List<GranNav.Step> steps){float l=0;for(int i=1;i<steps.Count;i++)l+=Vector3.Distance(steps[i-1].at,steps[i].at);return l;}
  void GranShow(string node,float yaw){granToken++;granWalking=false;var g=flat.grandmother;g.SetActive(true);granNode=node;g.transform.SetPositionAndRotation(NodeAt(node),Quaternion.Euler(0,yaw,0));GranPlay(granIdle);}
  void GranPlay(string clip){if(!granAnim||string.IsNullOrEmpty(clip))return;granAnim.enabled=true;var state=granAnim[clip];if(state==null)return;state.wrapMode=WrapMode.Loop;granAnim.CrossFade(clip,.2f);}
  // Full walk around the furniture on the grid; a closed unlocked door on the way is opened by her (she stops
  // clear of its swing, opens it, waits for the leaf, then re-plans with the open leaf as an obstacle).
  IEnumerator GranWalk(string to,float speed=.9f){
   var g=flat.grandmother.transform;if(!flat.grandmother.activeSelf)GranShow(granNode,0);
   int my=++granToken; // a newer walk order cancels an older one
   GranPlay(granWalk);granWalking=true;granTarget=to;granWaiting=0;bool aroundPlayer=false,askedToPass=false;
   while(true){
    // A player standing in her way for >1.2 s is walked around (re-plan with his capsule as an obstacle);
    // if there is no way around (doorway), she keeps waiting. Furniture and door collisions are never relaxed.
    Nav.Ensure();Nav.Avoid=aroundPlayer?player.transform.position:(Vector3?)null;Nav.AvoidRadius=GranNav.Radius+PlayerRadius+.08f;
    var r=Nav.Find(g.position,Nodes[to],CanOpen);
    if(r.steps==null){
     Nav.Avoid=null;
     if(aroundPlayer){aroundPlayer=false;GranPlay(granIdle);for(float t=0;t<1.2f;t+=Time.deltaTime){if(my!=granToken)yield break;granWaiting+=Time.deltaTime;yield return null;}GranPlay(granWalk);continue;}
     Debug.LogWarning("GRAN_ROUTE_FAIL "+granNode+"→"+to+" from "+g.position.ToString("F2")+" closest "+r.closest.ToString("F2")+": "+Nav.Blockers(r.closest,.12f));break;}
    int cut=r.steps.FindIndex(x=>x.door!=null);var door=cut>=0?r.steps[cut].door:null;var pts=new List<Vector3>{g.position};
    if(door){var hinge=door.leaf.position;hinge.y=0;float clear=GranNav.DoorWidth(door)+GranNav.Radius+.1f;int last=-1;
     for(int i=0;i<cut;i++){var d=r.steps[i].at-hinge;d.y=0;if(d.magnitude>=clear)last=i;}for(int i=1;i<=last;i++)pts.Add(r.steps[i].at);}
    else for(int i=1;i<r.steps.Count;i++)pts.Add(r.steps[i].at);
    var path=Nav.Smooth(pts);Nav.Avoid=null;bool replan=false;float waited=0;
    foreach(var target in path){while(true){if(my!=granToken)yield break;var d=target-g.position;d.y=0;if(d.magnitude<.05f)break;g.rotation=Quaternion.Slerp(g.rotation,Quaternion.LookRotation(d),1-Mathf.Exp(-8*Time.deltaTime));
      if(PlayerInTheWay(g.position,target)){GranPlay(granIdle);granWaiting+=Time.deltaTime;waited+=Time.deltaTime;if(granWaiting>4&&!askedToPass){askedToPass=true;Say("Бабушка","Мишаня, пропусти-ка",2.5f);}if(waited>1.2f){replan=true;break;}yield return null;GranPlay(granWalk);continue;}
      granWaiting=0;g.position=Vector3.MoveTowards(g.position,target,speed*Time.deltaTime);yield return null;}
     if(replan)break;}
    if(replan){aroundPlayer=true;GranPlay(granWalk);continue;}
    aroundPlayer=false;
    if(!door){granNode=to;break;}
    GranPlay(granIdle);yield return GranFace(door.transform.position,.4f);if(my!=granToken)yield break;
    door.open=true;sound.Play("door_open",door.transform.position);
    for(float t=0;t<1.6f&&Quaternion.Angle(door.leaf.localRotation,Quaternion.identity)<85;t+=Time.deltaTime){if(my!=granToken)yield break;yield return null;}
    GranPlay(granWalk);}
   GranPlay(granIdle);granWalking=false;granWaiting=0;granTarget=null;}
  float granWaiting;string granTarget;
  float PlayerRadius{get{var cc=player.GetComponent<CharacterController>();return cc?cc.radius:.3f;}}
  // QA: where she is, where she goes and how long she has been waiting for the player.
  public float QaGranWaiting=>granWaiting;public bool QaGranWalking=>granWalking;
  public string QaGranState=>flat.grandmother.activeSelf?"gran "+flat.grandmother.transform.position.ToString("F2")+(granWalking?" walking to "+granTarget+" waiting "+granWaiting.ToString("F1")+"s":" standing at "+granNode):"gran hidden";
  public Vector3 QaGranPosition=>flat.grandmother.transform.position;public Vector3? QaGranGoal=>granTarget!=null?Nodes[granTarget]:(Vector3?)null;
  IEnumerator GranFace(Vector3 point,float seconds=.6f){var g=flat.grandmother.transform;var d=point-g.position;d.y=0;if(d.sqrMagnitude<.001f)yield break;var from=g.rotation;var to=Quaternion.LookRotation(d);for(float t=0;t<seconds;t+=Time.deltaTime){g.rotation=Quaternion.Slerp(from,to,t/seconds);yield return null;}g.rotation=to;}
  void GranLookAtPlayerWhenNear(){if(!flat.grandmother.activeSelf||granWalking)return;var g=flat.grandmother.transform;var d=player.transform.position-g.position;d.y=0;if(d.magnitude<2.2f&&d.sqrMagnitude>.01f)g.rotation=Quaternion.Slerp(g.rotation,Quaternion.LookRotation(d),1-Mathf.Exp(-3*Time.deltaTime));}
  bool granWalking;int granToken;CapsuleCollider granBody;
  bool PlayerInTheWay(Vector3 from,Vector3 to){var p=player.transform.position;p.y=0;from.y=0;to.y=0;var seg=to-from;float len=seg.magnitude;if(len<.01f)return false;
   float along=Vector3.Dot(p-from,seg/len);if(along<0||along>Mathf.Min(len,.9f))return false;return Vector3.Distance(from+seg/len*along,p)<.5f;}
  IEnumerator GranGo(string to){yield return GranWalk(to);}

  // ---------- lifecycle ----------
  void Awake(){if(!loop)loop=GetComponent<LoopDirector>();savePath=System.IO.Path.Combine(Application.persistentDataPath,"story-checkpoint.json");}
  void Start(){granBody=flat.grandmother.GetComponent<CapsuleCollider>();if(!granBody){granBody=flat.grandmother.AddComponent<CapsuleCollider>();}var gs=flat.grandmother.transform.lossyScale;granBody.radius=GranNav.Radius/Mathf.Max(.01f,Mathf.Max(gs.x,gs.z));granBody.height=GranNav.Height/Mathf.Max(.01f,gs.y);granBody.center=Vector3.up*granBody.height*.5f;granBody.direction=1;granBody.enabled=false;
   RememberProps();tv=FindFirstObjectByType<ScenarioTelevision>();var anim=flat.grandmother.GetComponentInChildren<Animation>(true);granAnim=anim;
   if(anim)foreach(AnimationState s in anim){if(s.name.EndsWith("Oidle"))granIdle=s.name;if(s.name.EndsWith("Owalk"))granWalk=s.name;}}
  public bool HasSave=>System.IO.File.Exists(savePath);
  void Checkpoint(string at){if(loop.testing&&!qaAllowSave)return;System.IO.File.WriteAllText(savePath,JsonUtility.ToJson(new Save{chapter=at,flashlight=player.hasFlashlight,belt=belt}));}
  public void NewGame(){Begin("Prologue");}
  public void Continue(){var s=HasSave?JsonUtility.FromJson<Save>(System.IO.File.ReadAllText(savePath)):null;Begin(s?.chapter??"Prologue",s);}
  void Begin(string at,Save s=null){
   StopAll();loop.Thought("",0);player.hasFlashlight=s!=null&&s.flashlight;belt=s!=null&&s.belt;
   if(at=="Loop"){chapter=Chapter.Loop;flat.entrance.direction=1;loop.ResumeSavedLap();return;}
   running=StartCoroutine(at switch{"Evening"=>Evening(false),"Night1"=>Night1(false),"Night2"=>Night2(false),"Finale"=>Finale(),_=>Prologue()});
  }
  public void Abandon(){StopAll();chapter=Chapter.Loop;flat.entrance.direction=1;}
  void StopAll(){if(running!=null)StopCoroutine(running);StopAllCoroutines();acts.Clear();holdingAct=null;kettleBoiling=false;card=null;fade=0;objective="";choiceA=choiceB=null;scenes.Cancel();loop.chase.Stop();sound.ResetWorld();if(tv)tv.storyOn=false;if(strap)strap.enabled=false;}
  void Update(){
   if(granBody)granBody.enabled=OwnsFlat&&chapter!=Chapter.Ended; // in the loop the hanging vision needs clear sight to her
   if(!OwnsFlat||player.paused)return;
   if(chapter!=Chapter.Ended&&!loop.Busy&&string.IsNullOrEmpty(scenes.PlayingScene))TickActs();else loop.prompt.text="";
   GranLookAtPlayerWhenNear();
   // SCENARIO 12: idle 40 s in the evening -> current task reminder.
   if(chapter==Chapter.Evening&&!string.IsNullOrEmpty(objective)&&objective!="Свободное время"&&Time.time-lastProgress>40){lastProgress=Time.time;Say("","Бабушка просила… "+objective.ToLowerInvariant(),3.5f);}
  }

  // Common world state for the apartment outside the loop: warm, doors normal, loop props hidden.
  void Homely(bool warm){
   loop.Progress.RestoreCheckpoint(1);foreach(var g in flat.conditionGroups)g.SetActive(false);if(flat.dressing)flat.dressing.Apply(1);
   if(warm)flat.look.WarmReturn();else flat.look.Apply(FlatCondition.Blackout);
   flat.kitchen.locked=flat.bath.locked=false;flat.bedroom.locked=false;flat.bedroom.closedAngle=0;flat.living.locked=false;flat.living.open=true;
   // The flat door swings into the hall in the story: the 1 m vestibule leaves no room for an outward swing past the player.
   flat.carpet.locked=true;flat.carpet.open=false;flat.entrance.scripted=false;flat.entrance.direction=-1;flat.balconyDoor.locked=true;flat.balconyDoor.open=false;flat.balconyDoor.closedAngle=0;
   foreach(var d in flat.wardrobeLeaves)d.localRotation=Quaternion.identity;
   flat.guest.SetActive(false);flat.slipper.SetActive(false);flat.televisionLight.intensity=0;flat.clock.stopped=false;
   if(flat.hallCupboard)flat.hallCupboard.ResetForLap();flat.torchPickup.gameObject.SetActive(!player.hasFlashlight);
  }

  // ---------- household sound (SCENARIO 14): beds the story owns; the loop keeps dead silence + clock ----------
  static readonly Vector3 FridgeAt=new Vector3(8.24f,1f,2.43f),RadioAt=new Vector3(8.73f,1.45f,2f),SinkAt=new Vector3(6.9f,.95f,1.6f),StoveAt=new Vector3(8.48f,.95f,1.65f),TableAt=new Vector3(7.58f,.8f,.55f),StreetAt=new Vector3(4.5f,-3f,-9f);
  void HouseSound(bool radio,bool night){sound.Bed("fridge",FridgeAt,true);sound.Bed("radio",RadioAt,radio);sound.Every("drip",SinkAt,4,9,night);sound.Every("car",StreetAt,45,110,night);}
  // Before a frightening event the world goes out in layers: radio, fridge, drips/street (the clock stays).
  IEnumerator HouseQuiet(){sound.Bed("radio",RadioAt,false);yield return new WaitForSeconds(.6f);sound.Bed("fridge",FridgeAt,false);yield return new WaitForSeconds(.6f);sound.Every("drip",SinkAt,0,0,false);sound.Every("car",StreetAt,0,0,false);}
  IEnumerator LightGas(){sound.Play("switch",StoveAt);sound.Play("match",StoveAt);yield return new WaitForSeconds(.5f);sound.Play("gas_ignite",StoveAt);sound.Bed("gas",StoveAt,true);}
  void GasOff(bool click=true){if(click)sound.Play("switch",StoveAt);sound.Bed("gas",StoveAt,false);sound.Bed("kettle",StoveAt,false);sound.ReleaseCue("kettle_rise");kettleBoiling=false;}
  bool kettleBoiling;
  IEnumerator KettleBoils(){kettleBoiling=true;sound.Play("kettle_rise",StoveAt);for(float t=0;t<6.5f;t+=Time.deltaTime){if(!kettleBoiling)yield break;yield return null;}if(kettleBoiling)sound.Bed("kettle",StoveAt,true);}

  // ---------- 3. Prologue (only the door scene exists in the flat) ----------
  IEnumerator Prologue(){
   chapter=Chapter.Evening;loop.Pause(false);fade=1;Homely(true);
   flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(7.15f,0,1.2f),Quaternion.identity);
   flat.entrance.open=false;flat.entrance.locked=true;player.Place(Nodes["Landing"],180);player.control=false;Clock(19,10);flat.clock.stopped=true;if(scenes.landingLight){scenes.landingLight.color=new Color(1,.9f,.74f);scenes.landingLight.intensity=.7f;}
   yield return Card("15 ноября 1996. 19:10",3);
   yield return Card("Подъезд, пятый этаж. Холодно — изо рта идёт пар.\nЗа дверью 59 лает Тузик.",3.5f);
   yield return Fade(0,1.2f);player.control=true;Task("Позвонить в квартиру 60");
   yield return Use(flat.entrance.transform.position+Vector3.up*.3f,"Позвонить");
   sound.Play("doorbell",flat.entrance.transform.position);yield return new WaitForSeconds(.45f);sound.Play("doorbell",flat.entrance.transform.position);yield return new WaitForSeconds(2.2f);
   sound.Play("latch",flat.entrance.transform.position);yield return new WaitForSeconds(.45f);sound.Play("latch",flat.entrance.transform.position);yield return new WaitForSeconds(.4f);sound.Play("latch",flat.entrance.transform.position);yield return new WaitForSeconds(.4f);
   HouseSound(true,false);GranShow("Door",0);flat.entrance.locked=false;flat.entrance.open=true;sound.Play("door_open",flat.entrance.transform.position);
   yield return Line("Бабушка","Мишаня! Приехал! Замёрз весь — ну-ка, заходи, разувайся. Мать звонила, сказала — с сумкой, тяжёлой небось");
   StartCoroutine(GranGo("Kitchen"));Task("Войти");
   while(player.transform.position.z>5.85f)yield return null;
   flat.entrance.open=false;sound.Play("door_close",flat.entrance.transform.position);yield return new WaitForSeconds(.6f);flat.entrance.locked=true;if(scenes.landingLight)scenes.landingLight.intensity=0;
   yield return Evening(true);
  }

  // ---------- 4. Evening ----------
  IEnumerator Evening(bool continued){
   chapter=Chapter.Evening;
   if(!continued){loop.Pause(false);Homely(true);flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(7.15f,0,1.2f),Quaternion.identity);player.Place(ScenarioLayout.Spawn,180);GranShow("Kitchen",90);yield return Fade(0,.8f);}
   Checkpoint("Evening");HouseSound(true,false);flat.clock.stopped=true;Clock(19,15);OfferFlashlight();PropsAtEveningStart();
   // 4.1
   Task("Повесить куртку и разуться");
   yield return Use(new Vector3(8.72f,1.6f,5.1f),"Повесить куртку");sound.Play("carpet_step",player.transform.position);
   yield return Use(new Vector3(8.55f,.2f,5.2f),"Переобуться в тапки",0,2.2f);
   yield return Line("Бабушка","Тапки дедовы надевай, твои малы уже");
   // 4.2
   Clock(19,20);Task("Разобрать сумку на кухне");
   yield return Use(new Vector3(7.58f,.8f,.6f),"Поставить сумку на стол");Show("Bread",true);Show("Cans",true);
   yield return Use(new Vector3(7.4f,.8f,.6f),"Батон — в хлебницу");Show("Bread",false);
   yield return Use(new Vector3(6.85f,1.5f,1.6f),"Шпроты — в шкаф");Show("Cans",false);
   yield return Use(()=>flat.grandmother.transform.position+Vector3.up*1.2f,"Отдать «Корвалол» бабушке",0,2);
   yield return Line("Бабушка","Вот спасибо, а то кончился");
   // 4.3
   Clock(19,30);Task("Поставить чайник");
   yield return Use(new Vector3(8.24f,1.58f,2.43f),"Взять спички с ЗиЛа");Show("Matchbox",false);
   yield return Use(new Vector3(8.48f,.9f,1.65f),"Открыть газ и зажечь конфорку",.6f);yield return LightGas();
   var flame=FlameLight(new Vector3(8.48f,.95f,1.65f));
   yield return Use(()=>Prop("Kettle")?Prop("Kettle").position+Vector3.up*.12f:new Vector3(8.48f,.95f,1.85f),"Взять чайник",0,2);Show("Kettle",false);
   yield return UseHeld(new Vector3(6.9f,1f,1.6f),"Набрать воду в чайник",3,1.8f,"tap",SinkAt);
   yield return Use(new Vector3(8.48f,.95f,1.65f),"Поставить чайник на огонь");Put("Kettle",new Vector3(8.48f,.86f,1.55f));
   yield return Line("Бабушка","Пока греется — сходи на балкон, банку вишнёвого принеси. Только дверь там… шпингалет заедает. Дед всё ремнём её подвязывал, пока не помер, а я так и не починила",8);
   Task("Принести с балкона банку вишнёвого");
   yield return Use(flat.balcony.position+Vector3.up*.9f,"Отодвинуть шпингалет (заедает)",2);
   sound.Play("creak",flat.balcony.position);flat.balconyDoor.locked=false;flat.balconyDoor.open=true;sound.Play("wind",flat.balcony.position);
   yield return Use(()=>Prop("JamJar")?Prop("JamJar").position+Vector3.up*.08f:new Vector3(5.6f,1f,-.85f),"Взять банку варенья",0,2.2f);Show("JamJar",false);
   yield return Use(flat.balcony.position+Vector3.up*.9f,"Закрыть балконную дверь");
   flat.balconyDoor.open=false;flat.balconyDoor.closedAngle=1.6f;sound.Play("door_close",flat.balcony.position);
   yield return Line("","Не закрывается толком");
   Task("Заварить чай");StartCoroutine(KettleBoils());
   yield return Use(new Vector3(8.48f,.95f,1.65f),"Выключить газ — чайник свистит");GasOff();if(flame)Destroy(flame.gameObject);
   Put("JamJar",new Vector3(7.35f,.745f,.62f));Put("Matchbox",new Vector3(8.62f,.86f,1.95f));
   yield return Use(new Vector3(7.58f,.8f,.55f),"Заварить и разлить чай",1.2f);Show("TeaSet",true);sound.Play("spoon",TableAt);
   yield return Use(new Vector3(7.15f,.5f,1.2f),"Сесть за стол");
   player.control=false;
   yield return Line("Бабушка","Ну, рассказывай. Как в школе?");
   yield return Choose("Нормально, ба. Четвертная без троек.","Да как обычно. Учимся.",12);
   yield return Line("Бабушка","Ну и молодец. Мать не обижай");
   yield return Line("Бабушка","Пятый год как деда нет. Часы его в коридоре — ты завести потом поможешь");
   yield return Line("Бабушка","Говорят, в Заречье опять квартира пустая… Люди пропадают, а их никто вспомнить не может. Валька говорит — Гостья ходит. Ерунда, бабьи сказки",8);
   yield return Line("Бабушка","У Вали Коля болеет, сердце. Может, позовёт ещё");
   player.control=true;Clock(20,10);
   // 4.4
   Task("Выйти в прихожую");
   while(player.transform.position.z<3.0f)yield return null;
   var sconce=flat.look.warm.Length>1?flat.look.warm[1]:null;float sconceWas=sconce?sconce.intensity:0;if(sconce)sconce.intensity=0;sound.Play("switch",ScenarioLayout.Clock+Vector3.up*1.2f);
   StartCoroutine(GranGo("HallA"));
   yield return Line("Бабушка","Ох ты… Мишаня, табуретку с кухни возьми, лампочки на антресоли");
   Task("Поменять лампочку в бра");
   yield return Use(()=>flat.stool.transform.position+Vector3.up*.4f,"Взять табуретку",0,2);flat.stool.SetActive(false);
   yield return Use(new Vector3(6.6f,1.2f,5.25f),"Поставить табуретку под антресолью",0,2.2f);
   yield return Use(new Vector3(6.6f,2.2f,5.25f),"Достать лампочку с антресоли",0,2.6f);
   yield return UseHeld(ScenarioLayout.Clock+new Vector3(0,.9f,.1f),"Выкрутить старую и вкрутить новую",2,2.6f,"bulb",ScenarioLayout.Clock+new Vector3(0,.9f,.1f));
   Show("Bulb",true);if(sconce)sconce.intensity=Mathf.Max(sconceWas,1.8f);flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(5.5f,0,5.8f),Quaternion.identity);
   yield return GranGo("Door");
   yield return Line("Бабушка","Если свет выбьет — вот тут пробки, кнопочку нажать. Дед подписал");
   yield return Line("Бабушка","Табуретку оставь, потом унесу");
   Clock(20,35);
   // 4.5
   Task("Отнести платок в шифоньер");
   yield return Line("Бабушка","Отнеси в шифоньер, в спальне");
   StartCoroutine(GranGo("HallB"));
   yield return Use(flat.wardrobe.position+new Vector3(-.3f,1.2f,0),"Открыть шифоньер",0,2);
   foreach(var d in flat.wardrobeLeaves)d.localRotation=Quaternion.Euler(0,95,0);sound.Play("door_open",flat.wardrobe.position);
   yield return Use(flat.wardrobe.position+new Vector3(-.3f,1.5f,0),"Повесить платок на плечики",0,2);Show("Shawl",true);
   yield return Line("","Дедов ремень… Тот самый, которым дед балкон подвязывал",4);
   foreach(var d in flat.wardrobeLeaves)d.localRotation=Quaternion.identity;sound.Play("wardrobe",flat.wardrobe.position);
   Clock(20,40);
   // 4.6
   yield return GranGo("Clock");Task("Завести дедовы часы");
   yield return Line("Бабушка","Заведи-ка, у меня пальцы не те уже. Ключ сверху");
   yield return UseHeld(ScenarioLayout.Clock+Vector3.up*.25f,"Завести часы ключом",3,2,"clock_wind",ScenarioLayout.Clock+Vector3.up*1.6f);
   yield return Use(ScenarioLayout.Clock-Vector3.up*.3f,"Толкнуть маятник",0,2);
   flat.clock.stopped=false;Clock(21,0);
   yield return Line("Бабушка","Дед говорил: пока часы идут — дом помнит. Суеверный был");
   for(int i=0;i<9;i++){sound.Play("chime",ScenarioLayout.Clock);yield return new WaitForSeconds(1.1f);}
   // 4.7 free time — short, then 4.8
   Task("Свободное время");StartCoroutine(GranGo("Armchair"));
   yield return new WaitForSeconds(25);
   // 4.8
   Clock(21,30);Task("Включить новости");
   yield return Line("Бабушка","Мишаня, включи-ка новости");
   yield return Use(()=>flat.televisionScreen?flat.televisionScreen.bounds.center:flat.television.position,"Включить «Рубин»",0,2.4f); // the screen, not the cabinet edge (Codex story15)
   if(tv)tv.storyOn=true;flat.televisionLight.intensity=.12f;sound.Play("switch",flat.television.position);sound.Play("tv_on",flat.television.position);sound.Bed("crt_whine",flat.television.position,true);
   foreach(var l in new[]{"…Сегодня в милицию снова обратились жители Заречного района.","В квартире по улице Строителей обнаружен накрытый стол и включённый свет. По словам соседей, в квартире никто не проживал.","Однако на фотографиях, изъятых из квартиры, — пустые места рядом с людьми…","Специалисты рекомендуют: не открывайте дверь в позднее время. И — что бы ни случилось — запомните свой дом. Как следует запомните."})
    yield return Line("Диктор",l,5);
   yield return Line("Бабушка","Нечего на ночь глядеть");if(tv)tv.storyOn=false;flat.televisionLight.intensity=0;sound.Bed("crt_whine",flat.television.position,false);sound.Play("switch",flat.television.position);
   if(flat.look.icon)flat.look.icon.intensity=.14f;Clock(21,38);
   // 4.9
   yield return ValyaAtTheDoor(false);
   yield return Night1(true);
  }
  // "Фонарик в трюмо": the hall cupboard works in every story chapter (LoopDirector handles it only inside the loop).
  void OfferFlashlight(){var c=flat.hallCupboard;if(!c||player.hasFlashlight)return;OfferCupboardOpen();
   Offer(()=>c.interaction.position,"Взять фонарик-«жучок»",0,1.7f,Key.E,()=>!player.hasFlashlight&&c.ReadyToTake,()=>{c.Take(player);Say("","Фонарик. F — включить, держать F — подкачать",4);});}
  void OfferCupboardOpen(){var c=flat.hallCupboard;Offer(()=>c.interaction.position,"Открыть тумбу трюмо",0,1.7f,Key.E,()=>!player.hasFlashlight&&!c.door.open,()=>{sound.Play(c.door.ToggleWithCue(),c.interaction.position);if(!player.hasFlashlight)OfferCupboardOpen();});}
  // Ready story props placed by Editor/StoryPropsInstaller as "STORY <id>"; absent props are simply skipped.
  readonly Dictionary<string,(Vector3 at,Quaternion rot,bool on)> propStart=new Dictionary<string,(Vector3,Quaternion,bool)>();
  Transform Prop(string id)=>flat.apartment?flat.apartment.Find("STORY "+id):null;
  void Show(string id,bool on){var t=Prop(id);if(t)t.gameObject.SetActive(on);}
  void Put(string id,Vector3 at,bool on=true){var t=Prop(id);if(!t)return;t.position=at;t.gameObject.SetActive(on);}
  void RememberProps(){foreach(var id in new[]{"Kettle","Matchbox","JamJar","Belt","Bread","Cans","Bulb","Shawl","TeaSet"}){var t=Prop(id);if(t&&!propStart.ContainsKey(id))propStart[id]=(t.position,t.rotation,t.gameObject.activeSelf);}}
  void PropsAtEveningStart(){foreach(var kv in propStart){var t=Prop(kv.Key);if(!t)continue;t.SetPositionAndRotation(kv.Value.at,kv.Value.rot);t.gameObject.SetActive(kv.Value.on);}}
  void PropsAfterEvening(){PropsAtEveningStart();Put("Kettle",new Vector3(8.48f,.86f,1.55f));Put("JamJar",new Vector3(7.35f,.745f,.62f));Show("Bulb",true);Show("Shawl",true);Show("TeaSet",true);}
  Light FlameLight(Vector3 at){var g=new GameObject("Gas flame (story)");g.transform.position=at;var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.35f,.55f,1);l.intensity=.6f;l.range=1.2f;l.shadows=LightShadows.None;return l;}

  IEnumerator ValyaAtTheDoor(bool second){
   Task("");
   sound.Bed("radio",RadioAt,false);
   if(!second){Clock(21,40);sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.25f);sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.25f);sound.Play("knock",flat.entrance.transform.position);
    yield return Line("Голос Вали за дверью","Нинок! Нинок, открой!");
    yield return GranGo("Door");GranShow("Door",0);flat.entrance.locked=false;flat.entrance.open=true;sound.Play("door_open",flat.entrance.transform.position);
    yield return Line("Тётя Валя (на площадке)","Нинок, Коле плохо, давление, я одна боюсь — посиди со мной, а?",5);}
   else{GranShow("Door",180);flat.entrance.locked=false;flat.entrance.open=true;player.Place(new Vector3(7.95f,.02f,5.0f),0);}
   yield return GranFace(player.head.position);
   yield return Line("Бабушка","Мишаня, я у Вали, на полчасика. Никому не открывай. Я своим ключом. А если стучать буду — свои стучат вот так, слушай…",7);
   sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.22f);sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.7f);sound.Play("knock",flat.entrance.transform.position);
   Say("","тук-тук… тук",2);yield return new WaitForSeconds(1.5f);
   if(second){
    yield return Card("Это уже было",2.5f);
    yield return Choose("«Ба, не уходи!»","Промолчать",10);
    if(choice==1)yield return Line("Бабушка","Да что ты, Мишаня, белый весь… Валя одна, Коле плохо. Полчасика. Закройся",5);
   }else yield return Line("Бабушка","Запомнил? Фонарик в трюмо, если свет выбьет. Всё, закройся");
   yield return GranWalk("Landing");flat.grandmother.SetActive(false);
   flat.entrance.open=false;sound.Play("door_close",flat.entrance.transform.position);
   Task("Закрыть дверь на замок и цепочку");
   yield return Use(flat.entrance.transform.position+Vector3.up*.2f,"Закрыть замок");sound.Play("latch",flat.entrance.transform.position);
   yield return Use(flat.entrance.transform.position+Vector3.up*.45f,"Накинуть цепочку");sound.Play("latch",flat.entrance.transform.position);
   flat.entrance.locked=true;Clock(21,42);
  }

  // ---------- 5/6. First night ----------
  float nightSeconds;void NightClock(){flat.clock.Show(nightSeconds);}
  IEnumerator NightMinutesUntil22(){nightSeconds=21*3600+42*60;while(nightSeconds<22*3600){nightSeconds=Mathf.Min(22*3600,nightSeconds+Time.deltaTime*5f);NightClock();yield return null;}} // 12 s per game minute
  IEnumerator NightTo(int h,int m,float realSeconds){float target=h*3600+m*60;float start=nightSeconds;for(float t=0;t<realSeconds;t+=Time.deltaTime){nightSeconds=Mathf.Lerp(start,target,t/realSeconds);if(!flat.clock.stopped)NightClock();yield return null;}nightSeconds=target;NightClock();}
  bool doorOpenedDeath,phoneAnswered;
  IEnumerator Night1(bool continued){
   chapter=Chapter.Night1;night=1;
   if(!continued){loop.Pause(false);Homely(true);flat.clock.stopped=false;flat.entrance.locked=true;flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(5.5f,0,5.8f),Quaternion.identity);player.Place(ScenarioLayout.Spawn,180);yield return Fade(0,.8f);}
   if(!continued)PropsAfterEvening();Checkpoint("Night1");HouseSound(false,true);Task("Дождаться бабушку");if(!continued)OfferFlashlight();
   yield return NightMinutesUntil22();
   yield return NightEvents(1,false);
  }
  IEnumerator NightEvents(int which,bool from2200Restart){
   // 22:00 everything goes quiet, then the set turns itself on: test card, 1 kHz, announcer
   yield return HouseQuiet();yield return new WaitForSeconds(1.2f);
   if(tv)tv.storyOn=true;flat.televisionLight.intensity=.12f;sound.Play("tv_on",flat.television.position);sound.Bed("crt_whine",flat.television.position,true);yield return new WaitForSeconds(.4f);sound.Play("test_tone",flat.television.position);yield return new WaitForSeconds(2.8f);
   foreach(var l in new[]{"Внимание. Жителям Заречного района.","Атака на запоминание.","Не открывайте дверь. Не отвечайте знакомым голосам. Она боится света.","Запомните всё."})yield return Line("«Рубин», диктор",l,3.2f);
   if(which==2){yield return new WaitForSeconds(1.5f);yield return Line("«Рубин», диктор","Ты же помнишь, Миша?",3.5f);}
   if(tv)tv.storyOn=false;flat.televisionLight.intensity=0;sound.Bed("crt_whine",flat.television.position,false);
   // 22:00:30 the house across goes dark, 22:01 blackout
   yield return NightTo(22,0,1);if(flat.look.exterior)flat.look.exterior.Blackout(true);
   yield return NightTo(22,1,12);flat.look.Apply(FlatCondition.Blackout);sound.Play("breaker",new Vector3(8.75f,1.8f,5.5f));
   if(!player.hasFlashlight)Say("",which==1?"Темно… Фонарик был где-то…":"Фонарик в трюмо. Бабушка говорила",4);
   yield return NightTo(22,2,10);
   // 22:02 knocks at the entrance; E = peephole, R = open (death)
   doorOpenedDeath=false;bool peeped=false;
   var peep=Offer(()=>flat.entrance.transform.position+Vector3.up*.5f,"Глазок",0,1.8f,Key.E,null,()=>peeped=true);
   var open=Offer(()=>flat.entrance.transform.position+Vector3.up*.5f,"Открыть дверь",0,1.8f,Key.R,null,()=>doorOpenedDeath=true);
   for(int round=0;round<2&&!doorOpenedDeath;round++){
    for(int i=0;i<3;i++){sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.9f);}
    sound.Grandmother(flat.entrance.transform.position);Say("Голос бабушки за дверью",which==1?"Мишаня. Открой. Ключ у Вали забыла":"Мишаня… открой. Я же знаю, что ты помнишь",4);
    if(which==2&&round==0&&Vector3.Distance(player.transform.position,flat.entrance.transform.position)<3)Say("","Три удара. Бабушка стучит не так",3);
    for(float t=0;t<10&&!doorOpenedDeath;t+=Time.deltaTime){if(peeped){peeped=false;Withdraw(peep);yield return scenes.Play("Peephole");peep=Offer(()=>flat.entrance.transform.position+Vector3.up*.5f,"Глазок",0,1.8f,Key.E,null,()=>peeped=true);}yield return null;}
   }
   Withdraw(peep);Withdraw(open);
   if(doorOpenedDeath){yield return ShortDeath();yield return RestartAt2200(which);yield break;}
   // 22:03 phone
   yield return NightTo(22,3,4);phoneAnswered=false;
   var phone=Offer(()=>flat.phone.position,"Снять трубку",0,1.8f,Key.E,null,()=>phoneAnswered=true);
   for(int ring=0;ring<6&&!phoneAnswered;ring++){sound.Play("phone",flat.phone.position);for(float t=0;t<3&&!phoneAnswered;t+=Time.deltaTime)yield return null;}
   Withdraw(phone);
   if(phoneAnswered){
    sound.StopCue("phone");
    if(which==1){float d=sound.Speak(false,flat.phone.position);Say("Бабушка, шёпотом","Мишаня… Это я, я у Вали… Тут Коля… Коли нет, Мишаня, тут никого нет… Это не я за дверью! Не открывай!..",Mathf.Max(5,d));yield return new WaitForSeconds(Mathf.Max(5,d));sound.PlayReceiver("phone_shriek");yield return new WaitForSeconds(1.2f);}
    else{sound.PlayReceiver("phone_breath");yield return Line("Бабушка, растерянно","Мишаня?.. Я у Вали… А Вали нет. Тут пустая квартира, Мишаня, обои ободраны… Я не помню, зачем пришла. Мишаня, а ты… ты кто?..",8);}
    sound.PlayReceiver("busy");Say("","…короткие гудки",3);
   }
   // 22:04 scratching across the roof toward the balcony
   yield return NightTo(22,4,6);
   foreach(var at in new[]{new Vector3(8,2.6f,5),new Vector3(6,2.6f,3.5f),new Vector3(5,2.6f,1.2f),new Vector3(5.2f,2.6f,0)}){sound.Play("scratch",at);yield return new WaitForSeconds(2);}
   // 22:05 balcony
   yield return NightTo(22,5,4);
   if(which==2&&beltTied){yield return HandInTheGap();}
   else{
    flat.balconyDoor.locked=false;flat.balconyDoor.open=true;sound.Play("door_burst",flat.balcony.position);sound.Play("balcony",flat.balcony.position);
    if(LivingRoom()){scenes.BalconyWaiting();yield return new WaitForSeconds(1.5f);flat.guest.SetActive(false);}
    if(which==2){yield return NightTo(22,6,4);yield return Hunt(4);yield return ShortDeath();yield return RestartNight2At2142();yield break;}
   }
   // 22:06
   yield return NightTo(22,6,4);
   if(which==1){yield return Hunt(18);}
   else{
    yield return new WaitForSeconds(4);
    for(int i=0;i<3;i++){sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.9f);}
    float gap=3.5f;for(int i=0;i<6;i++){Say("Голос за дверью — твой собственный","Ба, открой. Это я",gap);yield return new WaitForSeconds(gap);gap=Mathf.Max(.6f,gap*.6f);}
   }
   // 22:07
   nightSeconds=22*3600+7*60;NightClock();
   if(which==1){
    yield return scenes.Play("DeathFull");sound.EndDeathRecovery();flat.clock.stopped=true;
    yield return Rewind();yield return Night2(true);
   }else{
    sound.Play("chime",ScenarioLayout.Clock);flat.clock.stopped=true;sound.ResetWorld();yield return new WaitForSeconds(3);
    flat.entrance.locked=false;flat.entrance.scripted=false;flat.entrance.open=true;sound.Play("creak",flat.entrance.transform.position);
    Say("","Она не вошла. Тогда она забрала квартиру",5);Task("");
    while(player.transform.position.z<5.95f)yield return null;
    flat.look.disruption=1;yield return new WaitForSeconds(.35f);flat.look.disruption=0;
    yield return EnterLoop();
   }
  }
  bool LivingRoom(){var p=player.transform.position;return p.x>2.9f&&p.x<6.5f&&p.z>0&&p.z<4.4f;}
  IEnumerator Hunt(float seconds){
   // 22:06: seen in glimpses 3–6 m away, never catching before 22:07.
   var spots=new[]{new Vector3(5.65f,0,4.35f),new Vector3(6.6f,0,5.25f),new Vector3(2.85f,0,3.75f),new Vector3(7.25f,0,2.95f),new Vector3(4.6f,0,5.25f),new Vector3(5.2f,0,.6f)};
   float until=Time.time+seconds;while(Time.time<until){
    Vector3 best=Vector3.zero;float bestScore=-1;foreach(var s in spots){float d=Vector3.Distance(s,player.transform.position);if(d<3||d>6.5f)continue;if(!ScenarioSight.Clear(player.transform,player.head.position,s+Vector3.up*1.2f))continue;float score=1-Mathf.Abs(d-4.5f)/3;if(score>bestScore){bestScore=score;best=s;}}
    if(bestScore>0){var g=flat.guest;g.SetActive(true);g.transform.position=best;var face=player.transform.position-best;face.y=0;g.transform.rotation=Quaternion.LookRotation(face);g.GetComponent<ProxyPose>().Crawl(best);sound.Play("palms",best);sound.Play("guest_breath",best);yield return new WaitForSeconds(1.4f);g.SetActive(false);}
    yield return new WaitForSeconds(UnityEngine.Random.Range(1.5f,3f));
   }
  }
  IEnumerator HandInTheGap(){
   // 7.6: the belt holds; the door jerks to 10 cm; fingers search the gap; light makes her shriek and let go.
   flat.balconyDoor.locked=true;flat.balconyDoor.open=false;flat.balconyDoor.closedAngle=8;sound.Play("door_burst",flat.balcony.position);sound.Play("latch",flat.balcony.position);
   scenes.BalconyWaiting(.0f);float lit=0;
   for(float t=0;t<8;t+=Time.deltaTime){scenes.BalconyWaiting(Mathf.Clamp01(t*.5f));
    bool torch=player.hasFlashlight&&player.flashlight.intensity>.5f&&Vector3.Distance(player.head.position,flat.balcony.position)<4.5f&&Vector3.Dot(player.head.forward,(flat.balcony.position+Vector3.up*.6f-player.head.position).normalized)>.93f;
    lit=torch?lit+Time.deltaTime:0;if(lit>.5f){sound.Play("shriek",flat.balcony.position);break;}yield return null;}
   flat.guest.SetActive(false);flat.balconyDoor.closedAngle=0;
   yield return Line("","Не вошла… Она не вошла",3.5f);
  }
  IEnumerator ShortDeath(){yield return scenes.Play("DeathShort");flat.look.disruption=1;yield return new WaitForSeconds(1);flat.look.disruption=0;sound.EndDeathRecovery();}
  IEnumerator RestartAt2200(int which){
   // Deaths 2/3 (SCENARIO 13): VHS jump back to 22:00 of the same night.
   QaRestarts++;acts.Clear();Homely(true);flat.entrance.locked=true;flat.clock.stopped=false;if(which==2&&beltTied)TieStrap(true);player.Place(ScenarioLayout.Spawn,180);nightSeconds=22*3600;NightClock();
   yield return NightEvents(which,true);
  }
  IEnumerator RestartNight2At2142(){QaRestarts++;acts.Clear();Homely(true);PropsAfterEvening();belt=false;beltTied=false;TieStrap(false);flat.entrance.locked=true;player.Place(ScenarioLayout.Spawn,180);yield return Night2Body();}

  // ---------- 6.1 Rewind ----------
  IEnumerator Rewind(){
   loop.prompt.text="";acts.Clear();fade=1;yield return new WaitForSeconds(1);
   flat.look.disruption=1;fade=0;Homely(true);PropsAfterEvening();flat.balconyDoor.closedAngle=0;
   string[] flashes={"…дверь балкона закрывается сама…","…побелка взлетает обратно на потолок…","…телефон… стук… «Рубин» гаснет внутрь…","…бабушка с вареньем…","…шифоньер. Дедов ремень…","…«тук-тук… тук»…","…лампочка…"};
   foreach(var f in flashes){card=f;sound.Play("clock",ScenarioLayout.Clock);yield return new WaitForSeconds(1.1f);}
   card=null;fade=1;yield return new WaitForSeconds(.15f);fade=0;flat.look.disruption=.4f;yield return new WaitForSeconds(.6f);flat.look.disruption=0;
  }

  // ---------- 8. Second night ----------
  IEnumerator Night2(bool continued){
   chapter=Chapter.Night2;night=2;
   if(!continued){loop.Pause(false);Homely(true);PropsAfterEvening();}
   flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(5.5f,0,5.8f),Quaternion.identity);flat.clock.stopped=false;Clock(21,40);
   Checkpoint("Night2");
   yield return ValyaAtTheDoor(true);
   yield return Night2Body();
  }
  IEnumerator Night2Body(){
   chapter=Chapter.Night2;Task("Запомни, как это было");OfferFlashlight();HouseSound(false,true);nightSeconds=21*3600+42*60;NightClock();
   // Preparation: belt, torch, (candles optional are not implemented)
   Act takeBelt=null,tie=null;
   takeBelt=Offer(()=>flat.wardrobe.position+new Vector3(-.3f,1.3f,0),"Взять дедов ремень",0,2,Key.E,()=>!belt,()=>{belt=true;Show("Belt",false);foreach(var d in flat.wardrobeLeaves)d.localRotation=Quaternion.Euler(0,95,0);sound.Play("door_open",flat.wardrobe.position);Say("","Ремень. Тот самый",2.5f);});
   tie=Offer(()=>flat.balcony.position+Vector3.up*.2f,"Закрыть шпингалет и подвязать дверь ремнём",2,2,Key.E,()=>belt&&!beltTied,()=>{beltTied=true;TieStrap(true);sound.Play("latch",flat.balcony.position);Say("","Держит",2);Checkpoint("Night2");});
   yield return new WaitForSeconds(3);Say("","Она придёт в 22:07. Я помню",4);
   bool hinted50=false,hinted55=false;
   while(nightSeconds<22*3600){nightSeconds=Mathf.Min(22*3600,nightSeconds+Time.deltaTime*5f);NightClock();
    if(!beltTied&&!hinted50&&nightSeconds>=21*3600+50*60){hinted50=true;Say("","Балкон. В прошлый раз она пришла с балкона",4);}
    if(!beltTied&&!hinted55&&nightSeconds>=21*3600+55*60){hinted55=true;Say("","Дед ремнём подвязывал… Ремень в шифоньере",4);}
    yield return null;}
   yield return NightEvents(2,false);
  }
  void TieStrap(bool on){
   if(!strap){var g=new GameObject("Grandfather's belt on the balcony handle");strap=g.AddComponent<LineRenderer>();strap.positionCount=3;strap.startWidth=strap.endWidth=.035f;strap.useWorldSpace=true;
    var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",new Color(.17f,.09f,.05f));m.SetFloat("_Smoothness",.45f);strap.sharedMaterial=m;strap.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
   strap.SetPosition(0,new Vector3(4.86f,1.02f,.06f));strap.SetPosition(1,new Vector3(4.72f,.7f,.12f));strap.SetPosition(2,new Vector3(4.55f,.48f,.12f));strap.enabled=on;
  }

  // ---------- 9/10. Loop and chase (existing core) ----------
  IEnumerator EnterLoop(){
   chapter=Chapter.Loop;objective="";if(strap)strap.enabled=false;flat.clock.stopped=false;flat.entrance.open=false;flat.entrance.leaf.localRotation=Quaternion.identity;flat.entrance.direction=1;
   loop.EnterLap(1);player.Place(ScenarioLayout.Spawn,180);Checkpoint("Loop");yield break;
  }
  public void ChaseFinished(){if(running!=null)StopCoroutine(running);running=StartCoroutine(Finale());}

  // ---------- 11. Finale ----------
  IEnumerator Finale(){
   chapter=Chapter.Finale;loop.Pause(false);Homely(true);PropsAfterEvening();Put("Kettle",new Vector3(8.48f,.86f,1.85f));flat.clock.stopped=false;Clock(22,0);
   flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(5.5f,0,5.8f),Quaternion.identity);if(strap)strap.enabled=false;flat.entrance.locked=true;
   if(flat.look.exterior)flat.look.exterior.Blackout(false);
   player.Place(ScenarioLayout.Spawn,180);Checkpoint("Finale");Task("Дождаться бабушку");HouseSound(true,false);
   yield return new WaitForSeconds(2);Say("","Получилось?..",3);
   nightSeconds=22*3600;yield return NightTo(22,10,75);
   // 22:10 the right knock
   bool peeped=false;openedDoorFinale=false;
   sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.22f);sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.7f);sound.Play("knock",flat.entrance.transform.position);
   yield return Line("Голос бабушки за дверью","Мишаня, это я. Открывай, замёрзла",3.5f);
   var peep=Offer(()=>flat.entrance.transform.position+Vector3.up*.5f,"Глазок",0,1.8f,Key.E,null,()=>peeped=true);
   var open=Offer(()=>flat.entrance.transform.position+Vector3.up*.5f,"Открыть: два замка и цепочка",0,1.8f,Key.R,null,()=>openedDoorFinale=true);
   float away=0;
   while(!openedDoorFinale&&away<15){
    if(peeped){peeped=false;Withdraw(peep);yield return PeepholeGrandmother();peep=Offer(()=>flat.entrance.transform.position+Vector3.up*.5f,"Глазок",0,1.8f,Key.E,null,()=>peeped=true);}
    away=Vector3.Distance(player.transform.position,flat.entrance.transform.position)>2.2f?away+Time.deltaTime:0;yield return null;}
   Withdraw(peep);Withdraw(open);
   if(openedDoorFinale){sound.Play("latch",flat.entrance.transform.position);yield return new WaitForSeconds(.3f);sound.Play("latch",flat.entrance.transform.position);flat.entrance.locked=false;flat.entrance.open=true;GranShow("Door",180);
    yield return Line("Бабушка","Замёрз? Сейчас чайку поставлю",3);}
   else{yield return Line("Голос бабушки за дверью","Я своим ключом, Мишаня",3);sound.Play("latch",flat.entrance.transform.position);flat.entrance.locked=false;flat.entrance.open=true;sound.Play("finger_crack",flat.entrance.transform.position);
    yield return new WaitForSeconds(2);GranShow("Door",180);yield return Line("Бабушка","Чего ж не открываешь, Мишаня?",3);}
   yield return GranWalk("Stove");Put("Kettle",new Vector3(8.48f,.86f,1.55f));flat.entrance.open=false;flat.entrance.locked=true;sound.Play("latch",flat.entrance.transform.position);
   Task("");Say("","Она напевает. Спички, газ, вода из крана — точно как ты вечером",6);
   yield return LightGas();var flame=FlameLight(new Vector3(8.48f,.95f,1.65f));yield return new WaitForSeconds(1.2f);
   sound.Play("tap",SinkAt);yield return new WaitForSeconds(3.2f);sound.ReleaseCue("tap");yield return new WaitForSeconds(1);
   for(float t=0;t<20&&Vector3.Distance(player.transform.position,Nodes["Stove"])>2.4f;t+=Time.deltaTime)yield return null;
   yield return GranFace(Nodes["Stove"]+Vector3.right,0.1f);
   StartCoroutine(KettleBoils());yield return new WaitForSeconds(4);
   yield return Line("Бабушка (не оборачиваясь)","Мишаня… а ты меня помнишь?",4);
   // 11.9 transformation (3.5 s) then 11.10 face tear
   yield return GranFace(player.head.position,1.5f);sound.Play("finger_crack",flat.grandmother.transform.position);
   var g=flat.guest;var at=flat.grandmother.transform.position;flat.grandmother.SetActive(false);g.SetActive(true);g.GetComponent<ProxyPose>().Stand();g.transform.position=at;var face=player.transform.position-at;face.y=0;g.transform.rotation=Quaternion.LookRotation(face);
   // 11.9: the whistle turns into her shriek; the radio song breaks off.
   var full=g.transform.localScale;GasOff(false);sound.Bed("radio",RadioAt,false);sound.Bed("fridge",FridgeAt,false);sound.Play("shriek",at);
   for(float t=0;t<2;t+=Time.deltaTime){g.transform.localScale=full*Mathf.Lerp(.7f,1.04f,t/2);yield return null;}
   if(flame)Destroy(flame.gameObject);
   yield return scenes.Play("DeathFull");sound.EndDeathRecovery();
   yield return Epilogue(!openedDoorFinale);
  }
  IEnumerator PeepholeGrandmother(){
   // 11.5: the grandmother looks perfect through the fish-eye. No breath, and Tuzik is silent.
   var p=player;var head=p.head;var savedPos=head.localPosition;var savedRot=head.localRotation;p.control=false;flat.look.peephole=1;
   var g=flat.grandmother;bool was=g.activeSelf;g.SetActive(true);g.transform.SetPositionAndRotation(new Vector3(8.05f,0,6.62f),Quaternion.Euler(0,180,0));GranPlay(granIdle);
   for(float t=0;t<4.5f;t+=Time.deltaTime){head.position=new Vector3(8.05f,1.5f,6.22f);head.rotation=Quaternion.identity;yield return null;}
   flat.look.peephole=0;head.localPosition=savedPos;head.localRotation=savedRot;p.control=true;g.SetActive(was);
  }
  IEnumerator Epilogue(bool refused){
   chapter=Chapter.Ended;objective="";loop.prompt.text="";fade=1;yield return new WaitForSeconds(2);
   yield return Card("НОВОСТИ ЗАРЕЧЬЯ\n\n«…В квартире номер шестьдесят по улице Строителей обнаружены накрытый стол, два стакана чая и включённый чайник. По данным жилконторы, квартира пустует с девяносто первого года. Соседка, Валентина Ивановна, утверждает, что никогда не видела жильцов этой квартиры…»",9);
   if(refused)yield return Card("«…однако, по её словам, в тот вечер она слышала, как мальчик за дверью кричал: „Ты не она!“»",5);
   yield return Card("АТАКА НА ЗАПОМИНАНИЕ\n«Пока дома тепло»\n\nСценарий и игра: Дима, Claude, Codex\nГостья, мебель и звуки — авторы CC0 / CC-BY, см. CREDITS.md",6);
   yield return Card("Фотоальбом. Последний разворот: лето 1993, у подъезда.\nВаля с Тузиком улыбаются рядом с пустотой.\nТеперь засвеченных пятен три: старое, на месте бабушки — и на месте Миши в центре.",7);
   sound.ResetWorld();yield return new WaitForSeconds(1.5f);
   if(!loop.testing&&System.IO.File.Exists(savePath))System.IO.File.Delete(savePath);
   loop.EndStory();
  }

  // ---------- GUI ----------
  GUIStyle big,small,task;
  void OnGUI(){
   if(!OwnsFlat&&fade<=0&&card==null)return;
   big??=new GUIStyle(GUI.skin.label){fontSize=28,alignment=TextAnchor.MiddleCenter,wordWrap=true};small??=new GUIStyle(GUI.skin.label){fontSize=22,alignment=TextAnchor.MiddleCenter,wordWrap=true};task??=new GUIStyle(GUI.skin.label){fontSize=18};
   if(fade>0||card!=null){GUI.color=new Color(0,0,0,card!=null?Mathf.Max(fade,.92f):fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;}
   if(card!=null)GUI.Label(new Rect(Screen.width*.12f,Screen.height*.2f,Screen.width*.76f,Screen.height*.6f),card,big);
   if(!string.IsNullOrEmpty(objective)&&card==null&&!player.paused&&!scenes.InDeath)GUI.Label(new Rect(24,20,Screen.width*.5f,30),"Задача: "+objective,task);
   if(choiceA!=null){GUI.Label(new Rect(Screen.width*.2f,Screen.height*.62f,Screen.width*.6f,40),"[1] "+choiceA,small);GUI.Label(new Rect(Screen.width*.2f,Screen.height*.62f+44,Screen.width*.6f,40),"[2] "+choiceB,small);}
  }
 }
}
