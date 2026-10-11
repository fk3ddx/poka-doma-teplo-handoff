using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Ataka.Tests
{
    public class CheckpointTests
    {
        string directory, file;
        CheckpointStore store;
        [SetUp] public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "AtakaTests-" + Guid.NewGuid().ToString("N"));
            file = Path.Combine(directory, "save.json");
            store = new CheckpointStore(file);
        }
        [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test] public void SaveLoadRoundtripIncludesDoorPickupAndPlayerPose()
        {
            var original = new Checkpoint { position = new Vector3(7.4f, 11.7f, 5.2f), yaw = 213, pitch = -13, crouched = true, rewindUsed = true };
            original.inventory.Add("matches"); original.objects.Add(new ObjectState("door", 1, -37));
            original.objects.Add(new ObjectState("lamp", 0));
            store.Save(original);
            Assert.IsTrue(store.TryLoad(out var loaded));
            Assert.AreEqual(JsonUtility.ToJson(original), JsonUtility.ToJson(loaded));
        }

        [Test] public void CorruptLatestCheckpointRecoversLastCompleteBackup()
        {
            store.Save(new Checkpoint { yaw = 90 }); store.Save(new Checkpoint { yaw = 180 });
            File.WriteAllText(file, "{broken");
            Assert.IsTrue(store.TryLoad(out var loaded)); Assert.AreEqual(90, loaded.yaw);
            File.Delete(file);
            Assert.IsTrue(store.Exists);
            Assert.IsTrue(store.TryLoad(out loaded)); Assert.AreEqual(90, loaded.yaw);
        }

        [Test] public void UnsupportedSchemaIsRejectedWithoutOverwritingFile()
        {
            Directory.CreateDirectory(directory); File.WriteAllText(file, "{\"schema\":999}");
            Assert.IsFalse(store.TryLoad(out _));
            Assert.AreEqual("{\"schema\":999}", File.ReadAllText(file));
        }

        [Test] public void NonFinitePositionCannotBeSaved()
        {
            Assert.Throws<ArgumentException>(() => store.Save(new Checkpoint { position = new Vector3(float.NaN, 0, 0) }));
            Assert.IsFalse(File.Exists(file));
        }
    }
}
