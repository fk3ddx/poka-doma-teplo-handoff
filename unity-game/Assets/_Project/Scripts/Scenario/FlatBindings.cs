using UnityEngine;
namespace Ataka.Scenario {
 public sealed class FlatBindings:MonoBehaviour {
  public ScenarioPlayer player;public ScenarioLook look;public Transform apartment;public ScenarioDressing dressing;
  public ScenarioDoor entrance,kitchen,bath,bedroom,carpet,vestibuleInner,balconyDoor,living;
  public Renderer[] chaseOverlapRenderers;public Collider[] chaseOverlapColliders;
  public Transform sourceVestibule,destinationVestibule,wardrobe,bed,balcony,phone,television,torchPickup;
  public Transform[] wardrobeLeaves; public ClockFace clock; public GameObject[] conditionGroups;
  public GameObject guest,grandmother,slipper,stool;public Light televisionLight;public Renderer televisionScreen;
 }
}
