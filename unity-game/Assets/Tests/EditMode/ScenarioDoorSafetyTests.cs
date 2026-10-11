using NUnit.Framework;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
using Ataka.Scenario;
namespace Ataka.Tests {
 public sealed class ScenarioDoorSafetyTests {
  [TestCase(.86f,1f/60,false)] [TestCase(.97f,1f/60,false)] [TestCase(.86f,.25f,false)] [TestCase(.86f,1f/60,true)]
  public void ActualCarpetDoorNeverSweepsThroughPlayerAndResumesWhenClear(float x,float dt,bool closing){
   var setup=EditorSceneManager.GetSceneManagerSetup();
   try{
    EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=Object.FindFirstObjectByType<FlatBindings>();
    var door=flat.carpet;var player=flat.player;var body=player.GetComponent<CharacterController>();
    body.enabled=false;player.transform.position=new Vector3(x,.02f,3.75f);body.enabled=true;
    door.leaf.localRotation=closing?Quaternion.Euler(0,95*door.direction,0):Quaternion.identity;door.open=!closing;door.locked=false;
    Physics.SyncTransforms();var before=player.transform.position;bool stopped=false;
    for(int n=0;n<180;n++){
     door.Advance(dt);Physics.SyncTransforms();stopped|=door.BlockedByPlayer;
     foreach(var box in door.leaf.GetComponentsInChildren<BoxCollider>()){
      bool hit=Physics.ComputePenetration(box,box.transform.position,box.transform.rotation,body,body.transform.position,body.transform.rotation,out _,out float depth);
      Assert.That(hit&&depth>.001f,Is.False,"Moving leaf penetrated the stationary player");
     }
    }
    Assert.That(stopped,Is.True,"The two reported approach positions must stop the leaf");Assert.That(player.transform.position,Is.EqualTo(before),"Door must never reposition camera/player");
    body.enabled=false;player.transform.position=new Vector3(1.1f,.02f,3.05f);body.enabled=true;Physics.SyncTransforms();
    for(int n=0;n<180;n++){door.Advance(dt);Physics.SyncTransforms();}
    Assert.That(Quaternion.Angle(door.leaf.localRotation,Quaternion.Euler(0,closing?0:95*door.direction,0)),Is.LessThan(.5f),"Door must resume after the player steps clear");
    var at=door.leaf.localRotation;door.Advance(0);Assert.That(Quaternion.Angle(at,door.leaf.localRotation),Is.LessThan(.001f));
   }finally{if(setup.Length>0&&setup.All(v=>!string.IsNullOrEmpty(v.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);}
  }
 }
}
