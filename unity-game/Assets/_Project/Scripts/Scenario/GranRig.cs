using System.Collections.Generic;
using UnityEngine;
namespace Ataka.Scenario {
 // A procedural layer over the approved Katcho rig, for the finale (11.6–11.9). The rig has 17 bones and a 256-vertex
 // mesh: no finger, jaw, eye or face bones, the hand is one bone ("fingers" = the hand lengthened along the forearm).
 // Applied in LateUpdate after the legacy Animation; with every weight at 0 the authored clip is untouched.
 //  - two-bone arm IK to a FINGERTIP target (shoulder → elbow → wrist, hand aimed at the target) with a pole hint;
 //  - held props follow the fingertip (kept upright, turning with her);
 //  - forearm / hand length scale, spine straightening, head bow / roll, a skin tint (MaterialPropertyBlock).
 [DefaultExecutionOrder(500)]
 public sealed class GranRig:MonoBehaviour {
  public Transform back,chest,neck,head,upperL,lowerL,handL,upperR,lowerR,handR;
  public bool Ready{get;private set;}
  [System.NonSerialized] public float weightL,weightR,forearmScale=1,handScale=1,straighten,headBow,headRoll;
  [System.NonSerialized] public Vector3 targetL,targetR;[System.NonSerialized] public Vector3? poleL,poleR;
  [System.NonSerialized] public Color tint=Color.white;
  Vector3 lowerScaleL,lowerScaleR,handScaleL,handScaleR,handDirL,handDirR;int lowerAxisL,lowerAxisR,handAxisL,handAxisR;Vector3 tipLocalL,tipLocalR;readonly List<Vector3> headVertices=new List<Vector3>();
  Transform[] posed;Quaternion[] pose;Animation clip;
  SkinnedMeshRenderer skin;MaterialPropertyBlock block;Color baseColor=Color.white;bool tinted;
  struct Held {public Transform prop;public bool right;public Vector3 offset;public Quaternion rotation;}
  readonly List<Held> held=new List<Held>();

  static Transform Find(Transform t,string name){if(t.name==name)return t;foreach(Transform c in t){var r=Find(c,name);if(r)return r;}return null;}
  static int Axis(Vector3 v){v=new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));return v.x>v.y&&v.x>v.z?0:v.y>v.z?1:2;}
  public void Init(){
   if(Ready)return;
   back=Find(transform,"Back");chest=Find(transform,"Chest");neck=Find(transform,"Neck");head=Find(transform,"Head");
   upperL=Find(transform,"Arm_Upper.L");lowerL=Find(transform,"Arm_Lower.L");handL=Find(transform,"Hand.L");upperR=Find(transform,"Arm_Upper.R");lowerR=Find(transform,"Arm_Lower.R");handR=Find(transform,"Hand.R");
   skin=GetComponentInChildren<SkinnedMeshRenderer>(true);
   if(!back||!chest||!neck||!head||!upperL||!lowerL||!handL||!upperR||!lowerR||!handR||!skin){Debug.LogWarning("GranRig: Katcho bones not found");return;}
   lowerScaleL=lowerL.localScale;lowerScaleR=lowerR.localScale;handScaleL=handL.localScale;handScaleR=handR.localScale;
   // Read fingertip geometry in bone-local bind space. Inferring it from elbow->wrist while the rig is in
   // its hanging pose chooses the wrong axis; scaling that axis stretched the palm sideways through the ceiling.
   var verts=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;var bones=skin.bones;var binds=skin.sharedMesh.bindposes;
   int iL=System.Array.IndexOf(bones,handL),iR=System.Array.IndexOf(bones,handR),iH=System.Array.IndexOf(bones,head);
   float farL=0,farR=0;
   for(int i=0;i<verts.Length&&i<weights.Length;i++){
    var w=weights[i];float Weight(int b)=>(w.boneIndex0==b?w.weight0:0)+(w.boneIndex1==b?w.weight1:0)+(w.boneIndex2==b?w.weight2:0)+(w.boneIndex3==b?w.weight3:0);
    if(iL>=0&&Weight(iL)>.5f){var v=binds[iL].MultiplyPoint3x4(verts[i]);if(v.sqrMagnitude>farL){farL=v.sqrMagnitude;tipLocalL=v;}}
    if(iR>=0&&Weight(iR)>.5f){var v=binds[iR].MultiplyPoint3x4(verts[i]);if(v.sqrMagnitude>farR){farR=v.sqrMagnitude;tipLocalR=v;}}
    if(iH>=0&&Weight(iH)>.5f)headVertices.Add(binds[iH].MultiplyPoint3x4(verts[i]));
   }
   if(farL<1e-8f||farR<1e-8f){Debug.LogWarning("GranRig: no weighted hand geometry for fingertip calibration");return;}
   handDirL=tipLocalL.normalized;handDirR=tipLocalR.normalized;
   lowerAxisL=Axis(handL.localPosition);lowerAxisR=Axis(handR.localPosition);handAxisL=Axis(handDirL);handAxisR=Axis(handDirR);
   var mat=skin.sharedMaterial;if(mat&&mat.HasProperty("_BaseColor"))baseColor=mat.GetColor("_BaseColor");else if(mat&&mat.HasProperty("_Color"))baseColor=mat.GetColor("_Color");
   posed=new[]{back,chest,neck,head,upperL,lowerL,handL,upperR,lowerR,handR};pose=new Quaternion[posed.Length];for(int i=0;i<posed.Length;i++)pose[i]=posed[i].localRotation;clip=GetComponentInChildren<Animation>(true);
   block=new MaterialPropertyBlock();Ready=true;
  }
  public float TipLength(bool right)=>(right?handR:handL).TransformVector(right?tipLocalR:tipLocalL).magnitude;
  Vector3 lastTipL,lastTipR,lastHead;bool tipSampled;
  public Vector3 HeadPosition=>tipSampled?transform.TransformPoint(lastHead):head.position;
  Bounds renderedHeadBounds;bool headBoundsSampled;
  // The Head bone is a neck pivot, not the centre of the visible head/scarf. Frame a hug from the rendered surface.
  public Vector3 HugEye=>headBoundsSampled?transform.TransformPoint(new Vector3(renderedHeadBounds.min.x,renderedHeadBounds.center.y,renderedHeadBounds.center.z))-transform.right*.10f+Vector3.up*.02f:HeadPosition-transform.right*.4f;
  public Bounds RenderedHeadBounds=>renderedHeadBounds;
  Vector3 RawTip(bool right)=>(right?handR:handL).TransformPoint(right?tipLocalR:tipLocalL);
  // Story coroutines run after Update, when last frame's IK has been undone for Animation. Queries must read the
  // last rendered pose, in actor-local coordinates so a walk/turn still moves it correctly this frame.
  public Vector3 Tip(bool right){Init();if(!Ready)return transform.position;return tipSampled?transform.TransformPoint(right?lastTipR:lastTipL):RawTip(right);}
  // Approximate top of her head (bones + measured offset); MeasureTop() bakes the mesh for QA.
  public float Top {get {if(!Ready||headVertices.Count==0)return transform.position.y+1.6f;float top=float.MinValue;foreach(var v in headVertices)top=Mathf.Max(top,head.TransformPoint(v).y);return top;}}
  public float LastBakedTop{get;private set;}public float LastCompensatedTop{get;private set;}public float LastSkinnedTop{get;private set;}
  public float MeasureTop(){
   Init();if(!skin)return Top;var mesh=new Mesh();
   float BakeTop(bool compensate){skin.BakeMesh(mesh,compensate);float top=float.MinValue;foreach(var p in mesh.vertices)top=Mathf.Max(top,skin.transform.TransformPoint(p).y);return top;}
   LastBakedTop=BakeTop(false);LastCompensatedTop=BakeTop(true);Destroy(mesh);
   // Independent world-space skinning of every original vertex, without multiplying the renderer scale twice.
   var vs=skin.sharedMesh.vertices;var ws=skin.sharedMesh.boneWeights;var bs=skin.bones;var bind=skin.sharedMesh.bindposes;var matrices=new Matrix4x4[bs.Length];
   for(int i=0;i<bs.Length;i++)matrices[i]=bs[i].localToWorldMatrix*bind[i];float max=float.MinValue;
   for(int i=0;i<vs.Length;i++){var w=ws[i];var v=matrices[w.boneIndex0].MultiplyPoint3x4(vs[i])*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(vs[i])*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(vs[i])*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(vs[i])*w.weight3;max=Mathf.Max(max,v.y);}
   LastSkinnedTop=max;return max;
  }
  public void Reach(bool right,Vector3 tip,float weight){if(right){targetR=tip;weightR=weight;}else{targetL=tip;weightL=weight;}}
  public void Hold(Transform prop,bool right){if(!prop)return;Release(prop);var tip=Tip(right);held.Add(new Held{prop=prop,right=right,offset=Quaternion.Inverse(transform.rotation)*(prop.position-tip),rotation=Quaternion.Inverse(transform.rotation)*prop.rotation});}
  public void Release(Transform prop){held.RemoveAll(h=>h.prop==prop);}
  public void ResetAll(){RestorePose();tipSampled=false;weightL=weightR=0;forearmScale=handScale=1;straighten=headBow=headRoll=0;tint=Color.white;poleL=poleR=null;held.Clear();
   if(Ready){lowerL.localScale=lowerScaleL;lowerR.localScale=lowerScaleR;handL.localScale=handScaleL;handR.localScale=handScaleR;}if(skin&&tinted){skin.SetPropertyBlock(null);tinted=false;}}

  static Vector3 Scaled(Vector3 b,int axis,float k){b[axis]*=k;return b;}
  bool Active=>weightL>0||weightR>0||forearmScale!=1||handScale!=1||straighten!=0||headBow!=0||headRoll!=0||held.Count>0;
  bool appliedLast;
  void RestorePose(){if(!Ready||!appliedLast)return;for(int i=0;i<posed.Length;i++)posed[i].localRotation=pose[i];lowerL.localScale=lowerScaleL;lowerR.localScale=lowerScaleR;handL.localScale=handScaleL;handR.localScale=handScaleR;appliedLast=false;}
  // Legacy clips do not necessarily key every bone. Undo last frame's procedural layer BEFORE animation samples,
  // otherwise an unkeyed arm keeps yesterday's IK and is treated as a new authored pose on the next frame.
  void Update(){RestorePose();}
  void LateUpdate(){
   if(!Ready)return;
   if(Active){for(int i=0;i<posed.Length;i++)pose[i]=posed[i].localRotation;Apply();appliedLast=true;}
   lastTipL=transform.InverseTransformPoint(RawTip(false));lastTipR=transform.InverseTransformPoint(RawTip(true));lastHead=transform.InverseTransformPoint(head.position);tipSampled=true;
   headBoundsSampled=false;foreach(var v in headVertices){var q=transform.InverseTransformPoint(head.TransformPoint(v));if(!headBoundsSampled){renderedHeadBounds=new Bounds(q,Vector3.zero);headBoundsSampled=true;}else renderedHeadBounds.Encapsulate(q);}
   if(tint!=Color.white){skin.GetPropertyBlock(block);var c=baseColor*tint;c.a=baseColor.a;block.SetColor("_BaseColor",c);block.SetColor("_Color",c);skin.SetPropertyBlock(block);tinted=true;}
   else if(tinted){skin.SetPropertyBlock(null);tinted=false;}
  }
  void Apply(){
   lowerL.localScale=Scaled(lowerScaleL,lowerAxisL,forearmScale);lowerR.localScale=Scaled(lowerScaleR,lowerAxisR,forearmScale);
   handL.localScale=Scaled(handScaleL,handAxisL,handScale);handR.localScale=Scaled(handScaleR,handAxisR,handScale);
   if(straighten>0){var up=(neck.position-back.position).normalized;chest.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(up,Vector3.up),Mathf.Clamp01(straighten))*chest.rotation;}
   if(headBow!=0||headRoll!=0)neck.rotation=Quaternion.AngleAxis(headBow,transform.right)*Quaternion.AngleAxis(headRoll,transform.forward)*neck.rotation;
   var side=transform.right;var pr=poleR??(side*.5f-transform.up*.8f-transform.forward*.2f);var pl=poleL??(-side*.5f-transform.up*.8f-transform.forward*.2f);
   Solve(upperR,lowerR,handR,handDirR,true,targetR,Mathf.Clamp01(weightR),pr);Solve(upperL,lowerL,handL,handDirL,false,targetL,Mathf.Clamp01(weightL),pl);
   foreach(var h in held)if(h.prop){h.prop.position=RawTip(h.right)+transform.rotation*h.offset;h.prop.rotation=transform.rotation*h.rotation;}
  }
  void Solve(Transform up,Transform low,Transform hand,Vector3 handDir,bool right,Vector3 target,float w,Vector3 pole){
   if(w<=0)return;
   var a=up.position;var b=low.position;var c=hand.position;float tip=TipLength(right);
   var natural=hand.TransformPoint(right?tipLocalR:tipLocalL);var tgt=Vector3.Lerp(natural,target,w);
   var wrist=tgt-(tgt-a).normalized*tip;
   float la=(b-a).magnitude,lb=(c-b).magnitude;var d=wrist-a;float dist=Mathf.Clamp(d.magnitude,Mathf.Abs(la-lb)+1e-3f,la+lb-1e-3f);var dir=d.sqrMagnitude>1e-8f?d.normalized:Vector3.down;
   float cos=Mathf.Clamp((la*la+dist*dist-lb*lb)/(2*la*dist),-1,1),sin=Mathf.Sqrt(1-cos*cos);
   var pd=Vector3.ProjectOnPlane(pole,dir);if(pd.sqrMagnitude<1e-6f)pd=Vector3.ProjectOnPlane(Vector3.down,dir);pd.Normalize();
   var elbow=a+dir*(la*cos)+pd*(la*sin);
   up.rotation=Quaternion.FromToRotation(b-a,elbow-a)*up.rotation;
   b=low.position;c=hand.position;low.rotation=Quaternion.FromToRotation(c-b,a+dir*dist-b)*low.rotation;
   c=hand.position;var hd=hand.TransformVector(right?tipLocalR:tipLocalL).normalized;var want=tgt-c;if(want.sqrMagnitude>1e-6f)hand.rotation=Quaternion.FromToRotation(hd,want.normalized)*hand.rotation;
  }
 }
}
