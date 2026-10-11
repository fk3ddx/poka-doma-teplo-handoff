using UnityEngine;
namespace Ataka.Scenario {
 public sealed class ScenarioDoor:MonoBehaviour {
  public Transform leaf;public bool locked,open;public float direction=1;
  public float closedAngle;public bool scripted;float shake;
  BoxCollider[] solids;CharacterController playerBody;
  public bool BlockedByPlayer{get;private set;}
  public bool IsClosed => !open && leaf && Quaternion.Angle(leaf.localRotation,Quaternion.identity)<.5f;
  public void Toggle(){if(locked){shake=.3f;return;}open=!open;}
  void Update(){Advance(Time.deltaTime);}
  // Use the live capsule pose, not an overlap query against potentially stale physics broadphase state.
  // No CharacterController.Move, teleport, or camera correction is performed by a door.
  public void Advance(float dt){
   BlockedByPlayer=false;if(!leaf||scripted||dt<=0)return;
   if(solids==null)solids=leaf.GetComponentsInChildren<BoxCollider>(true);
   if(!playerBody){var p=FindFirstObjectByType<ScenarioPlayer>();if(p)playerBody=p.GetComponent<CharacterController>();}
   float target=open?direction*95:direction*closedAngle;
   if(shake>0){shake-=dt;target+=Mathf.Sin(shake*90)*2;}
   var start=leaf.localRotation;var next=Quaternion.Slerp(start,Quaternion.Euler(0,target,0),1-Mathf.Exp(-dt*10));
   float angle=Quaternion.Angle(start,next);if(angle<.0001f)return;
   int steps=Mathf.Max(1,Mathf.CeilToInt(angle/1));
   var originalWorld=leaf.rotation;var safe=start;
   for(int step=1;step<=steps;step++){
    var rotation=Quaternion.Slerp(start,next,(float)step/steps);
    var world=leaf.parent?leaf.parent.rotation*rotation:rotation;
    var delta=world*Quaternion.Inverse(originalWorld);
    foreach(var box in solids){
     if(!box||!box.enabled||box.isTrigger||!box.gameObject.activeInHierarchy)continue;
     var center=leaf.position+delta*(box.transform.TransformPoint(box.center)-leaf.position);
     var scale=Abs(box.transform.lossyScale);var half=Vector3.Scale(box.size*.5f,scale);
     if(playerBody&&playerBody.enabled&&playerBody.gameObject.activeInHierarchy&&CapsuleTouchesBox(playerBody,center,delta*box.transform.rotation,half,.02f)){
      BlockedByPlayer=true;Apply(safe);return;
     }
    }
    safe=rotation;
   }
   Apply(next);
  }
  void Apply(Quaternion rotation){
   if(Quaternion.Angle(leaf.localRotation,rotation)<.0001f)return;
   leaf.localRotation=rotation;
   // CharacterController.Move later in the frame must see this collider at the same pose as its renderer.
   if(Application.isPlaying)Physics.SyncTransforms();
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
