using UnityEngine;
namespace Ataka.Scenario {
 // Characters on layer 31 exist only in the television's render, never behind the real player.
 public sealed class ScenarioTelevision:MonoBehaviour {
  public LoopDirector loop;public Camera feed;public Transform misha,visitor;public Renderer screen;public Material screenMaterial;
  RenderTexture texture;Material material;bool watched,turned;int lap;
  public bool storyOn;public Color storyColor=new Color(.35f,.45f,.75f); // story broadcasts (4.8 news, 6 announcer): lit screen, no live feed
  // What the story broadcast shows ON the screen (pc26): the test card, the "Новости Заречья" titles and the blue
  // announcer screen with his words as on-screen text. A small studio on layer 31 (TV-only, far below the flat) is
  // filmed by the existing feed camera into the same render texture. There is NO announcer figure (no model): his
  // look into the camera is not shown. None = the previous plain lit colour (also what the 6.1 rewind replays).
  public enum Show {None,TestCard,News,Announcer,Game} // Game (pc27): the set-top console, its sprites live under Studio
  [System.NonSerialized] public bool instant; // the 6.1 rewind replays the picture as recorded: no tube warm-up
  [System.NonSerialized] public Show show;[System.NonSerialized] public string caption="";
  public RenderTexture QaFeed=>texture;public bool QaFeedOn=>feed&&feed.enabled;public int QaFeedMask=>feed?feed.cullingMask:0;
  public float Brightness{get;private set;}Show shown;float since;
  Transform studio;SpriteRenderer card;TextMesh title,words;Texture2D testCard;Sprite cardSprite;
  int maskBefore;CameraClearFlags clearBefore;Color backgroundBefore;bool orthoBefore;float sizeBefore;
  void Start(){texture=new RenderTexture(320,240,16){name="Rubin live feed",filterMode=FilterMode.Bilinear};feed.targetTexture=texture;material=new Material(screenMaterial);screen.sharedMaterial=material;material.SetTexture("_BaseMap",texture);feed.cullingMask=(1<<0)|(1<<31);feed.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
   maskBefore=feed.cullingMask;clearBefore=feed.clearFlags;backgroundBefore=feed.backgroundColor;orthoBefore=feed.orthographic;sizeBefore=feed.orthographicSize;}
  // Procedural test card (USSR style): grey field, white grid, a big circle with colour bars, grey steps and gratings.
  static Texture2D TestCard(){
   const int w=256,h=192;var t=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,name="Rubin test card"};var px=new Color[w*h];
   Color[] bars={Color.white,new Color(1,1,0),new Color(0,1,1),Color.green,new Color(1,0,1),Color.red,Color.blue,Color.black};
   for(int y=0;y<h;y++)for(int x=0;x<w;x++){
    Color c=new Color(.42f,.42f,.42f);if(x%16==0||y%16==0)c=new Color(.9f,.9f,.9f);
    float dx=x-w/2f,dy=y-h/2f,r=Mathf.Sqrt(dx*dx+dy*dy);
    if(r<84){float u=(dx+84)/168f,v=(dy+84)/168f;
     if(v>.66f)c=bars[Mathf.Clamp((int)(u*8),0,7)];
     else if(v>.42f){float step=Mathf.Floor(u*6)/5f;c=new Color(step,step,step);}
     else if(v>.3f)c=Mathf.Sin(u*Mathf.PI*2*(4+u*24))>0?Color.white:Color.black;
     else c=((x/8+y/8)%2==0)?new Color(.15f,.15f,.15f):new Color(.85f,.85f,.85f);
     if(Mathf.Abs(dx)<1||Mathf.Abs(dy)<1)c=Color.white;}
    if(r>=84&&r<86.5f)c=Color.white;
    px[y*w+x]=c;}
   t.SetPixels(px);t.Apply();return t;}
  TextMesh Text(string name,Vector3 at,Color color){var go=new GameObject(name);go.layer=31;go.transform.SetParent(studio,false);go.transform.localPosition=at;var m=go.AddComponent<TextMesh>();var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");m.font=font;go.GetComponent<MeshRenderer>().sharedMaterial=font.material;m.fontSize=64;m.characterSize=.1f;m.anchor=TextAnchor.MiddleCenter;m.alignment=TextAlignment.Center;m.color=color;return m;}
  public Transform Studio{get{BuildStudio();return studio;}}
  void BuildStudio(){
   if(studio)return;studio=new GameObject("Rubin broadcast studio (TV layer)").transform;studio.position=new Vector3(0,-200,0);studio.gameObject.layer=31;
   testCard=TestCard();cardSprite=Sprite.Create(testCard,new Rect(0,0,testCard.width,testCard.height),new Vector2(.5f,.5f),128);
   var cg=new GameObject("test card");cg.layer=31;cg.transform.SetParent(studio,false);card=cg.AddComponent<SpriteRenderer>();card.sprite=cardSprite; // 2 x 1.5 units = the whole 4:3 frame
   title=Text("title",new Vector3(0,.5f,-.01f),Color.white);words=Text("words",new Vector3(0,-.15f,-.01f),Color.white);}
  // Fit a text mesh into a box (the mesh size of a TextMesh depends on the font; measure instead of guessing).
  // Re-fitted every frame: a TextMesh's bounds are only valid once its mesh has been rebuilt.
  static void Fit(TextMesh m,string text,float width,float height){if(m.text!=text)m.text=text;var b=m.GetComponent<Renderer>().bounds;float s=Mathf.Max(1e-4f,m.transform.localScale.x);if(b.size.x<1e-4f||b.size.y<1e-4f)return;m.transform.localScale=Vector3.one*s*Mathf.Min(width/b.size.x,height/b.size.y);}
  static string Wrap(string text,int columns){var words=text.Split(' ');var sb=new System.Text.StringBuilder();int line=0;foreach(var w in words){if(line>0&&line+1+w.Length>columns){sb.Append('\n');line=0;}else if(line>0){sb.Append(' ');line++;}sb.Append(w);line+=w.Length;}return sb.ToString();}
  void Broadcast(){
   BuildStudio();if(show!=shown){if(shown==Show.None)since=Time.time;shown=show;}
   // the picture comes up as the tube warms (4.8: "картинка проявляется"), and only flickers on a channel change
   Brightness=(instant?1:Mathf.Clamp01((Time.time-since)/1.2f))*(.94f+.06f*Mathf.PerlinNoise(Time.time*7,.3f));
   feed.enabled=true;feed.cullingMask=1<<31;feed.clearFlags=CameraClearFlags.SolidColor;feed.orthographic=true;feed.orthographicSize=.75f;
   feed.transform.SetPositionAndRotation(studio.position+Vector3.back*2,Quaternion.identity);
   card.enabled=show==Show.TestCard;
   feed.backgroundColor=show==Show.Announcer?new Color(.08f,.16f,.62f):show==Show.News?new Color(.04f,.06f,.18f):Color.black;
   string head=show==Show.TestCard?"ЗАРЕЧЬЕ":show==Show.News?"НОВОСТИ ЗАРЕЧЬЯ":""; // Game: no titles, the game draws its own
   Fit(title,head,show==Show.TestCard?.7f:1.7f,show==Show.TestCard?.14f:.24f);title.transform.localPosition=show==Show.TestCard?new Vector3(0,.58f,-.01f):new Vector3(0,.5f,-.01f);
   string body=show==Show.TestCard||show==Show.Game?"":Wrap(caption??"",show==Show.Announcer?22:30);
   Fit(words,body,1.8f,show==Show.Announcer?.95f:.8f);
   words.color=show==Show.News?new Color(.92f,.92f,.85f):Color.white;
   material.SetColor("_BaseColor",Color.white*Brightness);
  }
  void EndBroadcast(){if(shown==Show.None)return;shown=Show.None;feed.cullingMask=maskBefore;feed.clearFlags=clearBefore;feed.backgroundColor=backgroundBefore;feed.orthographic=orthoBefore;feed.orthographicSize=sizeBefore;feed.enabled=false;}
  void LateUpdate(){
   if(storyOn&&show!=Show.None){misha.gameObject.SetActive(false);visitor.gameObject.SetActive(false);Broadcast();return;}
   EndBroadcast();
   if(storyOn||(loop.story&&loop.story.OwnsFlat)){feed.enabled=false;misha.gameObject.SetActive(false);visitor.gameObject.SetActive(false);material.SetColor("_BaseColor",storyOn?storyColor:Color.black);return;}
   int n=loop.Progress.Lap;if(n!=lap){lap=n;watched=turned=false;}
   bool on=n==4&&!loop.Progress.EventComplete&&!loop.chase.Active;feed.enabled=on;misha.gameObject.SetActive(on);visitor.gameObject.SetActive(on&&!turned);material.SetColor("_BaseColor",on?new Color(.65f,.72f,.8f):Color.black);if(!on)return;
   var player=loop.flat.player;var direction=loop.flat.television.position-player.transform.position;direction.y=0;direction.Normalize();
   misha.position=player.transform.position;misha.rotation=Quaternion.LookRotation(direction);
   visitor.position=player.transform.position-direction;visitor.rotation=Quaternion.LookRotation(direction);
   feed.transform.position=player.transform.position-direction*2.5f+Vector3.up*1.55f;feed.transform.LookAt(player.transform.position+Vector3.up*1.3f+direction*.7f);
   float dot=Vector3.Dot(player.head.forward,(loop.flat.television.position-player.head.position).normalized);
   if(dot>.85f)watched=true;else if(watched&&dot<0)turned=true;
   if(turned)misha.rotation=Quaternion.LookRotation(-direction);
  }
  void OnDestroy(){if(texture){texture.Release();Destroy(texture);}if(material)Destroy(material);if(testCard)Destroy(testCard);if(cardSprite)Destroy(cardSprite);if(studio)Destroy(studio.gameObject);}
 }
}
