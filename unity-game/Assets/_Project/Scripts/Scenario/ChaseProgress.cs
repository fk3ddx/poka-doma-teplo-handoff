using UnityEngine;
namespace Ataka.Scenario {
 public sealed class ChaseProgress {
  public const float ModuleLength=6, WarmAfter=42, ClockDuration=45, WindDuration=2;
  public float Elapsed {get;private set;}
  public float Stalled {get;private set;}
  public float WindProgress {get;private set;}
  public float MissedFor {get;private set;}
  public bool WarmAllocated {get;private set;}
  public bool WarmMissed {get;private set;}
  public bool Dead {get;private set;}
  public bool Complete {get;private set;}
  public bool Wound=>WindProgress>=WindDuration;
  public float ClockSeconds=>22*3600+120-120*(Elapsed/ClockDuration);
  public float GuestDistance {get;private set;}=4;
  public void Tick(float dt,bool paused,bool moving,bool lookingBack,bool windingAtWarmClock){
   if(paused||Dead||Complete||dt<=0)return;
   Elapsed+=dt;
   if(windingAtWarmClock&&WarmAllocated&&!WarmMissed){
    Stalled=0;WindProgress=Mathf.Min(WindDuration,WindProgress+dt);GuestDistance=1.5f;return;
   }
   if(!Wound)WindProgress=0;
   if(moving){Stalled=0;GuestDistance=Mathf.MoveTowards(GuestDistance,lookingBack?1:4,dt*(lookingBack?1.5f:.7f));}
   else {Stalled+=dt;GuestDistance=Mathf.Lerp(4,1,Mathf.Clamp01(Stalled));}
   if(WarmMissed){MissedFor+=dt;if(MissedFor>=7)Dead=true;}
   if(Stalled>1.5f||GuestDistance<=1.01f&&lookingBack)Dead=true;
  }
  public bool AllocateNextModule(){if(Elapsed<WarmAfter||WarmAllocated)return false;WarmAllocated=true;return true;}
  public void PassedWarmModule(){if(WarmAllocated&&!Complete)WarmMissed=true;}
  public void Corner(){GuestDistance=Mathf.Max(1.1f,GuestDistance-1);}
  public bool PushPendulum(){if(!Wound||Dead||WarmMissed)return false;Complete=true;return true;}
 }
}
