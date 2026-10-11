using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Ataka.Editor
{
    public static class RealSizeAudit
    {
        public readonly struct Result
        {
            public readonly bool passed;
            public readonly Vector3 measuredMetres;
            public readonly string message;
            public Result(bool passed, Vector3 measuredMetres, string message)
            { this.passed = passed; this.measuredMetres = measuredMetres; this.message = message; }
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
        static Result Fail(string reason) => new Result(false, Vector3.zero, reason);

        public static Result Check(RealSize subject)
        {
            if (!subject) return Fail("No RealSize component.");
            var expected = subject.expectedMetres;
            if (!Finite(expected) || expected.x < 0 || expected.y < 0 || expected.z < 0 || expected == Vector3.zero)
                return Fail("Specify at least one positive dimension in metres; zero skips an axis.");
            if (!Finite(subject.tolerance) || subject.tolerance < 0 || subject.tolerance > .15f)
                return Fail("Tolerance must be between 0 and 15 percent.");
            if (string.IsNullOrWhiteSpace(subject.reference)) return Fail("Real dimensions need a reference.");
            if (subject.parts == null || subject.parts.Length == 0) return Fail("No explicit mesh parts selected.");

            // World metres, aligned to the subject's axes. Unlike world AABB this does not
            // make an object appear wider merely because it is rotated in the room.
            var unrotate = Quaternion.Inverse(subject.transform.rotation);
            var origin = subject.transform.position;
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            long count = 0;
            foreach (var part in subject.parts)
            {
                if (!part || !part.sharedMesh) return Fail("A selected mesh part is missing.");
                if (part.transform != subject.transform && !part.transform.IsChildOf(subject.transform))
                    return Fail("A mesh part belongs to another object.");
                if (part.sharedMesh.vertexCount == 0) return Fail("A selected mesh is empty.");
                try
                {
                    // Editor snapshot also supports imported meshes with Read/Write disabled.
                    // Read real vertices, not padded mesh bounds; never change the source mesh.
                    using var snapshot = MeshUtility.AcquireReadOnlyMeshData(part.sharedMesh);
                    using var vertices = new NativeArray<Vector3>(snapshot[0].vertexCount, Allocator.Temp);
                    snapshot[0].GetVertices(vertices);
                    foreach (var vertex in vertices)
                    {
                        var point = unrotate * (part.transform.TransformPoint(vertex) - origin);
                        if (!Finite(point)) return Fail("Non-finite mesh vertex or transform.");
                        min = Vector3.Min(min, point);
                        max = Vector3.Max(max, point);
                        count++;
                    }
                }
                catch (Exception e) { return Fail("Cannot measure mesh: " + e.Message); }
            }
            if (count == 0) return Fail("No vertices measured.");
            var size = max - min;
            string issues = "";
            for (int axis = 0; axis < 3; axis++)
            {
                if (expected[axis] == 0) continue;
                if (Mathf.Abs(size[axis] - expected[axis]) > expected[axis] * subject.tolerance + .00001f)
                    issues += $" {"XYZ"[axis]}={size[axis]:F3}m (target {expected[axis]:F3}m).";
            }
            return new Result(issues.Length == 0, size,
                issues.Length == 0 ? $"PASS: {size.ToString("F3")}m; {subject.reference}" : "Scale mismatch:" + issues);
        }

        [MenuItem("Ataka/Kitchen/Check selected object scale")]
        public static void CheckSelection()
        {
            var selected = Selection.activeGameObject;
            if (!selected || !selected.TryGetComponent<RealSize>(out var subject))
            { Debug.LogWarning("Select an imported object's root with RealSize metadata."); return; }
            var result = Check(subject);
            if (result.passed) Debug.Log(result.message, subject);
            else Debug.LogError(result.message, subject);
        }
    }
}
