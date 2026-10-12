using UnityEngine;
namespace Ataka.Scenario {
 public sealed class ScenarioDoor:MonoBehaviour {
  public Transform leaf;public bool locked,open;public float direction=1;
  public float closedAngle;public bool scripted;float shake;
  BoxCollider[] solids;CharacterController playerBody;bool openingNudge;
  public bool BlockedByPlayer{get;private set;}
  public bool IsClosed => !open && leaf && Quaternion.Angle(leaf.localRotation,Quaternion.identity)<.5f;
  public void Toggle(){if(locked){shake=.3f;return;}open=!open;openingNudge=open&&OpeningRestricted();}
  // Review05: a blocked handle still clicks and opens a safe visible crack, never pushing the capsule.
  public string ToggleWithCue(){Toggle();return locked||openingNudge?"latch":open?"door_open":"door_close";}
  void ResolveSolids(){
   if(solids==null&&leaf)solids=leaf.GetComponentsInChildren<BoxCollider>(true);
   if(!playerBody){var p=FindFirstObjectByType<ScenarioPlayer>();if(p)playerBody=p.GetComponent<CharacterController>();}
  }
  bool OpeningRestricted(){
   if(!leaf)return false;ResolveSolids();
   float current=RotationDistance(Quaternion.identity,leaf.localRotation);if(current>=20)return false;
   for(float angle=current+1;angle<21;angle+=1)if(TouchesPlayer(Quaternion.Euler(0,direction*Mathf.Min(angle,20),0)))return true;
   return false;
  }
  bool TouchesPlayer(Quaternion rotation){
   if(!playerBody||!playerBody.enabled||!playerBody.gameObject.activeInHierarchy)return false;
   var world=leaf.parent?leaf.parent.rotation*rotation:rotation;var delta=world*Quaternion.Inverse(leaf.rotation);
   foreach(var box in solids){
    if(!box||!box.enabled||box.isTrigger||!box.gameObject.activeInHierarchy)continue;
    var center=leaf.position+delta*(box.transform.TransformPoint(box.center)-leaf.position);
    var half=Vector3.Scale(box.size*.5f,Abs(box.transform.lossyScale));
    if(CapsuleTouchesBox(playerBody,center,delta*box.transform.rotation,half,.02f))return true;
   }
   return false;
  }
  void Update(){Advance(Time.deltaTime);}
  // Use the live capsule pose, not an overlap query against potentially stale physics broadphase state.
  // No CharacterController.Move, teleport, or camera correction is performed by a door.
  public void Advance(float dt){
   BlockedByPlayer=false;if(!leaf||scripted||dt<=0)return;
   ResolveSolids();
   if(!open||openingNudge&&!OpeningRestricted())openingNudge=false;
   BlockedByPlayer=openingNudge;
   float target=open?direction*(openingNudge?5:95):direction*closedAngle;
   if(shake>0){shake-=dt;target+=Mathf.Sin(shake*90)*2;}
   var start=leaf.localRotation;var next=Quaternion.Slerp(start,Quaternion.Euler(0,target,0),1-Mathf.Exp(-dt*10));
   float angle=RotationDistance(start,next);if(angle<.0001f)return;
   int steps=Mathf.Max(1,Mathf.CeilToInt(angle/1));
   var safe=start;
   for(int step=1;step<=steps;step++){
    var rotation=Quaternion.Slerp(start,next,(float)step/steps);
    if(TouchesPlayer(rotation)){BlockedByPlayer=true;Apply(safe);return;}
    safe=rotation;
   }
   Apply(next);
  }
  void Apply(Quaternion rotation){
   if(RotationDistance(leaf.localRotation,rotation)<.0001f)return;
   leaf.localRotation=rotation;
   // CharacterController.Move later in the frame must see this collider at the same pose as its renderer.
   if(Application.isPlaying)Physics.SyncTransforms();
  }
  // Quaternion.Angle intentionally treats very similar quaternions as equal (~0.16 degrees).
  // Applying that threshold to each smoothed step stalls a door about one degree short.
  static float RotationDistance(Quaternion a,Quaternion b){
   var q=Quaternion.Inverse(a)*b;
   return 2*Mathf.Atan2(Mathf.Sqrt(q.x*q.x+q.y*q.y+q.z*q.z),Mathf.Abs(q.w))*Mathf.Rad2Deg;
  }
  static Vector3 Abs(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
  static float PointBoxDistanceSquared(Vector3 p,Vector3 half){var d=Vector3.Max(Abs(p)-half,Vector3.zero);return d.sqrMagnitude;}
  public static bool CapsuleTouchesBox(CharacterController body,Vector3 center,Quaternion rotation,Vector3 half,float clearance){
   var scale=Abs(body.transform.lossyScale);float radius=body.radius*Mathf.Max(scale.x,scale.z);
   float segment=Mathf.Max(0,body.height*scale.y*.5f-radius);
   var inv=Quaternion.Inverse(rotation);var local=inv*(body.transform.TransformPoint(body.center)-center);var axis=inv*body.transform.up;
   float distance;
   if(Mathf.Abs(axis.y)>.99999f){
    // Exact for our upright doors, including capsule end caps and crouching.
    var d=Vector3.Max(Abs(local)-new Vector3(half.x,half.y+segment,half.z),Vector3.zero);distance=d.sqrMagnitude;
   }else{
    // Convex distance on a segment for a tilted box; 30 iterations and a 0.1mm conservative margin.
    var a=local-axis*segment;var b=local+axis*segment;float lo=0,hi=1;
    for(int i=0;i<30;i++){float t1=(2*lo+hi)/3,t2=(lo+2*hi)/3;if(PointBoxDistanceSquared(Vector3.Lerp(a,b,t1),half)<PointBoxDistanceSquared(Vector3.Lerp(a,b,t2),half))hi=t2;else lo=t1;}
    distance=PointBoxDistanceSquared(Vector3.Lerp(a,b,(lo+hi)*.5f),half);clearance+=.0001f;
   }
   return distance<(radius+clearance)*(radius+clearance);
  }
 }
}
