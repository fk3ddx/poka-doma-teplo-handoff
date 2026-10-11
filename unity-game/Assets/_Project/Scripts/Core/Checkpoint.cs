using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ataka
{
    [Serializable]
    public sealed class ObjectState
    {
        public string id;
        public float value;
        public float auxiliary;
        public ObjectState(string id, float value, float auxiliary = 0)
        { this.id = id; this.value = value; this.auxiliary = auxiliary; }
    }

    [Serializable]
    public sealed class Checkpoint
    {
        public int schema = 1;
        public string scene = "Apartment_M0";
        public Vector3 position;
        public float yaw;
        public float pitch;
        public bool crouched;
        public bool rewindUsed;
        public List<string> inventory = new List<string>();
        public List<ObjectState> objects = new List<ObjectState>();

        public bool TryGet(string id, out float value)
        {
            foreach (var item in objects)
                if (item.id == id) { value = item.value; return true; }
            value = 0;
            return false;
        }
    }

    // Staging and replace keep the last complete checkpoint if a write fails.
    public sealed class CheckpointStore
    {
        readonly string path;
        public CheckpointStore(string path) { this.path = path; }
        public bool Exists => File.Exists(path) || File.Exists(path + ".bak");

        public void Save(Checkpoint checkpoint)
        {
            if (!Valid(checkpoint)) throw new ArgumentException("Invalid checkpoint");
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var staging = path + ".tmp";
            using (var stream = new FileStream(staging, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(JsonUtility.ToJson(checkpoint, true));
                writer.Flush();
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(staging, path, path + ".bak");
            else File.Move(staging, path);
        }

        public bool TryLoad(out Checkpoint checkpoint)
        {
            return TryRead(path, out checkpoint) || TryRead(path + ".bak", out checkpoint);
        }

        static bool TryRead(string file, out Checkpoint checkpoint)
        {
            checkpoint = null;
            try
            {
                if (!File.Exists(file)) return false;
                var parsed = JsonUtility.FromJson<Checkpoint>(File.ReadAllText(file));
                if (!Valid(parsed)) return false;
                checkpoint = parsed;
                return true;
            }
            catch (Exception error) when (error is IOException || error is ArgumentException || error is UnauthorizedAccessException)
            { return false; }
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Valid(Checkpoint value)
        {
            if (value == null || value.schema != 1 || (value.scene != "Apartment_M0" && value.scene != "Apartment_M1") ||
                value.inventory == null || value.objects == null ||
                !Finite(value.position.x) || !Finite(value.position.y) || !Finite(value.position.z) ||
                !Finite(value.yaw) || !Finite(value.pitch)) return false;
            var ids = new HashSet<string>();
            foreach (var item in value.objects)
                if (item == null || string.IsNullOrEmpty(item.id) || !ids.Add(item.id) ||
                    !Finite(item.value) || !Finite(item.auxiliary)) return false;
            ids.Clear();
            foreach (var item in value.inventory)
                if (string.IsNullOrEmpty(item) || !ids.Add(item)) return false;
            return true;
        }
    }
}
