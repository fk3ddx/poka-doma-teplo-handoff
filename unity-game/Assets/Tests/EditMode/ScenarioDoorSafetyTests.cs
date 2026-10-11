using NUnit.Framework;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
using Ataka.Scenario;
namespace Ataka.Tests {
 public sealed class ScenarioDoorSafetyTests {
  [Test] public void SmallSmoothedStepsReachTheRequestedEndpoint(){
   var root=new GameObject("Isolated door endpoint regression");
   try{
    var door=root.AddComponent<ScenarioDoor>();door.leaf=new GameObject("Leaf").transform;door.leaf.SetParent(root.transform,false);
    door.leaf.localRotation=Quaternion.Euler(0,94,0);door.open=true;
    for(int i=0;i<120;i++)door.Advance(1f/60);
    Assert.That(Mathf.Abs(Mathf.DeltaAngle(95,door.leaf.localEulerAngles.y)),Is.LessThan(.01f),"Small steps must not be discarded by Quaternion.Angle equality threshold");
   }finally{Object.DestroyImmediate(root);}
  }
  [TestCase(false)] [TestCase(true)]
  public void NativeControllerGravityDoesNotGetPushedByTheOpeningDoor(bool controllerFirst){
   var setup=EditorSceneManager.GetSceneManagerSetup();
   try{
    EditorSceneManager.OpenScene("Assets/_Project/Scenario/Flat.unity");var flat=Object.FindFirstObjectByType<FlatBindings>();
    var door=flat.carpet;var body=flat.player.GetComponent<CharacterController>();
    door.leaf.localRotation=Quaternion.identity;door.open=false;door.locked=false;
    body.enabled=false;flat.player.transform.position=new Vector3(.86f,.02f,3.75f);body.enabled=true;Physics.SyncTransforms();
    for(int i=0;i<30;i++){body.Move(Vector3.down*.04f);Physics.SyncTransforms();}
    var before=flat.player.transform.position;door.open=true;bool stopped=false;
    for(int frame=0;frame<120;frame++){
     if(controllerFirst){body.Move(Vector3.down/30);Physics.SyncTransforms();}
     door.Advance(1f/60);Physics.SyncTransforms();
     if(!controllerFirst){body.Move(Vector3.down/30);Physics.SyncTransforms();}
     stopped|=door.BlockedByPlayer;
     var after=flat.player.transform.position;
     Assert.That(Vector2.Distance(new Vector2(before.x,before.z),new Vector2(after.x,after.z)),Is.LessThanOrEqualTo(.001f),"Native CharacterController.Move was pushed sideways by the rotating leaf");
    }
    Assert.That(stopped,Is.True);
    TestContext.WriteLine("NATIVE_CC before="+before+" after="+flat.player.transform.position+" playerFirst="+controllerFirst);
   }finally{if(setup.Length>0&&setup.All(v=>!string.IsNullOrEmpty(v.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);}
  }
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
    for(int n=0;n<Mathf.CeilToInt(1f/dt);n++){door.Advance(dt);Physics.SyncTransforms();}
    Assert.That(Quaternion.Angle(door.leaf.localRotation,Quaternion.Euler(0,closing?0:95*door.direction,0)),Is.LessThan(.5f),"Door must resume after the player steps clear");
    var at=door.leaf.localRotation;door.Advance(0);Assert.That(Quaternion.Angle(at,door.leaf.localRotation),Is.LessThan(.001f));
   }finally{if(setup.Length>0&&setup.All(v=>!string.IsNullOrEmpty(v.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);}
  }
 }
}
