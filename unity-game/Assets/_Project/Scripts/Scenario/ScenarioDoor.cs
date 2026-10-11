using UnityEngine;
namespace Ataka.Scenario {
 public sealed class ScenarioDoor:MonoBehaviour {
  public Transform leaf;public bool locked,open;public float direction=1;float shake;
  public void Toggle(){if(locked){shake=.3f;return;}open=!open;}
  void Update(){if(!leaf)return;float target=open?direction*95:0;if(shake>0){shake-=Time.deltaTime;target=Mathf.Sin(shake*90)*2;}leaf.localRotation=Quaternion.Slerp(leaf.localRotation,Quaternion.Euler(0,target,0),1-Mathf.Exp(-Time.deltaTime*10));}
 }
}
