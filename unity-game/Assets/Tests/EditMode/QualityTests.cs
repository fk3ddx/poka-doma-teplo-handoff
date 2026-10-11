using System;
using System.IO;
using NUnit.Framework;
namespace Ataka.Tests {
 public class QualitySettingsStoreTests {
  string path;
  [SetUp] public void Setup(){path=Path.Combine(Path.GetTempPath(),"Ataka-settings-test-"+Guid.NewGuid()+".json");}
  [TearDown] public void Cleanup(){if(File.Exists(path))File.Delete(path);}
  [Test] public void AllProfilesAndControlPreferencesSurviveDiskRoundTrip(){
   foreach(var quality in new[]{VisualQuality.High,VisualQuality.Medium,VisualQuality.Low}){
    var saved=new GameSettings{quality=quality,fpsLimit=120,fieldOfView=83,sensitivity=.12f,invertY=true,vhs=false};
    GameSettingsStore.Save(path,saved);var loaded=GameSettingsStore.Load(path);
    Assert.AreEqual(quality,loaded.quality);Assert.AreEqual(120,loaded.fpsLimit);Assert.AreEqual(83,loaded.fieldOfView);Assert.That(loaded.sensitivity,Is.EqualTo(.12f).Within(.0001f));Assert.IsTrue(loaded.invertY);Assert.IsFalse(loaded.vhs);
   }
  }
  [Test] public void LegacyFileKeepsControlsAndDefaultsToHigh(){
   File.WriteAllText(path,"{\"sensitivity\":0.1,\"fieldOfView\":78,\"fpsLimit\":60,\"vhs\":true}");
   var loaded=GameSettingsStore.Load(path);Assert.AreEqual(VisualQuality.High,loaded.quality);Assert.AreEqual(78,loaded.fieldOfView);Assert.That(loaded.sensitivity,Is.EqualTo(.1f).Within(.0001f));
  }
  [Test] public void CorruptAndMissingFilesRemainPlayable(){
   Assert.AreEqual(VisualQuality.High,GameSettingsStore.Load(path).quality);
   File.WriteAllText(path,"this is not json");Assert.AreEqual(60,GameSettingsStore.Load(path).fpsLimit);
   GameSettingsStore.Save(path,new GameSettings{quality=(VisualQuality)999,fpsLimit=0,fieldOfView=float.NaN,sensitivity=float.PositiveInfinity});
   var loaded=GameSettingsStore.Load(path);Assert.AreEqual(VisualQuality.Low,loaded.quality);Assert.AreEqual(60,loaded.fpsLimit);Assert.AreEqual(70,loaded.fieldOfView);Assert.AreEqual(.085f,loaded.sensitivity);
  }
 }
}
