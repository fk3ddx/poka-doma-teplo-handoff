using UnityEngine;
namespace Ataka.Scenario {
 // Head look-at over the authored clip (the Guest creature's Idle turns its head aside). GuestInstaller finds the
 // head bone (parent of "Jaw") and its face direction in bone space in the bind pose; scenes set target (Misha's eyes).
 public sealed class GuestLookAt:MonoBehaviour {
  public Transform headBone;public Vector3 headFaceLocal;
  [System.NonSerialized] public Vector3? target;[System.NonSerialized] public float weight=1;
  void LateUpdate(){
   if(!target.HasValue||!headBone||headFaceLocal.sqrMagnitude<1e-6f)return;
   var face=headBone.rotation*headFaceLocal;var want=target.Value-headBone.position;if(want.sqrMagnitude<1e-4f)return;
   headBone.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(face,want.normalized),Mathf.Clamp01(weight))*headBone.rotation;
  }
 }
}
