using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Editor {
 // Ataka.Editor.PrologueInstaller.Install  (batch: -executeMethod Ataka.Editor.PrologueInstaller.Install)
 // SCENARIO 3, the prologue route, into Flat.unity: the north yard (garages, the covered car, sodium lamps, snow), the
 // building's back entrance (iron door on a spring), the ground-floor lobby (mailboxes, notice board) and the stairwell
 // up five floors to flat 60's outer door. The stairwell is the M1 authored staircase (ApartmentStaircase /
 // MilestoneBuilder.Landing: same sizes, green oil paint, concrete steps, metal balusters, wooden handrail, upholstered
 // doors 59/61, attic hatch, bare bulb) shifted 0.9 m north to start behind the existing tambour and repeated per floor
 // (2.8 m). Materials are the existing Art/M1Materials and CC0 assets; nothing new is modelled. All its lights start at
 // intensity 0 (PrologueSet switches them during the prologue only); the accepted flat lights / VHS are checked unchanged.
 public static class PrologueInstaller {
  const string M="Assets/_Project/Art/M1Materials/",Out="Assets/_Project/Art/Prologue/",RootName="Prologue вЂў north yard and stairwell (M1 style)";
  const float H=2.8f,Dz=.9f,Ground=-12.6f;
  // SCENARIO 3.2: В«Р”Рѕ РїРѕРґСЉРµР·РґР° 40 РјВ» вЂ” the start at the garages is 40 m from the entrance door (z 12.65); the garages and
  // the yard reach 7.35 m further north than pc29 (which had 33 m)
  const float Route=40,Far=7.35f,YardLen=46.5f,YardMid=12.75f+YardLen/2;
  static Transform root;static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
  static Material Mat(string n){if(!mats.TryGetValue(n,out var m)){m=AssetDatabase.LoadAssetAtPath<Material>(M+n+".mat");if(!m)throw new System.Exception("Missing material "+n);mats[n]=m;}return m;}
  static GameObject Box(string name,Vector3 p,Vector3 s,string mat,bool collider=true){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=Mat(mat);if(!collider)Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
  static GameObject Span(string name,Vector3 a,Vector3 b,string mat,bool collider=true)=>Box(name,(a+b)/2,new Vector3(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y),Mathf.Abs(b.z-a.z)),mat,collider);
  // pc29b: an invisible walking ramp over a flight. His CharacterController steps 0.10 m and a step rises 0.117 m, so the
  // capsule walks the ramp laid on the step nosings (26В°, under the 45В° slope limit); the steps themselves are visual only.
  static void Ramp(string name,float x,float width,Vector3 low,Vector3 high){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);var a=new Vector3(x,low.y,low.z);var b=new Vector3(x,high.y,high.z);var dir=(b-a).normalized;var up=Vector3.Cross(dir,Vector3.right).normalized;if(up.y<0)up=-up;g.transform.localRotation=Quaternion.LookRotation(dir,up);g.transform.localPosition=(a+b)/2-up*.05f;g.transform.localScale=new Vector3(width,.1f,(b-a).magnitude);Object.DestroyImmediate(g.GetComponent<MeshRenderer>());Object.DestroyImmediate(g.GetComponent<MeshFilter>());}
  static void Rod(string name,Vector3 a,Vector3 b,float r,string mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=(a+b)/2;g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);g.transform.localScale=new Vector3(r*2,(b-a).magnitude/2,r*2);g.GetComponent<Renderer>().sharedMaterial=Mat(mat);Object.DestroyImmediate(g.GetComponent<Collider>());}
  static GameObject Quad(string name,Vector3 p,Quaternion rot,Vector2 size,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localRotation=rot;g.transform.localScale=new Vector3(size.x,size.y,1);g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
  static Light Lamp(string name,Vector3 p,Color c,float range,LightType type=LightType.Point){var g=new GameObject(name);g.transform.SetParent(root,false);g.transform.localPosition=p;var l=g.AddComponent<Light>();l.type=type;l.color=c;l.range=range;l.intensity=0;l.shadows=LightShadows.None;return l;}
  static Transform Mark(string name,Vector3 p,float yaw=0){var t=new GameObject(name).transform;t.SetParent(root,false);t.localPosition=p;t.localRotation=Quaternion.Euler(0,yaw,0);return t;}
  static TextMesh Number(string text,Vector3 p,Quaternion rot){var g=new GameObject("Door number "+text);g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localRotation=rot;var m=g.AddComponent<TextMesh>();var f=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");m.font=f;g.GetComponent<MeshRenderer>().sharedMaterial=f.material;m.text=text;m.fontSize=64;m.characterSize=.012f;m.anchor=TextAnchor.MiddleCenter;m.color=new Color(.73f,.68f,.52f);return m;}
  static Texture2D SaveTexture(string file,Texture2D t){Directory.CreateDirectory(Out);File.WriteAllBytes(Out+file,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(Out+file);return AssetDatabase.LoadAssetAtPath<Texture2D>(Out+file);}
  static Material TextureMat(string name,Texture2D tex){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetTexture("_BaseMap",tex);m.SetFloat("_Smoothness",.12f);string p=Out+name+".mat";AssetDatabase.DeleteAsset(p);AssetDatabase.CreateAsset(m,p);return m;}
  // Procedural paper / tin textures until imagegen pictures are supplied (Resources/Prologue/* replace them at runtime).
  static Texture2D NoticeTexture(){const int w=256,h=340;var t=new Texture2D(w,h);var r=new System.Random(3);for(int y=0;y<h;y++)for(int x=0;x<w;x++){var c=new Color(.86f,.84f,.78f);
    if(y>h-60&&x>20&&x<w-20&&(y/8)%2==0&&r.NextDouble()<.85)c=new Color(.12f,.1f,.1f); // the headline
    else if(y>140&&y<h-80&&x>70&&x<w-70)c=Color.Lerp(new Color(.97f,.97f,.96f),new Color(.6f,.58f,.55f),Mathf.Clamp01(Mathf.Abs(x-128)/70f+Mathf.Abs(y-200)/80f)*.6f); // the over-exposed photo
    else if(y<130&&y>20&&x>24&&x<w-24&&(y/6)%2==0&&r.NextDouble()<.8)c=new Color(.3f,.28f,.27f);t.SetPixel(x,y,c);}t.Apply();return t;}
  static Texture2D MailboxTexture(){const int w=256,h=160;var t=new Texture2D(w,h);for(int y=0;y<h;y++)for(int x=0;x<w;x++){int cx=x%42,cy=y%40;var c=new Color(.32f,.42f,.47f);if(cx<2||cy<2)c=new Color(.12f,.14f,.15f);else if(cy>28&&cy<32&&cx>8&&cx<34)c=new Color(.05f,.05f,.05f);else if(cy>8&&cy<14&&cx>16&&cx<26)c=new Color(.75f,.72f,.6f);t.SetPixel(x,y,c);}t.Apply();return t;}
  static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);
  [MenuItem("Ataka/Prologue/Install yard and stairwell")]
  public static void InstallAndBuild(){Install();ScenarioBuilder.Build();}
  public static void Install(){
   Directory.CreateDirectory(Out);AssetDatabase.Refresh();
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=Object.FindFirstObjectByType<FlatBindings>();
   foreach(var old in scene.GetRootGameObjects().Where(g=>g.name==RootName).ToArray())Object.DestroyImmediate(old);
   var accepted=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.InstanceID);string before=string.Join("\n",accepted.Select(l=>EditorJsonUtility.ToJson(l)));string vhs=EditorJsonUtility.ToJson(flat.look.vhs);
   // Swing into the vestibule, away from a visitor ringing on the landing.
   foreach(var d in Object.FindObjectsByType<ScenarioDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(d.name=="Entry vestibule closed outer door"){d.direction=-1;EditorUtility.SetDirty(d);}
   root=new GameObject(RootName).transform;var set=root.gameObject.AddComponent<PrologueSet>();var stair=new List<Light>();var yard=new List<Light>();
   // ---- the stairwell shell: side walls and the north (rear) wall from the lobby to the roof; door hole at the bottom
   foreach(float x in new[]{6.55f,8.85f})Span("Stairwell side",new Vector3(x-.1f,Ground-.1f,7.2f),new Vector3(x+.1f,2.7f,12.65f),"Plaster");
   Span("Stairwell rear (west of the door)",new Vector3(6.45f,Ground-.1f,12.55f),new Vector3(7.7f,2.7f,12.75f),"Plaster");
   Span("Stairwell rear (east of the door)",new Vector3(8.7f,Ground-.1f,12.55f),new Vector3(8.95f,2.7f,12.75f),"Plaster");
   Span("Stairwell rear (over the door)",new Vector3(7.7f,Ground+2.15f,12.55f),new Vector3(8.7f,2.7f,12.75f),"Plaster");
   Span("Stairwell ceiling",new Vector3(6.45f,2.6f,7.2f),new Vector3(8.95f,2.8f,12.75f),"Plaster");
   for(int k=5;k>=1;k--){float y=-(5-k)*H;
    Box("Floor "+k+" landing",new Vector3(7.7f,y-.1f,8.1f),new Vector3(2.3f,.2f,1.8f),"Concrete");
    foreach(float x in new[]{6.55f,8.85f})Box("Floor "+k+" green paint",new Vector3(x+(x<7?.105f:-.105f),y+.7f,9.9f),new Vector3(.009f,1.4f,5.4f),"StairGreen",false);
    // half landing and the two flights below this floor (the lowest pair ends in the lobby)
    float half=y-1.4f;Box("Floor "+k+" half landing",new Vector3(7.7f,half-.1f,12.0f),new Vector3(2.3f,.2f,.9f),"Concrete");
    for(int i=0;i<12;i++){float z=8.13f+Dz+i*.235f,top=y-(i+1)*1.4f/12;
     Box("Descending concrete step",new Vector3(7.12f,top-.12f,z),new Vector3(.94f,.24f,.238f),"Concrete",false);
     if(k>1)Box("Returning concrete step",new Vector3(8.27f,y-H+(i+1)*1.4f/12-.12f,z),new Vector3(.94f,.24f,.238f),"Concrete",false);
     if(i%2==0){Rod("Stair baluster",new Vector3(7.60f,top,z),new Vector3(7.60f,top+.93f,z),.015f,"Metal");if(k>1)Rod("Stair baluster",new Vector3(7.79f,y-H+(i+1)*1.4f/12,z),new Vector3(7.79f,y-H+.93f+(i+1)*1.4f/12,z),.015f,"Metal");}}
    // the walking surfaces: the descending flight from this landing's edge (z 9.0) to its half landing, the returning one
    // from the lower landing to the same half landing (both through the step nosings)
    Ramp("Floor "+k+" descending flight (walk surface)",7.12f,.94f,new Vector3(0,y-1.4f,11.73f),new Vector3(0,y,9.0f));
    if(k>1)Ramp("Floor "+k+" returning flight (walk surface)",8.27f,.94f,new Vector3(0,y-H,8.676f),new Vector3(0,y-1.4f,11.496f));
    Rod("Wooden handrail",new Vector3(7.60f,y+.82f,8.13f+Dz),new Vector3(7.60f,y-.47f,10.72f+Dz),.029f,"Wood");if(k>1)Rod("Wooden handrail",new Vector3(7.79f,y-H+1.05f,8.13f+Dz),new Vector3(7.79f,y-.47f,10.72f+Dz),.029f,"Wood");
    if(k>1)Quad("Stairwell window (night)",new Vector3(7.7f,half+1.55f,12.54f),Quaternion.identity,new Vector2(1.1f,1.0f),Mat("WindowDark"));
    // the neighbours' upholstered doors on the side walls, and (below the fifth) the third door at the south end
    int left=3*k+44,mid=3*k+45,right=3*k+46;
    foreach(float x in new[]{6.666f,8.734f}){Box("Neighbour upholstered door "+(x<7?left:right),new Vector3(x,y+1,7.22f+Dz),new Vector3(.04f,2,.81f),"TVBrown");
     var num=x<7?left:right;var rot=Quaternion.Euler(0,x<7?-90:90,0);var at=new Vector3(x+(x<7?.026f:-.026f),y+1.65f,7.22f+Dz);
     if(num==59||num==61)Quad("Door plate "+num,at,rot,new Vector2(.07f,.045f),Mat("Number"+num));else Number(num.ToString(),at,rot);} // a plate on the west door faces +X (yaw -90), on the east door -X (+90)
    if(k<5){Box("Floor "+k+" end wall",new Vector3(7.7f,y+1.35f,6.20f+Dz),new Vector3(2.5f,2.7f,.18f),"StairGreen");Box("Floor "+k+" closed door "+mid,new Vector3(8.06f,y+1.0f,6.305f+Dz),new Vector3(.88f,2.0f,.04f),"Wood");Number(mid.ToString(),new Vector3(8.06f,y+1.58f,6.332f+Dz),Quaternion.Euler(0,180,0));}
    // the bare bulb of each landing (the third floor's is the dying one)
    var bulb=GameObject.CreatePrimitive(PrimitiveType.Sphere);bulb.name="Landing bare bulb";bulb.transform.SetParent(root,false);bulb.transform.localPosition=new Vector3(8.25f,y+2.26f,7.45f+Dz);bulb.transform.localScale=new Vector3(.08f,.12f,.08f);bulb.GetComponent<Renderer>().sharedMaterial=Mat("Bulb");Object.DestroyImmediate(bulb.GetComponent<Collider>());
    Rod("Bulb wire",new Vector3(8.25f,y+2.31f,7.45f+Dz),new Vector3(8.25f,y+2.49f,7.45f+Dz),.004f,"Black");
    var l=Lamp("Prologue landing bulb floor "+k,new Vector3(8.25f,y+2.17f,7.45f+Dz),new Color(1,.78f,.52f),4);if(k==3)set.blinkLight=l;else stair.Add(l);
    if(k==4)set.door56=Mark("Door 56 (TV behind it)",new Vector3(6.9f,y,7.22f+Dz));
    if(k==5){set.door59=Mark("Door 59 (Tuzik behind it)",new Vector3(6.9f,y,7.22f+Dz));set.landing5=Mark("Fifth floor landing (in front of 60)",new Vector3(8.05f,y+.02f,7.75f),180);}}
   // fifth floor: closes the gaps round the existing tambour, the attic hatch over the landing, the old graffiti
   Span("Fifth floor landing south (west of the tambour)",new Vector3(6.65f,0,7.08f),new Vector3(7.5f,2.6f,7.18f),"StairGreen");Span("Fifth floor landing south (east of the tambour)",new Vector3(8.6f,0,7.08f),new Vector3(8.75f,2.6f,7.18f),"StairGreen");
   Box("Fifth floor threshold",new Vector3(8.05f,-.09f,7.14f),new Vector3(.9f,.18f,.12f),"Concrete");
   set.hatch=Box("Attic hatch",new Vector3(7.64f,2.485f,7.4f+Dz),new Vector3(.73f,.028f,.88f),"Wood",false).transform;
   Quad("Graffiti",new Vector3(6.659f,1.65f,8.77f+Dz),Quaternion.Euler(0,-90,0),new Vector2(.55f,.13f),Mat("Graffiti"));
   // ---- the lobby (half a floor below the first floor), mailboxes, notice board, the entrance door
   set.lobby=Mark("Lobby",new Vector3(7.7f,Ground+.02f,11.6f));Box("Lobby floor",new Vector3(7.7f,Ground-.1f,10.9f),new Vector3(2.3f,.2f,3.5f),"Concrete");
   Span("Under the first-floor landing",new Vector3(6.65f,Ground,7.2f),new Vector3(8.75f,-11.3f,9.0f),"StairGreen");
   var mail=TextureMat("PrologueMailboxes",SaveTexture("mailboxes_procedural.png",MailboxTexture()));Quad("Mailboxes",new Vector3(6.662f,Ground+1.35f,10.6f),Quaternion.Euler(0,-90,0),new Vector2(1.0f,.62f),mail);
   var noticeMat=TextureMat("PrologueNotice",SaveTexture("missing_person_procedural.png",NoticeTexture()));var board=Quad("Notice board В«РџР РћРџРђР› Р§Р•Р›РћР’Р•РљВ»",new Vector3(8.738f,Ground+1.45f,10.9f),Quaternion.Euler(0,90,0),new Vector2(.42f,.56f),noticeMat);set.notice=board.transform;set.noticeRenderer=board.GetComponent<Renderer>();
   stair.Add(Lamp("Prologue lobby bulb",new Vector3(7.7f,Ground+2.3f,10.9f),new Color(1,.8f,.56f),5));
   var frame=new GameObject("Prologue entrance door").transform;frame.SetParent(root,false);frame.localPosition=new Vector3(8.2f,Ground+1,12.65f);
   var hinge=new GameObject("Hinge").transform;hinge.SetParent(frame,false);hinge.localPosition=new Vector3(-.5f,-1,0);
   var leaf=GameObject.CreatePrimitive(PrimitiveType.Cube);leaf.name="Iron entrance door leaf";leaf.transform.SetParent(hinge,false);leaf.transform.localPosition=new Vector3(.5f,1,0);leaf.transform.localScale=new Vector3(1.0f,2.0f,.05f);leaf.GetComponent<Renderer>().sharedMaterial=Mat("KitchenPaint");
   var door=frame.gameObject.AddComponent<ScenarioDoor>();door.leaf=hinge;door.direction=-1;door.locked=false;door.open=false;set.entranceDoor=door; // opens outwards, onto the yard
   set.entranceInside=Mark("Entrance inside",new Vector3(8.2f,Ground+.02f,12.0f),180);set.entranceOutside=Mark("Entrance outside",new Vector3(8.2f,Ground+.02f,13.6f),180);
   Box("Entrance canopy",new Vector3(8.2f,Ground+2.35f,13.1f),new Vector3(1.7f,.1f,.9f),"Concrete");var glass=Box("Entrance light glass",new Vector3(8.2f,Ground+2.22f,12.9f),new Vector3(.34f,.12f,.12f),"SodiumGlass",false);
   yard.Add(Lamp("Prologue entrance light",new Vector3(8.2f,Ground+2.15f,13.0f),new Color(1,.65f,.35f),6));
   // ---- the north facade, roof parapet, the yard
   Span("North facade west",new Vector3(-1,Ground-.2f,12.75f),new Vector3(6.45f,3.0f,13.05f),"FacadeRender");Span("North facade east",new Vector3(8.95f,Ground-.2f,12.75f),new Vector3(13,3.0f,13.05f),"FacadeRender");
   Span("North facade over the stairwell",new Vector3(6.45f,2.7f,12.75f),new Vector3(8.95f,3.0f,13.05f),"FacadeRender");
   Span("Roof parapet",new Vector3(-1,3.0f,12.7f),new Vector3(13,3.35f,13.1f),"FacadeRender");set.roofFigure=Mark("Roof edge over the entrance",new Vector3(8.2f,3.35f,12.85f),0);
   for(int k=1;k<=5;k++)for(float x=0;x<12.5f;x+=2.2f){if(x>5.9f&&x<9.2f)continue;bool lit=((k*7+(int)x)%3)==0;Quad("North window",new Vector3(x,-(5-k)*H+1.5f,13.06f),Quaternion.Euler(0,180,0),new Vector2(1.2f,1.3f),Mat(lit?"WindowWarm":"WindowDark"));}
   Box("North yard ground",new Vector3(8,Ground-.1f,YardMid),new Vector3(64,.2f,YardLen),"Road");
   var rng=new System.Random(29);for(int i=0;i<10;i++)Box("Dirty snowbank",new Vector3(-12+i*4.4f+(float)rng.NextDouble(),Ground+.02f,16+(float)rng.NextDouble()*2),new Vector3(2.6f,.07f,1.2f),"Snow",false);
   for(int i=0;i<6;i++){float x=-3+i*3.4f;Box("Garage",new Vector3(x,Ground+1.2f,49+Far),new Vector3(3.2f,2.4f,5.6f),"GaragePaint");Box("Garage door",new Vector3(x,Ground+1.0f,46.17f+Far),new Vector3(2.5f,2.0f,.06f),"Metal",false);}
   // the yard has edges: invisible walls so he cannot walk off the ground into the void
   foreach(var (c,sz) in new[]{(new Vector3(-24,Ground+1.5f,YardMid),new Vector3(.2f,3,YardLen)),(new Vector3(40,Ground+1.5f,YardMid),new Vector3(.2f,3,YardLen)),(new Vector3(8,Ground+1.5f,12.75f+YardLen),new Vector3(64,3,.2f))})Box("Yard boundary (invisible)",c,sz,"Concrete").GetComponent<Renderer>().enabled=false;
   set.start=Mark("Prologue start (at the garages)",new Vector3(8.2f,Ground+.02f,12.65f+Route),180);
   foreach(var p in new[]{new Vector3(2,0,24),new Vector3(14,0,34),new Vector3(4,0,46)}){var b=new Vector3(p.x,Ground,p.z);Rod("Yard lamp pole",b,b+Vector3.up*5,.065f,"Metal");Rod("Lamp arm",b+Vector3.up*5,b+new Vector3(.7f,5.1f,0),.035f,"Metal");Box("Sodium lamp",b+new Vector3(.7f,5.05f,0),new Vector3(.45f,.12f,.2f),"SodiumGlass",false);
    var s=Lamp("Prologue sodium",b+new Vector3(.7f,4.8f,0),new Color(1,.52f,.16f),22,LightType.Spot);s.transform.localRotation=Quaternion.Euler(90,0,0);s.spotAngle=125;s.innerSpotAngle=65;yard.Add(s);}
   var car=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/CC0/covered_car/fbx.fbx");if(car){var c=(GameObject)PrefabUtility.InstantiatePrefab(car);c.name="covered_car (Poly Haven CC0)";c.transform.SetParent(root,false);var rs=c.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);c.transform.localScale*=1.45f/b.size.y;b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);c.transform.position+=new Vector3(3.5f,Ground,30)-new Vector3(b.center.x,b.min.y,b.center.z);var cm=AssetDatabase.LoadAssetAtPath<Material>(M+"covered_car.mat");if(cm)foreach(var r in rs)r.sharedMaterial=cm;}
   set.yardSnow=Mark("Yard snow emitter",new Vector3(8,Ground+8,32));set.swing=Mark("Swing (heard, no model)",new Vector3(-4,Ground+.5f,27));
   // the stairwell's stone echo for every sound heard inside it
   var zone=new GameObject("Stairwell echo").AddComponent<AudioReverbZone>();zone.transform.SetParent(root,false);zone.transform.localPosition=new Vector3(7.7f,-5,10);zone.minDistance=4;zone.maxDistance=8;zone.reverbPreset=AudioReverbPreset.StoneCorridor;
   set.stairLights=stair.ToArray();set.stairIntensity=set.stairLights.Select(x=>x.name.Contains("lobby")?.55f:.43f).ToArray();set.yardLights=yard.ToArray();set.yardIntensity=set.yardLights.Select(x=>x.type==LightType.Spot?30f:3.5f).ToArray();
   foreach(var r in root.GetComponentsInChildren<Renderer>())if(!r.transform.IsChildOf(frame))GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic); // the entrance door leaf moves
   string after=string.Join("\n",accepted.Select(l=>EditorJsonUtility.ToJson(l)));if(before!=after||vhs!=EditorJsonUtility.ToJson(flat.look.vhs))throw new System.Exception("Accepted lighting/VHS changed");
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Directory.CreateDirectory("QA/Prologue");File.WriteAllText("QA/Prologue/install.txt","Prologue set: "+root.GetComponentsInChildren<Renderer>().Length+" renderers, "+(set.stairLights.Length+1)+" stair lights, "+set.yardLights.Length+" yard lights (all 0 until the prologue), ground "+Ground+", entrance door "+frame.position+", start "+set.start.position+", startв†’door "+Vector3.Distance(Flat(set.start.position),Flat(frame.position)).ToString("F1")+" m\n");
   Debug.Log("PROLOGUE_INSTALLED "+File.ReadAllText("QA/Prologue/install.txt"));
  }
 }
}
