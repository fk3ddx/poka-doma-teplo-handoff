using UnityEngine;
namespace Ataka {
 public sealed class ArmchairSeat : Interactable {
  public Transform seatedAnchor, exitAnchor;
  public GameObject lap;
  public float eyeHeight=1.12f, duration=.65f;
  public bool IsTransitioning=>owner!=null&&elapsed<duration;
  public float Facing=>seatedAnchor.eulerAngles.y;
  FirstPersonController owner;
  bool exiting;
  float elapsed,fromEye;
  Vector3 fromPosition;
  Quaternion fromRotation;
  float fromPitch;
  readonly Collider[] obstacles=new Collider[32];
  public override string Prompt=>owner==null?"Сесть в кресло":"Встать из кресла";
  public override void Interact(GameSession session){
   if(IsTransitioning)return;
   if(owner==null)Enter(session.player,false);
   else if(CanExit())BeginExit();
   else session.Notify("Перед креслом недостаточно места, чтобы встать.");
  }
  public void Enter(FirstPersonController player,bool immediate){
   if(player.Seat!=null)player.Seat.Release();
   owner=player;player.Seat=this;player.GetComponent<CharacterController>().enabled=false;
   player.crouched=false;exiting=false;elapsed=immediate?duration:0;
   fromPosition=player.transform.position;fromEye=player.view.transform.localPosition.y;
   float yaw=Facing+Mathf.Clamp(Mathf.DeltaAngle(Facing,player.transform.eulerAngles.y),-75,75);
   fromRotation=player.transform.rotation;fromPitch=player.pitch;
   if(immediate)player.transform.rotation=Quaternion.Euler(0,yaw,0);
   lap.SetActive(true);Place();player.SuppressLook();
  }
  public bool CanExit(){
   Physics.SyncTransforms();Vector3 p=exitAnchor.position;
   int count=Physics.OverlapCapsuleNonAlloc(p+Vector3.up*.27f,p+Vector3.up*1.45f,.245f,obstacles,~0,QueryTriggerInteraction.Ignore);
   if(count==obstacles.Length)return false;
   for(int i=0;i<count;i++)if(obstacles[i].GetComponentInParent<FirstPersonController>()==null)return false;
   return true;
  }
  void BeginExit(){exiting=true;elapsed=0;fromPosition=owner.transform.position;fromEye=owner.view.transform.localPosition.y;}
  void Update(){
   if(owner==null||elapsed>=duration||Time.timeScale==0)return;
   elapsed=Mathf.Min(duration,elapsed+Time.deltaTime);Place();
   if(exiting&&elapsed>=duration){
    var player=owner;Vector3 p=exitAnchor.position;float yaw=player.transform.eulerAngles.y,pitch=player.pitch;
    Release();player.Restore(p,yaw,pitch,false);
   }
  }
  void Place(){
   float t=Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.01f,duration));
   owner.transform.position=Vector3.Lerp(fromPosition,exiting?exitAnchor.position:seatedAnchor.position,t);
   owner.view.transform.localPosition=Vector3.up*Mathf.Lerp(fromEye,exiting?1.55f:eyeHeight,t);
   if(!exiting&&elapsed<duration){
    owner.transform.rotation=Quaternion.Slerp(fromRotation,Quaternion.Euler(0,Facing+25,0),t);
    owner.pitch=Mathf.Lerp(fromPitch,8,t);owner.view.transform.localRotation=Quaternion.Euler(owner.pitch,0,0);
   }
   lap.SetActive(!exiting||t<.65f);
  }
  public void Release(){if(owner!=null)owner.Seat=null;owner=null;lap.SetActive(false);elapsed=duration;}
  // Occupancy belongs to the player's checkpoint, not the furniture state.
  public override float Capture()=>0;
  public override void Restore(float value)=>Release();
 }
}
