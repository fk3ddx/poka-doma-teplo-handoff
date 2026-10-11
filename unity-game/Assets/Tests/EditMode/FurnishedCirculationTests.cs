using System.Linq;
using Ataka.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Ataka.Tests {
 public class FurnishedCirculationTests {
  [SetUp] public void Setup(){EditorSceneManager.OpenScene(ApartmentArtBuilder.ScenePath);foreach(var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))door.Restore(1);Physics.SyncTransforms();}
  [Test] public void KitchenCanBeReachedWithoutWalkingThroughFurniture(){Check(new[]{new Vector2(5.8f,3.7f),new Vector2(5.65f,4.82f),new Vector2(7.2f,4.82f),new Vector2(7.2f,3.4f),new Vector2(7.2f,2.5f),new Vector2(7.65f,2.25f),new Vector2(7.65f,1.7f)});}
  [Test] public void ArmchairDoesNotBlockTheBalcony(){Check(new[]{new Vector2(5.15f,2.85f),new Vector2(5.30f,1.55f),new Vector2(5.2f,.4f),new Vector2(5.175f,-.65f)});}
  [Test] public void BedroomCanBeReachedFromLivingRoom(){Check(new[]{new Vector2(4.55f,3.1f),new Vector2(3.8f,3.4f),new Vector2(3.15f,3.75f),new Vector2(2.35f,3.75f)});}
  [Test] public void BedroomBedsideCanBeReached(){Check(new[]{new Vector2(2.35f,3.75f),new Vector2(2.15f,3.20f),new Vector2(2.15f,2.95f),new Vector2(2.15f,1.50f)});}
  [Test] public void BathroomThresholdHasStandingClearance(){Check(new[]{new Vector2(5.15f,5.25f),new Vector2(4.90f,5.25f),new Vector2(4.36f,5.25f)});}
  [Test] public void EntranceRouteClearsTheCoatAndDresser(){Check(new[]{new Vector2(7.2f,4.82f),new Vector2(7.7f,4.82f),new Vector2(8.05f,5.3f),new Vector2(8.05f,6.6f)});}
  static void Check(Vector2[] points){for(int i=1;i<points.Length;i++){int samples=Mathf.CeilToInt(Vector2.Distance(points[i-1],points[i])/.06f);for(int j=0;j<=samples;j++){var p=Vector2.Lerp(points[i-1],points[i],(float)j/samples);var feet=new Vector3(p.x,11.7f,p.y);var collisions=Physics.OverlapCapsule(feet+Vector3.up*.4f,feet+Vector3.up*1.42f,.24f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);Assert.IsEmpty(collisions,"Blocked at "+p+": "+string.Join(",",collisions.Select(x=>x.name)));}}}
 }
}
