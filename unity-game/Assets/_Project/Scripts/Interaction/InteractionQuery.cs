using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ataka
{
    // Only colliders reached before a solid obstruction are candidates. Priority never sees through walls.
    public sealed class InteractionQuery
    {
        readonly RaycastHit[] hits = new RaycastHit[64];
        sealed class DistanceComparer : IComparer<RaycastHit>
        {
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
        static readonly DistanceComparer comparer = new DistanceComparer();

        public Interactable Resolve(Ray ray, float reach = 1.8f, int mask = Physics.DefaultRaycastLayers)
        {
            int count = Physics.RaycastNonAlloc(ray, hits, reach, mask, QueryTriggerInteraction.Ignore);
            // Failing closed is preferable to selecting a hidden object if the scene is excessively dense.
            if (count == hits.Length) return null;
            Array.Sort(hits, 0, count, comparer);
            Interactable best = null;
            float firstDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var target = hits[i].collider.GetComponentInParent<Interactable>();
                if (target == null || !target.Available) break;
                if (best == null) { best = target; firstDistance = hits[i].distance; }
                if (hits[i].distance > firstDistance + .15f) break;
                if (target.priority > best.priority) best = target;
            }
            return best;
        }
    }
}
