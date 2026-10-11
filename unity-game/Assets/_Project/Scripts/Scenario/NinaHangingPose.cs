using UnityEngine;
namespace Ataka.Scenario {
 // Uses the approved Katcho rig and a held pose, not a new character mesh.
 public sealed class NinaHangingPose : MonoBehaviour {
  public Animation animationPlayer; public AnimationClip hangingPose;
  public Transform neck,head,leftFoot,rightFoot;
  public Vector3 NeckPosition=>neck.position;
  public Vector3 FacePosition=>head.position+transform.forward*.07f+Vector3.up*.06f;
  public Vector3 FeetPosition=>(leftFoot.position+rightFoot.position)*.5f;
  public void Evaluate(float seconds,Vector3 viewer){
   animationPlayer.Stop();animationPlayer.enabled=false;
   hangingPose.SampleAnimation(animationPlayer.gameObject,0);
   var anchor=new Vector3(1.65f,1.55f,2.3f);var toward=viewer-anchor;toward.y=0;
   if(toward.sqrMagnitude<.001f)toward=Vector3.forward;
   var front=Quaternion.LookRotation(toward);var away=Quaternion.LookRotation(-toward);
   transform.rotation=Quaternion.Slerp(away,front,Mathf.SmoothStep(0,1,Mathf.Clamp01(seconds-3.5f)));
   // Rotate around the suspension point, so the rope stays attached to the neck.
   transform.position+=anchor-neck.position;
  }
 }
}
