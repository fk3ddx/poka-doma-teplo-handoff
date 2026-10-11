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
 }
}
