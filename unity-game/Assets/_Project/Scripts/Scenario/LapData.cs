using UnityEngine;
namespace Ataka.Scenario {
 public enum LapGate { Wardrobe, UnderBed, BalconyPalm, Television, HangingVision, Peephole }
 public enum FlatCondition { Familiar, Cold, Frozen, Blackout, Rot, RedRot }
 [CreateAssetMenu(menuName="Ataka/Scenario/Lap")]
 public sealed class LapData : ScriptableObject {
  public int number; public int hour=22,minute; public FlatCondition condition; public LapGate gate;
  [TextArea] public string thought; public string sourceSection="SCENARIO 9.4";
 }
}
