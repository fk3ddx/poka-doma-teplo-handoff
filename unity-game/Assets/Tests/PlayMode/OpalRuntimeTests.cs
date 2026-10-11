using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Ataka.Tests {
 public class OpalRuntimeTests {
  [UnityTest] public IEnumerator ChandelierDiffusersFollowLivingSwitchWithoutChangingFloorLampMaterial(){
   Time.timeScale=1;yield return SceneManager.LoadSceneAsync("Apartment_M1");yield return null;
   var session=Object.FindFirstObjectByType<GameSession>();session.NewGame();session.player.inputEnabled=false;
   var lighting=Object.FindFirstObjectByType<ApartmentLightingStates>();
   var opal=GameObject.Find("Living_Opal").GetComponent<Renderer>();var fabric=GameObject.Find("Living_Shade").GetComponent<Renderer>();
   Assert.Contains(opal,lighting.glowingSurfaces);Assert.AreNotSame(opal.sharedMaterial,fabric.sharedMaterial);
   Assert.IsNull(opal.GetComponent<Collider>(),"Decorative diffusers must not add collision");
   Assert.IsTrue(opal.sharedMaterial.IsKeywordEnabled("_EMISSION"));Assert.Greater(opal.sharedMaterial.GetColor("_EmissionColor").maxColorComponent,.5f);
   var bounds=opal.GetComponent<MeshFilter>().sharedMesh.bounds;Assert.Greater(bounds.min.y,2.03f);Assert.Less(fabric.GetComponent<MeshFilter>().sharedMesh.bounds.max.y,2.03f);
   var living=lighting.switches.Single(x=>x.persistentId=="lamp-living");var block=new MaterialPropertyBlock();
   living.Restore(0);yield return null;yield return null;opal.GetPropertyBlock(block);Assert.AreEqual(Color.black,block.GetColor("_EmissionColor"));
   Assert.IsTrue(lighting.switches.Any(x=>x.persistentId!="lamp-living"&&x.powered),"Check individual switch before full blackout");
   foreach(var sw in lighting.switches)sw.Restore(0);yield return null;yield return null;
   opal.GetPropertyBlock(block);Assert.AreEqual(Color.black,block.GetColor("_EmissionColor"));
   session.NewGame();yield return null;yield return null;block.Clear();opal.GetPropertyBlock(block);Assert.IsTrue(block.isEmpty,"New game restores material emission");
   session.SetPaused(true);Time.timeScale=1;AudioListener.pause=false;
  }
 }
}
