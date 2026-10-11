using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Timeline;
using Ataka.Scenario;
namespace Ataka.Tests {
 public sealed class ScenarioSceneTests {
  [Test]public void TimelinesHavePersistedCueTracksAndExactCriticalDurations(){
   foreach(var item in new[]{("DeathShort",3.5),("Hanging",6.1),("UnderBed",1.4),("Peephole",7.0)}){
    var t=AssetDatabase.LoadAssetAtPath<TimelineAsset>("Assets/_Project/Scenario/"+item.Item1+".playable");Assert.That(t,Is.Not.Null);
    var tracks=t.GetOutputTracks().ToArray();Assert.That(tracks.Length,Is.EqualTo(1));var clips=tracks[0].GetClips().ToArray();Assert.That(clips.Length,Is.EqualTo(1));Assert.That(clips[0].duration,Is.EqualTo(item.Item2).Within(.001));Assert.That(clips[0].asset,Is.InstanceOf<ScenarioCue>());Assert.That(AssetDatabase.Contains(clips[0].asset),Is.True);
   }
  }
  [Test]public void FlatContainsSixLapDefinitionsAndSixPooledModules(){
   var setup=EditorSceneManager.GetSceneManagerSetup();
   try{EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var loop=Object.FindFirstObjectByType<LoopDirector>();Assert.That(loop.laps.Length,Is.EqualTo(6));Assert.That(loop.chase.pool.Length,Is.EqualTo(6));for(int i=0;i<6;i++){Assert.That(loop.laps[i].minute,Is.EqualTo(7-i));Assert.That((int)loop.laps[i].gate,Is.EqualTo(i));}
    Assert.That(loop.flat.sourceVestibule,Is.Not.Null);Assert.That(loop.flat.destinationVestibule,Is.Not.Null);Assert.That(loop.flat.player.hasFlashlight,Is.False);
   }finally{if(setup.Length>0&&setup.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);}
  }
  [Test]public void VestibuleDoorsMatchAfterMappingAndLeaveCapsuleClearance(){
   var setup=EditorSceneManager.GetSceneManagerSetup();
   try{EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=Object.FindFirstObjectByType<FlatBindings>();
    var from=flat.sourceVestibule;var to=flat.destinationVestibule;
    var mapped=VestibuleMapping.Map(new Pose(flat.vestibuleInner.transform.position,flat.vestibuleInner.transform.rotation),from,to);
    Assert.That(Vector3.Distance(mapped.position,flat.entrance.transform.position),Is.LessThan(.001f),"Door must not jump when the chamber maps");
    Assert.That(Quaternion.Angle(mapped.rotation,flat.entrance.transform.rotation),Is.LessThan(.01f));
    foreach(var pair in new[]{(flat.carpet,from),(flat.entrance,to)}){
     var door=pair.Item1;var frame=pair.Item2;var collider=door.leaf.GetComponentInChildren<BoxCollider>();
     for(int angle=0;angle<=95;angle+=5){door.leaf.localRotation=Quaternion.Euler(0,angle*door.direction,0);Physics.SyncTransforms();
      foreach(float x in new[]{-.18f,.18f})foreach(float z in new[]{-.16f,.16f}){
       var center=frame.TransformPoint(new Vector3(x,1,z));var closest=collider.ClosestPoint(center);
       Assert.That(Vector3.Distance(center,closest),Is.GreaterThan(.235f),"Door sweep intersects transfer capsule at "+angle);
      }
     }
    }
    var sourceParts=from.GetComponentsInChildren<BoxCollider>();var destinationParts=to.GetComponentsInChildren<BoxCollider>();Assert.That(sourceParts.Length,Is.EqualTo(destinationParts.Length));
    for(int n=0;n<sourceParts.Length;n++){Assert.That(sourceParts[n].transform.localPosition,Is.EqualTo(destinationParts[n].transform.localPosition));Assert.That(sourceParts[n].transform.localScale,Is.EqualTo(destinationParts[n].transform.localScale));}
   }finally{if(setup.Length>0&&setup.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);}
  }
  [Test]public void ChaseRestoresPreviouslyDisabledApartmentComponents(){
   var setup=EditorSceneManager.GetSceneManagerSetup();
   try{EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var chase=Object.FindFirstObjectByType<ChaseCorridor>();
    var hidden=chase.flat.balconyDoor.leaf.GetComponentInChildren<Renderer>();Assert.That(hidden.enabled,Is.False);
    var collider=chase.flat.apartment.GetComponentInChildren<Collider>();collider.enabled=false;
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    typeof(ChaseCorridor).GetMethod("Start",flags).Invoke(chase,null);
    var visibility=typeof(ChaseCorridor).GetMethod("FlatVisible",flags);visibility.Invoke(chase,new object[]{false});visibility.Invoke(chase,new object[]{false});visibility.Invoke(chase,new object[]{true});
    Assert.That(hidden.enabled,Is.False,"Hidden balcony surface must stay hidden after restart");Assert.That(collider.enabled,Is.False);
    Assert.That(chase.flat.apartment.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),Is.True);
   }finally{if(setup.Length>0&&setup.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);}
  }

  [Test]public void ReadyFurnitureUsesHumanScaleAndMatchingCollision(){
   var setup=EditorSceneManager.GetSceneManagerSetup();
   try{EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");
    foreach(var entry in new[]{("SovietSofa",.85f),("SovietArmchair",.85f),("SovietCoffeeTable",.45f)}){
     var root=GameObject.Find("READY CC0 "+entry.Item1);Assert.That(root,Is.Not.Null);
     var renderers=root.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
     Assert.That(bounds.size.y,Is.EqualTo(entry.Item2).Within(.002f),entry.Item1+" height");Assert.That(bounds.min.y,Is.EqualTo(0).Within(.002f));
     Physics.SyncTransforms();var solid=root.GetComponent<BoxCollider>();Assert.That(Vector3.Distance(bounds.size,solid.bounds.size),Is.LessThan(.002f));Assert.That(Vector3.Distance(bounds.center,solid.bounds.center),Is.LessThan(.002f));
    }
   }finally{if(setup.Length>0&&setup.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);}
  }

 }
}
