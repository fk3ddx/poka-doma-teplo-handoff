using UnityEngine;
namespace Ataka.Scenario {
 // 11.6б: her hand in the chain gap becomes the READY claw hand (the Guest's own forearm+hand+claws that the 7.4 death
 // uses, same normalisation) while the fingers stretch. The Katcho hand is one bone (a block): while the claw is on,
 // that hand is collapsed (GranRig handScale ≈ 0) so there are never two hands. The claw sits on her rendered wrist,
 // its forearm part sunk back into her sleeve, fingers along her forearm, palm down; `stretch` lengthens it along the
 // fingers only. Runs after GranRig (500) and before DoorChain (600), which may hang the chain on its tip.
 [DefaultExecutionOrder(550)]
 public sealed class GapClaw:MonoBehaviour {
  public const float Length=.42f,Sink=.45f; // normalised model length (forearm end → claw tips) and the share hidden in the sleeve
  GranRig rig;Transform wrapper;[System.NonSerialized] public float stretch=.62f;
  public bool On{get;private set;}
  public Vector3 Tip=>wrapper?wrapper.position+wrapper.forward*Length*stretch:rig.Tip(true);
  public float Protrusion=>Length*stretch*(1-Sink); // how far the claw reaches past her wrist
  public static GapClaw Create(GranRig rig,FaceTearDeath tear){
   if(!rig||!tear)return null;var w=tear.MakeLooseClaw(Length);if(!w)return null;
   var c=new GameObject("Finale gap claw").AddComponent<GapClaw>();c.rig=rig;c.wrapper=w;w.SetParent(c.transform,true);return c;}
  public void Show(bool on){On=on;if(wrapper)wrapper.gameObject.SetActive(on);rig.handScale=on?.02f:1;}
  void LateUpdate(){
   if(!On||!wrapper||!rig||!rig.Ready)return;
   var wrist=rig.handR.position;var dir=wrist-rig.lowerR.position;if(dir.sqrMagnitude<1e-6f)return;dir.Normalize();
   wrapper.SetPositionAndRotation(wrist-dir*Length*stretch*Sink,Quaternion.LookRotation(dir,Vector3.up));wrapper.localScale=new Vector3(1,1,stretch);
  }
  void OnDestroy(){if(wrapper)Destroy(wrapper.gameObject);}
 }
}
