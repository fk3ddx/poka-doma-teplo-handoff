using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Ataka.Tests
{
    public class InteractionTests
    {
        readonly List<GameObject> created = new List<GameObject>();
        GameObject Box(Vector3 position, Vector3 size)
        {
            var box = new GameObject("Test collider"); box.transform.position = position;
            box.AddComponent<BoxCollider>().size = size; created.Add(box); return box;
        }
        PickUp Item(float z, int priority = 0)
        {
            var item = Box(new Vector3(1000, 1000, z), new Vector3(.2f,.2f,.05f)).AddComponent<PickUp>();
            item.priority = priority; return item;
        }
        Ray Ray => new Ray(new Vector3(1000,1000,0),Vector3.forward);
        [TearDown] public void Cleanup()
        {
            foreach (var item in created) Object.DestroyImmediate(item);
            created.Clear(); Physics.SyncTransforms();
        }
        [Test] public void HigherPriorityPickupWithinFifteenCentimetersWins()
        {
            Item(1); var matches = Item(1.1f,20); Physics.SyncTransforms();
            Assert.AreEqual(matches,new InteractionQuery().Resolve(Ray));
        }
        [Test] public void PriorityCannotReachThroughWall()
        {
            Box(new Vector3(1000,1000,.9f),new Vector3(.5f,.5f,.1f)); Item(1,100);
            Physics.SyncTransforms(); Assert.IsNull(new InteractionQuery().Resolve(Ray));
        }
        [Test] public void PickupOutsideReachCannotBeTaken()
        {
            Item(2,100); Physics.SyncTransforms(); Assert.IsNull(new InteractionQuery().Resolve(Ray));
        }
        [Test] public void PriorityCannotSkipDistantForegroundObject()
        {
            var near = Item(.8f); Item(1.3f,100); Physics.SyncTransforms();
            Assert.AreEqual(near,new InteractionQuery().Resolve(Ray));
        }
    }
}
