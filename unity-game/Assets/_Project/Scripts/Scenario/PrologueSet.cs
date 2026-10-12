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
  public bool IsOn{get;private set;}
  public void On(bool on){IsOn=on;for(int i=0;i<stairLights.Length;i++)if(stairLights[i])stairLights[i].intensity=on?stairIntensity[i]:0;for(int i=0;i<yardLights.Length;i++)if(yardLights[i])yardLights[i].intensity=on?yardIntensity[i]:0;if(blinkLight)blinkLight.intensity=on?blinkIntensity:0;}
  // 3.6: the third-floor bulb flickers (a dying bulb: mostly on, short irregular drop-outs)
  void Update(){if(!IsOn||!blinkLight)return;float n=Mathf.PerlinNoise(Time.time*7.3f,.37f);blinkLight.intensity=n<.32f?blinkIntensity*.05f:n<.4f?blinkIntensity*.5f:blinkIntensity;}
 }
}
