using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Ataka.Tests {
 public class TelevisionTests {
  [Test] public void ImportedTelevisionKeepsCurvedDisplayAndTexturedBody(){
   EditorSceneManager.OpenScene(Ataka.Editor.ApartmentArtBuilder.ScenePath);
   var body=GameObject.Find("Living_Television_01");
   var glass=GameObject.Find("Living_TVScreen");
   Assert.NotNull(body);Assert.NotNull(glass);
   var bodyMesh=body.GetComponent<MeshFilter>().sharedMesh;
   var screenMesh=glass.GetComponent<MeshFilter>().sharedMesh;
   Assert.Less(bodyMesh.triangles.Length/3+screenMesh.triangles.Length/3,2500,"A small CRT must not become an expensive sculpt");
   Assert.Greater(screenMesh.bounds.size.x,.02f,"The display must retain physical curvature along its viewing axis");
   Assert.That(bodyMesh.bounds.size.z,Is.InRange(.59f,.61f),"Retain the real 60cm width");
   Assert.NotNull(body.GetComponent<MeshCollider>());
   var material=body.GetComponent<Renderer>().sharedMaterial;
   foreach(var property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"})Assert.NotNull(material.GetTexture(property),property);
   Assert.AreEqual("Ataka/CRT",glass.GetComponent<Renderer>().sharedMaterial.shader.name);
   Assert.IsNull(GameObject.Find("Living_TVBrown"),"Do not leave the obsolete procedural TV overlapping the new asset");
  }
 }
}
