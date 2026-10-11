using UnityEngine;
namespace Ataka.Scenario {
 public sealed class CloseupHand:MonoBehaviour {
  public Transform palm,forearm;public bool left;
  Quaternion baseRotation;Vector3 direction;
  void Awake(){baseRotation=transform.rotation;direction=(palm.position-forearm.position).normalized;}
  public void World(Vector3 target,Vector3 aim){gameObject.SetActive(true);transform.rotation=Quaternion.FromToRotation(direction,aim)*baseRotation;transform.position+=target-palm.position;}
  public void Frame(Transform camera,float t){
   bool visible=t>=.3f&&t<2.5f;gameObject.SetActive(visible);if(!visible)return;
   float enter=Mathf.Clamp01((t-.3f)/.18f),tear=Mathf.Clamp01((t-1.5f)/.7f);
   float x=(left?-1:1)*Mathf.Lerp(.45f,.145f,enter),y=t<1?-.015f:Mathf.Lerp(.15f,-.28f,tear);
   Vector3 toward=left?camera.right:-camera.right;if(t>=1)toward=Vector3.Lerp(toward,camera.up,.7f);
   transform.rotation=Quaternion.FromToRotation(direction,toward)*baseRotation;
   transform.position+=camera.position+camera.right*x+camera.up*y+camera.forward*.25f-palm.position;
  }
 }
}
