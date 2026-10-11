using System.Collections.Generic;
using System.Linq;
using Ataka.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ataka.Tests
{
    [TestFixture(MilestoneBuilder.ScenePath)]
    [TestFixture(ApartmentArtBuilder.ScenePath)]
    public class ApartmentTests
    {
        readonly string scenePath;
        public ApartmentTests(string scenePath) { this.scenePath=scenePath; }
        [SetUp] public void LoadScene()
        {
            EditorSceneManager.OpenScene(scenePath);
            Physics.SyncTransforms();
        }
        [Test] public void PersistentIdsAreNonEmptyAndUnique()
        {
            var items = Object.FindObjectsByType<Interactable>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            Assert.GreaterOrEqual(items.Length,13);
            Assert.IsTrue(items.All(x => !string.IsNullOrWhiteSpace(x.persistentId)));
            Assert.AreEqual(items.Length,items.Select(x => x.persistentId).Distinct().Count());
        }
        [Test] public void PickupsAreReachableFromThreeVisibleApproaches()
        {
            var positions = new Dictionary<string, Vector3>
            {
                {"matches",new Vector3(8.05f,0,1.72f)},
                {"recipe",new Vector3(4.3f,0,3.1f)}
            };
            foreach (var item in Object.FindObjectsByType<PickUp>(FindObjectsSortMode.None))
            foreach (float height in item.persistentId == "matches" ? new[] {1.55f} : new[] {1.55f,.94f})
            foreach (float side in new[] {-.1f,0,.1f})
            {
                Vector3 origin = positions[item.persistentId]+new Vector3(side,11.7f+height,0);
                Vector3 target = item.GetComponent<Collider>().bounds.center;
                Assert.AreEqual(item,new InteractionQuery().Resolve(new Ray(origin,(target-origin).normalized)),item.persistentId+" height "+height);
            }
        }
        [Test] public void CrouchingCannotPickMatchesThroughFridgeBody()
        {
            var item = Object.FindObjectsByType<PickUp>(FindObjectsSortMode.None).Single(x=>x.persistentId=="matches");
            var origin = new Vector3(8.05f,12.64f,1.72f);
            Assert.IsNull(new InteractionQuery().Resolve(new Ray(origin,(item.transform.position-origin).normalized)));
        }
        [Test] public void EveryOpenDoorHasCapsuleClearanceAtItsThreshold()
        {
            foreach (var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None)) door.Restore(1);
            Physics.SyncTransforms();
            foreach (var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                float width = door.leaf.size.x;
                var center = door.transform.TransformPoint(new Vector3(width/2,0,0));
                var overlaps = Physics.OverlapCapsule(center+Vector3.up*.28f,center+Vector3.up*1.42f,.24f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                Assert.IsEmpty(overlaps,door.persistentId+": "+string.Join(",",overlaps.Select(x=>x.name)));
            }
        }
    }
}
