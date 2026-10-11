using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Ataka.Tests {
 public class QualityRuntimeTests {
  [UnityTest] public IEnumerator ProfilesChangeRealResourcesAndRestoreWithoutAffectingLightingOrAssets(){
   yield return SceneManager.LoadSceneAsync("Apartment_M1");yield return null;
   var session=Object.FindFirstObjectByType<GameSession>();session.settings.quality=VisualQuality.High;session.ApplySettings();yield return null;
   var quality=session.Quality;Assert.IsNotNull(quality);
   var original=(UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline;float originalScale=original.renderScale;
   var runtime=quality.Pipeline;Assert.AreNotSame(original,runtime);
   var mirrors=Object.FindObjectsByType<PlanarMirror>(FindObjectsSortMode.None);Assert.AreEqual(2,mirrors.Length);
   var lamps=Object.FindObjectsByType<LampSwitch>(FindObjectsSortMode.None);var power=lamps.Select(l=>l.controlledLight.intensity).ToArray();
   var maps=LightmapSettings.lightmaps.Select(m=>m.lightmapColor).ToArray();
   foreach(var mode in new[]{VisualQuality.Low,VisualQuality.Medium,VisualQuality.High}){
    session.settings.quality=mode;session.ApplySettings();yield return null;
    bool low=mode==VisualQuality.Low,medium=mode==VisualQuality.Medium;
    Assert.That(runtime.renderScale,Is.EqualTo(RetroRaster.Scale(session.settings.rasterStyle,Screen.height)).Within(.001f));
    Assert.AreEqual(low?1024:medium?2048:4096,runtime.additionalLightsShadowmapResolution);
    Assert.AreEqual(1,runtime.msaaSampleCount);
    Assert.AreEqual(UpscalingFilterSelection.Point,runtime.upscalingFilter);
    foreach(var mirror in mirrors){Assert.AreEqual(low?256:medium?384:512,mirror.ReflectionTexture.width);Assert.IsTrue(mirror.ReflectionTexture.IsCreated());Assert.AreEqual(low?5:medium?8:10,mirror.Frequency);}
    foreach(var volume in Object.FindObjectsByType<LocalLightFog>(FindObjectsInactive.Include,FindObjectsSortMode.None))Assert.AreEqual(!low,volume.enabled);
    CollectionAssert.AreEqual(power,lamps.Select(l=>l.controlledLight.intensity).ToArray());CollectionAssert.AreEqual(maps,LightmapSettings.lightmaps.Select(m=>m.lightmapColor).ToArray());
    int revision=quality.Revision;var textures=mirrors.Select(m=>m.ReflectionTexture).ToArray();for(int i=0;i<100;i++)session.ApplySettings();Assert.AreEqual(revision,quality.Revision);CollectionAssert.AreEqual(textures,mirrors.Select(m=>m.ReflectionTexture).ToArray());
    Assert.AreEqual(originalScale,original.renderScale);
   }
   session.settings.quality=VisualQuality.Low;session.ApplySettings();yield return null;
   yield return SceneManager.LoadSceneAsync("Apartment_M1");yield return null;
   session=Object.FindFirstObjectByType<GameSession>();Assert.AreEqual(session.settings.quality,session.Quality.Current);Assert.AreEqual(originalScale,original.renderScale);
  }
 }
}
