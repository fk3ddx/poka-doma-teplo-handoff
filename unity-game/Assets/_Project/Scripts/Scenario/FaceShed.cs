using System.Collections.Generic;
using UnityEngine;
namespace Ataka.Scenario {
 // 11.9, second by second, from geometry that is already in the game (no new model):
 //  - her head is rebuilt from her OWN Katcho triangles (bone Head, her 256 px atlas, same UV): the front ones become a
 //    loose FACE SKIN, the rest stays as the scalp/bun; her skinned head is collapsed while they stand in for it;
 //  - inside it sits the Guest's own head (its Head-weighted triangles, its materials, scaled to fit) and its JAW
 //    (Jaw-weighted triangles, hinged at the Jaw joint) — the grey skin and eye hollows under her face.
 // At(t): 1.0 one eye of the skin slides down · 1.3 the skin splits at the temple · 1.5–2.2 it slides off the chin like
 // wet cloth and hangs at the neck · 2.0 the jaw drops · 2.6 the skin falls off. All follows the RENDERED head (after
 // GranRig, order 560) and her growth. End() removes everything and gives her skinned head back.
 [DefaultExecutionOrder(560)]
 public sealed class FaceShed:MonoBehaviour {
  GranRig rig;Transform root,follow,skinT,scalpT,guestT,jawT;Quaternion offset;Vector3 rootScale0,headScale0;
  Mesh skinMesh,scalpMesh,guestMesh,jawMesh;Vector3[] skin0,skinNow;int[] eye,temple;float skinTop,skinBottom,skinWidth;Vector3 jawPivot;
  Vector3 fall,fallVelocity;bool fallen;
  public bool Ready{get;private set;}public bool Shown{get;private set;}
  public int SkinTris,ScalpTris,GuestTris,JawTris;
  public float EyeDrop{get;private set;}public float SeamGap{get;private set;}public float Slide{get;private set;}public float JawAngle{get;private set;}
  public Transform QaSkin=>skinT;public Transform QaGuestHead=>guestT;public Transform QaJaw=>jawT;
  // what is rendered as her head top now (her skinned head is collapsed): the rebuilt scalp / skin, in world space
  public float Top{get{if(!Shown)return float.NaN;float top=float.MinValue;foreach(var t in new[]{scalpT,skinT})if(t){var b=t.GetComponent<Renderer>().bounds;top=Mathf.Max(top,b.max.y);}return top;}}

  static int Dominant(BoneWeight w){int b=w.boneIndex0;float m=w.weight0;if(w.weight1>m){m=w.weight1;b=w.boneIndex1;}if(w.weight2>m){m=w.weight2;b=w.boneIndex2;}if(w.weight3>m){b=w.boneIndex3;}return b;}
  static Mesh Build(string name,List<Vector3> v,List<Vector2> uv,List<int>[] subs){var m=new Mesh{name=name};m.SetVertices(v);if(uv.Count==v.Count)m.SetUVs(0,uv);m.subMeshCount=subs.Length;for(int i=0;i<subs.Length;i++)m.SetTriangles(subs[i],i);m.RecalculateNormals();m.RecalculateBounds();return m;}
  Transform Part(string name,Mesh mesh,Material[] mats){var g=new GameObject(name);g.transform.SetParent(follow,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();g.SetActive(false);r.sharedMaterials=mats;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return g.transform;}

  public static FaceShed Create(GranRig rig,GameObject guest){var f=new GameObject("Finale face shed (11.9)").AddComponent<FaceShed>();f.rig=rig;f.Extract(guest);return f;}
  void Extract(GameObject guest){
   rig.Init();var g=rig.transform;root=g;var head=rig.head;var skin=g.GetComponentInChildren<SkinnedMeshRenderer>(true);if(!skin||!head)return;
   rootScale0=g.lossyScale;headScale0=head.localScale;
   offset=Quaternion.Inverse(head.rotation)*Quaternion.LookRotation(g.forward,Vector3.up);var frame=head.rotation*offset;var inv=Quaternion.Inverse(frame);
   follow=new GameObject("follow").transform;follow.SetParent(transform,false);follow.SetPositionAndRotation(head.position,frame);
   // her head, from the posed (baked, scale-compensated) mesh into the follower frame (x right, y up, z out of her face)
   var baked=new Mesh();skin.BakeMesh(baked,true);var bv=baked.vertices;var buv=skin.sharedMesh.uv;var bw=skin.sharedMesh.boneWeights;int hi=System.Array.IndexOf(skin.bones,head);var tris=skin.sharedMesh.triangles;Destroy(baked);
   var local=new Vector3[bv.Length];for(int i=0;i<bv.Length;i++)local[i]=inv*(skin.transform.TransformPoint(bv[i])-head.position);
   var headTris=new List<int>();for(int i=0;i<tris.Length;i+=3)if(Dominant(bw[tris[i]])==hi&&Dominant(bw[tris[i+1]])==hi&&Dominant(bw[tris[i+2]])==hi)headTris.Add(i);
   if(headTris.Count==0){Debug.LogWarning("FaceShed: no Head-weighted triangles in her mesh");return;}
   float zMin=float.MaxValue,zMax=float.MinValue;foreach(int i in headTris)for(int k=0;k<3;k++){float z=local[tris[i+k]].z;zMin=Mathf.Min(zMin,z);zMax=Mathf.Max(zMax,z);}
   float zFace=Mathf.Lerp(zMin,zMax,.55f);
   var sv=new List<Vector3>();var suv=new List<Vector2>();var st=new List<int>();var cv=new List<Vector3>();var cuv=new List<Vector2>();var ct=new List<int>();
   foreach(int i in headTris){var a=local[tris[i]];var b=local[tris[i+1]];var c=local[tris[i+2]];bool front=(a.z+b.z+c.z)/3>zFace; // the front slice of her head (winding-independent)
    var V=front?sv:cv;var U=front?suv:cuv;var T=front?st:ct;for(int k=0;k<3;k++){V.Add(local[tris[i+k]]);U.Add(tris[i+k]<buv.Length?buv[tris[i+k]]:Vector2.zero);T.Add(V.Count-1);}}
   var mat=new[]{skin.sharedMaterial};
   skinMesh=Build("her face skin",sv,suv,new[]{st});scalpMesh=Build("her scalp",cv,cuv,new[]{ct});SkinTris=st.Count/3;ScalpTris=ct.Count/3;
   skinT=Part("face skin",skinMesh,mat);scalpT=Part("scalp",scalpMesh,mat);skin0=skinMesh.vertices;skinNow=(Vector3[])skin0.Clone();
   skinTop=float.MinValue;skinBottom=float.MaxValue;float xMax=float.MinValue,xMin=float.MaxValue;foreach(var v in skin0){skinTop=Mathf.Max(skinTop,v.y);skinBottom=Mathf.Min(skinBottom,v.y);xMax=Mathf.Max(xMax,v.x);xMin=Mathf.Min(xMin,v.x);}skinWidth=Mathf.Max(.01f,xMax-xMin);
   // one eye: the skin vertices nearest the painted eye
   float h=Mathf.Max(.01f,skinTop-skinBottom);var eyeAt=new Vector3(xMin+skinWidth*.71f,skinBottom+h*.42f,0);var eyes=new List<int>(); // her painted eyes are 42 % up the front slice (Blender check of her atlas)
   float best=float.MaxValue;for(int i=0;i<skin0.Length;i++){var d=new Vector2(skin0[i].x-eyeAt.x,skin0[i].y-eyeAt.y).magnitude;if(d<best)best=d;}
   for(int i=0;i<skin0.Length;i++)if(new Vector2(skin0[i].x-eyeAt.x,skin0[i].y-eyeAt.y).magnitude<best+h*.12f)eyes.Add(i);eye=eyes.ToArray();
   var side=new List<int>();for(int i=0;i<skin0.Length;i++)if(skin0[i].x<xMin+skinWidth*.18f&&skin0[i].y>skinBottom+h*.45f)side.Add(i);temple=side.ToArray(); // her right temple
   // the Guest's head and jaw, in bind pose, into the same frame, scaled to sit inside her head
   var look=guest?guest.GetComponent<GuestLookAt>():null;SkinnedMeshRenderer gs=null;
   // The actor also retains inactive proxy renderers. Select the mesh bound to the actual Guest head,
   // never the first child (which can be an unreadable Quaternius Eyebrows mesh).
   if(guest&&look&&look.headBone)foreach(var candidate in guest.GetComponentsInChildren<SkinnedMeshRenderer>(true))
    if(candidate.sharedMesh&&System.Array.IndexOf(candidate.bones,look.headBone)>=0){gs=candidate;break;}
   if(gs&&!gs.sharedMesh.isReadable){Debug.LogError("FaceShed: the actual Guest mesh must be readable: "+gs.sharedMesh.name);return;}
   if(gs&&look&&look.headBone){
    var gm=gs.sharedMesh;var gb=gm.bindposes;var gvs=gm.vertices;var gw=gm.boneWeights;int gh=System.Array.IndexOf(gs.bones,look.headBone);int gj=-1;for(int i=0;i<gs.bones.Length;i++)if(gs.bones[i]&&gs.bones[i].name.Contains("Jaw"))gj=i;
    // Guest rig: the head is joint4 (no weights of its own), the lower jaw is weighted to joint18 under the Jaw bone
    var jawSet=new HashSet<int>();if(gj>=0)for(int i=0;i<gs.bones.Length;i++)if(gs.bones[i]&&gs.bones[i].IsChildOf(gs.bones[gj]))jawSet.Add(i);
    var under=new HashSet<int>{gh};for(int i=0;i<gs.bones.Length;i++)if(gs.bones[i]&&gs.bones[i].IsChildOf(look.headBone)&&!jawSet.Contains(i))under.Add(i);
    var gface=gb[gh].inverse.MultiplyVector(look.headFaceLocal).normalized;var gup=gs.transform.InverseTransformDirection(guest.transform.up).normalized;gface=Vector3.ProjectOnPlane(gface,gup).normalized;var grot=Quaternion.Inverse(Quaternion.LookRotation(gface,gup));
    var hv=new List<Vector3>();var huv=new List<Vector2>();var hs=new List<int>[gm.subMeshCount];var jv=new List<Vector3>();var juv=new List<Vector2>();var js=new List<int>[gm.subMeshCount];var guv=gm.uv;
    var centre=Vector3.zero;int n=0;for(int i=0;i<gvs.Length;i++){int d=Dominant(gw[i]);if(under.Contains(d)||jawSet.Contains(d)){centre+=gvs[i];n++;}}if(n>0)centre/=n;
    float gTop=float.MinValue,gBot=float.MaxValue;
    for(int s=0;s<gm.subMeshCount;s++){hs[s]=new List<int>();js[s]=new List<int>();var t=gm.GetTriangles(s);
     for(int i=0;i<t.Length;i+=3){int d0=Dominant(gw[t[i]]),d1=Dominant(gw[t[i+1]]),d2=Dominant(gw[t[i+2]]);int inJaw=(jawSet.Contains(d0)?1:0)+(jawSet.Contains(d1)?1:0)+(jawSet.Contains(d2)?1:0);bool all=(jawSet.Contains(d0)||under.Contains(d0))&&(jawSet.Contains(d1)||under.Contains(d1))&&(jawSet.Contains(d2)||under.Contains(d2));bool isJaw=all&&inJaw>=2;bool isHead=all&&!isJaw;if(!isJaw&&!isHead)continue;
      var V=isJaw?jv:hv;var U=isJaw?juv:huv;var T=isJaw?js[s]:hs[s];for(int k=0;k<3;k++){var p=grot*(gvs[t[i+k]]-centre);V.Add(p);U.Add(t[i+k]<guv.Length?guv[t[i+k]]:Vector2.zero);T.Add(V.Count-1);if(!isJaw){gTop=Mathf.Max(gTop,p.y);gBot=Mathf.Min(gBot,p.y);}}}}
    float k2=(h*1.15f)/Mathf.Max(.01f,gTop-gBot); // its face about the height of hers (forehead to chin)
    var herCentre=new Vector3((xMin+xMax)/2,(skinTop+skinBottom)/2,zFace-.01f);
    for(int i=0;i<hv.Count;i++)hv[i]=herCentre+hv[i]*k2;for(int i=0;i<jv.Count;i++)jv[i]=herCentre+jv[i]*k2;
    var jawBone=gj>=0?gb[gj].inverse.MultiplyPoint(Vector3.zero):centre;jawPivot=herCentre+grot*(jawBone-centre)*k2;
    for(int i=0;i<jv.Count;i++)jv[i]-=jawPivot; // the jaw mesh hinges at its joint
    var gmats=gs.sharedMaterials;guestMesh=Build("Guest head",hv,huv,hs);jawMesh=Build("Guest jaw",jv,juv,js);GuestTris=hv.Count/3;JawTris=jv.Count/3;
    guestT=Part("Guest head (under her face)",guestMesh,gmats);jawT=Part("Guest jaw",jawMesh,gmats);jawT.localPosition=jawPivot;}
   else Debug.LogWarning("FaceShed: Guest head bone not found — no grey head under her face");
   foreach(Transform c in follow)c.gameObject.SetActive(false);Ready=SkinTris>0;
  }
  public void Show(){if(!Ready||Shown)return;Shown=true;foreach(Transform c in follow)c.gameObject.SetActive(true);rig.head.localScale=headScale0*.001f;Place();}
  void LateUpdate(){if(Shown)Place();}
  void Place(){var head=rig.head;follow.SetPositionAndRotation(head.position,head.rotation*offset);float s=root.lossyScale.y/Mathf.Max(1e-5f,rootScale0.y);follow.localScale=Vector3.one*s;}
  // t = seconds into 11.9
  public void At(float t){
   if(!Ready)return;if(t>=.95f)Show();if(!Shown)return;
   float h=skinTop-skinBottom;EyeDrop=.035f*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1f)/.3f));SeamGap=.014f*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1.3f)/.12f));Slide=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1.5f)/.7f));JawAngle=38*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-2f)/.25f));
   for(int i=0;i<skin0.Length;i++)skinNow[i]=skin0[i];
   foreach(int i in eye)skinNow[i]+=Vector3.down*EyeDrop;
   foreach(int i in temple)skinNow[i]+=new Vector3(-SeamGap,0,-SeamGap*.6f);
   if(Slide>0)for(int i=0;i<skin0.Length;i++){float yn=Mathf.Clamp01((skin0[i].y-skinBottom)/Mathf.Max(.01f,h));
    // wet cloth: everything slides down past the chin, the upper part more, folding forward and outwards as it hangs
    skinNow[i]+=Vector3.down*Slide*h*(.85f+.35f*yn)+Vector3.forward*Slide*.035f*(1-yn)+new Vector3(skin0[i].x*.25f*Slide*yn,0,0);}
   skinMesh.vertices=skinNow;skinMesh.RecalculateNormals();skinMesh.RecalculateBounds();
   if(jawT)jawT.localRotation=Quaternion.AngleAxis(JawAngle,Vector3.right);
   // 2.6 the skin drops off her and falls to the floor
   if(t>=2.6f&&!fallen&&skinT){fallen=true;skinT.SetParent(transform,true);fallVelocity=Vector3.zero;}
   if(fallen&&skinT&&skinT.position.y>.05f){fallVelocity+=Physics.gravity*Time.deltaTime;skinT.position+=fallVelocity*Time.deltaTime;skinT.rotation=Quaternion.Slerp(skinT.rotation,Quaternion.Euler(80,skinT.eulerAngles.y,0),Time.deltaTime*4);}
  }
  public void End(){if(rig&&rig.head&&Shown)rig.head.localScale=headScale0;Shown=false;Destroy(gameObject);}
  void OnDestroy(){if(rig&&rig.head&&Shown)rig.head.localScale=headScale0;foreach(var m in new[]{skinMesh,scalpMesh,guestMesh,jawMesh})if(m)Destroy(m);}
 }
}
