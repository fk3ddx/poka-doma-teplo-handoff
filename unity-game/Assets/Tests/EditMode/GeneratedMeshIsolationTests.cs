using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Ataka.Tests {
 public class GeneratedMeshIsolationTests {
  [Test] public void CourtyardMustNotReuseApartmentUvMeshes(){
   EditorSceneManager.OpenScene(Ataka.Editor.ApartmentArtBuilder.ScenePath);
   var shell=GameObject.Find("Apartment • M0 metric shell").GetComponentsInChildren<MeshFilter>().Select(x=>x.sharedMesh).ToHashSet();
   var pavement=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(x=>x.name=="Courtyard paving tile").ToArray();
   Assert.Greater(pavement.Length,0);
   Assert.IsEmpty(pavement.Where(x=>shell.Contains(x.sharedMesh)).Select(x=>x.name).ToArray(),"Ground refinement must never overwrite wall/window UV assets");
  }
 }
}
