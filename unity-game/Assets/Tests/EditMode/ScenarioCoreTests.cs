using NUnit.Framework;
using UnityEngine;
using Ataka.Scenario;
namespace Ataka.Tests {
 public sealed class ScenarioCoreTests {
  [TestCase(false)][TestCase(true)]
  public void HangingSightIgnoresOwnCapsuleButStillRespectsWalls(bool wallPresent){
   var observer=new GameObject("observer");var wall=new GameObject("sight-wall");
   try{
    observer.transform.position=new Vector3(1000,0,1000);var body=observer.AddComponent<CharacterController>();body.height=1.7f;body.center=Vector3.up*.85f;body.radius=.3f;
    var eye=observer.transform.position+Vector3.up*1.55f;var feet=observer.transform.position+new Vector3(-1,.5f,-1.3f);
    wall.transform.position=Vector3.Lerp(eye,feet,.55f);wall.AddComponent<BoxCollider>().size=Vector3.one*.25f;wall.SetActive(wallPresent);Physics.SyncTransforms();
    Assert.That(ScenarioSight.Clear(observer.transform,eye,feet),Is.EqualTo(!wallPresent));
   }finally{Object.DestroyImmediate(observer);Object.DestroyImmediate(wall);}
  }
  [TestCase(new string[]{"game"},false,false)]
  [TestCase(new string[]{"game","-audio-capture","capture.wav"},true,false)]
  [TestCase(new string[]{"game","-audio-capture"},true,true)]
  [TestCase(new string[]{"game","-audio-capture","-logFile","test.log"},true,true)]
  [TestCase(new string[]{"game","-audio-capture"," "},true,true)]
  public void QaCaptureCannotSilentlyIgnoreAnInvalidOutput(string[] args,bool requested,bool invalid){
   Assert.That(ScenarioAudioCapture.ReadOutputArgument(args,out var output,out var error),Is.EqualTo(requested));
   Assert.That(error!=null,Is.EqualTo(invalid));
   if(requested&&!invalid)Assert.That(output,Is.EqualTo("capture.wav"));
  }
  [TestCase("record")][TestCase("not-started")][TestCase("stopping")][TestCase("format-change")][TestCase("overflow")]
  public void QaCaptureSilencesDeviceEvenWhenRecordingCannotContinue(string state){
   var host=new GameObject("silent-capture-test");var capture=host.AddComponent<ScenarioAudioCapture>();
   var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
   var type=typeof(ScenarioAudioCapture);
   void Set(string key,object value)=>type.GetField(key,flags).SetValue(capture,value);
   var data=new[]{.25f,-.5f,.75f,-.125f};var original=(float[])data.Clone();
   try{
    if(state!="not-started")Set("worker",new System.Threading.Thread(()=>{}));
    if(state=="stopping")Set("stopping",true);
    if(state=="format-change")Set("channels",1);
    if(state=="overflow")Set("written",(long)(1<<20));
    type.GetMethod("OnAudioFilterRead",flags).Invoke(capture,new object[]{data,2});
    Assert.That(data,Is.All.Zero,"QA audio must never reach the device, including error/exit paths");
    if(state=="record"){
     var ring=(float[])type.GetField("ring",flags).GetValue(capture);
     for(int i=0;i<original.Length;i++)Assert.That(ring[i],Is.EqualTo(original[i]),"Keep unmuted samples for the WAV writer");
     Assert.That(type.GetField("written",flags).GetValue(capture),Is.EqualTo(4L));
    }
   }finally{Set("worker",null);Object.DestroyImmediate(host);}
  }

  [Test] public void SixLapsRequireTheirOwnCompletedEventAndClosedVestibule(){
   var p=new LoopProgress();
   for(int lap=1;lap<=6;lap++){
    Assert.That(p.Lap,Is.EqualTo(lap));Assert.That(p.ClockSeconds,Is.EqualTo(22*3600+(8-lap)*60));
    foreach(LapGate g in System.Enum.GetValues(typeof(LapGate)))if(g!=p.RequiredGate)Assert.That(p.Complete(g),Is.False);
    Assert.That(p.CrossVestibule(true,true),Is.False);Assert.That(p.Complete(p.RequiredGate),Is.True);
    Assert.That(p.Complete(p.RequiredGate),Is.False);
    Assert.That(p.CrossVestibule(false,true),Is.False);Assert.That(p.CrossVestibule(true,false),Is.False);
    if(lap<6)Assert.That(p.CrossVestibule(true,true),Is.True);
   }
   Assert.That(p.CarpetDoorUnlocked,Is.False);Assert.That(p.ChaseReady,Is.True);Assert.That(p.CrossVestibule(true,true),Is.False);
  }
  [Test] public void DeathOnSixRestartsSixWithoutUnlockOrRewind(){var p=new LoopProgress();p.RestoreCheckpoint(6);p.Complete(LapGate.Peephole);p.RestoreCheckpoint(6);Assert.That(p.OpenEntranceIsFatal,Is.True);Assert.That(p.ChaseReady,Is.False);}
  [Test] public void ClosedVestibulePreservesLocalCameraPose(){
   var a=new GameObject();var b=new GameObject();
   try{a.transform.SetPositionAndRotation(new Vector3(1,0,5),Quaternion.Euler(0,180,0));b.transform.SetPositionAndRotation(new Vector3(8,0,7),Quaternion.identity);
    var local=new Vector3(.12f,1.55f,.32f);var turn=Quaternion.Euler(-23,38,0);
    var mapped=VestibuleMapping.Map(new Pose(a.transform.TransformPoint(local),a.transform.rotation*turn),a.transform,b.transform);
    Assert.That(Vector3.Distance(b.transform.InverseTransformPoint(mapped.position),local),Is.LessThan(.01f));Assert.That(Quaternion.Angle(Quaternion.Inverse(b.transform.rotation)*mapped.rotation,turn),Is.LessThan(.5f));
   }finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
  }
  [Test] public void ChaseWarmAllocationUsesTimerNotModuleIndex(){var c=new ChaseProgress();for(int i=0;i<200;i++)Assert.That(c.AllocateNextModule(),Is.False);c.Tick(41.9f,false,true,false,false);Assert.That(c.AllocateNextModule(),Is.False);c.Tick(.11f,false,true,false,false);Assert.That(c.AllocateNextModule(),Is.True);Assert.That(c.AllocateNextModule(),Is.False);}
  [Test] public void ChasePauseFreezesClockAndCatch(){var c=new ChaseProgress();c.Tick(20,false,true,false,false);c.Tick(100,true,false,false,false);Assert.That(c.Elapsed,Is.EqualTo(20));Assert.That(c.Stalled,Is.Zero);Assert.That(c.Dead,Is.False);}
  [Test] public void ChaseClockRewindsExactlyTwoMinutesIn45Seconds(){var c=new ChaseProgress();c.Tick(45,false,true,false,false);Assert.That(c.ClockSeconds,Is.EqualTo(22*3600).Within(.001));}
  [Test] public void BlockedMovementKillsAfterOneAndAHalfSeconds(){var c=new ChaseProgress();c.Tick(1,false,false,false,false);Assert.That(c.GuestDistance,Is.EqualTo(1));Assert.That(c.Dead,Is.False);c.Tick(.51f,false,false,false,false);Assert.That(c.Dead,Is.True);}
  [Test] public void WindingProtectsPlayerButPendulumIsStillRequired(){var c=new ChaseProgress();c.Tick(42,false,true,false,false);c.AllocateNextModule();c.Tick(1.9f,false,false,false,true);Assert.That(c.PushPendulum(),Is.False);c.Tick(.11f,false,false,false,true);Assert.That(c.Dead,Is.False);Assert.That(c.Complete,Is.False);Assert.That(c.PushPendulum(),Is.True);}
  [Test] public void CannotWindWithoutWarmModule(){var c=new ChaseProgress();c.Tick(2,false,false,false,true);Assert.That(c.Wound,Is.False);Assert.That(c.Dead,Is.True);}
  [Test] public void MissingWarmModuleRestartsAfterSevenSeconds(){var c=new ChaseProgress();c.Tick(42,false,true,false,false);c.AllocateNextModule();c.PassedWarmModule();c.Tick(6.9f,false,true,false,false);Assert.That(c.Dead,Is.False);c.Tick(.11f,false,true,false,false);Assert.That(c.Dead,Is.True);}
  [Test] public void ApartmentDimensionsAreScenarioDimensions(){Assert.That(ScenarioLayout.Rooms.Length,Is.EqualTo(8));Assert.That(ScenarioLayout.Rooms[1].bounds.width,Is.EqualTo(3.6f).Within(.001));Assert.That(ScenarioLayout.Rooms[2].bounds.height,Is.EqualTo(2.9f));Assert.That(ScenarioLayout.Ceiling,Is.EqualTo(2.5f));}
 }
}
