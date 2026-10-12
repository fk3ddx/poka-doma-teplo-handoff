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
  [Serializable] class Save {public string chapter;public bool flashlight,belt;public NightRewind.Memory[] memories;} // memories: the four real evening moments for the 6.1 rewind

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
  // QA: a walking route that crosses NO closed door (doors as they are now).
  public bool QaWalkableNow(Vector3 from,Vector3 to){Nav.Invalidate();Nav.Ensure();return Nav.Find(from,to,null).steps!=null;}
  public static Vector3 QaNode(string name)=>Nodes[name];
  public void QaEnterLoop(){StopAll();running=StartCoroutine(EnterLoop());} // the real story→loop hand-off of 22:07, night 2
  public List<ScenarioDoor> QaPlayerRoute(Vector3 from,Vector3 to){Nav.Ensure();var r=Nav.Find(from,to,QaCanOpen);return r.steps!=null?r.crossed:null;}
  void Fire(Act a){a.fired=true;acts.Remove(a);lastProgress=Time.time;a.done?.Invoke();}
  // QA surface for StoryAutopilot (-story-smoke): it may only fire an act after the real aim selection picked it.
  public List<Act> OpenActs=>acts;public Act Aimed=>aimed;public void QaFire(Act a){if(a==aimed)Fire(a);}
  // A second key on the same aimed spot (peephole E / open R) is reachable whenever its partner is aimed.
  public void QaFireCompanion(Act a){if(aimed!=null&&(a==aimed||(a.at()-aimed.at()).sqrMagnitude<.05f))Fire(a);}
  public bool Choosing=>choiceA!=null;public void QaChoose(int c){choice=c;}public string CurrentCard=>card;public string Objective=>objective;
  public void QaBegin(string at){Begin(at);}
  // QA: the real night sequence from 22:00 (announcer, knocks, phone, balcony, hunt, 22:07 death) without the evening.
  // QA (RewindQA): the four evening memories staged at their real places when the evening was not played in this run.
  // The four evening moments in their canonical state (props after the evening, her at her real places). Used for a
  // night-1 save made before memories were saved, and by RewindQA (which does not play the evening). The camera of each
  // memory is directed by NightRewind at the subject's bounds, so the player's own position does not matter here.
  void RememberCanonicalEvening(){
   var feet=player.transform.position;var yaw=player.transform.eulerAngles.y;var look=Mathf.DeltaAngle(0,player.head.localEulerAngles.x);bool granWas=flat.grandmother.activeSelf;
   var lookAt=new System.Func<Vector3,Vector3,float>((a,b)=>Mathf.Atan2(b.x-a.x,b.z-a.z)*Mathf.Rad2Deg);
   GranShow("Kitchen",lookAt(Nodes["Kitchen"],new Vector3(7.66f,0,.5f)));rewind.Remember("tea","spoon",true);
   flat.grandmother.SetActive(false);rewind.Remember("bulb","bulb");
   foreach(var d in flat.wardrobeLeaves)d.localRotation=Quaternion.Euler(0,95,0);rewind.Remember("belt","door_open");foreach(var d in flat.wardrobeLeaves)d.localRotation=Quaternion.identity;
   GranShow("Door",180);rewind.Remember("knock","knock",true);
   flat.grandmother.SetActive(granWas);player.Place(feet,yaw,look);}
  public void QaRememberEvening(){rewind.ForgetEvening();RememberCanonicalEvening();}
  public void QaCheckpoint(string at){Checkpoint(at);}
  public void QaReplayRewind(){if(running!=null)StopCoroutine(running);running=StartCoroutine(Rewind());}
  public void QaCancelRewind(){if(running!=null)StopCoroutine(running);running=null;if(rewind)rewind.Cleanup();}
  public float QaNightSeconds=>nightSeconds;
  public void QaStopAll()=>StopAll(); // QA: what a menu / new game / chapter change does mid-scene
  public void QaNightEvents(int which){EndMirrorSeed();chapter=which==1?Chapter.Night1:Chapter.Night2;night=which;if(running!=null)StopCoroutine(running);nightSeconds=22*3600;NightClock();running=StartCoroutine(NightEvents(which,false));}
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
  NightRewind rewind;public NightRewind Rewinder=>rewind;
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
  void GranLookAtPlayerWhenNear(){if(!flat.grandmother.activeSelf||granWalking||granHeld)return;var g=flat.grandmother.transform;var d=player.transform.position-g.position;d.y=0;if(d.magnitude<2.2f&&d.sqrMagnitude>.01f)g.rotation=Quaternion.Slerp(g.rotation,Quaternion.LookRotation(d),1-Mathf.Exp(-3*Time.deltaTime));}
  bool granWalking;int granToken;CapsuleCollider granBody;
  bool PlayerInTheWay(Vector3 from,Vector3 to){var p=player.transform.position;p.y=0;from.y=0;to.y=0;var seg=to-from;float len=seg.magnitude;if(len<.01f)return false;
   float along=Vector3.Dot(p-from,seg/len);if(along<0||along>Mathf.Min(len,.9f))return false;return Vector3.Distance(from+seg/len*along,p)<.5f;}
  IEnumerator GranGo(string to){yield return GranWalk(to);}

  // ---------- lifecycle ----------
  void Awake(){if(!loop)loop=GetComponent<LoopDirector>();savePath=System.IO.Path.Combine(Application.persistentDataPath,"story-checkpoint.json");}
  void Start(){rewind=GetComponent<NightRewind>();if(!rewind)rewind=gameObject.AddComponent<NightRewind>();rewind.loop=loop;rewind.prop=Prop;
   granBody=flat.grandmother.GetComponent<CapsuleCollider>();if(!granBody){granBody=flat.grandmother.AddComponent<CapsuleCollider>();}var gs=flat.grandmother.transform.lossyScale;granBody.radius=GranNav.Radius/Mathf.Max(.01f,Mathf.Max(gs.x,gs.z));granBody.height=GranNav.Height/Mathf.Max(.01f,gs.y);granBody.center=Vector3.up*granBody.height*.5f;granBody.direction=1;granBody.enabled=false;
   RememberProps();tv=FindFirstObjectByType<ScenarioTelevision>();var anim=flat.grandmother.GetComponentInChildren<Animation>(true);granAnim=anim;
   if(anim)foreach(AnimationState s in anim){if(s.name.EndsWith("Oidle"))granIdle=s.name;if(s.name.EndsWith("Owalk"))granWalk=s.name;}}
  public bool HasSave=>System.IO.File.Exists(savePath);
  void Checkpoint(string at){if(loop.testing&&!qaAllowSave)return;System.IO.File.WriteAllText(savePath,JsonUtility.ToJson(new Save{chapter=at,flashlight=player.hasFlashlight,belt=belt,memories=rewind?rewind.Export():null}));}
  public void NewGame(){Begin("Prologue");}
  public void Continue(){var s=HasSave?JsonUtility.FromJson<Save>(System.IO.File.ReadAllText(savePath)):null;rewind.Import(s?.memories);Begin(s?.chapter??"Prologue",s);}
  void Begin(string at,Save s=null){
   StopAll();loop.Thought("",0);if(at=="Prologue"||at=="Evening")rewind.ForgetEvening();player.hasFlashlight=s!=null&&s.flashlight;belt=s!=null&&s.belt;
   if(at=="Loop"){chapter=Chapter.Loop;flat.entrance.direction=1;loop.ResumeSavedLap();return;}
   running=StartCoroutine(at switch{"Evening"=>Evening(false),"Night1"=>Night1(false),"Night2"=>Night2(false),"Finale"=>Finale(),_=>Prologue()});
  }
  public void Abandon(){StopAll();chapter=Chapter.Loop;flat.entrance.direction=1;}
  void StopAll(){EndMirrorSeed();if(rewind)rewind.Cleanup();FinaleReset();if(running!=null)StopCoroutine(running);StopAllCoroutines();acts.Clear();holdingAct=null;kettleBoiling=false;card=null;fade=0;objective="";choiceA=choiceB=null;scenes.Cancel();loop.chase.Stop();sound.ResetWorld();if(tv){tv.storyOn=false;tv.show=ScenarioTelevision.Show.None;}if(bedside)bedside.timeSource=flat.clock;QaInFreeTime=false;if(strap)strap.enabled=false;}
  void Update(){
   if(granBody)granBody.enabled=OwnsFlat&&chapter!=Chapter.Ended; // in the loop the hanging vision needs clear sight to her
   if(!OwnsFlat||player.paused)return;
   if(chapter!=Chapter.Ended&&!loop.Busy&&string.IsNullOrEmpty(scenes.PlayingScene))TickActs();else loop.prompt.text="";
   if(!rewind||!rewind.Active)GranLookAtPlayerWhenNear(); // frozen during the rewind
   // SCENARIO 12: idle 40 s in the evening -> current task reminder.
   if(chapter==Chapter.Evening&&!string.IsNullOrEmpty(objective)&&objective!="Свободное время"&&Time.time-lastProgress>40){lastProgress=Time.time;Say("","Бабушка просила… "+objective.ToLowerInvariant(),3.5f);}
  }

  // Common world state for the apartment outside the loop: warm, doors normal, loop props hidden.
  void Homely(bool warm){
   loop.Progress.RestoreCheckpoint(1);foreach(var g in flat.conditionGroups)g.SetActive(false);if(flat.dressing)flat.dressing.Apply(1);
   if(warm)flat.look.WarmReturn();else flat.look.Apply(FlatCondition.Blackout);
   flat.kitchen.locked=flat.bath.locked=false;flat.bedroom.locked=false;flat.bedroom.closedAngle=0;flat.living.locked=false;flat.living.open=true;
   // The story has no room-door action (only the loop has one): the warm flat keeps the kitchen and bedroom open, so a
   // continued evening (grandmother already in the kitchen) or a second playthrough after lap 5 cannot shut the player out.
   flat.kitchen.open=true;flat.bedroom.open=true;
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
   Checkpoint("Evening");HouseSound(true,false);PropsAtEveningStart();if(flat.look.icon)flat.look.icon.intensity=0; // 4.7/4.8: the icon lamp is lit later (by itself or by her)
   Put("Bag",BagAtDoor);Show("Corvalol",false);flat.clock.stopped=true;Clock(19,15);OfferFlashlight();
   // 4.1
   Task("Повесить куртку и разуться");
   yield return Use(new Vector3(8.72f,1.6f,5.1f),"Повесить куртку");sound.Play("carpet_step",player.transform.position);
   yield return Use(new Vector3(8.55f,.2f,5.2f),"Переобуться в тапки",0,2.2f);
   yield return Line("Бабушка","Тапки дедовы надевай, твои малы уже");
   // 4.2
   Clock(19,20);Task("Разобрать сумку на кухне");
   yield return Use(()=>Prop("Bag")&&Prop("Bag").gameObject.activeSelf?Prop("Bag").position+Vector3.up*.25f:new Vector3(7.58f,.8f,.6f),"Взять сумку",0,2.2f);Show("Bag",false);
   yield return Use(new Vector3(7.58f,.8f,.6f),"Поставить сумку на стол");Put("Bag",BagOnTable);Show("Bread",true);Show("Cans",true);
   yield return Use(new Vector3(7.4f,.8f,.6f),"Батон — в хлебницу");Show("Bread",false);
   yield return Use(new Vector3(6.85f,1.5f,1.6f),"Шпроты — в шкаф");Show("Cans",false);
   yield return Use(()=>flat.grandmother.transform.position+Vector3.up*1.2f,"Отдать «Корвалол» бабушке",0,2);
   Put("Corvalol",CorvalolOnTable);Show("Bag",false); // she keeps the bottle by the table; the bag goes to the room
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
   rewind.Remember("tea","spoon");
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
   yield return Use(new Vector3(6.6f,.3f,5.4f),"Поставить табуретку под антресолью",0,2.2f);
   flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(6.6f,0,5.4f),Quaternion.identity);
   Put("Bulb",BulbOnShelf); // visible on the antresol before it is taken
   yield return Use(()=>Prop("Bulb")&&Prop("Bulb").gameObject.activeSelf?Prop("Bulb").position+Vector3.up*.05f:new Vector3(6.6f,2.2f,5.25f),"Достать лампочку с антресоли",0,2.6f);Show("Bulb",false);
   yield return UseHeld(ScenarioLayout.Clock+new Vector3(0,.9f,.1f),"Выкрутить старую и вкрутить новую",2,2.6f,"bulb",ScenarioLayout.Clock+new Vector3(0,.9f,.1f));
   if(propStart.TryGetValue("Bulb",out var bulbHome))Put("Bulb",bulbHome.at);else Show("Bulb",true);if(sconce)sconce.intensity=Mathf.Max(sconceWas,1.8f);rewind.Remember("bulb","bulb");flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(5.5f,0,5.8f),Quaternion.identity);
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
   rewind.Remember("belt","door_open");
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
   // 4.7 free time with the four seeds, then 4.8 the news on the «Рубин» screen
   yield return FreeTime();
   yield return News();
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
  void RememberProps(){foreach(var id in new[]{"Kettle","Matchbox","JamJar","Belt","Bread","Cans","Bulb","Shawl","TeaSet","Bag","Corvalol"}){var t=Prop(id);if(t&&!propStart.ContainsKey(id))propStart[id]=(t.position,t.rotation,t.gameObject.activeSelf);}}
  void PropsAtEveningStart(){foreach(var kv in propStart){var t=Prop(kv.Key);if(!t)continue;t.SetPositionAndRotation(kv.Value.at,kv.Value.rot);t.gameObject.SetActive(kv.Value.on);}}
  static readonly Vector3 BulbOnShelf=new Vector3(6.75f,2.293f,5.71f); // on the top front edge of the hall antresol (AntresolInstaller: cabinet x6.19–7.01 z5.67–6.0 y1.65–2.29)
  static readonly Vector3 BagAtDoor=new Vector3(8.35f,0,5.45f),BagOnTable=new Vector3(7.95f,.745f,.62f),CorvalolOnTable=new Vector3(7.25f,.745f,.75f);
  void PropsAfterEvening(){PropsAtEveningStart();Show("Bag",false);Show("Corvalol",false);Put("Kettle",new Vector3(8.48f,.86f,1.55f));Put("JamJar",new Vector3(7.35f,.745f,.62f));Show("Bulb",true);Show("Shawl",true);Show("TeaSet",true);}
  Light FlameLight(Vector3 at){var g=new GameObject("Gas flame (story)");g.transform.position=at;var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.35f,.55f,1);l.intensity=.6f;l.range=1.2f;l.shadows=LightShadows.None;return l;}

  IEnumerator ValyaAtTheDoor(bool second){
   Task("");
   sound.Bed("radio",RadioAt,false);
   if(!second){Clock(21,40);sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.25f);sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.25f);sound.Play("knock",flat.entrance.transform.position);
    yield return Line("Голос Вали за дверью","Нинок! Нинок, открой!");
    yield return GranGo("Door");GranShow("Door",0);flat.entrance.locked=false;flat.entrance.open=true;sound.Play("door_open",flat.entrance.transform.position);
    yield return Line("Тётя Валя (на площадке)","Нинок, Коле плохо, давление, я одна боюсь — посиди со мной, а?",5);}
   else{GranShow("Door",180);flat.entrance.locked=false;flat.entrance.open=true;player.crouched=false;player.Place(new Vector3(7.95f,.02f,3.85f),0);}
   yield return GranFace(player.head.position);
   yield return Line("Бабушка","Мишаня, я у Вали, на полчасика. Никому не открывай. Я своим ключом. А если стучать буду — свои стучат вот так, слушай…",7);
   sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.22f);sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.7f);sound.Play("knock",flat.entrance.transform.position);
   if(!second)rewind.Remember("knock","knock");
   Say("","тук-тук… тук",2);yield return new WaitForSeconds(1.5f);
   if(second){
    yield return Card("Это уже было",2.5f);
    yield return Choose("«Ба, не уходи!»","Промолчать",10);
    if(choice==1)yield return Line("Бабушка","Да что ты, Мишаня, белый весь… Валя одна, Коле плохо. Полчасика. Закройся",5);
   }else yield return Line("Бабушка","Запомнил? Фонарик в трюмо, если свет выбьет. Всё, закройся");
   Show("Corvalol",false); // 4.9: «берёт «Корвалол»»
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
   if(!continued)PropsAfterEvening();if(rewind.MemoriesRecorded<4){Debug.Log("REWIND evening memories missing ("+rewind.MemoriesRecorded+"): canonical ones");RememberCanonicalEvening();}Checkpoint("Night1");rewind.Begin();HouseSound(false,true);Task("Дождаться бабушку");if(!continued)OfferFlashlight();
   yield return NightMinutesUntil22();
   yield return NightEvents(1,false);
  }
  IEnumerator NightEvents(int which,bool from2200Restart){
   // 22:00 everything goes quiet, then the set turns itself on: test card, 1 kHz, announcer
   yield return HouseQuiet();yield return new WaitForSeconds(1.2f);
   if(tv){tv.storyOn=true;tv.show=ScenarioTelevision.Show.TestCard;tv.caption="";}flat.televisionLight.intensity=.12f;sound.Play("tv_on",flat.television.position);sound.Bed("crt_whine",flat.television.position,true);yield return new WaitForSeconds(.4f);sound.Play("test_tone",flat.television.position);yield return new WaitForSeconds(2.8f);
   if(tv)tv.show=ScenarioTelevision.Show.Announcer; // the blue screen, his words on it (no announcer figure: no model)
   foreach(var l in new[]{"Внимание. Жителям Заречного района.","Атака на запоминание.","Не открывайте дверь. Не отвечайте знакомым голосам. Она боится света.","Запомните всё."}){if(tv)tv.caption=l;yield return Line("«Рубин», диктор",l,3.2f);}
   if(which==2){yield return new WaitForSeconds(1.5f);if(tv)tv.caption="Ты же помнишь, Миша?";yield return Line("«Рубин», диктор","Ты же помнишь, Миша?",3.5f);}
   yield return new WaitForSeconds(1);if(tv){tv.storyOn=false;tv.show=ScenarioTelevision.Show.None;}flat.televisionLight.intensity=0;sound.Bed("crt_whine",flat.television.position,false);
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
    rewind.Stop();yield return scenes.Play("DeathFull");sound.EndDeathRecovery();flat.clock.stopped=true;
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
   // 6.1: the recorded night played backwards (NightRewind), then the frozen frame at the start of night 2 and PLAY.
   loop.prompt.text="";acts.Clear();card=null;fade=0;
   yield return rewind.Play(PrepareNight2,frozen=>{if(granAnim)granAnim.enabled=!frozen;});
  }
  // The first frame of night 2 (21:40, Valya at the door — ValyaAtTheDoor(true) continues from exactly here).
  void PrepareNight2(){
   acts.Clear();card=null;fade=0;loop.prompt.text="";Homely(true);PropsAfterEvening();flat.balconyDoor.closedAngle=0;flat.clock.stopped=false;Clock(21,40);
   flat.entrance.locked=false;flat.entrance.open=true;player.crouched=false;player.Place(new Vector3(7.95f,.02f,3.85f),0);GranShow("Door",180);
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

  // ---------- 4.7 free time (21:00 → 21:30) and 4.8 news ----------
  // Free time with the four "anxious seeds" (SCENARIO 4.7), one at a time and only where Misha is:
  //  1 mirror  — in the dresser mirror a dark figure taller than the doorway stands behind him for 0.3 s. It is the
  //              Guest model, black, on the TV-only layer 31 which only the hall reflection probe is told to see — so
  //              turning round shows nothing. The mirror is the accepted rough probe-128 metal: a dark smear, not a sharp image;
  //  2 clock   — the hall grandfather clock runs 7 minutes behind. There is no kitchen "Янтарь": the bedside alarm clock
  //              (normally slaved to the hall clock) shows the true time instead;
  //  3 radio   — the switched-off kitchen radio point hisses by itself (procedural noise);
  //  4 lamp    — the icon lamp in the bedroom, unlit this evening, lights itself while he looks (she says later she did it).
  // Absent from 4.7: the album, the Dendy game, «Теленеделя», the radio programme and weather report.
  public readonly List<(string seed,float at)> QaSeeds=new List<(string,float)>();public bool QaInFreeTime{get;private set;}public bool QaNewsDone{get;private set;}
  public ClockFace QaBedsideClock=>bedside;public float QaFreeTimeSeconds=>freeSeconds;
  public const float FreeTimeLength=75;public const int ClockLag=7*60;
  ClockFace bedside;float freeSeconds;bool lampLitItself;
  ClockFace Bedside(){if(!bedside)foreach(var c in FindObjectsByType<ClockFace>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(c.timeSource==flat.clock){bedside=c;break;}return bedside;}
  public Renderer QaMirror=>Mirror();Renderer Mirror(){if(!flat.hallCupboard)return null;foreach(var r in flat.hallCupboard.GetComponentsInChildren<Renderer>(true))if(r.name.Contains("mirror_0"))return r;return null;}
  bool InKitchen(Vector3 p)=>p.x>6.6f&&p.z<2.95f&&p.z>-.1f;bool InBedroom(Vector3 p)=>p.x<2.85f&&p.z<4.45f&&p.z>-.1f;
  bool Looking(Vector3 at,float cos){var d=at-player.head.position;return Vector3.Dot(player.head.forward,d.normalized)>cos;}
  IEnumerator FreeTime(){
   Task("Свободное время");QaSeeds.Clear();QaInFreeTime=true;lampLitItself=false;StartCoroutine(GranGo("Armchair"));
   sound.Bed("radio",RadioAt,false); // she switched the radio point off for the news later
   var bed=Bedside();if(bed)bed.timeSource=null;
   var done=new HashSet<string>();float lastSeed=-99;
   for(freeSeconds=0;freeSeconds<FreeTimeLength;freeSeconds+=Time.deltaTime){
    float now=21*3600+freeSeconds/FreeTimeLength*30*60;flat.clock.Show(now-ClockLag);if(bed)bed.Show(now);
    var p=player.transform.position;bool ready=freeSeconds-lastSeed>6&&player.control;
    if(ready&&!done.Contains("mirror")){var m=Mirror();if(m){var c=m.bounds.center;var flatD=c-p;flatD.y=0;if(flatD.magnitude<2.2f&&p.x>c.x+.3f&&Looking(c,.85f)){done.Add("mirror");lastSeed=freeSeconds;yield return MirrorSeed(m);continue;}}}
    if(ready&&!done.Contains("clock")&&Vector3.Distance(p,ScenarioLayout.Clock)<2.6f&&Looking(ScenarioLayout.Clock+Vector3.up*.6f,.9f)){done.Add("clock");lastSeed=freeSeconds;QaSeeds.Add(("clock",Time.time));Say("","Отстают, что ли?.. Будильник в спальне на семь минут больше показывал",4);}
    if(ready&&!done.Contains("radio")&&freeSeconds>6&&InKitchen(p)){done.Add("radio");lastSeed=freeSeconds;QaSeeds.Add(("radio",Time.time));sound.Play("radio_hiss",RadioAt);StartCoroutine(SayLater(1.6f,"Радио же выключено…"));}
    if(ready&&!done.Contains("lamp")&&flat.look.icon&&InBedroom(p)&&Looking(flat.look.icon.transform.position,.8f)){done.Add("lamp");lastSeed=freeSeconds;QaSeeds.Add(("lamp",Time.time));lampLitItself=true;yield return LampLights();}
    yield return null;}
   if(bed){bed.timeSource=flat.clock;}QaInFreeTime=false;
  }
  IEnumerator SayLater(float s,string line){yield return new WaitForSeconds(s);Say("",line,3);}
  IEnumerator LampLights(){var l=flat.look.icon;for(float t=0;t<.7f;t+=Time.deltaTime){l.intensity=.14f*Mathf.Clamp01(t/.7f)*(.6f+.4f*Mathf.PerlinNoise(t*30,.2f));yield return null;}l.intensity=.14f;}
  // Everything the mirror seed changes on the shared Guest and the hall probe, kept until it is put back. Put back by
  // the seed itself, and by StopAll / QaNightEvents / OnDisable / OnDestroy if the seed is cut off mid-way (a stopped
  // coroutine runs no cleanup): otherwise the Guest would stay on layer 31 (invisible), black and 2.2 m tall.
  sealed class MirrorState {public readonly List<(Transform t,int layer)> layers=new List<(Transform,int)>();public readonly List<(Renderer r,MaterialPropertyBlock block)> blocks=new List<(Renderer,MaterialPropertyBlock)>();public Vector3 position,scale;public Quaternion rotation;public bool active;public HallReflections probe;public int mask;}
  MirrorState mirrorState;public bool QaMirrorActive=>mirrorState!=null;
  void EndMirrorSeed(){
   var s=mirrorState;if(s==null)return;mirrorState=null;if(!flat||!flat.guest)return;var g=flat.guest;var gt=g.transform;
   foreach(var x in s.blocks)if(x.r)x.r.SetPropertyBlock(x.block);foreach(var x in s.layers)if(x.t)x.t.gameObject.layer=x.layer;
   gt.localScale=s.scale;gt.SetPositionAndRotation(s.position,s.rotation);g.SetActive(s.active);
   if(s.probe){s.probe.probe.cullingMask=s.mask;s.probe.Recapture();}}
  void OnDisable(){EndMirrorSeed();}void OnDestroy(){EndMirrorSeed();}
  IEnumerator MirrorSeed(Renderer mirror){
   EndMirrorSeed();var probe=FindFirstObjectByType<HallReflections>();var g=flat.guest;var gt=g.transform;
   var s=new MirrorState{position=gt.position,rotation=gt.rotation,scale=gt.localScale,active=g.activeSelf,probe=probe,mask=probe?probe.probe.cullingMask:0};
   foreach(var t in g.GetComponentsInChildren<Transform>(true))s.layers.Add((t,t.gameObject.layer));
   var rs=g.GetComponentsInChildren<Renderer>(true);foreach(var r in rs){MaterialPropertyBlock saved=null;if(r.HasPropertyBlock()){saved=new MaterialPropertyBlock();r.GetPropertyBlock(saved);}s.blocks.Add((r,saved));}
   mirrorState=s; // from here on every change below is undone by EndMirrorSeed, whoever calls it
   var p=player.transform.position;var c=mirror.bounds.center;var back=p-c;back.y=0;back.Normalize();
   var at=p+back*1.3f;at.x=Mathf.Clamp(at.x,7.0f,8.55f);at.z=Mathf.Clamp(at.z,3.1f,5.8f);at.y=0;
   foreach(var x in s.layers)x.t.gameObject.layer=31;
   var pose=g.GetComponent<ProxyPose>();g.SetActive(true);if(pose)pose.Stand();gt.SetPositionAndRotation(at,Quaternion.LookRotation(-back));
   var b=rs.Length>0?rs[0].bounds:new Bounds(at,Vector3.one);foreach(var r in rs)b.Encapsulate(r.bounds);if(b.size.y>.1f)gt.localScale=s.scale*(2.2f/b.size.y);
   var black=new MaterialPropertyBlock();black.SetColor("_BaseColor",new Color(.02f,.018f,.016f,1));black.SetFloat("_Smoothness",0);foreach(var r in rs)r.SetPropertyBlock(black);
   int before=probe?probe.CompletedCaptures:0;
   if(probe){probe.probe.cullingMask=s.mask|(1<<31);probe.Recapture();}
   for(float t=0;t<.6f&&probe&&probe.CompletedCaptures==before;t+=Time.unscaledDeltaTime)yield return null; // the probe renders its faces over a few frames
   QaSeeds.Add(("mirror",Time.time));QaMirrorFigure=at;yield return new WaitForSeconds(.3f);
   EndMirrorSeed();
  }
  public Vector3 QaMirrorFigure{get;private set;}
  IEnumerator News(){
   Clock(21,30);Task("Включить новости");
   yield return Line("Бабушка","Мишаня, включи-ка новости");
   yield return Use(()=>flat.televisionScreen?flat.televisionScreen.bounds.center:flat.television.position,"Включить «Рубин»",0,2.4f); // the screen, not the cabinet edge (Codex story15)
   if(tv){tv.storyOn=true;tv.show=ScenarioTelevision.Show.News;tv.caption="";}flat.televisionLight.intensity=.12f;sound.Play("switch",flat.television.position);sound.Play("tv_on",flat.television.position);sound.Bed("crt_whine",flat.television.position,true);
   yield return new WaitForSeconds(1.4f); // the picture comes up
   foreach(var l in new[]{"…Сегодня в милицию снова обратились жители Заречного района.","В квартире по улице Строителей обнаружен накрытый стол и включённый свет. По словам соседей, в квартире никто не проживал.","Однако на фотографиях, изъятых из квартиры, — пустые места рядом с людьми…","Специалисты рекомендуют: не открывайте дверь в позднее время. И — что бы ни случилось — запомните свой дом. Как следует запомните."})
    {if(tv)tv.caption=l;yield return Line("Диктор",l,5);}
   yield return Line("Бабушка","Нечего на ночь глядеть");if(tv){tv.storyOn=false;tv.show=ScenarioTelevision.Show.None;}flat.televisionLight.intensity=0;sound.Bed("crt_whine",flat.television.position,false);sound.Play("switch",flat.television.position);
   if(lampLitItself)yield return Line("Бабушка","Лампадку я зажгла, я, не пугайся");
   if(flat.look.icon)flat.look.icon.intensity=.14f;Clock(21,38);QaNewsDone=true;
  }
  // QA: 4.7 + 4.8 alone (≈2 min instead of the 20-minute evening), the evening state as at 21:00.
  public void QaFreeTimeAndNews(){StopAll();running=StartCoroutine(QaEveningEnd());}
  IEnumerator QaEveningEnd(){chapter=Chapter.Evening;QaNewsDone=false;loop.Pause(false);Homely(true);PropsAfterEvening();HouseSound(true,false);flat.clock.stopped=false;Clock(21,0);if(flat.look.icon)flat.look.icon.intensity=0;GranShow("Armchair",0);
   yield return FreeTime();yield return News();}
  // ---------- 11. Finale ----------
  // 11.6–11.10 on the approved Katcho rig (GranRig: arm IK, held props, bone length, tint) and a ready ball chain
  // (DoorChain). Not made (no mesh / no rig part / no recording): her eye sliding, the temple seam, the face sliding off
  // the chin, the jaw, Misha's face mesh in 11.10, the ring, finger bones, humming / coat / chain sounds, voices.
  // QA (FinaleQA): the beats in order with the game time they started.
  public readonly List<(string beat,float at)> QaFinaleBeats=new List<(string,float)>();public string QaFinaleBeat{get;private set;}="";
  public bool QaRefusedCard{get;private set;}public GranRig QaRig=>rig;public DoorChain QaChain=>chain;public Transform QaProp(string id)=>Prop(id);
  public void QaFinaleOpen(){openedDoorFinale=true;}
  public const float ChainGap=8.5f;public static readonly Vector3 GapView=new Vector3(6.95f,.02f,5.6f); // his staged view of the chain slot (11.6б)
  bool chainTaken;GapClaw gapClaw;float? nearBefore;
  void NearClip(float v){var cam=scenes.cine;if(!cam)return;var l=cam.Lens;if(!nearBefore.HasValue)nearBefore=l.NearClipPlane;l.NearClipPlane=v;cam.Lens=l;}
  void RestoreNear(){var cam=scenes.cine;if(!cam||!nearBefore.HasValue)return;var l=cam.Lens;l.NearClipPlane=nearBefore.Value;cam.Lens=l;nearBefore=null;}
  // QA: the stretching hand — the ready claw while it is on, else the Katcho fingertip.
  public GapClaw QaGapClaw=>gapClaw;public Vector3 HandTip=>gapClaw&&gapClaw.On?gapClaw.Tip:rig?rig.Tip(true):Vector3.zero;
  public float QaClawMaxStretch{get;private set;}(float intensity,Color color)? landingBefore;
  void RestoreLanding(){var l=scenes.landingLight;if(l&&landingBefore.HasValue){l.intensity=landingBefore.Value.intensity;l.color=landingBefore.Value.color;}landingBefore=null;}
  IEnumerator HoldLook(ScenarioPlayer p,Vector3 at,Func<bool> until){while(!until()){p.head.rotation=Quaternion.Slerp(p.head.rotation,Quaternion.LookRotation(at-p.head.position),1-Mathf.Exp(-6*Time.deltaTime));yield return null;}} // leaf angle on the chain: ~10 cm between the jamb corner and the free edge (wall 0.35 m)
  public static readonly Vector3 FridgeStand=new Vector3(7.95f,0,2.15f),SinkStand=new Vector3(7.35f,0,1.6f),StoveStand=new Vector3(8.0f,0,1.65f),KettleFront=new Vector3(8.48f,.86f,1.55f),KettleBack=new Vector3(8.48f,.86f,1.85f),TapAt=new Vector3(7.0f,1.16f,1.6f);
  GranRig rig;DoorChain chain;Vector3 granScale;bool granHeld;
  void Beat(string b){QaFinaleBeat=b;QaFinaleBeats.Add((b,Time.time));}
  // QA: how far her fingertip ended from each reach target (the arm is 0.62 m with the hand; IK clamps beyond that).
  public readonly Dictionary<string,float> QaReachError=new Dictionary<string,float>();
  Vector3 Stand(Vector3 raw){Nav.Ensure();return Nav.Snap(raw,.5f,out var q)?q:raw;}
  GranRig Rig(){if(!rig){rig=flat.grandmother.GetComponent<GranRig>();if(!rig)rig=flat.grandmother.AddComponent<GranRig>();granScale=flat.grandmother.transform.localScale;}rig.Init();return rig;}
  void FinaleReset(){RestoreLanding();if(gapClaw){Destroy(gapClaw.gameObject);gapClaw=null;}RestoreNear();if(rig){rig.ResetAll();flat.grandmother.transform.localScale=granScale;}if(chain)Destroy(chain.gameObject);chain=null;granHeld=false;if(granAnim)granAnim.enabled=true;
   foreach(var l in FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(l.name=="Match flame (story)")Destroy(l.gameObject);}
  IEnumerator Wait(float s){for(float t=0;t<s;t+=Time.deltaTime)yield return null;}
  // She walks a short straight stretch inside one room (the kitchen, the doorway) with the authored walk clip.
  IEnumerator GranStep(Vector3 to,float speed=.8f){var g=flat.grandmother.transform;to.y=g.position.y;GranPlay(granWalk);
   while((g.position-to).sqrMagnitude>.0004f){var d=to-g.position;d.y=0;if(d.sqrMagnitude>1e-4f)g.rotation=Quaternion.Slerp(g.rotation,Quaternion.LookRotation(d),1-Mathf.Exp(-10*Time.deltaTime));g.position=Vector3.MoveTowards(g.position,to,speed*Time.deltaTime);yield return null;}
   GranPlay(granIdle);}
  // Her right fingertip to a point over `seconds` (smooth), weight up to 1.
  IEnumerator GranReach(bool right,Vector3 to,float seconds,string qa=null){var r=Rig();var from=r.Tip(right);float w0=right?r.weightR:r.weightL;
   for(float t=0;t<seconds;t+=Time.deltaTime){float u=Mathf.SmoothStep(0,1,t/seconds);r.Reach(right,Vector3.Lerp(from,to,u),Mathf.Lerp(w0,1,u));yield return null;}r.Reach(right,to,1);yield return new WaitForEndOfFrame();if(qa!=null){QaReachError[qa]=Vector3.Distance(r.Tip(right),to);}}
  IEnumerator GranLower(bool right,float seconds){var r=Rig();float w0=right?r.weightR:r.weightL;var at=right?r.targetR:r.targetL;for(float t=0;t<seconds;t+=Time.deltaTime){r.Reach(right,at,Mathf.Lerp(w0,0,t/seconds));yield return null;}r.Reach(right,at,0);}
  bool Sees(Vector3 point){var cam=player.view?player.view:Camera.main;if(!cam)return false;var v=cam.WorldToViewportPoint(point);if(v.z<.2f||v.x<.15f||v.x>.85f||v.y<.15f||v.y>.85f)return false;var from=cam.transform.position;var d=point-from;
   foreach(var h in Physics.RaycastAll(from,d.normalized,d.magnitude-.05f,~(1<<31),QueryTriggerInteraction.Ignore))if(!h.transform.IsChildOf(player.transform)&&!h.transform.IsChildOf(flat.grandmother.transform))return false;return true;}

  IEnumerator Finale(){
   FinaleReset();QaFinaleBeats.Clear();QaReachError.Clear();QaRefusedCard=false;QaClawMaxStretch=0;
   chapter=Chapter.Finale;loop.Pause(false);Homely(true);PropsAfterEvening();Put("Kettle",KettleBack);flat.clock.stopped=false;Clock(22,0);
   flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(5.5f,0,5.8f),Quaternion.identity);if(strap)strap.enabled=false;flat.entrance.locked=true;
   if(flat.look.exterior)flat.look.exterior.Blackout(false);
   chain=DoorChain.Create(flat.entrance);chain.Engage();Rig();
   player.Place(ScenarioLayout.Spawn,180);Checkpoint("Finale");Task("Дождаться бабушку");HouseSound(true,false);Beat("22:00");
   yield return new WaitForSeconds(2);Say("","Получилось?..",3);
   nightSeconds=22*3600;yield return NightTo(22,10,75);
   // 22:10 the right knock
   bool peeped=false;openedDoorFinale=false;Beat("knock");
   sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.22f);sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(.7f);sound.Play("knock",flat.entrance.transform.position);
   yield return Line("Голос бабушки за дверью","Мишаня, это я. Открывай, замёрзла",3.5f);
   var peep=Offer(()=>flat.entrance.transform.position+Vector3.up*.5f,"Глазок",0,1.8f,Key.E,null,()=>peeped=true);
   var open=Offer(()=>flat.entrance.transform.position+Vector3.up*.5f,"Открыть: два замка и цепочка",0,1.8f,Key.R,null,()=>openedDoorFinale=true);
   float away=0;Beat("choice");
   while(!openedDoorFinale&&away<15){
    if(peeped){peeped=false;Withdraw(peep);yield return PeepholeGrandmother();peep=Offer(()=>flat.entrance.transform.position+Vector3.up*.5f,"Глазок",0,1.8f,Key.E,null,()=>peeped=true);}
    away=Vector3.Distance(player.transform.position,flat.entrance.transform.position)>2.2f?away+Time.deltaTime:0;yield return null;}
   Withdraw(peep);Withdraw(open);Task("");
   if(openedDoorFinale)yield return FinaleOpened();else yield return FinaleOwnKey();
   // 11.7 the door locks itself behind her; she goes to the kitchen
   flat.entrance.scripted=false;flat.entrance.open=false;flat.entrance.locked=true;sound.Play("latch",flat.entrance.transform.position);Beat("door-locked");
   yield return GranWalk("Stove");yield return GranFace(StoveAt,.3f);Beat("stove");
   for(float t=0;t<20&&Vector3.Distance(player.transform.position,Nodes["Stove"])>3.2f;t+=Time.deltaTime)yield return null; // the only way is after her
   yield return FinaleStove();
   // 11.8 back to him at the stove; the kettle starts to whistle
   Say("","Она напевает. Радио играет",5);yield return Wait(6);
   StartCoroutine(KettleBoils());Beat("whistle");yield return new WaitForSeconds(4);
   Beat("question");yield return Line("Бабушка (не оборачиваясь)","Мишаня… а ты меня помнишь?",4);
   yield return FinaleTransformation();
   // 11.10 the short face tear with the 3.2 beat of the full one (3.5 s), then 11.11 black and silence
   Beat("tear");yield return scenes.Play("DeathFinale");sound.EndDeathRecovery();FinaleReset();
   Beat("epilogue");yield return Epilogue(!openedDoorFinale);
  }
  // 11.6а: two locks and the chain; she comes in and hugs him, his face at her shoulder (the headscarf in frame).
  IEnumerator FinaleOpened(){
   var door=flat.entrance.transform.position;Beat("open");granHeld=true;
   sound.Play("latch",door);yield return Wait(.35f);sound.Play("latch",door);yield return Wait(.3f);chain.Hang();sound.Play("latch",chain.Anchor);yield return Wait(.25f);
   flat.entrance.locked=false;flat.entrance.open=true;sound.Play("door_open",door);
   var p=player;var body=p.GetComponent<CharacterController>();p.control=false;if(body)body.enabled=false;
   var fwd=p.transform.forward;fwd.y=0;fwd.Normalize();var stand=p.transform.position+fwd*.34f;stand.y=0;var outside=new Vector3(7.95f,0,6.7f);
   var g=flat.grandmother;g.SetActive(true);g.transform.SetPositionAndRotation(outside,Quaternion.LookRotation(stand-outside));GranPlay(granIdle);yield return Wait(.6f);
   yield return GranStep(stand,.9f);g.transform.rotation=Quaternion.LookRotation(-fwd);
   // the hug (story25-f: looking AT her shoulder from 0.25 m filled the frame with her coat and both forearms, which
   // reached for a point just behind the lens). Cheek to cheek instead: his eye beside her left ear, looking PAST her
   // into the lit hall and the open door behind her; the back of her head and headscarf at the frame edge, her
   // shoulder below; her hands meet behind his back, i.e. behind the camera. Near plane .02 for the hug only.
   var r=Rig();var gt=g.transform;var hugHeadStart=p.head.position;var hugLookStart=p.head.rotation;Beat("hug");Say("Бабушка","Замёрз? Сейчас чайку поставлю",3);NearClip(.02f);
   r.poleL=-gt.right-gt.up*.3f;r.poleR=gt.right-gt.up*.3f;
   for(float t=0;t<3.2f;t+=Time.deltaTime){
    float w=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.5f))*(t>2.7f?Mathf.Clamp01((3.2f-t)/.5f):1);float lean=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.7f))*(t>2.5f?Mathf.Clamp01((3.2f-t)/.7f):1);
    var ear=r.head.position-gt.right*.17f+gt.forward*.02f-Vector3.up*.02f;p.head.position=Vector3.Lerp(hugHeadStart,ear,lean);var eye=p.head.position;
    var past=eye-gt.forward*2+gt.right*.45f-Vector3.up*.35f; // ~12° towards her, ~10° down
    p.head.rotation=Quaternion.Slerp(hugLookStart,Quaternion.LookRotation(past-eye),lean);
    var torso=eye+gt.forward*.1f-Vector3.up*.35f;r.Reach(false,torso+gt.forward*.14f+gt.right*.12f,w);r.Reach(true,torso+gt.forward*.14f-gt.right*.12f,w);
    yield return null;}
   p.head.position=hugHeadStart;RestoreNear();r.weightL=r.weightR=0;r.poleL=r.poleR=null;Beat("hug-end");
   yield return GranStep(g.transform.position-g.transform.forward*.45f+Vector3.zero,.6f);
   HandBack(p,body);
  }
  void HandBack(ScenarioPlayer p,CharacterController body){var e=p.head.rotation.eulerAngles;p.transform.rotation=Quaternion.Euler(0,e.y,0);p.pitch=Mathf.Clamp(Mathf.DeltaAngle(0,e.x),-80,80);p.head.localRotation=Quaternion.Euler(p.pitch,0,0);if(body)body.enabled=true;p.control=true;}
  // 11.6б: her own key; the door stops on the chain (~10 cm); her hand gropes through the gap until he sees it, the
  // fingers (the one-bone hand) stretch for 2 s with cracking, the long nail lifts the chain off, the hand is normal again.
  IEnumerator FinaleOwnKey(){
   var e=flat.entrance;var door=e.transform.position;Beat("key");
   yield return Line("Голос бабушки за дверью","Я своим ключом, Мишаня",3);sound.Play("latch",door);e.locked=false;e.scripted=true;e.open=false;
   var g=flat.grandmother;g.SetActive(true);granHeld=true;g.transform.SetPositionAndRotation(new Vector3(7.86f,0,6.36f),Quaternion.Euler(0,180,0));GranPlay(granIdle);
   for(float t=0;t<.45f;t+=Time.deltaTime){e.leaf.localRotation=Quaternion.Euler(0,e.direction*Mathf.Lerp(0,ChainGap,t/.45f),0);yield return null;}
   e.leaf.localRotation=Quaternion.Euler(0,e.direction*ChainGap,0);Physics.SyncTransforms();sound.Play("latch",chain.Anchor);Beat("gap");
   // story25-key-a: from the hall the 10 cm slot was a black seam and the hand a 30 px blob. The landing light now
   // shines through the slot (hand, arm and chain read as a silhouette against it) and his view is taken to the wall
   // beside the door, 0.75 m from the slot, which opens towards him; control returns when the hand has gone.
   var landing=scenes.landingLight;if(landing){landingBefore=(landing.intensity,landing.color);landing.color=new Color(1,.86f,.66f);landing.intensity=.8f;}
   var p=player;var body=p.GetComponent<CharacterController>();p.control=false;if(body)body.enabled=false;
   var slot=new Vector3(7.62f,1.43f,5.93f);var fromFeet=p.transform.position;var fromLook=p.head.rotation;var toFeet=GapView;
   for(float t=0;t<.9f;t+=Time.deltaTime){float u=Mathf.SmoothStep(0,1,t/.9f);p.transform.position=Vector3.Lerp(fromFeet,toFeet,u);var look=Quaternion.LookRotation(slot-p.head.position);p.head.rotation=Quaternion.Slerp(fromLook,look,u);yield return null;}
   chainTaken=false;StartCoroutine(HoldLook(p,slot,()=>chainTaken));
   var r=Rig();r.poleR=g.transform.right*.6f-g.transform.up*.8f;
   var gap=new Vector3(7.60f,1.43f,5.955f);var outside=new Vector3(7.72f,1.38f,6.25f);
   yield return GranReach(true,outside,.4f);yield return GranReach(true,gap,.6f,"gap");Beat("grope");
   float seen=0;for(float t=0;t<20&&seen<1.2f;t+=Time.deltaTime){var le=chain.LeafEnd;r.Reach(true,le+new Vector3(-.04f+Mathf.Sin(t*2.3f)*.03f,Mathf.Sin(t*3.1f)*.025f,-.01f),1);if(Sees(r.Tip(true)))seen+=Time.deltaTime;yield return null;}
   // The fingers stretch (2 s): her block hand lengthens for 0.45 s, then on a crack it IS the ready claw hand (GapClaw,
   // her own hand collapsed), which keeps lengthening; the long nail lifts the chain; on the way back the claw shrinks
   // and her ordinary hand returns. Without a ready claw model the block hand stretches as before.
   Beat("fingers");float[] cracks={.15f,.45f,1.1f,1.6f};int c=0;var claw=GapClaw.Create(r,scenes.faceTear);gapClaw=claw;
   Vector3 Aim(Vector3 nail)=>claw&&claw.On?nail-(nail-r.upperR.position).normalized*claw.Protrusion:nail; // the wrist target that puts the claw tips on `nail`
   for(float t=0;t<2;t+=Time.deltaTime){
    if(claw&&t>=.45f){if(!claw.On)claw.Show(true);claw.stretch=Mathf.Lerp(.62f,1,Mathf.SmoothStep(0,1,(t-.45f)/1.55f));QaClawMaxStretch=Mathf.Max(QaClawMaxStretch,claw.stretch);}
    else r.handScale=Mathf.Lerp(1,claw?1.6f:2.2f,Mathf.SmoothStep(0,1,claw?t/.45f:t/2));
    r.forearmScale=Mathf.Lerp(1,1.12f,t/2);r.Reach(true,Aim(chain.LeafEnd+Vector3.down*.01f),1);
    if(c<cracks.Length&&t>=cracks[c]){sound.Play("finger_crack",HandTip);c++;}yield return null;}
   Beat("chain-off");chain.Hold(()=>HandTip);
   var from=chain.LeafEnd;var to=chain.Anchor+(chain.LeafEnd-chain.Anchor)*.25f+Vector3.up*.03f;
   for(float t=0;t<.6f;t+=Time.deltaTime){r.Reach(true,Aim(Vector3.Lerp(from,to,Mathf.SmoothStep(0,1,t/.6f))),1);yield return null;}
   chain.Hang();sound.Play("latch",chain.Anchor); // no chain-jingle recording: the latch clink stands in
   for(float t=0;t<.4f;t+=Time.deltaTime){if(claw&&claw.On)claw.stretch=Mathf.Lerp(1,.62f,t/.4f);else r.handScale=Mathf.Lerp(2.2f,1,t/.4f);r.forearmScale=Mathf.Lerp(1.12f,1,t/.4f);yield return null;}
   if(claw){claw.Show(false);sound.Play("finger_crack",r.Tip(true));Destroy(claw.gameObject);gapClaw=null;}r.handScale=r.forearmScale=1;Beat("hand-normal");
   yield return GranReach(true,outside,.4f);yield return GranLower(true,.3f);r.poleR=null;chainTaken=true;yield return null;HandBack(p,body);
   e.scripted=false;e.open=true;sound.Play("door_open",door);yield return Wait(.7f);RestoreLanding();
   Beat("enter");yield return GranStep(Nodes["Door"],.8f);yield return GranFace(player.head.position,.4f);
   yield return Line("Бабушка","Чего ж не открываешь, Мишаня?",3);
  }
  // 11.7: matches from the ZiL, gas, water from the tap, the kettle on the fire — what Misha did in the evening.
  IEnumerator FinaleStove(){
   var r=Rig();var g=flat.grandmother.transform;var box=Prop("Matchbox");var kettle=Prop("Kettle");
   Beat("matches");yield return GranStep(Stand(FridgeStand),.7f);if(box)yield return GranFace(box.position,.3f);
   if(box){yield return GranReach(true,box.position+Vector3.up*.02f,.6f,"matchbox");r.Hold(box,true);}
   yield return GranReach(true,g.position+g.forward*.28f+Vector3.up*1.12f,.4f);Beat("strike");sound.Play("match",r.Tip(true));
   var match=new GameObject("Match flame (story)").AddComponent<Light>();match.type=LightType.Point;match.color=new Color(1,.7f,.35f);match.intensity=.35f;match.range=.7f;match.shadows=LightShadows.None;match.transform.position=r.Tip(true)+Vector3.up*.03f;r.Hold(match.transform,true);
   yield return GranStep(Stand(StoveStand),.7f);yield return GranFace(StoveAt,.25f);
   yield return GranReach(true,StoveAt+new Vector3(-.12f,.03f,0),.5f,"burner");
   Beat("gas");yield return LightGas();var flame=FlameLight(StoveAt);flameLight=flame;r.Release(match.transform);Destroy(match.gameObject);if(box){r.Release(box);box.gameObject.SetActive(false);} // the matches go into her pocket
   yield return GranLower(true,.3f); // the kettle is on the back burner, on her left: the left hand takes it
   Beat("kettle");if(kettle){yield return GranReach(false,kettle.position+Vector3.up*.2f,.6f,"kettle");r.Hold(kettle,false);}
   yield return GranReach(false,g.position+g.forward*.3f-g.right*.15f+Vector3.up*1.05f,.35f);yield return GranStep(Stand(SinkStand),.7f);yield return GranFace(SinkAt,.25f);
   yield return GranReach(false,TapAt,.5f,"tap");Beat("water");sound.Play("tap",SinkAt);Say("","Она напевает",3);yield return Wait(3.2f);sound.ReleaseCue("tap");
   yield return GranReach(false,g.position+g.forward*.3f-g.right*.15f+Vector3.up*1.05f,.35f);yield return GranStep(Stand(StoveStand),.7f);yield return GranFace(StoveAt,.25f);
   if(kettle){yield return GranReach(false,KettleFront+Vector3.up*.2f,.6f,"kettle-front");r.Release(kettle);kettle.SetPositionAndRotation(KettleFront,Quaternion.Euler(0,90,0));}
   Beat("kettle-on");yield return GranLower(false,.5f);
  }
  Light flameLight;
  // 11.9 (3.5 s): she turns, her face still; the head jerks (the eye/seam/face/jaw have no mesh: see the header); the
  // skin greys; she straightens and grows to 2.4 m, bowing her head under the 2.5 m ceiling; forearms and hands
  // stretch; the whistle runs into her shriek; joints crack on each movement.
  IEnumerator FinaleTransformation(){
   var r=Rig();var g=flat.grandmother.transform;var p=player;p.control=false;if(granAnim)granAnim.enabled=false;Beat("transform");
   var from=g.rotation;var face=p.transform.position-g.position;face.y=0;var to=Quaternion.LookRotation(face);
   float h0=Mathf.Max(1,r.Top-g.position.y),grow=2.4f/h0;var baseScale=g.localScale;
   float[] cracks={1f,1.9f,2.5f,3.1f};int c=0;bool seam=false,shriek=false,off=false;
   for(float t=0;t<3.5f;t+=Time.deltaTime){
    g.rotation=Quaternion.Slerp(from,to,Mathf.SmoothStep(0,1,Mathf.Clamp01(t/1f)));
    r.headRoll=Mathf.Lerp(0,9,Mathf.Clamp01((t-1f)/.15f));
    if(!seam&&t>=1.3f){seam=true;sound.Play("wet_tear",r.head.position);}
    r.tint=Color.Lerp(Color.white,new Color(.6f,.62f,.58f),Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1.5f)/.9f)));
    float u=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1.6f)/1.9f));r.straighten=u;g.localScale=baseScale*Mathf.Lerp(1,grow,u);r.forearmScale=Mathf.Lerp(1,1.55f,u);r.handScale=Mathf.Lerp(1,2.2f,u);
    r.headBow=12*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1.5f)/.6f))+35*Mathf.Clamp01((r.Top-2.3f)/.15f);
    var eye=p.head.position;var pr=p.transform.right;r.Reach(false,eye+pr*.45f+Vector3.down*.2f,u);r.Reach(true,eye-pr*.45f+Vector3.down*.2f,u);
    if(!shriek&&t>=1.5f){shriek=true;sound.Play("shriek",g.position);}
    if(!off&&t>=2.1f){off=true;GasOff(false);sound.Bed("radio",RadioAt,false);sound.Bed("fridge",FridgeAt,false);if(flameLight)Destroy(flameLight.gameObject);}
    if(c<cracks.Length&&t>=cracks[c]){sound.Play("finger_crack",g.position+Vector3.up*1.4f);c++;}
    var look=r.HeadPosition+g.forward*.1f;p.head.rotation=Quaternion.Slerp(p.head.rotation,Quaternion.LookRotation(look-eye),1-Mathf.Exp(-6*Time.deltaTime));
    yield return null;}
   yield return new WaitForEndOfFrame();QaGrownTop=r.MeasureTop();
  }
  public float QaGrownTop{get;private set;}
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
   if(refused){QaRefusedCard=true;}if(refused)yield return Card("«…однако, по её словам, в тот вечер она слышала, как мальчик за дверью кричал: „Ты не она!“»",5);
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
   if(!string.IsNullOrEmpty(objective)&&card==null&&!player.paused&&!scenes.InDeath&&!(rewind&&rewind.Active))GUI.Label(new Rect(24,20,Screen.width*.5f,30),"Задача: "+objective,task);
   if(choiceA!=null){GUI.Label(new Rect(Screen.width*.2f,Screen.height*.62f,Screen.width*.6f,40),"[1] "+choiceA,small);GUI.Label(new Rect(Screen.width*.2f,Screen.height*.62f+44,Screen.width*.6f,40),"[2] "+choiceB,small);}
  }
 }
}
