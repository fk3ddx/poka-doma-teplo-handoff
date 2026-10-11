using UnityEngine;
namespace Ataka.Scenario {
 public sealed class ClockFace:MonoBehaviour {
  public Transform hour,minute,second,pendulum;public TextMesh label;public float seconds=22*3600+7*60;
  public void Show(float time){seconds=time;if(label){int t=Mathf.FloorToInt(time);label.text=$"{t/3600:00}:{t/60%60:00}";}}
  void Update(){if(hour)hour.localRotation=Quaternion.Euler(0,0,-seconds/120);if(minute)minute.localRotation=Quaternion.Euler(0,0,-seconds/10);if(second)second.localRotation=Quaternion.Euler(0,0,-seconds*6);if(pendulum)pendulum.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.time*3)*6);}
 }
}
