using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Ataka.Tests {
 public class ExteriorFogTests {
  [UnityTest] public IEnumerator YardVolumesFollowTheirLightsAndStayOutsideApartment(){
   yield return SceneManager.LoadSceneAsync("Apartment_M1");yield return null;
   var session=Object.FindFirstObjectByType<GameSession>();session.settings.quality=VisualQuality.High;session.ApplySettings();yield return null;
   var volumes=Object.FindObjectsByType<LocalLightFog>(FindObjectsSortMode.None);
   Assert.AreEqual(6,volumes.Length);
   foreach(var volume in volumes){
    Assert.IsNotNull(volume.sourceLight);Assert.IsNull(volume.GetComponent<Collider>());
    Assert.Less(volume.transform.position.y+volume.radius,11.7f,"Local volumes must not enter the apartment floor");
    Assert.IsTrue(volume.GetComponent<Renderer>().enabled);
   }
   var first=volumes.First();var source=first.sourceLight;source.enabled=false;yield return null;yield return null;
   Assert.IsFalse(first.GetComponent<Renderer>().enabled,"Disabled lamp must not retain a glowing fog volume");
   source.enabled=true;yield return null;yield return null;Assert.IsTrue(first.GetComponent<Renderer>().enabled);
   source.intensity=0;yield return null;yield return null;Assert.IsFalse(first.GetComponent<Renderer>().enabled);
  }
  [UnityTest] public IEnumerator OpaqueForegroundOccludesRenderedScattering(){
   Assert.AreNotEqual(GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"This test requires real GPU rendering");
   var shader=Shader.Find("Ataka/CourtyardLightFog");Assert.IsNotNull(shader);Assert.IsTrue(shader.isSupported);
   var root=new GameObject("Fog GPU verification");
   var cameraGo=new GameObject("Fog verification camera");cameraGo.transform.SetParent(root.transform);var camera=cameraGo.AddComponent<Camera>();
   camera.enabled=false;camera.cullingMask=1<<28;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.allowHDR=true;camera.allowMSAA=false;
   var data=camera.GetUniversalAdditionalCameraData();data.requiresDepthOption=CameraOverrideOption.On;data.renderPostProcessing=false;data.volumeLayerMask=0;
   var lightGo=new GameObject("Fog verification light");lightGo.transform.SetParent(root.transform);lightGo.transform.position=new Vector3(0,0,4);lightGo.transform.rotation=Quaternion.Euler(0,180,0);
   var light=lightGo.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=150;light.innerSpotAngle=120;light.intensity=1;light.color=Color.white;light.cullingMask=0;
   var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.transform.SetParent(root.transform);sphere.layer=28;Object.Destroy(sphere.GetComponent<Collider>());
   var material=new Material(shader);sphere.GetComponent<Renderer>().sharedMaterial=material;var volume=sphere.AddComponent<LocalLightFog>();volume.sourceLight=light;volume.radius=2.5f;volume.referenceIntensity=1;volume.gain=20;volume.density=.2f;
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);wall.layer=28;wall.transform.position=new Vector3(0,0,1.2f);wall.transform.localScale=new Vector3(10,10,.2f);
   var wallMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));wallMaterial.SetColor("_BaseColor",Color.black);wall.GetComponent<Renderer>().sharedMaterial=wallMaterial;wall.SetActive(false);
   var target=new RenderTexture(96,96,24,RenderTextureFormat.ARGBHalf);target.Create();
   bool wasFog=RenderSettings.fog;RenderSettings.fog=false;
   try {
    yield return null;volume.Refresh();
    float visible=Brightness(camera,target);Assert.Greater(visible,.02f,"Scattering must visibly render before occlusion is tested");
    light.color=new Color(.8f,.5f,.3f);volume.Refresh();var coloured=AverageColour(camera,target);
    Assert.That(coloured.g/coloured.r,Is.EqualTo(Mathf.GammaToLinearSpace(.5f)/Mathf.GammaToLinearSpace(.8f)).Within(.035f),"Lamp colour must be converted exactly once, not gamma-converted again by SetColor");
    light.intensity=2;volume.Refresh();var brighter=AverageColour(camera,target);
    Assert.That(brighter.r/coloured.r,Is.EqualTo(2).Within(.05f),"HDR scattering must scale linearly with lamp radiance");
    wall.SetActive(true);yield return null;float occluded=Brightness(camera,target);
    light.enabled=false;yield return null;volume.Refresh();float baseline=Brightness(camera,target);
    Assert.Less(Mathf.Abs(occluded-baseline),.003f,"Light fog leaked through an opaque foreground wall");
    Assert.Less(occluded,visible*.05f);
   } finally {
    RenderSettings.fog=wasFog;Object.Destroy(root);Object.Destroy(material);Object.Destroy(wallMaterial);target.Release();Object.Destroy(target);
   }
  }
  static float Brightness(Camera camera,RenderTexture target){var c=AverageColour(camera,target);return (c.r+c.g+c.b)/3;}
  static Color AverageColour(Camera camera,RenderTexture target){
   RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
   var previous=RenderTexture.active;var pixels=new Texture2D(16,16,TextureFormat.RGBAFloat,false,true);
   try {RenderTexture.active=target;pixels.ReadPixels(new Rect(40,40,16,16),0,0);pixels.Apply();var values=pixels.GetPixels();return new Color(values.Average(c=>c.r),values.Average(c=>c.g),values.Average(c=>c.b),1);}
   finally {RenderTexture.active=previous;Object.DestroyImmediate(pixels);}
  }
 }
}
