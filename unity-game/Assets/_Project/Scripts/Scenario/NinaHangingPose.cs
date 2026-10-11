using UnityEngine;
using System.Collections.Generic;
namespace Ataka.Scenario {
 // Uses the approved Katcho rig and a held pose, not a new character mesh.
 public sealed class NinaHangingPose : MonoBehaviour {
  public Animation animationPlayer; public AnimationClip hangingPose;
  public Transform neck,head,leftFoot,rightFoot;
  public Vector3 NeckPosition=>neck.position;
  public Vector3 KnotPosition=>neck.position+transform.rotation*new Vector3(.055f,.06f,-.035f);
  Vector3 rightToe;SkinnedMeshRenderer skin;Mesh baked;readonly List<Vector3> vertices=new List<Vector3>();
  public Vector3 RightToePosition=>rightToe;
  void UpdateToe(){
   if(!skin){skin=GetComponentInChildren<SkinnedMeshRenderer>();baked=new Mesh();}
   skin.BakeMesh(baked);baked.GetVertices(vertices);rightToe=rightFoot.position;float min=float.MaxValue;
   foreach(var vertex in vertices){var p=skin.transform.TransformPoint(vertex);if(Vector3.Distance(p,rightFoot.position)>.24f||Vector3.Distance(p,rightFoot.position)>Vector3.Distance(p,leftFoot.position))continue;if(p.y<min){min=p.y;rightToe=p;}}
  }
  void OnDestroy(){if(baked)Destroy(baked);}
  public Vector3 FacePosition=>head.position+transform.forward*.07f+Vector3.up*.06f;
  public Vector3 FeetPosition=>(leftFoot.position+rightFoot.position)*.5f;
  public void Evaluate(float seconds,Vector3 viewer){
   animationPlayer.Stop();animationPlayer.enabled=false;
   hangingPose.SampleAnimation(animationPlayer.gameObject,0);
   var anchor=new Vector3(1.65f,1.72f,2.3f);var toward=viewer-anchor;toward.y=0;
   if(toward.sqrMagnitude<.001f)toward=Vector3.forward;
   var front=Quaternion.LookRotation(toward);var away=Quaternion.LookRotation(-toward);
   float turn=Mathf.Clamp01((seconds-3.5f)/1.4f);turn*=turn;
   transform.rotation=Quaternion.AngleAxis(Mathf.Sin(seconds*Mathf.PI*2/7)*6,Vector3.up)*Quaternion.Slerp(away,front,turn);
   anchor.y+=.06f;
   // Keep the knot under the hook while the approved pose turns below it.
   transform.position+=anchor-KnotPosition;UpdateToe();
  }
 }
}
