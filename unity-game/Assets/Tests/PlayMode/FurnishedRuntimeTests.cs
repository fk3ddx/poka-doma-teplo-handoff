using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Ataka.Tests {
 public class FurnishedRuntimeTests {
  GameSession session;
  [UnitySetUp] public IEnumerator Setup(){Time.timeScale=1;yield return SceneManager.LoadSceneAsync("Apartment_M1");yield return null;session=Object.FindFirstObjectByType<GameSession>();session.NewGame();session.player.inputEnabled=false;}
  [UnityTearDown] public IEnumerator Cleanup(){session.SetPaused(true);yield return null;Time.timeScale=1;AudioListener.pause=false;}
  [UnityTest] public IEnumerator BlackoutSwapsBakedMapsAndRestoresLivingFixtures(){
   var lighting=Object.FindFirstObjectByType<ApartmentLightingStates>();Assert.NotNull(lighting);Assert.IsTrue(lighting.enabled);
   var living=lighting.switches.Single(x=>x.persistentId=="lamp-living");living.Restore(0);yield return null;yield return null;
   Assert.IsTrue(lighting.supplementary.All(x=>x.intensity==0));
   foreach(var sw in lighting.switches)sw.Restore(0);yield return null;yield return null;
   Assert.AreSame(lighting.blackout.colors[0],LightmapSettings.lightmaps[0].lightmapColor);
   foreach(var sw in lighting.switches)sw.Restore(1);yield return null;yield return null;
   Assert.AreSame(lighting.warm.colors[0],LightmapSettings.lightmaps[0].lightmapColor);
   Assert.IsTrue(lighting.supplementary.All(x=>x.intensity>0));
  }
  [UnityTest] public IEnumerator BothStairFlightsCanBeWalkedDownAndBackUp(){
   session.player.Restore(new Vector3(7.12f,11.72f,7.5f),0,0,false);session.player.inputEnabled=false;
   var body=session.player.GetComponent<CharacterController>();
   yield return Walk(body,new Vector2(7.12f,11.08f));yield return Walk(body,new Vector2(8.27f,11.08f));yield return Walk(body,new Vector2(8.27f,7.40f));
   Assert.AreEqual(8.9f,body.transform.position.y,.16f,"Fourth floor must be reached on foot");
   yield return Walk(body,new Vector2(8.27f,11.08f));yield return Walk(body,new Vector2(7.12f,11.08f));yield return Walk(body,new Vector2(7.12f,7.50f));
   Assert.AreEqual(11.7f,body.transform.position.y,.16f,"Fifth floor must be reachable without jumping");
  }
  static IEnumerator Walk(CharacterController body,Vector2 destination){
   int frames=0;while(Vector2.Distance(new Vector2(body.transform.position.x,body.transform.position.z),destination)>.025f && frames++<240){
    var p=body.transform.position;var delta=Vector2.ClampMagnitude(destination-new Vector2(p.x,p.z),.03f);body.Move(new Vector3(delta.x,-.09f,delta.y));yield return null;
   }
   Assert.Less(frames,240,"Stuck on stairs at "+body.transform.position+" towards "+destination);
  }
 }
}
