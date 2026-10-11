using UnityEngine;
namespace Ataka.Scenario {
 public static class ScenarioSight {
  // A downward ray from eye height can enter the top of the player's capsule.
  // Ignore only the observer's hierarchy; walls and door leaves still block sight.
  public static bool Clear(Transform observer,Vector3 origin,Vector3 target){
   var direction=target-origin;float distance=direction.magnitude;if(distance<.001f)return true;
   foreach(var hit in Physics.RaycastAll(origin,direction/distance,distance,~(1<<31),QueryTriggerInteraction.Ignore))
    if(!hit.transform.IsChildOf(observer))return false;
   return true;
  }
 }
}
