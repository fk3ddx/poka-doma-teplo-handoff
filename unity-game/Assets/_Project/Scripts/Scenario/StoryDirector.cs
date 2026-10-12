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
  public sealed class Act {public Func<Vector3> at;public string label;public float range=1.8f,hold,cos=.86f;public Key key=Key.E;public Func<bool> when;public bool fired;public Action done;}
  readonly List<Act> acts=new List<Act>();Act aimed;float held;
  Act Offer(Func<Vector3> at,string label,float hold=0,float range=1.8f,Key key=Key.E,Func<bool> when=null,Action done=null){var a=new Act{at=at,label=label,hold=hold,range=range,key=key,when=when,done=done};acts.Add(a);return a;}
  IEnumerator Use(Vector3 point,string label,float hold=0,float range=1.8f){var a=Offer(()=>point,label,hold,range);while(!a.fired)yield return null;}
  IEnumerator Use(Func<Vector3> at,string label,float hold=0,float range=1.8f){var a=Offer(at,label,hold,range);while(!a.fired)yield return null;}
  void Withdraw(Act a){if(a!=null)acts.Remove(a);}
  void TickActs(){
   aimed=null;float best=-1;var eye=player.head.position;var forward=player.head.forward;
   foreach(var a in acts){if(a.fired||(a.when!=null&&!a.when()))continue;var d=a.at()-eye;if(d.magnitude>a.range)continue;float dot=Vector3.Dot(forward,d.normalized);if(dot>a.cos&&dot>best){best=dot;aimed=a;}}
   string text="";
   if(aimed!=null){
    var k=Keyboard.current;bool pressed=k!=null&&k[aimed.key].wasPressedThisFrame,down=k!=null&&k[aimed.key].isPressed;string key=aimed.key==Key.E?"E":aimed.key.ToString();
    if(aimed.hold<=0){text="["+key+"] "+aimed.label;if(pressed)Fire(aimed);}
    else{held=down?held+Time.deltaTime:0;text="["+key+" удерживать] "+aimed.label+(held>0?"  "+Mathf.RoundToInt(100*Mathf.Clamp01(held/aimed.hold))+"%":"");if(held>=aimed.hold){held=0;Fire(aimed);}}
   }else held=0;
   // Two prompts on one door (peephole / open) are shown together.
   foreach(var a in acts)if(a!=aimed&&!a.fired&&(a.when==null||a.when())&&aimed!=null&&(a.at()-aimed.at()).sqrMagnitude<.05f){var k=Keyboard.current;text+="   ["+a.key+"] "+a.label;if(k!=null&&k[a.key].wasPressedThisFrame)Fire(a);}
   loop.prompt.text=text;
  }
  void Fire(Act a){a.fired=true;acts.Remove(a);a.done?.Invoke();}
  // QA surface for StoryAutopilot (-story-smoke): it may only fire an act after the real aim selection picked it.
  public List<Act> OpenActs=>acts;public Act Aimed=>aimed;public void QaFire(Act a){if(a==aimed)Fire(a);}
  public bool Choosing=>choiceA!=null;public void QaChoose(int c){choice=c;}public string CurrentCard=>card;public string Objective=>objective;
  public void QaBegin(string at){Begin(at);}
  public bool QaBelt=>beltTied;

  // ---------- presentation ----------
  void Say(string who,string line,float seconds=0){loop.Thought(string.IsNullOrEmpty(who)?line:who+": «"+line+"»",seconds>0?seconds:Mathf.Clamp(line.Length*.07f,2.5f,7f));}
  IEnumerator Line(string who,string line,float seconds=0){float s=seconds>0?seconds:Mathf.Clamp(line.Length*.07f,2.5f,7f);Say(who,line,s);yield return new WaitForSeconds(s);}
  void Task(string text){objective=text;}
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
   {"Door",new Vector3(7.85f,0,5.25f)},{"HallA",new Vector3(7.75f,0,4.4f)},{"KitchenDoor",new Vector3(7.25f,0,3.25f)},{"Kitchen",new Vector3(7.5f,0,2.1f)},{"Stove",new Vector3(8.0f,0,1.65f)},
   {"HallJoint",new Vector3(6.9f,0,5.25f)},{"HallB",new Vector3(5.65f,0,5.3f)},{"Clock",new Vector3(5.5f,0,5.45f)},{"LivingDoor",new Vector3(5.65f,0,4.0f)},{"LivingMid",new Vector3(4.4f,0,3.5f)},
   {"Aisle",new Vector3(4.15f,0,2.8f)},{"Armchair",new Vector3(3.95f,0,1.05f)},{"BedroomDoor",new Vector3(3.2f,0,3.75f)},{"Bedroom",new Vector3(2.0f,0,3.5f)},{"Landing",new Vector3(8.05f,0,6.62f)}};
  static readonly string[][] Edges={new[]{"Landing","Door"},new[]{"Door","HallA"},new[]{"HallA","KitchenDoor"},new[]{"KitchenDoor","Kitchen"},new[]{"Kitchen","Stove"},new[]{"HallA","HallJoint"},new[]{"Door","HallJoint"},
   new[]{"HallJoint","HallB"},new[]{"HallB","Clock"},new[]{"HallB","LivingDoor"},new[]{"LivingDoor","LivingMid"},new[]{"LivingMid","Aisle"},new[]{"Aisle","Armchair"},new[]{"LivingMid","BedroomDoor"},new[]{"BedroomDoor","Bedroom"}};
  string granNode="Door";
  void GranShow(string node,float yaw){granToken++;granWalking=false;var g=flat.grandmother;g.SetActive(true);granNode=node;g.transform.SetPositionAndRotation(Nodes[node],Quaternion.Euler(0,yaw,0));GranPlay(granIdle);}
  void GranPlay(string clip){if(!granAnim||string.IsNullOrEmpty(clip))return;granAnim.enabled=true;var state=granAnim[clip];if(state==null)return;state.wrapMode=WrapMode.Loop;granAnim.CrossFade(clip,.2f);}
  List<string> Route(string from,string to){var prev=new Dictionary<string,string>{{from,null}};var q=new Queue<string>();q.Enqueue(from);
   while(q.Count>0){var n=q.Dequeue();if(n==to)break;foreach(var e in Edges){string next=e[0]==n?e[1]:e[1]==n?e[0]:null;if(next!=null&&!prev.ContainsKey(next)){prev[next]=n;q.Enqueue(next);}}}
   var path=new List<string>();for(var n=to;n!=null;n=prev.ContainsKey(n)?prev[n]:null)path.Insert(0,n);return path;}
  IEnumerator GranWalk(string to,float speed=.9f){
   var g=flat.grandmother.transform;if(!flat.grandmother.activeSelf)GranShow(granNode,0);
   int my=++granToken; // a newer walk order cancels an older one
   GranPlay(granWalk);granWalking=true;
   foreach(var node in Route(granNode,to)){var target=Nodes[node];while(true){if(my!=granToken)yield break;var d=target-g.position;d.y=0;if(d.magnitude<.05f)break;g.rotation=Quaternion.Slerp(g.rotation,Quaternion.LookRotation(d),1-Mathf.Exp(-8*Time.deltaTime));g.position=Vector3.MoveTowards(g.position,target,speed*Time.deltaTime);yield return null;}granNode=node;}
   GranPlay(granIdle);granWalking=false;}
  IEnumerator GranFace(Vector3 point,float seconds=.6f){var g=flat.grandmother.transform;var d=point-g.position;d.y=0;if(d.sqrMagnitude<.001f)yield break;var from=g.rotation;var to=Quaternion.LookRotation(d);for(float t=0;t<seconds;t+=Time.deltaTime){g.rotation=Quaternion.Slerp(from,to,t/seconds);yield return null;}g.rotation=to;}
  void GranLookAtPlayerWhenNear(){if(!flat.grandmother.activeSelf||granWalking)return;var g=flat.grandmother.transform;var d=player.transform.position-g.position;d.y=0;if(d.magnitude<2.2f&&d.sqrMagnitude>.01f)g.rotation=Quaternion.Slerp(g.rotation,Quaternion.LookRotation(d),1-Mathf.Exp(-3*Time.deltaTime));}
  bool granWalking;int granToken;
  IEnumerator GranGo(string to){yield return GranWalk(to);}

  // ---------- lifecycle ----------
  void Awake(){if(!loop)loop=GetComponent<LoopDirector>();savePath=System.IO.Path.Combine(Application.persistentDataPath,"story-checkpoint.json");}
  void Start(){tv=FindFirstObjectByType<ScenarioTelevision>();var anim=flat.grandmother.GetComponentInChildren<Animation>(true);granAnim=anim;
   if(anim)foreach(AnimationState s in anim){if(s.name.EndsWith("Oidle"))granIdle=s.name;if(s.name.EndsWith("Owalk"))granWalk=s.name;}}
  public bool HasSave=>System.IO.File.Exists(savePath);
  void Checkpoint(string at){if(loop.testing)return;System.IO.File.WriteAllText(savePath,JsonUtility.ToJson(new Save{chapter=at,flashlight=player.hasFlashlight,belt=belt}));}
  public void NewGame(){Begin("Prologue");}
  public void Continue(){var s=HasSave?JsonUtility.FromJson<Save>(System.IO.File.ReadAllText(savePath)):null;Begin(s?.chapter??"Prologue",s);}
  void Begin(string at,Save s=null){
   StopAll();loop.Thought("",0);player.hasFlashlight=s!=null&&s.flashlight;belt=s!=null&&s.belt;
   if(at=="Loop"){chapter=Chapter.Loop;flat.entrance.direction=1;loop.ResumeSavedLap();return;}
   running=StartCoroutine(at switch{"Evening"=>Evening(false),"Night1"=>Night1(false),"Night2"=>Night2(false),"Finale"=>Finale(),_=>Prologue()});
  }
  public void Abandon(){StopAll();chapter=Chapter.Loop;flat.entrance.direction=1;}
  void StopAll(){if(running!=null)StopCoroutine(running);StopAllCoroutines();acts.Clear();card=null;fade=0;objective="";choiceA=choiceB=null;scenes.Cancel();loop.chase.Stop();sound.ResetWorld();if(tv)tv.storyOn=false;if(strap)strap.enabled=false;}
  void Update(){
   if(!OwnsFlat||player.paused)return;
   if(chapter!=Chapter.Ended&&!loop.Busy&&string.IsNullOrEmpty(scenes.PlayingScene))TickActs();else loop.prompt.text="";
   GranLookAtPlayerWhenNear();
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

  // ---------- 3. Prologue (only the door scene exists in the flat) ----------
  IEnumerator Prologue(){
   chapter=Chapter.Evening;loop.Pause(false);fade=1;Homely(true);
   flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(7.15f,0,1.2f),Quaternion.identity);
   flat.entrance.open=false;flat.entrance.locked=true;player.Place(Nodes["Landing"],180);player.control=false;Clock(19,10);flat.clock.stopped=true;if(scenes.landingLight){scenes.landingLight.color=new Color(1,.9f,.74f);scenes.landingLight.intensity=.7f;}
   yield return Card("15 ноября 1996. 19:10",3);
   yield return Card("Подъезд, пятый этаж. Холодно — изо рта идёт пар.\nЗа дверью 59 лает Тузик.",3.5f);
   yield return Fade(0,1.2f);player.control=true;Task("Позвонить в квартиру 60");
   yield return Use(flat.entrance.transform.position+Vector3.up*.3f,"Позвонить");
   sound.Play("knock",flat.entrance.transform.position);yield return new WaitForSeconds(1.2f);sound.Play("latch",flat.entrance.transform.position);yield return new WaitForSeconds(.5f);
   GranShow("Door",0);flat.entrance.locked=false;flat.entrance.open=true;sound.Play("door_open",flat.entrance.transform.position);
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
   Checkpoint("Evening");flat.clock.stopped=true;Clock(19,15);OfferFlashlight();
   // 4.1
   Task("Повесить куртку и разуться");
   yield return Use(new Vector3(8.72f,1.6f,5.1f),"Повесить куртку");sound.Play("carpet_step",player.transform.position);
   yield return Use(new Vector3(8.55f,.2f,5.2f),"Переобуться в тапки",0,2.2f);
   yield return Line("Бабушка","Тапки дедовы надевай, твои малы уже");
   // 4.2
   Clock(19,20);Task("Разобрать сумку на кухне");
   yield return Use(new Vector3(7.58f,.8f,.6f),"Поставить сумку на стол");
   yield return Use(new Vector3(7.4f,.8f,.6f),"Батон — в хлебницу");
   yield return Use(new Vector3(6.85f,1.5f,1.6f),"Шпроты — в шкаф");
   yield return Use(()=>flat.grandmother.transform.position+Vector3.up*1.2f,"Отдать «Корвалол» бабушке",0,2);
   yield return Line("Бабушка","Вот спасибо, а то кончился");
   // 4.3
   Clock(19,30);Task("Поставить чайник");
   yield return Use(new Vector3(8.24f,1.58f,2.43f),"Взять спички с ЗиЛа");
   yield return Use(new Vector3(8.48f,.9f,1.65f),"Открыть газ и зажечь конфорку",.6f);sound.Play("latch",new Vector3(8.48f,.9f,1.65f));
   var flame=FlameLight(new Vector3(8.48f,.95f,1.65f));
   yield return Use(new Vector3(6.9f,1f,1.6f),"Набрать воду в чайник",3);
   yield return Use(new Vector3(8.48f,.95f,1.65f),"Поставить чайник на огонь");
   yield return Line("Бабушка","Пока греется — сходи на балкон, банку вишнёвого принеси. Только дверь там… шпингалет заедает. Дед всё ремнём её подвязывал, пока не помер, а я так и не починила",8);
   Task("Принести с балкона банку вишнёвого");
   yield return Use(flat.balcony.position+Vector3.up*.9f,"Отодвинуть шпингалет (заедает)",2);
   sound.Play("creak",flat.balcony.position);flat.balconyDoor.locked=false;flat.balconyDoor.open=true;sound.Play("wind",flat.balcony.position);
   yield return Use(new Vector3(5.6f,1f,-.85f),"Взять банку варенья",0,2.2f);
   yield return Use(flat.balcony.position+Vector3.up*.9f,"Закрыть балконную дверь");
   flat.balconyDoor.open=false;flat.balconyDoor.closedAngle=1.6f;sound.Play("door_close",flat.balcony.position);
   yield return Line("","Не закрывается толком");
   Task("Заварить чай");
   yield return Use(new Vector3(8.48f,.95f,1.65f),"Выключить газ — чайник свистит");if(flame)Destroy(flame.gameObject);
   yield return Use(new Vector3(7.58f,.8f,.55f),"Заварить и разлить чай",1.2f);
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
   var sconce=flat.look.warm.Length>1?flat.look.warm[1]:null;float sconceWas=sconce?sconce.intensity:0;if(sconce)sconce.intensity=0;sound.Play("latch",ScenarioLayout.Clock+Vector3.up);
   StartCoroutine(GranGo("HallA"));
   yield return Line("Бабушка","Ох ты… Мишаня, табуретку с кухни возьми, лампочки на антресоли");
   Task("Поменять лампочку в бра");
   yield return Use(()=>flat.stool.transform.position+Vector3.up*.4f,"Взять табуретку",0,2);flat.stool.SetActive(false);
   yield return Use(new Vector3(6.6f,1.2f,5.25f),"Поставить табуретку под антресолью",0,2.2f);
   yield return Use(new Vector3(6.6f,2.2f,5.25f),"Достать лампочку с антресоли",0,2.6f);
   yield return Use(ScenarioLayout.Clock+new Vector3(0,.9f,.1f),"Выкрутить старую и вкрутить новую",2,2.6f);
   if(sconce)sconce.intensity=Mathf.Max(sconceWas,1.8f);flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(5.5f,0,5.8f),Quaternion.identity);
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
   yield return Use(flat.wardrobe.position+new Vector3(-.3f,1.5f,0),"Повесить платок на плечики",0,2);
   yield return Line("","Дедов ремень… Тот самый, которым дед балкон подвязывал",4);
   foreach(var d in flat.wardrobeLeaves)d.localRotation=Quaternion.identity;sound.Play("wardrobe",flat.wardrobe.position);
   Clock(20,40);
   // 4.6
   yield return GranGo("Clock");Task("Завести дедовы часы");
   yield return Line("Бабушка","Заведи-ка, у меня пальцы не те уже. Ключ сверху");
   yield return Use(ScenarioLayout.Clock+Vector3.up*.25f,"Завести часы ключом",3,2);
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
   yield return Use(flat.television.position,"Включить «Рубин»",0,2.4f);
   if(tv)tv.storyOn=true;flat.televisionLight.intensity=.12f;
   foreach(var l in new[]{"…Сегодня в милицию снова обратились жители Заречного района.","В квартире по улице Строителей обнаружен накрытый стол и включённый свет. По словам соседей, в квартире никто не проживал.","Однако на фотографиях, изъятых из квартиры, — пустые места рядом с людьми…","Специалисты рекомендуют: не открывайте дверь в позднее время. И — что бы ни случилось — запомните свой дом. Как следует запомните."})
    yield return Line("Диктор",l,5);
   yield return Line("Бабушка","Нечего на ночь глядеть");if(tv)tv.storyOn=false;flat.televisionLight.intensity=0;
   if(flat.look.icon)flat.look.icon.intensity=.14f;Clock(21,38);
   // 4.9
   yield return ValyaAtTheDoor(false);
   yield return Night1(true);
  }
  // "Фонарик в трюмо": the hall cupboard works in every story chapter (LoopDirector handles it only inside the loop).
  void OfferFlashlight(){var c=flat.hallCupboard;if(!c||player.hasFlashlight)return;OfferCupboardOpen();
   Offer(()=>c.interaction.position,"Взять фонарик-«жучок»",0,1.7f,Key.E,()=>!player.hasFlashlight&&c.ReadyToTake,()=>{c.Take(player);Say("","Фонарик. F — включить, держать F — подкачать",4);});}
  void OfferCupboardOpen(){var c=flat.hallCupboard;Offer(()=>c.interaction.position,"Открыть тумбу трюмо",0,1.7f,Key.E,()=>!player.hasFlashlight&&!c.door.open,()=>{sound.Play(c.door.ToggleWithCue(),c.interaction.position);if(!player.hasFlashlight)OfferCupboardOpen();});}
  Light FlameLight(Vector3 at){var g=new GameObject("Gas flame (story)");g.transform.position=at;var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.35f,.55f,1);l.intensity=.6f;l.range=1.2f;l.shadows=LightShadows.None;return l;}

  IEnumerator ValyaAtTheDoor(bool second){
   Task("");
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
   Checkpoint("Night1");Task("Дождаться бабушку");if(!continued)OfferFlashlight();
   yield return NightMinutesUntil22();
   yield return NightEvents(1,false);
  }
  IEnumerator NightEvents(int which,bool from2200Restart){
   // 22:00 announcer
   if(tv)tv.storyOn=true;flat.televisionLight.intensity=.12f;sound.Play("chime",flat.television.position);
   foreach(var l in new[]{"Внимание. Жителям Заречного района.","Атака на запоминание.","Не открывайте дверь. Не отвечайте знакомым голосам. Она боится света.","Запомните всё."})yield return Line("«Рубин», диктор",l,3.2f);
   if(which==2){yield return new WaitForSeconds(1.5f);yield return Line("«Рубин», диктор","Ты же помнишь, Миша?",3.5f);}
   if(tv)tv.storyOn=false;flat.televisionLight.intensity=0;
   // 22:00:30 the house across goes dark, 22:01 blackout
   yield return NightTo(22,0,1);if(flat.look.exterior)flat.look.exterior.Blackout(true);sound.StopCue("fridge");
   yield return NightTo(22,1,12);flat.look.Apply(FlatCondition.Blackout);sound.Play("latch",ScenarioLayout.Spawn+Vector3.up*1.7f);
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
    Say("","…короткие гудки",2);
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
   acts.Clear();Homely(true);flat.entrance.locked=true;flat.clock.stopped=false;if(which==2&&beltTied)TieStrap(true);player.Place(ScenarioLayout.Spawn,180);nightSeconds=22*3600;NightClock();
   yield return NightEvents(which,true);
  }
  IEnumerator RestartNight2At2142(){acts.Clear();Homely(true);belt=false;beltTied=false;TieStrap(false);flat.entrance.locked=true;player.Place(ScenarioLayout.Spawn,180);yield return Night2Body();}

  // ---------- 6.1 Rewind ----------
  IEnumerator Rewind(){
   loop.prompt.text="";acts.Clear();fade=1;yield return new WaitForSeconds(1);
   flat.look.disruption=1;fade=0;Homely(true);flat.balconyDoor.closedAngle=0;
   string[] flashes={"…дверь балкона закрывается сама…","…побелка взлетает обратно на потолок…","…телефон… стук… «Рубин» гаснет внутрь…","…бабушка с вареньем…","…шифоньер. Дедов ремень…","…«тук-тук… тук»…","…лампочка…"};
   foreach(var f in flashes){card=f;sound.Play("clock",ScenarioLayout.Clock);yield return new WaitForSeconds(1.1f);}
   card=null;fade=1;yield return new WaitForSeconds(.15f);fade=0;flat.look.disruption=.4f;yield return new WaitForSeconds(.6f);flat.look.disruption=0;
  }

  // ---------- 8. Second night ----------
  IEnumerator Night2(bool continued){
   chapter=Chapter.Night2;night=2;
   if(!continued){loop.Pause(false);Homely(true);}
   flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(5.5f,0,5.8f),Quaternion.identity);flat.clock.stopped=false;Clock(21,40);
   Checkpoint("Night2");
   yield return ValyaAtTheDoor(true);
   yield return Night2Body();
  }
  IEnumerator Night2Body(){
   chapter=Chapter.Night2;Task("Запомни, как это было");OfferFlashlight();nightSeconds=21*3600+42*60;NightClock();
   // Preparation: belt, torch, (candles optional are not implemented)
   Act takeBelt=null,tie=null;
   takeBelt=Offer(()=>flat.wardrobe.position+new Vector3(-.3f,1.3f,0),"Взять дедов ремень",0,2,Key.E,()=>!belt,()=>{belt=true;foreach(var d in flat.wardrobeLeaves)d.localRotation=Quaternion.Euler(0,95,0);sound.Play("door_open",flat.wardrobe.position);Say("","Ремень. Тот самый",2.5f);});
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
   chapter=Chapter.Finale;loop.Pause(false);Homely(true);flat.clock.stopped=false;Clock(22,0);
   flat.stool.SetActive(true);flat.stool.transform.SetPositionAndRotation(new Vector3(5.5f,0,5.8f),Quaternion.identity);if(strap)strap.enabled=false;flat.entrance.locked=true;
   if(flat.look.exterior)flat.look.exterior.Blackout(false);
   player.Place(ScenarioLayout.Spawn,180);Checkpoint("Finale");Task("Дождаться бабушку");
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
   yield return GranWalk("Stove");flat.entrance.open=false;flat.entrance.locked=true;sound.Play("latch",flat.entrance.transform.position);
   Task("");var flame=FlameLight(new Vector3(8.48f,.95f,1.65f));
   yield return Line("","Она напевает. Газ, вода из крана — точно как ты вечером",4);
   for(float t=0;t<20&&Vector3.Distance(player.transform.position,Nodes["Stove"])>2.4f;t+=Time.deltaTime)yield return null;
   yield return GranFace(Nodes["Stove"]+Vector3.right,0.1f);
   Say("","…чайник начинает свистеть",3);yield return new WaitForSeconds(2.5f);
   yield return Line("Бабушка (не оборачиваясь)","Мишаня… а ты меня помнишь?",4);
   // 11.9 transformation (3.5 s) then 11.10 face tear
   yield return GranFace(player.head.position,1.5f);sound.Play("finger_crack",flat.grandmother.transform.position);
   var g=flat.guest;var at=flat.grandmother.transform.position;flat.grandmother.SetActive(false);g.SetActive(true);g.GetComponent<ProxyPose>().Stand();g.transform.position=at;var face=player.transform.position-at;face.y=0;g.transform.rotation=Quaternion.LookRotation(face);
   var full=g.transform.localScale;sound.Play("shriek",at);
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
   if(System.IO.File.Exists(savePath))System.IO.File.Delete(savePath);
   loop.EndStory();
  }

  // ---------- GUI ----------
  GUIStyle big,small,task;
  void OnGUI(){
   if(!OwnsFlat&&fade<=0&&card==null)return;
   big??=new GUIStyle(GUI.skin.label){fontSize=28,alignment=TextAnchor.MiddleCenter,wordWrap=true};small??=new GUIStyle(GUI.skin.label){fontSize=22,alignment=TextAnchor.MiddleCenter,wordWrap=true};task??=new GUIStyle(GUI.skin.label){fontSize=18};
   if(fade>0||card!=null){GUI.color=new Color(0,0,0,card!=null?Mathf.Max(fade,.92f):fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;}
   if(card!=null)GUI.Label(new Rect(Screen.width*.12f,Screen.height*.2f,Screen.width*.76f,Screen.height*.6f),card,big);
   if(!string.IsNullOrEmpty(objective)&&card==null&&!player.paused)GUI.Label(new Rect(24,20,Screen.width*.5f,30),"Задача: "+objective,task);
   if(choiceA!=null){GUI.Label(new Rect(Screen.width*.2f,Screen.height*.62f,Screen.width*.6f,40),"[1] "+choiceA,small);GUI.Label(new Rect(Screen.width*.2f,Screen.height*.62f+44,Screen.width*.6f,40),"[2] "+choiceB,small);}
  }
 }
}
