using UnityEngine;
namespace Ataka.Scenario {
 public sealed class ProxyPose:MonoBehaviour {
  Animator animator;Transform[] bones;Quaternion[] rotations;
  void Ensure(){if(bones!=null)return;animator=GetComponent<Animator>();animator.enabled=true;animator.Rebind();animator.Update(0);animator.enabled=false;bones=GetComponentsInChildren<Transform>();rotations=new Quaternion[bones.Length];for(int i=0;i<bones.Length;i++)rotations[i]=bones[i].localRotation;}
  void ResetPose(){Ensure();for(int i=1;i<bones.Length;i++)bones[i].localRotation=rotations[i];}
  Transform Bone(HumanBodyBones id)=>animator.GetBoneTransform(id);
  void Aim(HumanBodyBones from,HumanBodyBones to,Vector3 direction){var a=Bone(from);var b=Bone(to);if(a&&b)a.rotation=Quaternion.FromToRotation(b.position-a.position,direction)*a.rotation;}
  void Start(){Stand();}
  public void Stand(){ResetPose();Aim(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,-transform.up-transform.right*.08f);Aim(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,-transform.up+transform.right*.08f);}
  // Upside-down figure behind the balcony glass: arms dangle toward the ground, the right one can reach to a palm point.
  public void HangInverted(Vector3 palm,float reach){
   ResetPose();var side=Vector3.Cross(Vector3.down,transform.forward).normalized;
   Aim(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,Vector3.down-side*.12f);Aim(HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,Vector3.down);
   Aim(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,Vector3.down+side*.12f);Aim(HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,Vector3.down);
   var shoulder=Bone(HumanBodyBones.RightUpperArm);var hand=Bone(HumanBodyBones.RightHand);
   if(reach>0&&shoulder&&hand){var goal=Vector3.Lerp(hand.position,palm,Mathf.Clamp01(reach));var toward=goal-shoulder.position;Aim(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,toward);Aim(HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,toward);}
   Aim(HumanBodyBones.RightHand,HumanBodyBones.RightMiddleProximal,Vector3.down);Aim(HumanBodyBones.LeftHand,HumanBodyBones.LeftMiddleProximal,Vector3.down);
  }
  public void Crawl(Vector3 ground){ResetPose();var hips=Bone(HumanBodyBones.Hips);hips.rotation=Quaternion.FromToRotation(Bone(HumanBodyBones.Neck).position-hips.position,transform.forward)*hips.rotation;transform.position+=ground+Vector3.up*.65f-hips.position;
   Aim(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,-Vector3.up+transform.forward*.35f-transform.right*.35f);Aim(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,-Vector3.up+transform.forward*.35f+transform.right*.35f);
   Aim(HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,-Vector3.up);Aim(HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,-Vector3.up);
   Aim(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,-transform.forward+Vector3.up*.7f-transform.right*.2f);Aim(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,-transform.forward+Vector3.up*.7f+transform.right*.2f);
   Aim(HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,-Vector3.up);Aim(HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,-Vector3.up);
  }
 }
}
