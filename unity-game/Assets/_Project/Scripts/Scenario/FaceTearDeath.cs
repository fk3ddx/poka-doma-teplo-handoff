using UnityEngine;
namespace Ataka.Scenario {
 // SCENARIO 7.4: the face is torn off by her clawed hands, always in first person.
 // Full version (first death) 7 s, short version (every later death) 3.5 s.
 // The hands are a ready authored model (installer assigns clawSource); this component only stages them
 // in camera space and draws the screen-side wounds (punctures, blood, lost left eye, red, black).
 public sealed class FaceTearDeath:MonoBehaviour {
  public GameObject clawSource;                       // ready claw-hand model, right hand
  public Vector3 clawEuler;                          // extra rotation after the automatic orientation (0 = as designed); without clawFingers it is the whole orientation
  public float clawLength=.58f;                      // forearm end to claw tip, metres (GuestInstaller sets it)
  // Set by GuestInstaller from the mesh itself (model-root space): direction to the claws and the palm side.
  public Vector3 clawFingers,clawPalm;public float clawRoll=90;
  public float fillIntensity=.6f,fillRange=1.6f;public Color fillColor=new Color(.75f,.8f,.92f); // dim cold light on the hands while they are in
  public Color skinTint=new Color(.55f,.52f,.40f);   // grey-yellow wet skin for the temporary fallback only
  public bool UsingFallback{get;private set;}
  Transform left,right,head;bool full;float t;Light fill;
  Texture2D dot,drip,streak,shade,black;
  struct Key {public float t;public Vector3 wrist,fingers,palm;public Key(float t,Vector3 w,Vector3 f,Vector3 p){this.t=t;wrist=w;fingers=f;palm=p;}}
  // Head-space keys (x right, y up, z forward) for the authored forearm+hand cut from the Guest (GuestClaw):
  // "wrist" = the forearm end (model origin), "fingers" = forearm→claws direction, "palm" = palm normal.
  // Composition designed against a Blender emulation of this exact maths (docs/…/18): her head/chest 0.62 m ahead,
  // both clawed hands framing the face at 0.55–1.3, the right hand at the hairline 1.6–2.0, dragging down by 2.6.
  // story18: every forearm origin is outside the frustum (arms enter from the frame edges, no visible cut end); clawLength .58.
  static readonly Key[] RightFull={
   new Key(0f,new Vector3(1f,.02f,.65f),new Vector3(-.8f,.1f,-.2f),new Vector3(-.6f,0f,-.8f)),
   new Key(0.3f,new Vector3(1f,.02f,.65f),new Vector3(-.8f,.1f,-.2f),new Vector3(-.6f,0f,-.8f)),
   new Key(0.55f,new Vector3(.75f,.02f,.6f),new Vector3(-.8f,.1f,-.2f),new Vector3(-.6f,0f,-.8f)),
   new Key(1.3f,new Vector3(.72f,.03f,.58f),new Vector3(-.8f,.1f,-.25f),new Vector3(-.6f,0f,-.8f)),
   new Key(1.6f,new Vector3(.12f,.62f,.6f),new Vector3(-.1f,-.85f,-.45f),new Vector3(0f,-.3f,-1f)),
   new Key(2f,new Vector3(.12f,.62f,.58f),new Vector3(-.1f,-.85f,-.45f),new Vector3(0f,-.3f,-1f)),
   new Key(2.6f,new Vector3(.1f,.6f,.7f),new Vector3(-.05f,-.95f,-.3f),new Vector3(0f,-.2f,-1f)),
   new Key(3f,new Vector3(.55f,-.6f,.55f),new Vector3(-.3f,.6f,-.5f),new Vector3(-1f,0f,0f)),
   new Key(3.4f,new Vector3(.1f,-.7f,.55f),new Vector3(-.05f,.95f,-.1f),new Vector3(0f,0f,-1f)),
   new Key(4.2f,new Vector3(.1f,-.7f,.55f),new Vector3(-.05f,.95f,-.1f),new Vector3(0f,0f,-1f)),
   new Key(4.7f,new Vector3(.6f,-.9f,.6f),new Vector3(0f,.9f,.2f),new Vector3(-1f,0f,0f)),
   new Key(9f,new Vector3(.6f,-.9f,.6f),new Vector3(0f,.9f,.2f),new Vector3(-1f,0f,0f))};
  static readonly Key[] LeftFull={
   new Key(0f,new Vector3(-1f,.02f,.65f),new Vector3(.8f,.1f,-.2f),new Vector3(.6f,0f,-.8f)),
   new Key(0.3f,new Vector3(-1f,.02f,.65f),new Vector3(.8f,.1f,-.2f),new Vector3(.6f,0f,-.8f)),
   new Key(0.55f,new Vector3(-.75f,.02f,.6f),new Vector3(.8f,.1f,-.2f),new Vector3(.6f,0f,-.8f)),
   new Key(2.6f,new Vector3(-.72f,.03f,.58f),new Vector3(.8f,.1f,-.25f),new Vector3(.6f,0f,-.8f)),
   new Key(3f,new Vector3(-.7f,-.7f,.45f),new Vector3(.4f,.6f,-.4f),new Vector3(1f,0f,0f)),
   new Key(9f,new Vector3(-.9f,-.9f,.4f),new Vector3(.4f,.6f,-.4f),new Vector3(1f,0f,0f))};
  // Short version keeps the same gestures on the compressed 3.5 s clock of 7.4.
  static float FullTime(float t,bool full)=>full?t:t<.3f?t:t<1f?Mathf.Lerp(.3f,1.6f,(t-.3f)/.7f):t<1.5f?Mathf.Lerp(1.6f,2.0f,(t-1f)/.5f):Mathf.Lerp(2.0f,3.0f,(t-1.5f)/1f);
  // 11.10 finale: the short 7.4 (lunge, hands, nails, tear) and then the full version's 3.2 beat — her hand rises with
  // Misha's face and pulls it over hers — on 3.5 s: 0–1.5 short, 1.5–2.0 = full 2.0–2.6, 2.0–2.9 = full 3.0–4.2, black.
  // There is NO mesh of Misha's face: in that beat the claw hand rises into her face empty (see docs 25-finale).
  public bool Finale{get;private set;}
  // pc28: the stand-in for Misha's face in the finale's 3.2 beat (FinaleFacesInstaller; adult Quaternius face, labelled)
  public Mesh standInFace;public Material standInFaceMaterial;Transform faceProp;public Transform QaFaceProp=>faceProp;
  // Finale 3.2 beat (f 3.0–4.2): the right claw hand rises from below the frame to her face and covers it (story25-a:
  // the full-version keys kept it below the frame — the beat showed no hand at all).
  static readonly Key[] RightFinale={
   // story25-f: tips ended 0.5 m above her head; story26-key-b: then the palm sat INSIDE her head (keys are in the
   // camera's space, pitched 22° up: her head bone ≈ (0, .05, .69), the face surface ≈ z .59). Now the hand is in
   // front of the face: palm ≈ (.03, .05, .44), back of the hand to the camera, claw tips on her brow at z ≈ .52.
   new Key(3f,new Vector3(.3f,-.45f,.3f),new Vector3(-.2f,.95f,.25f),new Vector3(0f,-.1f,1f)),
   new Key(3.5f,new Vector3(.12f,-.35f,.32f),new Vector3(-.08f,.95f,.28f),new Vector3(0f,-.2f,1f)),
   new Key(4.2f,new Vector3(.03f,-.26f,.34f),new Vector3(-.03f,.95f,.3f),new Vector3(0f,-.3f,.95f)),
   new Key(9f,new Vector3(.03f,-.26f,.34f),new Vector3(-.03f,.95f,.3f),new Vector3(0f,-.3f,.95f))};
  static float FinaleTime(float t)=>t<1.5f?FullTime(t,false):t<2f?Mathf.Lerp(2f,2.6f,(t-1.5f)/.5f):t<2.9f?Mathf.Lerp(3f,4.2f,(t-2f)/.9f):4.2f+(t-2.9f);
  float F(float t)=>Finale?FinaleTime(t):FullTime(t,full);
  public float Duration(bool fullDeath)=>fullDeath?7:3.5f;
  void Awake(){
   dot=Blob(32,false);drip=Blob(32,true);streak=Gradient(8,64,true);shade=Gradient(64,8,false);black=Solid(Color.black);
  }
  static Texture2D Blob(int n,bool tail){
   // Disc (puncture) or drip: round head at the bottom of the texture, thinning tail above it.
   int height=tail?n*4:n;var tex=new Texture2D(n,height,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
   for(int y=0;y<height;y++)for(int x=0;x<n;x++){float dx=(x+.5f)/n*2-1,r;
    if(y<n){float dy=(y+.5f)/n*2-1;r=Mathf.Sqrt(dx*dx+dy*dy);}else{float width=Mathf.Max(.18f,1-.75f*(y-n)/(float)(height-n));r=Mathf.Abs(dx)/width;}
    tex.SetPixel(x,y,new Color(.16f,.01f,.01f,Mathf.Clamp01((1-r)*3)*.95f));}
   tex.Apply();return tex;}
  static Texture2D Gradient(int w,int h,bool vertical){var tex=new Texture2D(w,h,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
   // vertical: blood streak, opaque at the top; horizontal: darkness, opaque at the left edge.
   for(int y=0;y<h;y++)for(int x=0;x<w;x++){float a=vertical?(y+.5f)/h:1-(x+.5f)/w;tex.SetPixel(x,y,vertical?new Color(.18f,.01f,.01f,Mathf.Clamp01(a*1.3f)*.9f):new Color(0,0,0,a));}
   tex.Apply();return tex;}
  static Texture2D Solid(Color c){var tex=new Texture2D(1,1,TextureFormat.RGBA32,false);tex.SetPixel(0,0,c);tex.Apply();return tex;}

  public void Begin(Transform cameraHead,bool fullDeath,GameObject fallback,bool finale=false){
   head=cameraHead;full=fullDeath&&!finale;Finale=finale;t=0;End();
   var source=clawSource;UsingFallback=!source;if(!source)source=fallback;
   if(!source){Debug.LogError("FaceTearDeath: no claw model and no fallback");return;}
   right=Make(source,false);left=Make(source,true);
   if(Finale&&standInFace&&standInFaceMaterial){var fg=new GameObject("Misha face (STAND-IN: adult Quaternius face, not 12-year-old)");fg.transform.SetParent(head,false);fg.AddComponent<MeshFilter>().sharedMesh=standInFace;var fr=fg.AddComponent<MeshRenderer>();fr.sharedMaterial=standInFaceMaterial;fr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;fg.SetActive(false);faceProp=fg.transform;}
   var lg=new GameObject("Death fill light").transform;lg.SetParent(head,false);lg.localPosition=new Vector3(0,.3f,.05f);fill=lg.gameObject.AddComponent<Light>();
   fill.type=LightType.Point;fill.color=fillColor;fill.intensity=0;fill.range=fillRange;fill.shadows=LightShadows.None;
   if(UsingFallback)Debug.LogWarning("FaceTearDeath: ready claw model not installed — temporary proxy hand (see README)");
  }
  // Finale 11.6б: a loose copy of the READY claw hand, normalised like the death hands (+Z fingers, -Y palm), inactive.
  public Transform MakeLooseClaw(float length){if(!clawSource)return null;var w=Make(clawSource,false,null,length);w.name="Claw (finale door gap)";return w;}
  Transform Make(GameObject source,bool mirror,Transform parent=null,float length=-1){
   if(length<=0)length=clawLength;var wrapper=new GameObject(mirror?"Claw L (death)":"Claw R (death)").transform;wrapper.SetParent(parent?parent:head,false);
   var model=Instantiate(source,wrapper,false);model.name="model";
   var proxy=model.GetComponent<CloseupHand>();Vector3 proxyFingers=Vector3.zero;
   if(proxy&&proxy.palm&&proxy.forearm)proxyFingers=model.transform.InverseTransformDirection(proxy.palm.position-proxy.forearm.position);
   foreach(var b in model.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;model.SetActive(true);
   foreach(var c in model.GetComponentsInChildren<Collider>(true))Destroy(c);
   foreach(var a in model.GetComponentsInChildren<Animation>(true))a.enabled=false;foreach(var a in model.GetComponentsInChildren<Animator>(true))a.enabled=false;
   model.transform.localPosition=Vector3.zero;model.transform.localRotation=proxyFingers.sqrMagnitude>1e-6f?Quaternion.FromToRotation(proxyFingers.normalized,Vector3.forward):clawFingers.sqrMagnitude>1e-6f&&clawPalm.sqrMagnitude>1e-6f?Quaternion.Euler(clawEuler)*Quaternion.AngleAxis(clawRoll,Vector3.forward)*Quaternion.Inverse(Quaternion.LookRotation(clawFingers,-clawPalm)):Quaternion.Euler(clawEuler);model.transform.localScale=Vector3.one;
   // Normalise: longest extent = clawLength, wrist at the origin, nails toward +Z.
   var rs=model.GetComponentsInChildren<Renderer>(true);if(rs.Length>0){
    var b=Local(wrapper,rs);float longest=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));if(longest>1e-4f)model.transform.localScale=Vector3.one*(length/longest);
    b=Local(wrapper,rs);model.transform.localPosition-=new Vector3(b.center.x,b.center.y,b.min.z);
    foreach(var r in rs){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;if(r is SkinnedMeshRenderer s)s.updateWhenOffscreen=true;
     if(UsingFallback){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",skinTint);block.SetFloat("_Smoothness",.55f);r.SetPropertyBlock(block);}}
   }
   if(mirror)wrapper.localScale=new Vector3(-1,1,1);
   wrapper.gameObject.SetActive(false);return wrapper;
  }
  static Bounds Local(Transform space,Renderer[] rs){bool first=true;var b=new Bounds();
   foreach(var r in rs){var w=r.bounds;for(int i=0;i<8;i++){var c=w.center+Vector3.Scale(w.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var p=space.InverseTransformPoint(c);if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}
   return b;}

  static void Pose(Transform hand,Key[] keys,float t,bool mirrored){
   int i=0;while(i<keys.Length-2&&t>keys[i+1].t)i++;var a=keys[i];var b=keys[i+1];float u=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-a.t)/Mathf.Max(.0001f,b.t-a.t)));
   var wrist=Vector3.Lerp(a.wrist,b.wrist,u);var fingers=Vector3.Slerp(a.fingers.normalized,b.fingers.normalized,u);var palm=Vector3.Slerp(a.palm.normalized,b.palm.normalized,u);
   hand.localPosition=wrist;
   // Wrapper +Z = fingers, wrapper -Y = palm normal. A mirrored wrapper flips X, so its basis is built the same way.
   var up=-Vector3.ProjectOnPlane(palm,fingers);if(up.sqrMagnitude<1e-4f)up=Vector3.up;hand.localRotation=Quaternion.LookRotation(fingers,up);
  }

  // Returns the camera pitch offset (degrees, negative = look up) and the shake for this moment.
  public void Frame(float time,out float pitchUp,out Vector2 shake){
   t=time;float f=F(t);pitchUp=0;shake=Vector2.zero;if(!head)return;
   bool handsIn=f>=.3f&&f<(full?5.2f:Finale?4.3f:3.0f);
   if(right){right.gameObject.SetActive(handsIn);if(handsIn)Pose(right,Finale&&f>=3f?RightFinale:RightFull,f,false);}
   if(left){left.gameObject.SetActive(handsIn&&f<3.0f);if(left.gameObject.activeSelf)Pose(left,LeftFull,f,true);}
   // 11.10 / 7.4 at 3.2: her hand comes in holding Misha's face — slack skin with brows and nose, eye holes — and pulls
   // it over her own. Until 3.6 it hangs from her claws (face to the camera, sagging); by 4.2 it is on her face.
   if(faceProp){bool on=Finale&&right&&f>=3f&&f<4.3f;faceProp.gameObject.SetActive(on);
    if(on){var palm=right.localPosition+right.localRotation*(Vector3.forward*clawLength*.62f)+new Vector3(0,.02f,-.05f);var onHer=new Vector3(0,.07f,.56f);float u=Mathf.SmoothStep(0,1,Mathf.Clamp01((f-3.6f)/.6f));
     faceProp.localPosition=Vector3.Lerp(palm,onHer,u);faceProp.localRotation=Quaternion.Slerp(Quaternion.Euler(28,180,8),Quaternion.Euler(4,180,0),u); // face towards the camera; hanging, then stretched on
     faceProp.localScale=new Vector3(1,Mathf.Lerp(1.18f,1.04f,u),1);}}
   if(fill)fill.intensity=handsIn?fillIntensity*Mathf.Clamp01((f-.25f)/.2f):0;
   float grip=f>=.3f&&f<2.6f?1:0;shake=new Vector2(Mathf.Sin(t*55)*1.5f,Mathf.Cos(t*41))*(1+grip*1.5f);
   if(full&&f>=.8f&&f<4.5f)pitchUp=-22*Mathf.SmoothStep(0,1,Mathf.Clamp01((f-.8f)/.3f));
   if(Finale&&f>=3f)pitchUp=-22*Mathf.SmoothStep(0,1,Mathf.Clamp01((f-3f)/.3f)); // looking up at her as she pulls the face on
  }
  public float FlapTime(float time)=>F(time)-.5f;   // DeathVeil runs 1.5→2.25 on its own clock
  public float Red(float time){float f=F(time);return f<2?0:.5f*Mathf.Clamp01((f-2)*2);} // a red tint (alpha ≤ .43), not an opaque card: the hands stay visible
  public float Black(float time)=>full?Mathf.Clamp01((time-5.6f)/1.2f):Finale?Mathf.Clamp01((time-2.9f)/.1f):time>=2.5f?1:0;
  public float Disruption(float time)=>full?Mathf.Clamp01((time-5f)/1.5f):0;
  public void End(){if(faceProp)Destroy(faceProp.gameObject);faceProp=null;if(right)Destroy(right.gameObject);if(left)Destroy(left.gameObject);if(fill)Destroy(fill.gameObject);right=left=null;fill=null;t=-1;}

  void OnGUI(){
   if(t<0||!head)return;float f=F(t);float w=Screen.width,h=Screen.height;
   if(f>=1.5f){ // four punctures under the nails at the hairline, then blood runs down
    float grow=Mathf.Clamp01((f-1.5f)/.25f);for(int i=0;i<4;i++){float x=w*(.38f+i*.075f+Mathf.Sin(i*7.1f)*.01f),s=h*.03f*grow;
     GUI.DrawTexture(new Rect(x-s/2,h*.02f,s,s),dot);
     float run=Mathf.Clamp01((f-1.65f-i*.07f)/(full?3.5f:1.6f));if(run>0)GUI.DrawTexture(new Rect(x-s*.3f,h*.02f+s*.4f,s*.6f,h*(.08f+.55f*run)*(.6f+.4f*Mathf.Sin(i*3.3f+1)*.5f+.2f)),drip);}
   }
   if(f>=2.0f){ // the left eye is gone: left half falls dark
    float a=Mathf.Clamp01((f-2.0f)/.35f);GUI.color=new Color(1,1,1,a);GUI.DrawTexture(new Rect(0,0,w*.62f,h),shade,ScaleMode.StretchToFill);
    if(Finale)GUI.DrawTexture(new Rect(0,0,w*.3f,h),shade,ScaleMode.StretchToFill); // finale: soft falloff, no hard-edged black slab (story25-a)
    else GUI.DrawTexture(new Rect(0,0,w*.36f,h),black);GUI.color=Color.white;}
   if(full&&t>=5f){ // blood streaks from the top
    float run=Mathf.Clamp01((t-5f)/2f);for(int i=0;i<7;i++){float x=w*(.08f+i*.13f+Mathf.Sin(i*5.3f)*.03f),sw=w*(.025f+.02f*Mathf.Abs(Mathf.Sin(i*2.1f)));GUI.DrawTexture(new Rect(x,0,sw,h*Mathf.Clamp01(run*(1.1f+.4f*Mathf.Sin(i*1.7f)))),streak);}}
  }
  void OnDestroy(){End();foreach(var tex in new[]{dot,drip,streak,shade,black})if(tex)Destroy(tex);}
 }
}
