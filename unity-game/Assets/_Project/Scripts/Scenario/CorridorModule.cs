using UnityEngine;
namespace Ataka.Scenario {
 public sealed class CorridorModule:MonoBehaviour {
  public Renderer[] surfaces;public Collider[] solids;public GameObject cornerCap,straightLeftEnd;public Light lamp;public ClockFace clock;
  public const float HalfWidth=.745f,CornerZ=6-HalfWidth;
  public bool turn,warm; public int index;
  public void Visible(bool on){foreach(var r in surfaces)r.enabled=on;foreach(var c in solids)c.enabled=on;lamp.intensity=on?(warm?2:1.1f):0;}
  public void Configure(int number,bool isWarm){index=number;turn=number%3==2;warm=isWarm;
   foreach(var r in cornerCap.GetComponentsInChildren<Renderer>())r.enabled=turn;foreach(var c in cornerCap.GetComponentsInChildren<Collider>())c.enabled=turn;
   foreach(var r in straightLeftEnd.GetComponentsInChildren<Renderer>())r.enabled=!turn;foreach(var c in straightLeftEnd.GetComponentsInChildren<Collider>())c.enabled=!turn;
   lamp.color=warm?new Color(1,.706f,.42f):new Color(1,.025f,.01f);lamp.intensity=warm?2:1.1f;
  }
  public Pose Next()=>new Pose(transform.TransformPoint(turn?new Vector3(-HalfWidth,0,CornerZ):new Vector3(0,0,6)),transform.rotation*Quaternion.Euler(0,turn?-90:0,0));
  public bool Crossed(Vector3 world){var local=transform.InverseTransformPoint(world);return turn?local.x<-HalfWidth-.01f&&local.z>6-2*HalfWidth:local.z>=6;}
 }
}
