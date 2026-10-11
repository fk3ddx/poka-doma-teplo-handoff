using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Ataka.Tests {
 public sealed class DrawerRuntimeTests {
  GameSession session;
  SlidingDrawer drawer;
  [UnitySetUp] public IEnumerator Setup(){
   Time.timeScale=1;yield return SceneManager.LoadSceneAsync("Apartment_M1");yield return null;
   session=Object.FindFirstObjectByType<GameSession>();session.NewGame();session.player.inputEnabled=false;
   drawer=Object.FindFirstObjectByType<SlidingDrawer>();Assert.NotNull(drawer);
   session.player.Restore(new Vector3(7.90f,11.72f,5.71f),270,48,false);Physics.SyncTransforms();
  }
  [UnityTearDown] public IEnumerator Cleanup(){session.SetPaused(true);yield return null;Time.timeScale=1;AudioListener.pause=false;}
  [UnityTest] public IEnumerator ReachableDrawerOpensAndClosesFromPlayerView(){
   // Resolve through the same occlusion query as E, not a direct reference-only test.
   Vector3 handle=drawer.transform.parent.TransformPoint(new Vector3(7.215f,.66f,5.71f));
   Vector3 eye=session.player.view.transform.position;
   var target=new InteractionQuery().Resolve(new Ray(eye,(handle-eye).normalized));
   Assert.AreSame(drawer,target,"Dresser top or frame must not occlude the reachable drawer front");
   target.Interact(session);yield return new WaitForSeconds(1);
   Assert.AreEqual(drawer.travel,drawer.distance,.001f);
   Assert.AreEqual(drawer.closedLocalPosition.x+drawer.travel,drawer.movingPart.localPosition.x,.001f);
   target.Interact(session);yield return new WaitForSeconds(1);Assert.AreEqual(0,drawer.distance,.001f);
  }
  [UnityTest] public IEnumerator PauseFreezesDrawerAndNewGameRestoresClosed(){
   drawer.Interact(session);yield return new WaitForSeconds(.2f);session.SetPaused(true);
   float partial=drawer.distance;Assert.Greater(partial,0);Assert.Less(partial,drawer.travel);
   yield return new WaitForSecondsRealtime(.15f);Assert.AreEqual(partial,drawer.distance);
   session.NewGame();session.player.inputEnabled=false;yield return null;
   Assert.IsFalse(drawer.opened);Assert.AreEqual(0,drawer.distance);Assert.AreEqual(drawer.closedLocalPosition,drawer.movingPart.localPosition);
  }
  [UnityTest] public IEnumerator DrawerStopsBeforePlayerAndResumesWhenClear(){
   session.player.Restore(new Vector3(7.57f,11.72f,5.71f),270,40,false);Physics.SyncTransforms();
   drawer.Interact(session);yield return new WaitForSeconds(1);
   Assert.Less(drawer.distance,drawer.travel-.04f,"Drawer must stop before entering the player's capsule");
   Assert.IsFalse(Physics.ComputePenetration(drawer.envelope,drawer.envelope.transform.position,drawer.envelope.transform.rotation,
    session.player.GetComponent<CharacterController>(),session.player.transform.position,session.player.transform.rotation,out _,out _));
   session.player.Restore(new Vector3(7.90f,11.72f,5.71f),270,40,false);Physics.SyncTransforms();yield return new WaitForSeconds(1);
   Assert.AreEqual(drawer.travel,drawer.distance,.001f);
  }
  [UnityTest] public IEnumerator SaveRestoresPartialDrawerAndOldSaveUsesClosedDefault(){
   string file=Path.Combine(Path.GetTempPath(),"AtakaDrawer-"+System.Guid.NewGuid().ToString("N")+".json");
   var store=new CheckpointStore(file);
   typeof(GameSession).GetField("store",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(session,store);
   try {
    drawer.RestoreSnapshot(new ObjectState(drawer.persistentId,1,.08f));session.SetPaused(true);session.Save();
    drawer.Restore(0);session.Load();session.SetPaused(true);yield return null;
    Assert.IsTrue(drawer.opened);Assert.AreEqual(.08f,drawer.distance,.001f);
    var old=session.Capture();old.objects.RemoveAll(x=>x.id==drawer.persistentId);store.Save(old);
    drawer.Restore(1);session.Load();session.SetPaused(true);yield return null;
    Assert.IsFalse(drawer.opened);Assert.AreEqual(0,drawer.distance);
   } finally {foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(file+suffix))File.Delete(file+suffix);}
  }
 }
}
