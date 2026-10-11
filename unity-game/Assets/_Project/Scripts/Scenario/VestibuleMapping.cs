using UnityEngine;
namespace Ataka.Scenario {
 public static class VestibuleMapping {
  // Frames have matching forward directions into their inner doors; no extra 180-degree turn.
  public static Pose Map(Pose worldPose,Transform from,Transform to){
   var local=from.InverseTransformPoint(worldPose.position);
   var rotation=Quaternion.Inverse(from.rotation)*worldPose.rotation;
   return new Pose(to.TransformPoint(local),to.rotation*rotation);
  }
 }
}
