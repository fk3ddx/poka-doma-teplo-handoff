using UnityEngine;

namespace Ataka
{
    // Import/authoring metadata only. Measurement runs in the editor, never per frame.
    [DisallowMultipleComponent]
    public sealed class RealSize : MonoBehaviour
    {
        [Tooltip("Physical dimensions along this object's axes, in metres. Zero skips an axis.")]
        public Vector3 expectedMetres;
        [Range(0f, 0.15f)] public float tolerance = 0.15f;
        [Tooltip("Source for the real dimensions: manufacturer, measured reference, or VIBE standard.")]
        public string reference;
        [Tooltip("Explicit mesh parts belonging to this object. Do not include its room or shadows.")]
        public MeshFilter[] parts = new MeshFilter[0];
    }
}
