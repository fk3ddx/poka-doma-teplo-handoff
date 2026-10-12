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
  Vector3 lowerScaleL,lowerScaleR,handScaleL,handScaleR,handDirL,handDirR;int lowerAxisL,lowerAxisR,handAxisL,handAxisR;float tipL=.08f,tipR=.08f,headTop=.2f;
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
   handDirL=handL.InverseTransformDirection((handL.position-lowerL.position).normalized);handDirR=handR.InverseTransformDirection((handR.position-lowerR.position).normalized);
   lowerAxisL=Axis(lowerL.InverseTransformDirection(handL.position-lowerL.position));lowerAxisR=Axis(lowerR.InverseTransformDirection(handR.position-lowerR.position));handAxisL=Axis(handDirL);handAxisR=Axis(handDirR);
   // Fingertip distance and head top from the real (baked) mesh, per unit of her scale.
   var mesh=new Mesh();skin.BakeMesh(mesh);var v=mesh.vertices;var w=skin.sharedMesh.boneWeights;var bones=skin.bones;int iL=System.Array.IndexOf(bones,handL),iR=System.Array.IndexOf(bones,handR);float s=Mathf.Max(1e-4f,transform.lossyScale.y);float top=float.MinValue;
   var dL=handL.TransformDirection(handDirL);var dR=handR.TransformDirection(handDirR);float bestL=0,bestR=0;
   for(int i=0;i<v.Length;i++){var p=skin.transform.TransformPoint(v[i]);top=Mathf.Max(top,p.y);if(i>=w.Length)continue;
    if(w[i].boneIndex0==iL&&w[i].weight0>.5f)bestL=Mathf.Max(bestL,Vector3.Dot(p-handL.position,dL));if(w[i].boneIndex0==iR&&w[i].weight0>.5f)bestR=Mathf.Max(bestR,Vector3.Dot(p-handR.position,dR));}
   if(bestL>.01f)tipL=bestL/s;if(bestR>.01f)tipR=bestR/s;if(top>float.MinValue)headTop=(top-head.position.y)/s;Destroy(mesh);
   var mat=skin.sharedMaterial;if(mat&&mat.HasProperty("_BaseColor"))baseColor=mat.GetColor("_BaseColor");else if(mat&&mat.HasProperty("_Color"))baseColor=mat.GetColor("_Color");
   posed=new[]{back,chest,neck,head,upperL,lowerL,handL,upperR,lowerR,handR};pose=new Quaternion[posed.Length];for(int i=0;i<posed.Length;i++)pose[i]=posed[i].localRotation;clip=GetComponentInChildren<Animation>(true);
   block=new MaterialPropertyBlock();Ready=true;
  }
  public float TipLength(bool right)=>(right?tipR:tipL)*transform.lossyScale.y*handScale;
  public Vector3 Tip(bool right){Init();if(!Ready)return transform.position;var h=right?handR:handL;return h.position+h.TransformDirection(right?handDirR:handDirL)*TipLength(right);}
  // Approximate top of her head (bones + measured offset); MeasureTop() bakes the mesh for QA.
  public float Top=>Ready?head.position.y+headTop*transform.lossyScale.y:transform.position.y+1.6f;
  public float MeasureTop(){Init();if(!skin)return Top;var mesh=new Mesh();skin.BakeMesh(mesh);float top=float.MinValue;foreach(var p in mesh.vertices)top=Mathf.Max(top,skin.transform.TransformPoint(p).y);Destroy(mesh);return top;}
  public void Reach(bool right,Vector3 tip,float weight){if(right){targetR=tip;weightR=weight;}else{targetL=tip;weightL=weight;}}
  public void Hold(Transform prop,bool right){if(!prop)return;Release(prop);var tip=Tip(right);held.Add(new Held{prop=prop,right=right,offset=Quaternion.Inverse(transform.rotation)*(prop.position-tip),rotation=Quaternion.Inverse(transform.rotation)*prop.rotation});}
  public void Release(Transform prop){held.RemoveAll(h=>h.prop==prop);}
  public void ResetAll(){weightL=weightR=0;forearmScale=handScale=1;straighten=headBow=headRoll=0;tint=Color.white;poleL=poleR=null;held.Clear();
   if(Ready){lowerL.localScale=lowerScaleL;lowerR.localScale=lowerScaleR;handL.localScale=handScaleL;handR.localScale=handScaleR;}if(skin&&tinted){skin.SetPropertyBlock(null);tinted=false;}}

  static Vector3 Scaled(Vector3 b,int axis,float k){b[axis]*=k;return b;}
  bool Active=>weightL>0||weightR>0||forearmScale!=1||handScale!=1||straighten!=0||headBow!=0||headRoll!=0||held.Count>0;
  void LateUpdate(){
   if(!Ready)return;
   // The edits below are relative to the clip's pose of THIS frame: while the clip plays it has just written it; when it
   // is frozen (11.9) the last played pose is put back first, so nothing accumulates. Idle (all zero) = untouched, so a
   // pose sampled by someone else (the hanging, the rewind freeze) is never overwritten.
   bool playing=clip&&clip.enabled&&clip.isPlaying;
   if(playing)for(int i=0;i<posed.Length;i++)pose[i]=posed[i].localRotation;
   if(!Active){if(scaled){lowerL.localScale=lowerScaleL;lowerR.localScale=lowerScaleR;handL.localScale=handScaleL;handR.localScale=handScaleR;scaled=false;}}
   else{if(!playing)for(int i=0;i<posed.Length;i++)posed[i].localRotation=pose[i];scaled=true;Apply();}
   if(tint!=Color.white){skin.GetPropertyBlock(block);var c=baseColor*tint;c.a=baseColor.a;block.SetColor("_BaseColor",c);block.SetColor("_Color",c);skin.SetPropertyBlock(block);tinted=true;}
   else if(tinted){skin.SetPropertyBlock(null);tinted=false;}
  }
  bool scaled;
  void Apply(){
   lowerL.localScale=Scaled(lowerScaleL,lowerAxisL,forearmScale);lowerR.localScale=Scaled(lowerScaleR,lowerAxisR,forearmScale);
   handL.localScale=Scaled(handScaleL,handAxisL,handScale);handR.localScale=Scaled(handScaleR,handAxisR,handScale);
   if(straighten>0){var up=(neck.position-back.position).normalized;chest.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(up,Vector3.up),Mathf.Clamp01(straighten))*chest.rotation;}
   if(headBow!=0||headRoll!=0)neck.rotation=Quaternion.AngleAxis(headBow,transform.right)*Quaternion.AngleAxis(headRoll,transform.forward)*neck.rotation;
   var side=transform.right;var pr=poleR??(side*.5f-transform.up*.8f-transform.forward*.2f);var pl=poleL??(-side*.5f-transform.up*.8f-transform.forward*.2f);
   Solve(upperR,lowerR,handR,handDirR,true,targetR,Mathf.Clamp01(weightR),pr);Solve(upperL,lowerL,handL,handDirL,false,targetL,Mathf.Clamp01(weightL),pl);
   foreach(var h in held)if(h.prop){h.prop.position=Tip(h.right)+transform.rotation*h.offset;h.prop.rotation=transform.rotation*h.rotation;}
  }
  void Solve(Transform up,Transform low,Transform hand,Vector3 handDir,bool right,Vector3 target,float w,Vector3 pole){
   if(w<=0)return;
   var a=up.position;var b=low.position;var c=hand.position;float tip=TipLength(right);
   var natural=c+hand.TransformDirection(handDir)*tip;var tgt=Vector3.Lerp(natural,target,w);
   var wrist=tgt-(tgt-a).normalized*tip;
   float la=(b-a).magnitude,lb=(c-b).magnitude;var d=wrist-a;float dist=Mathf.Clamp(d.magnitude,Mathf.Abs(la-lb)+1e-3f,la+lb-1e-3f);var dir=d.sqrMagnitude>1e-8f?d.normalized:Vector3.down;
   float cos=Mathf.Clamp((la*la+dist*dist-lb*lb)/(2*la*dist),-1,1),sin=Mathf.Sqrt(1-cos*cos);
   var pd=Vector3.ProjectOnPlane(pole,dir);if(pd.sqrMagnitude<1e-6f)pd=Vector3.ProjectOnPlane(Vector3.down,dir);pd.Normalize();
   var elbow=a+dir*(la*cos)+pd*(la*sin);
   up.rotation=Quaternion.FromToRotation(b-a,elbow-a)*up.rotation;
   b=low.position;c=hand.position;low.rotation=Quaternion.FromToRotation(c-b,a+dir*dist-b)*low.rotation;
   c=hand.position;var hd=hand.TransformDirection(handDir);var want=tgt-c;if(want.sqrMagnitude>1e-6f)hand.rotation=Quaternion.FromToRotation(hd,want.normalized)*hand.rotation;
  }
 }
}
