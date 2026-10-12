using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Ataka.Scenario {
 // SCENARIO 4.7: the set-top console on the «Рубин» — a short (45 s round) shooter with its own 8-bit sprites drawn
 // here from pixel maps (no foreign brands, no imported art). It runs in the TV studio (layer 31) and is filmed by the
 // feed camera onto the real CRT. Virtual screen 256 × 192 px = 2 × 1.5 studio units (128 px per unit).
 // Keys: ←/→ or A/D move, Space or J fire, Enter start / pause. Esc is the story's (leave the console).
 // Game time is Time.deltaTime, so the game menu's pause (timeScale 0) freezes it too.
 public sealed class ConsoleGame:MonoBehaviour {
  public const float Round=45;const float Ppu=128,HalfW=128,HalfH=96;
  public enum Mode {Title,Play,Paused,Over}
  public Mode State{get;private set;}public int Score{get;private set;}public int Lives{get;private set;}public float Elapsed{get;private set;}
  public int Shots{get;private set;}public int Kills{get;private set;}public int Waves{get;private set;}
  // QA drives the same controls (no keyboard in an automated run)
  [System.NonSerialized] public bool qaDrive;[System.NonSerialized] public float qaMove;[System.NonSerialized] public bool qaFire,qaStart;
  ScenarioSound sound;Vector3 soundAt;Transform root;TextMesh hud,banner;
  Sprite ship,saucerA,saucerB,shot,bomb,boom,star;
  SpriteRenderer player;float px,cooldown,blink,bombClock,flap;
  sealed class Thing {public SpriteRenderer r;public Vector2 p,v;public float t;public int kind;}
  readonly List<Thing> enemies=new List<Thing>(),shots=new List<Thing>(),bombs=new List<Thing>(),stars=new List<Thing>(),booms=new List<Thing>();
  readonly List<Texture2D> textures=new List<Texture2D>();

  static readonly string[] ShipMap={"......#......",".....###.....",".....###.....","..#..###..#..","..#.#####.#..","#############","#####.#.#####","##...###...##"};
  static readonly string[] SaucerAMap={"....######....","..##########..",".##.##.##.##.","##############","..###....###..","...#......#..."};
  static readonly string[] SaucerBMap={"....######....","..##########..",".#.##.##.##.#.","##############","...###..###...","..#........#.."};
  static readonly string[] ShotMap={"#","#","#","#"};
  static readonly string[] BombMap={".#.","###",".#."};
  static readonly string[] BoomMap={"#...#...#","..#.#.#..","...###...","####.####","...###...","..#.#.#..","#...#...#"};
  Sprite Draw(string[] map,Color c){int h=map.Length,w=map[0].Length;var t=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
   for(int y=0;y<h;y++)for(int x=0;x<w;x++)t.SetPixel(x,h-1-y,map[y][x]=='#'?c:Color.clear);t.Apply();textures.Add(t);return UnityEngine.Sprite.Create(t,new Rect(0,0,w,h),new Vector2(.5f,.5f),Ppu);}
  SpriteRenderer Make(Sprite s,string name){var g=new GameObject(name);g.layer=31;g.transform.SetParent(root,false);var r=g.AddComponent<SpriteRenderer>();r.sprite=s;return r;}
  static Vector3 At(Vector2 p,float z=0)=>new Vector3(p.x/Ppu,p.y/Ppu,z);
  TextMesh Text(Vector2 at,TextAnchor anchor){var g=new GameObject("text");g.layer=31;g.transform.SetParent(root,false);g.transform.localPosition=At(at,-.02f);var m=g.AddComponent<TextMesh>();var f=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");m.font=f;g.GetComponent<MeshRenderer>().sharedMaterial=f.material;m.fontSize=48;m.characterSize=.1f;m.anchor=anchor;m.color=Color.white;return m;}

  public static ConsoleGame Begin(ScenarioTelevision tv,ScenarioSound sound,Vector3 soundAt){
   var g=new GameObject("Console game (4.7)").AddComponent<ConsoleGame>();g.sound=sound;g.soundAt=soundAt;g.root=new GameObject("console screen").transform;g.root.gameObject.layer=31;g.root.SetParent(tv.Studio,false);g.root.localPosition=Vector3.zero;g.Build();return g;}
  void Build(){
   ship=Draw(ShipMap,new Color(.35f,.95f,.45f));saucerA=Draw(SaucerAMap,new Color(.95f,.35f,.85f));saucerB=Draw(SaucerBMap,new Color(.95f,.35f,.85f));shot=Draw(ShotMap,new Color(1,1,.6f));bomb=Draw(BombMap,new Color(1,.45f,.2f));boom=Draw(BoomMap,new Color(1,.8f,.3f));
   var st=new Texture2D(1,1,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};st.SetPixel(0,0,Color.white);st.Apply();textures.Add(st);star=UnityEngine.Sprite.Create(st,new Rect(0,0,1,1),new Vector2(.5f,.5f),Ppu);
   var rnd=new System.Random(11);for(int i=0;i<36;i++){var s=Make(star,"star");var th=new Thing{r=s,p=new Vector2((float)rnd.NextDouble()*256-128,(float)rnd.NextDouble()*192-96),v=new Vector2(0,-(10+(float)rnd.NextDouble()*30))};s.color=new Color(1,1,1,.35f+.4f*(float)rnd.NextDouble());stars.Add(th);}
   player=Make(ship,"ship");hud=Text(new Vector2(-122,90),TextAnchor.UpperLeft);banner=Text(new Vector2(0,10),TextAnchor.MiddleCenter);
   State=Mode.Title;Lives=3;px=0;Show();}
  void Wave(){Waves++;for(int row=0;row<2;row++)for(int i=0;i<6;i++){var e=new Thing{r=Make(saucerA,"saucer"),p=new Vector2(-75+i*30,60-row*20),v=new Vector2(28+Waves*6,0),kind=row};enemies.Add(e);}}
  void Clear(List<Thing> list){foreach(var t in list)if(t.r)Destroy(t.r.gameObject);list.Clear();}
  void Restart(){Clear(enemies);Clear(shots);Clear(bombs);Clear(booms);Score=0;Lives=3;Elapsed=0;Waves=0;px=0;Wave();State=Mode.Play;sound.Play("nes_start",soundAt);}
  bool Pressed(Key k)=>Keyboard.current!=null&&Keyboard.current[k].wasPressedThisFrame;bool Held(Key k)=>Keyboard.current!=null&&Keyboard.current[k].isPressed;
  void Update(){
   float dt=Time.deltaTime;if(dt<=0)return; // the game menu is open
   bool start=qaDrive?Take(ref qaStart):Pressed(Key.Enter)||Pressed(Key.NumpadEnter);
   if(State==Mode.Title||State==Mode.Over){if(start)Restart();Animate(dt,false);Show();return;}
   if(start){State=State==Mode.Paused?Mode.Play:Mode.Paused;Show();return;}
   if(State==Mode.Paused){Show();return;}
   Elapsed+=dt;float move=qaDrive?qaMove:(Held(Key.RightArrow)||Held(Key.D)?1:0)-(Held(Key.LeftArrow)||Held(Key.A)?1:0);
   px=Mathf.Clamp(px+move*110*dt,-118,118);cooldown-=dt;blink-=dt;
   bool fire=qaDrive?qaFire:Held(Key.Space)||Held(Key.J);
   if(fire&&cooldown<=0){cooldown=.28f;Shots++;shots.Add(new Thing{r=Make(shot,"shot"),p=new Vector2(px,-70),v=new Vector2(0,230)});sound.Play("nes_shot",soundAt);}
   // the formation sweeps and steps down at the edges; one of them drops a bomb now and then
   bool edge=false;foreach(var e in enemies){e.p+=e.v*dt;if(e.p.x>116||e.p.x<-116)edge=true;}
   if(edge)foreach(var e in enemies){e.v.x=-e.v.x;e.p.y-=8;e.p.x=Mathf.Clamp(e.p.x,-116,116);}
   bombClock-=dt;if(bombClock<=0&&enemies.Count>0){bombClock=Mathf.Max(.5f,1.6f-Waves*.2f);var e=enemies[Random.Range(0,enemies.Count)];bombs.Add(new Thing{r=Make(bomb,"bomb"),p=e.p-new Vector2(0,6),v=new Vector2(0,-70-Waves*8)});}
   for(int i=shots.Count-1;i>=0;i--){var s=shots[i];s.p+=s.v*dt;bool gone=s.p.y>100;
    for(int j=enemies.Count-1;j>=0&&!gone;j--){var e=enemies[j];if(Mathf.Abs(e.p.x-s.p.x)<8&&Mathf.Abs(e.p.y-s.p.y)<6){Explode(e.p);Destroy(e.r.gameObject);enemies.RemoveAt(j);Score+=100;Kills++;gone=true;sound.Play("nes_boom",soundAt);}}
    if(gone){Destroy(s.r.gameObject);shots.RemoveAt(i);}}
   for(int i=bombs.Count-1;i>=0;i--){var b=bombs[i];b.p+=b.v*dt;bool hit=blink<=0&&Mathf.Abs(b.p.x-px)<7&&Mathf.Abs(b.p.y+76)<6;if(hit)Hit();if(hit||b.p.y<-100){Destroy(b.r.gameObject);bombs.RemoveAt(i);}}
   foreach(var e in enemies)if(e.p.y<-66){Hit();Clear(enemies);break;}
   if(enemies.Count==0&&State==Mode.Play)Wave();
   if(Lives<=0||Elapsed>=Round){State=Mode.Over;sound.Play("nes_over",soundAt);}
   Animate(dt,true);Show();
  }
  bool Take(ref bool flag){bool v=flag;flag=false;return v;}
  void Hit(){if(blink>0)return;Lives--;blink=1.5f;Explode(new Vector2(px,-76));sound.Play("nes_hit",soundAt);}
  void Explode(Vector2 at){booms.Add(new Thing{r=Make(boom,"boom"),p=at,t=.35f});}
  void Animate(float dt,bool playing){
   foreach(var s in stars){s.p+=s.v*dt;if(s.p.y<-96)s.p.y+=192;s.r.transform.localPosition=At(s.p,.01f);}
   flap+=dt;foreach(var e in enemies){e.r.sprite=flap%.5f<.25f?saucerA:saucerB;e.r.transform.localPosition=At(e.p);}
   foreach(var s in shots)s.r.transform.localPosition=At(s.p);foreach(var b in bombs)b.r.transform.localPosition=At(b.p);
   for(int i=booms.Count-1;i>=0;i--){var b=booms[i];b.t-=dt;b.r.transform.localPosition=At(b.p,-.01f);if(b.t<=0){Destroy(b.r.gameObject);booms.RemoveAt(i);}}
   player.transform.localPosition=At(new Vector2(px,-76));player.enabled=State!=Mode.Title&&(blink<=0||Mathf.Repeat(blink,.2f)<.1f);}
  void Show(){
   hud.text=State==Mode.Title?"":"ОЧКИ "+Score.ToString("00000")+"   ЖИЗНИ "+Mathf.Max(0,Lives)+"   "+Mathf.CeilToInt(Mathf.Max(0,Round-Elapsed)).ToString("00");
   banner.text=State==Mode.Title?"КОСМОПАТРУЛЬ\n\nENTER — СТАРТ":State==Mode.Paused?"ПАУЗА":State==Mode.Over?"ИГРА ОКОНЧЕНА\nОЧКИ "+Score+"\n\nENTER — ЕЩЁ   ESC — ВЫЙТИ":"";
   Fit(hud,1.9f,.075f);Fit(banner,1.6f,State==Mode.Paused?.14f:.75f);}
  // TextMesh size depends on the font: scale to the measured bounds (re-fitted every frame, bounds lag a frame)
  static void Fit(TextMesh m,float w,float h){if(string.IsNullOrEmpty(m.text))return;var b=m.GetComponent<Renderer>().bounds;float s=Mathf.Max(1e-4f,m.transform.localScale.x);if(b.size.x<1e-4f||b.size.y<1e-4f)return;m.transform.localScale=Vector3.one*s*Mathf.Min(w/b.size.x,h/b.size.y);}
  public void End(){Destroy(gameObject);}
  void OnDestroy(){if(root)Destroy(root.gameObject);foreach(var t in textures)if(t)Destroy(t);}
 }
}
