using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Ataka.Tests
{
    public class SessionTests
    {
        GameSession session;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Apartment_M0");
            yield return null;
            session = Object.FindFirstObjectByType<GameSession>();
            Assert.NotNull(session);
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            session.SetPaused(true);
            yield return null;
            Time.timeScale = 1; AudioListener.pause = false;
        }

        [UnityTest] public IEnumerator PauseFreezesDoorMotionAndAudio()
        {
            session.NewGame();
            var door = Object.FindObjectsByType<Door>(FindObjectsSortMode.None).Single(x=>x.persistentId=="balcony-door");
            door.Interact(session);
            yield return null; yield return null;
            session.SetPaused(true);
            float before = door.angle;
            yield return new WaitForSecondsRealtime(.12f);
            Assert.AreEqual(before,door.angle);
            Assert.AreEqual(0,Time.timeScale); Assert.IsTrue(AudioListener.pause);
            Assert.IsFalse(session.player.inputEnabled);
            session.SetPaused(false);
            yield return new WaitForSecondsRealtime(.12f);
            Assert.AreNotEqual(before,door.angle);
        }

        [UnityTest] public IEnumerator NewGameRestoresCollectedObjectsAndDoorState()
        {
            session.NewGame();
            var pickup = Object.FindObjectsByType<PickUp>(FindObjectsSortMode.None).First();
            var door = Object.FindObjectsByType<Door>(FindObjectsSortMode.None).First();
            pickup.Interact(session); pickup.Interact(session); door.Restore(1);
            Assert.AreEqual(1,session.inventory.Count);
            Assert.IsFalse(pickup.gameObject.activeSelf);
            session.NewGame(); yield return null;
            Assert.IsEmpty(session.inventory); Assert.IsTrue(pickup.gameObject.activeSelf);
            Assert.IsFalse(door.opened); Assert.AreEqual(0,door.angle);
        }

        [UnityTest] public IEnumerator LowCeilingPreventsStanding()
        {
            var player = session.player;
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.position = player.transform.position + Vector3.up * 1.4f;
            block.transform.localScale = new Vector3(1,.15f,1);
            Physics.SyncTransforms();
            Assert.IsFalse(player.CanStand());
            Object.Destroy(block); yield return null;
            Physics.SyncTransforms(); Assert.IsTrue(player.CanStand());
        }

        [UnityTest] public IEnumerator RestorePreservesPoseWithoutMovingCameraThroughFloor()
        {
            var point = new Vector3(7.4f,11.72f,4.2f);
            session.player.Restore(point,225,-30,true);
            yield return null;
            var state = session.Capture();
            Assert.Less(Vector3.Distance(point,state.position),.001f);
            Assert.AreEqual(225,state.yaw,.01f); Assert.AreEqual(-30,state.pitch,.01f);
            Assert.IsTrue(state.crouched);
            Assert.AreEqual(.94f,session.player.view.transform.localPosition.y,.01f);
        }

        [UnityTest] public IEnumerator SaveAndLoadRestoreInventoryLightsAndPartialDoorTogether()
        {
            string file = Path.Combine(Path.GetTempPath(), "AtakaSession-" + System.Guid.NewGuid().ToString("N") + ".json");
            typeof(GameSession).GetField("store", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(session, new CheckpointStore(file));
            try
            {
                session.NewGame();
                var pickup = Object.FindObjectsByType<PickUp>(FindObjectsSortMode.None).First();
                var door = Object.FindObjectsByType<Door>(FindObjectsSortMode.None).First();
                var lamp = Object.FindObjectsByType<LampSwitch>(FindObjectsSortMode.None).First();
                pickup.Interact(session);
                door.RestoreSnapshot(new ObjectState(door.persistentId, 1, -37));
                lamp.Restore(0);
                var point = new Vector3(7.4f, 11.72f, 4.2f);
                session.player.Restore(point, 225, -30, true);
                session.SetPaused(true);
                session.Save();
                session.NewGame();
                Assert.IsEmpty(session.inventory);
                session.Load();
                session.SetPaused(true);
                yield return null;
                Assert.IsTrue(session.inventory.Contains(pickup.itemId));
                Assert.IsFalse(pickup.gameObject.activeSelf);
                Assert.IsTrue(door.opened); Assert.AreEqual(-37, door.angle, .001f);
                Assert.AreEqual(0, lamp.Capture());
                Assert.Less(Vector3.Distance(point, session.player.transform.position), .001f);
                Assert.AreEqual(225, session.player.transform.eulerAngles.y, .01f);
                Assert.IsTrue(session.player.crouched);
            }
            finally
            {
                foreach (string suffix in new[] { "", ".bak", ".tmp" })
                    if (File.Exists(file + suffix)) File.Delete(file + suffix);
            }
        }
    }
}
