using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Ataka.Tests {
 public sealed class SeatRuntimeTests {
  GameSession session;ArmchairSeat seat;
  [UnitySetUp] public IEnumerator Setup(){
   Time.timeScale=1;yield return SceneManager.LoadSceneAsync("Apartment_M1");yield return null;
   session=Object.FindFirstObjectByType<GameSession>();session.NewGame();session.player.inputEnabled=false;
   seat=Object.FindFirstObjectByType<ArmchairSeat>();Assert.NotNull(seat);
   session.player.Restore(seat.exitAnchor.position,180,35,false);Physics.SyncTransforms();
  }
  [UnityTearDown] public IEnumerator Cleanup(){session.SetPaused(true);yield return null;Time.timeScale=1;AudioListener.pause=false;}
  [UnityTest] public IEnumerator ExteriorSleetSurvivesSceneLoadAndEmitsOutsideTheApartment(){
   var snow=GameObject.Find("Windblown wet snow");Assert.NotNull(snow);
   var particles=snow.GetComponent<ParticleSystem>();Assert.NotNull(particles,"Required particle module must survive serialized scene loading");
   yield return new WaitForSeconds(.2f);Assert.IsTrue(particles.isPlaying);Assert.Greater(particles.particleCount,0);
   var data=new ParticleSystem.Particle[particles.main.maxParticles];int count=particles.GetParticles(data);
   for(int i=0;i<count;i++)Assert.Less(data[i].position.z,-2,"Sleet must stay outside the apartment");
  }
  [UnityTest] public IEnumerator ChairIsReachableAndSeatedViewDropsThenExitsWithoutPenetration(){
   var player=session.player;Vector3 eye=player.view.transform.position;
   Vector3 aim=seat.transform.position+new Vector3(0,.68f,.34f);
   var target=new InteractionQuery().Resolve(new Ray(eye,(aim-eye).normalized));
   Assert.AreSame(seat,target,"E must reach chair before solid furniture blocks it");
   float oldEye=eye.y;target.Interact(session);yield return new WaitForSeconds(.22f);
   Assert.IsTrue(seat.IsTransitioning);Assert.Less(player.view.transform.position.y,oldEye);
   yield return new WaitForSeconds(.6f);
   Assert.AreSame(seat,player.Seat);Assert.IsFalse(player.GetComponent<CharacterController>().enabled);
   Assert.AreEqual(seat.eyeHeight,player.view.transform.localPosition.y,.002f);Assert.IsTrue(seat.lap.activeSelf);
   Assert.IsTrue(seat.CanExit(),"Authored standing spot must be unobstructed");
   seat.Interact(session);yield return new WaitForSeconds(.8f);
   Assert.IsNull(player.Seat);Assert.IsFalse(seat.lap.activeSelf);Assert.IsTrue(player.GetComponent<CharacterController>().enabled);
   Assert.Less(Vector3.Distance(player.transform.position,seat.exitAnchor.position),.005f);
   Assert.IsTrue(seat.CanExit());Assert.AreEqual(1.55f,player.view.transform.localPosition.y,.002f);
  }
  [UnityTest] public IEnumerator PausedTransitionFreezesAndBlockedExitKeepsPlayerSafelySeated(){
   seat.Interact(session);yield return new WaitForSeconds(.2f);session.SetPaused(true);
   Vector3 eye=session.player.view.transform.position;yield return new WaitForSecondsRealtime(.15f);
   Assert.AreEqual(eye,session.player.view.transform.position);session.SetPaused(false);session.player.inputEnabled=false;
   yield return new WaitForSeconds(.6f);
   var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=seat.exitAnchor.position+Vector3.up*.8f;obstacle.transform.localScale=new Vector3(.6f,1.6f,.6f);
   Physics.SyncTransforms();Assert.IsFalse(seat.CanExit());seat.Interact(session);yield return null;Assert.AreSame(seat,session.player.Seat);
   Object.Destroy(obstacle);yield return null;Assert.IsTrue(seat.CanExit());
   session.NewGame();session.player.inputEnabled=false;Assert.IsNull(session.player.Seat);Assert.IsFalse(seat.lap.activeSelf);Assert.IsTrue(session.player.GetComponent<CharacterController>().enabled);
  }
  [UnityTest] public IEnumerator CheckpointRestoresSeatedPoseAndLegacyCheckpointRemainsStanding(){
   string file=Path.Combine(Path.GetTempPath(),"AtakaSeat-"+System.Guid.NewGuid().ToString("N")+".json");
   var store=new CheckpointStore(file);typeof(GameSession).GetField("store",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(session,store);
   try {
    seat.Enter(session.player,true);session.SetPaused(true);session.Save();
    session.NewGame();session.Load();session.SetPaused(true);yield return null;
    Assert.AreSame(seat,session.player.Seat);Assert.AreEqual(seat.eyeHeight,session.player.view.transform.localPosition.y,.002f);Assert.IsTrue(seat.lap.activeSelf);
    var legacy=session.Capture();legacy.seatedAt=null;legacy.position=seat.exitAnchor.position;store.Save(legacy);
    session.Load();session.SetPaused(true);yield return null;
    Assert.IsNull(session.player.Seat);Assert.IsTrue(session.player.GetComponent<CharacterController>().enabled);Assert.IsFalse(seat.lap.activeSelf);
   }finally{foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(file+suffix))File.Delete(file+suffix);}
  }
 }
}
