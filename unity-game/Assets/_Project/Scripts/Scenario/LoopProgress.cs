using System;
namespace Ataka.Scenario {
 // Progress is independent of animation duration. Only the completed scene releases its gate.
 public sealed class LoopProgress {
  static readonly LapGate[] gates={LapGate.Wardrobe,LapGate.UnderBed,LapGate.BalconyPalm,LapGate.Television,LapGate.HangingVision,LapGate.Peephole};
  public int Lap {get;private set;}=1;
  public bool EventComplete {get;private set;}
  public bool CarpetDoorUnlocked=>Lap<6&&EventComplete;
  public bool ChaseReady=>Lap==6&&EventComplete;
  public LapGate RequiredGate=>gates[Lap-1];
  public int ClockSeconds=>22*3600+(8-Lap)*60;
  public void RestoreCheckpoint(int lap){if(lap<1||lap>6)throw new ArgumentOutOfRangeException(nameof(lap));Lap=lap;EventComplete=false;}
  public bool Complete(LapGate gate){if(gate!=RequiredGate||EventComplete)return false;EventComplete=true;return true;}
  public bool CrossVestibule(bool bedroomDoorClosed,bool innerDoorClosed){
   if(!CarpetDoorUnlocked||!bedroomDoorClosed||!innerDoorClosed)return false;
   Lap++;EventComplete=false;return true;
  }
  public bool OpenEntranceIsFatal=>Lap==6;
 }
}
