using UnityEngine;
namespace Ataka {
 public sealed class SlidingDrawer : Interactable {
  public Transform movingPart;
  public BoxCollider envelope;
  public Vector3 closedLocalPosition;
  public float travel=.22f;
  public float distance;
  public bool opened;
  readonly Collider[] contacts=new Collider[24];
  public override string Prompt=>(opened?"Закрыть: ":"Открыть: ")+label;
  public override void Interact(GameSession session){opened=!opened;}
  public override float Capture()=>opened?1:0;
  public override float CaptureAuxiliary()=>distance;
  public override void Restore(float value){opened=value>.5f;distance=opened?travel:0;Place();}
  public override void RestoreSnapshot(ObjectState value){opened=value.value>.5f;distance=Mathf.Clamp(value.auxiliary,0,travel);Place();}
  void Place(){movingPart.localPosition=closedLocalPosition+Vector3.right*distance;}
  void Update(){
   float next=Mathf.MoveTowards(distance,opened?travel:0,.32f*Time.deltaTime);
   if(Mathf.Approximately(next,distance))return;
   var shift=movingPart.parent.TransformVector(Vector3.right*(next-distance));
   var center=envelope.transform.TransformPoint(envelope.center)+shift;
   var extents=Vector3.Scale(envelope.size*.5f,envelope.transform.lossyScale)+Vector3.one*.015f;
   int count=Physics.OverlapBoxNonAlloc(center,extents,contacts,envelope.transform.rotation,~0,QueryTriggerInteraction.Ignore);
   if(count==contacts.Length)return;
   for(int i=0;i<count;i++)if(contacts[i].GetComponentInParent<FirstPersonController>()!=null)return;
   distance=next;Place();
  }
 }
}
