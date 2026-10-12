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
 // pc28d (story28-key-d / face-d: black face at 1.2, a small toothy head floating over a spike of neck at 2.8):
 //  - everything is built in the Head bone's BIND space (bindposes), not from the pose she happens to stand in: the face
 //    slice and the Guest's fit were taken along her body's forward while her real head was bowed/tilted, so the upright
 //    Guest stuck out through her face (the black) and the slice cut the wrong triangles;
 //  - her Head bone is no longer collapsed to 0.001: that pulled every neck vertex with some Head weight into a spike at
 //    the pivot (the "floating" head). Her skinned mesh is swapped for a copy without exactly the head triangles we
 //    rebuild, so the neck stays whole and the rebuilt head (head bone × bind, = her skinning) closes onto it;
 //  - the Guest's head appears only from the temple seam (1.3); normals come from her mesh (no flat re-shading).
 [DefaultExecutionOrder(560)]
 public sealed class FaceShed:MonoBehaviour {
  GranRig rig;Transform root,follow,skinT,scalpT,guestT,jawT;Quaternion offset;Vector3 rootScale0,headScale0;SkinnedMeshRenderer body;Mesh bodyMesh,headless;int[] seamIndex;Vector3[] seamFrame;
  Mesh skinMesh,scalpMesh,guestMesh,jawMesh;Vector3[] skin0,skinNow;int[] eye,temple;float skinTop,skinBottom,skinWidth;Vector3 jawPivot;
  Vector3 fall,fallVelocity;bool fallen;
  public bool Ready{get;private set;}public bool Shown{get;private set;}
  public int SkinTris,ScalpTris,GuestTris,JawTris;
  public float EyeDrop{get;private set;}public float SeamGap{get;private set;}public float Slide{get;private set;}public float JawAngle{get;private set;}
  public Transform QaSkin=>skinT;public Transform QaGuestHead=>guestT;public Transform QaJaw=>jawT;
  // what is rendered as her head top now (her skinned head is collapsed): the rebuilt scalp / skin, in world space
  // pc28c: the exact top over the rendered vertices (Renderer.bounds is the AABB of the rotated mesh and overstates it)
  public float Top{get{if(!Shown)return float.NaN;float top=float.MinValue;foreach(var t in new[]{scalpT,skinT,guestT})if(t&&t.IsChildOf(follow)){var m=t.GetComponent<MeshFilter>().sharedMesh;foreach(var v in m.vertices)top=Mathf.Max(top,t.TransformPoint(v).y);}return top;}}
  public bool Mirrored{get;private set;}public bool GuestInside{get;private set;}public float GuestScale{get;private set;}Vector3 herMin,herMax;
  // QA: share of a part's triangles that face the camera — her face skin must face him, not show its back
  public float FacingCamera(Transform part,Camera cam){if(!part||!cam)return 0;var m=part.GetComponent<MeshFilter>().sharedMesh;var v=m.vertices;var t=m.triangles;int n=0,f=0;
   for(int i=0;i<t.Length;i+=3){var a=part.TransformPoint(v[t[i]]);var b=part.TransformPoint(v[t[i+1]]);var c=part.TransformPoint(v[t[i+2]]);var nrm=Vector3.Cross(b-a,c-a);var mid=(a+b+c)/3;n++;if(Vector3.Dot(nrm,cam.transform.position-mid)>0)f++;}return n>0?f/(float)n:0;}
  public Vector3 SkinCentre=>skinT?skinT.GetComponent<Renderer>().bounds.center:Vector3.zero;public Vector3 HerChin=>follow?follow.TransformPoint(new Vector3((herMin.x+herMax.x)/2,herMin.y,herMax.z)):Vector3.zero;

  static int Dominant(BoneWeight w){int b=w.boneIndex0;float m=w.weight0;if(w.weight1>m){m=w.weight1;b=w.boneIndex1;}if(w.weight2>m){m=w.weight2;b=w.boneIndex2;}if(w.weight3>m){b=w.boneIndex3;}return b;}
  static Mesh Build(string name,List<Vector3> v,List<Vector2> uv,List<int>[] subs){var m=new Mesh{name=name};m.SetVertices(v);if(uv.Count==v.Count)m.SetUVs(0,uv);m.subMeshCount=subs.Length;for(int i=0;i<subs.Length;i++)m.SetTriangles(subs[i],i);m.RecalculateNormals();m.RecalculateBounds();return m;}
  Transform Part(string name,Mesh mesh,Material[] mats){var g=new GameObject(name);g.transform.SetParent(follow,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();g.SetActive(false);r.sharedMaterials=mats;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return g.transform;}

  public static FaceShed Create(GranRig rig,GameObject guest){var f=new GameObject("Finale face shed (11.9)").AddComponent<FaceShed>();f.rig=rig;f.Extract(guest);return f;}
  void Extract(GameObject guest){
   rig.Init();var g=rig.transform;root=g;var head=rig.head;var skin=g.GetComponentInChildren<SkinnedMeshRenderer>(true);if(!skin||!head)return;
   rootScale0=g.lossyScale;headScale0=head.localScale;body=skin;bodyMesh=skin.sharedMesh;
   if(!bodyMesh.isReadable){Debug.LogError("FaceShed: her mesh must be readable: "+bodyMesh.name);return;}
   int hi=System.Array.IndexOf(skin.bones,head);if(hi<0||hi>=bodyMesh.bindposes.Length){Debug.LogWarning("FaceShed: Head is not one of her skin bones");return;}
   // her face frame inside the Head bone (bind pose): forward/up of her body expressed in head-bone space
   var bind=bodyMesh.bindposes[hi];var faceB=bind.MultiplyVector(skin.transform.InverseTransformDirection(g.forward)).normalized;var upB=bind.MultiplyVector(skin.transform.InverseTransformDirection(g.up));upB=Vector3.ProjectOnPlane(upB,faceB).normalized;
   offset=Quaternion.LookRotation(faceB,upB);var inv=Quaternion.Inverse(offset);
   follow=new GameObject("follow").transform;follow.SetParent(transform,false);Mirrored=head.localToWorldMatrix.determinant<0;
   // her head in that frame (x right, y up, z out of her face): bind-space vertices, normals and UVs of her own mesh
   var bv=bodyMesh.vertices;var bn=bodyMesh.normals;var buv=bodyMesh.uv;var bw=bodyMesh.boneWeights;var tris=bodyMesh.triangles;
   var local=new Vector3[bv.Length];var nrm=new Vector3[bv.Length];for(int i=0;i<bv.Length;i++){local[i]=inv*bind.MultiplyPoint3x4(bv[i]);nrm[i]=i<bn.Length?(inv*bind.MultiplyVector(bn[i])).normalized:Vector3.forward;}
   var headTris=new List<int>();var headTri=new HashSet<int>();for(int i=0;i<tris.Length;i+=3)if(Dominant(bw[tris[i]])==hi&&Dominant(bw[tris[i+1]])==hi&&Dominant(bw[tris[i+2]])==hi){headTris.Add(i);headTri.Add(i/3);}
   if(headTris.Count==0){Debug.LogWarning("FaceShed: no Head-weighted triangles in her mesh");return;}
   float zMin=float.MaxValue,zMax=float.MinValue;foreach(int i in headTris)for(int k=0;k<3;k++){float z=local[tris[i+k]].z;zMin=Mathf.Min(zMin,z);zMax=Mathf.Max(zMax,z);}
   float zFace=Mathf.Lerp(zMin,zMax,.55f);
   var sv=new List<Vector3>();var suv=new List<Vector2>();var st=new List<int>();var sn=new List<Vector3>();var cv=new List<Vector3>();var cuv=new List<Vector2>();var ct=new List<int>();var cn=new List<Vector3>();
   foreach(int i in headTris){var a=local[tris[i]];var b=local[tris[i+1]];var c=local[tris[i+2]];bool front=(a.z+b.z+c.z)/3>zFace; // the front slice of her head (winding-independent)
    var V=front?sv:cv;var U=front?suv:cuv;var T=front?st:ct;var N=front?sn:cn;for(int k=0;k<3;k++){V.Add(local[tris[i+k]]);N.Add(nrm[tris[i+k]]);U.Add(tris[i+k]<buv.Length?buv[tris[i+k]]:Vector2.zero);T.Add(V.Count-1);}}
   // the seam: vertices shared by a rebuilt head triangle and a triangle her skinned (headless) body still draws
   var kept=new HashSet<int>();for(int i=0;i<tris.Length;i+=3)if(!headTri.Contains(i/3)){kept.Add(tris[i]);kept.Add(tris[i+1]);kept.Add(tris[i+2]);}
   var seam=new List<int>();foreach(int i in headTris)for(int k=0;k<3;k++)if(kept.Contains(tris[i+k])&&!seam.Contains(tris[i+k]))seam.Add(tris[i+k]);seamIndex=seam.ToArray();seamFrame=new Vector3[seamIndex.Length];for(int i=0;i<seamIndex.Length;i++)seamFrame[i]=local[seamIndex[i]];
   // her body without exactly those triangles (submeshes kept), swapped in while the rebuilt head shows
   headless=Instantiate(bodyMesh);headless.name=bodyMesh.name+" (no head, 11.9)";for(int s=0;s<bodyMesh.subMeshCount;s++){var t=bodyMesh.GetTriangles(s);var keep=new List<int>(t.Length);for(int i=0;i<t.Length;i+=3){bool hd=Dominant(bw[t[i]])==hi&&Dominant(bw[t[i+1]])==hi&&Dominant(bw[t[i+2]])==hi;if(!hd){keep.Add(t[i]);keep.Add(t[i+1]);keep.Add(t[i+2]);}}headless.SetTriangles(keep,s);}
   herMin=Vector3.positiveInfinity;herMax=Vector3.negativeInfinity;foreach(var v in sv){herMin=Vector3.Min(herMin,v);herMax=Vector3.Max(herMax,v);}foreach(var v in cv){herMin=Vector3.Min(herMin,v);herMax=Vector3.Max(herMax,v);}
   var mat=new[]{skin.sharedMaterial};
   skinMesh=Build("her face skin",sv,suv,new[]{st});scalpMesh=Build("her scalp",cv,cuv,new[]{ct});skinMesh.SetNormals(sn);scalpMesh.SetNormals(cn);SkinTris=st.Count/3;ScalpTris=ct.Count/3;
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
    // pc28c (story28-key-c: it stood over her bun like a cap and floated over the neck): the Guest's head AND jaw are fitted
    // INSIDE her rebuilt head (92 % of the tightest axis) and stand on her chin, so nothing of it shows until her face slides.
    var gMin=Vector3.positiveInfinity;var gMax=Vector3.negativeInfinity;foreach(var p in hv){gMin=Vector3.Min(gMin,p);gMax=Vector3.Max(gMax,p);}foreach(var p in jv){gMin=Vector3.Min(gMin,p);gMax=Vector3.Max(gMax,p);}
    var gSize=gMax-gMin;var herSize=herMax-herMin;float k2=.92f*Mathf.Min(herSize.x/Mathf.Max(.01f,gSize.x),Mathf.Min(herSize.y/Mathf.Max(.01f,gSize.y),herSize.z/Mathf.Max(.01f,gSize.z)));
    var shift=new Vector3((herMin.x+herMax.x)/2-(gMin.x+gMax.x)/2*k2,herMin.y+.01f-gMin.y*k2,(herMin.z+herMax.z)/2-(gMin.z+gMax.z)/2*k2-.005f);
    for(int i=0;i<hv.Count;i++)hv[i]=shift+hv[i]*k2;for(int i=0;i<jv.Count;i++)jv[i]=shift+jv[i]*k2;GuestScale=k2;
    var jawBone=gj>=0?gb[gj].inverse.MultiplyPoint(Vector3.zero):centre;jawPivot=shift+grot*(jawBone-centre)*k2;
    GuestInside=true;foreach(var p in hv)if(p.x<herMin.x-.005f||p.x>herMax.x+.005f||p.y<herMin.y-.005f||p.y>herMax.y+.005f||p.z<herMin.z-.005f||p.z>herMax.z+.005f){GuestInside=false;break;}
    for(int i=0;i<jv.Count;i++)jv[i]-=jawPivot; // the jaw mesh hinges at its joint
    var gmats=gs.sharedMaterials;guestMesh=Build("Guest head",hv,huv,hs);jawMesh=Build("Guest jaw",jv,juv,js);GuestTris=hv.Count/3;JawTris=jv.Count/3;
    guestT=Part("Guest head (under her face)",guestMesh,gmats);jawT=Part("Guest jaw",jawMesh,gmats);jawT.localPosition=jawPivot;}
   else Debug.LogWarning("FaceShed: Guest head bone not found — no grey head under her face");
   foreach(Transform c in follow)c.gameObject.SetActive(false);Ready=SkinTris>0;
  }
  public void Show(){if(!Ready||Shown)return;Shown=true;foreach(Transform c in follow)if(c!=guestT&&c!=jawT)c.gameObject.SetActive(true);if(body&&headless)body.sharedMesh=headless;Place();}
  public bool HeadlessBody=>body&&headless&&body.sharedMesh==headless;public bool GuestShown=>guestT&&guestT.gameObject.activeSelf;
  void LateUpdate(){if(Shown)Place();}
  // the follower IS her head bone (position, rotation, scale incl. her growth) turned into the face frame: a rebuilt
  // vertex lands where her own skinning puts a Head-weighted vertex
  void Place(){var head=rig.head;follow.SetPositionAndRotation(head.position,head.rotation*offset);var ls=head.lossyScale;follow.localScale=Vector3.one*((Mathf.Abs(ls.x)+Mathf.Abs(ls.y)+Mathf.Abs(ls.z))/3);}
  // QA: the largest distance between where her skinned (headless) body draws a seam vertex and where the rebuilt head has it
  public float SeamError(){if(!Shown||!body||seamIndex==null||seamIndex.Length==0)return float.NaN;var baked=new Mesh();body.BakeMesh(baked,true);var v=baked.vertices;Destroy(baked);float worst=0;
   for(int i=0;i<seamIndex.Length;i++)if(seamIndex[i]<v.Length)worst=Mathf.Max(worst,Vector3.Distance(body.transform.TransformPoint(v[seamIndex[i]]),follow.TransformPoint(seamFrame[i])));return worst;}
  public Bounds ScreenFace(Camera cam){var b=skinT?skinT.GetComponent<Renderer>().bounds:new Bounds();var min=new Vector3(1e9f,1e9f,0);var max=new Vector3(-1e9f,-1e9f,0);for(int i=0;i<8;i++){var c=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var s=cam.WorldToScreenPoint(c);min=Vector3.Min(min,s);max=Vector3.Max(max,s);}var r=new Bounds();r.SetMinMax(new Vector3(min.x,min.y,0),new Vector3(max.x,max.y,0));return r;}
  // t = seconds into 11.9
  public void At(float t){
   if(!Ready)return;if(t>=.95f)Show();if(!Shown)return;if(t>=1.3f&&guestT&&!guestT.gameObject.activeSelf){guestT.gameObject.SetActive(true);if(jawT)jawT.gameObject.SetActive(true);} // the grey head only once her skin splits
   float h=skinTop-skinBottom;EyeDrop=.035f*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1f)/.3f));SeamGap=.014f*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1.3f)/.12f));Slide=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1.5f)/.7f));JawAngle=38*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-2f)/.25f));
   for(int i=0;i<skin0.Length;i++)skinNow[i]=skin0[i];
   foreach(int i in eye)skinNow[i]+=Vector3.down*EyeDrop;
   foreach(int i in temple)skinNow[i]+=new Vector3(-SeamGap,0,-SeamGap*.6f);
   if(Slide>0)for(int i=0;i<skin0.Length;i++){float yn=Mathf.Clamp01((skin0[i].y-skinBottom)/Mathf.Max(.01f,h));
    // wet cloth: everything slides down past the chin, the upper part more, folding forward and outwards as it hangs;
    // pc28d: it ends hanging from her chin over the neck (SCENARIO «висит на шее»), not on her chest
    skinNow[i]+=Vector3.down*Slide*h*(.55f+.3f*yn)+Vector3.forward*Slide*.035f*(1-yn)+new Vector3(skin0[i].x*.25f*Slide*yn,0,0);}
   skinMesh.vertices=skinNow;skinMesh.RecalculateBounds();
   if(jawT)jawT.localRotation=Quaternion.AngleAxis(JawAngle,Vector3.right);
   // 2.6 the skin drops off her and falls to the floor
   if(t>=2.6f&&!fallen&&skinT){fallen=true;skinT.SetParent(transform,true);fallVelocity=Vector3.zero;}
   if(fallen&&skinT&&skinT.position.y>.05f){fallVelocity+=Physics.gravity*Time.deltaTime;skinT.position+=fallVelocity*Time.deltaTime;skinT.rotation=Quaternion.Slerp(skinT.rotation,Quaternion.Euler(80,skinT.eulerAngles.y,0),Time.deltaTime*4);}
  }
  void GiveBack(){if(body&&bodyMesh&&body.sharedMesh!=bodyMesh)body.sharedMesh=bodyMesh;if(rig&&rig.head)rig.head.localScale=headScale0;}
  public void End(){GiveBack();Shown=false;Destroy(gameObject);}
  void OnDestroy(){GiveBack();foreach(var m in new[]{skinMesh,scalpMesh,guestMesh,jawMesh,headless})if(m)Destroy(m);}
 }
}
