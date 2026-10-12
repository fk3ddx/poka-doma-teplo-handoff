using UnityEngine;
namespace Ataka.Scenario {
 // SCENARIO 3 (prologue): the north yard, the building's back entrance and the stairwell from the ground-floor lobby to
 // the fifth-floor landing at flat 60's outer door, built by Ataka.Editor.PrologueInstaller in the M1 authored style.
 // Everything here is dark until the story switches it on (On(true)) and off again before the evening, so the flat's
 // accepted light is never touched. References only; the story drives it.
 public sealed class PrologueSet:MonoBehaviour {
  public Transform start,roofFigure,lobby,notice,door56,door59,landing5,hatch,yardSnow,swing;
  public ScenarioDoor entranceDoor;public Transform entranceInside,entranceOutside;
  public Renderer noticeRenderer;
  public Light[] stairLights,yardLights;public float[] stairIntensity,yardIntensity;public Light blinkLight;public float blinkIntensity=.43f;
  // pc29c: the third floor's downlight dies with its bulb; objects shown only while the prologue is on (the roof glow)
  public Light blinkSpot;public float blinkSpotIntensity=6;public GameObject[] onlyDuring;public Transform padlock;
  public bool IsOn{get;private set;}
  public void On(bool on){IsOn=on;for(int i=0;i<stairLights.Length;i++)if(stairLights[i])stairLights[i].intensity=on?stairIntensity[i]:0;for(int i=0;i<yardLights.Length;i++)if(yardLights[i])yardLights[i].intensity=on?yardIntensity[i]:0;if(blinkLight)blinkLight.intensity=on?blinkIntensity:0;if(blinkSpot)blinkSpot.intensity=on?blinkSpotIntensity:0;if(onlyDuring!=null)foreach(var g in onlyDuring)if(g)g.SetActive(on);}
  // 3.6: the third-floor bulb flickers (a dying bulb: mostly on, short irregular drop-outs).
  // pc29c: only the stairwell lights within a floor and a half of him burn (their soft shadows cost; nobody sees the others).
  void Update(){
   if(!IsOn)return;var cam=Camera.main;float eye=cam?cam.transform.position.y:float.NaN;
   for(int i=0;i<stairLights.Length;i++)if(stairLights[i]){bool near=float.IsNaN(eye)||Mathf.Abs(stairLights[i].transform.position.y-eye)<4.4f;stairLights[i].intensity=near?stairIntensity[i]:0;}
   if(!blinkLight)return;float n=Mathf.PerlinNoise(Time.time*7.3f,.37f);float k=n<.32f?.05f:n<.4f?.5f:1;blinkLight.intensity=blinkIntensity*k;if(blinkSpot)blinkSpot.intensity=blinkSpotIntensity*k;}
 }
}
