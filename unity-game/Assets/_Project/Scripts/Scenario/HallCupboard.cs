using UnityEngine;
namespace Ataka.Scenario {
 // A ready-made cupboard leaf uses the same capsule-safe movement as architectural doors.
 public sealed class HallCupboard:MonoBehaviour {
  public ScenarioDoor door;public Transform interaction;public Transform pickup;float closeAt=-1;
  public bool ReadyToTake=>door.open&&Quaternion.Angle(Quaternion.identity,door.leaf.localRotation)>65;
  public void ResetForLap(){closeAt=-1;door.locked=false;door.open=false;}
  public void Take(ScenarioPlayer player){if(!ReadyToTake||player.hasFlashlight)return;player.hasFlashlight=true;pickup.gameObject.SetActive(false);closeAt=Time.time+.8f;}
  void Update(){if(closeAt>=0&&Time.time>=closeAt){door.open=false;closeAt=-1;}}
 }
}
