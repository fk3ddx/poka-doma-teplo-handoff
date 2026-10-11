using System;
using System.Collections;
using UnityEngine;
namespace Ataka.Scenario {
 // Optional fixed-distance QA pass. Never enabled in a normal player launch.
 public sealed class ScenarioAudioCalibration:MonoBehaviour {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-audio-calibrate")>=0)new GameObject("Audio calibration QA").AddComponent<ScenarioAudioCalibration>();
  }
  IEnumerator Start(){
   Application.runInBackground=true;Application.targetFrameRate=60;yield return null;
   var loop=FindFirstObjectByType<LoopDirector>();var sound=loop.sound;var player=loop.flat.player;
   loop.Pause(false);loop.enabled=false;player.control=false;player.enabled=false;
   sound.ResetWorld();sound.calibrating=true;UnityEngine.Random.InitState(127);
   var capture=FindFirstObjectByType<ScenarioAudioCapture>();
   if(!capture){Debug.LogError("Calibration requires -audio-capture");Application.Quit(2);yield break;}
   yield return new WaitForSeconds(1);
   var ids=new[]{"clock","phone","chime","shriek","guest_breath","step","phone_voice","phone_shriek","phone_breath","chime-ten"};
   foreach(var id in ids){
    sound.ResetWorld();yield return new WaitForSeconds(.5f);capture.Mark("calibration-"+id);
    Vector3 point=sound.listener.position+sound.listener.forward*1.5f;
    if(id=="phone_voice")sound.Speak(false,point);
    else if(id=="phone_shriek"||id=="phone_breath")sound.PlayReceiver(id);
    else if(id=="chime-ten"){
     for(int n=0;n<10;n++){capture.Mark("strike-"+n);sound.Play("chime",point);yield return new WaitForSeconds(1);}
    }else sound.Play(id,point);
    yield return new WaitForSeconds(id=="clock"?8:id=="chime-ten"?4:id=="phone_voice"?sound.PhoneDuration+1:4);
    capture.Mark("end-"+id);
   }
   sound.ResetWorld();yield return new WaitForSeconds(.4f);capture.Save();Application.Quit(0);
  }
 }
}