using UnityEngine;

namespace Ataka
{
    public sealed class Door : Interactable
    {
        public Transform hinge;
        public BoxCollider leaf;
        public float openAngle = -95;
        public bool opened;
        public float angle;
        readonly Collider[] contacts = new Collider[16];
        public override string Prompt => (opened ? "Закрыть: " : "Открыть: ") + label;
        public override void Interact(GameSession session) { opened = !opened; }
        public override float Capture() => opened ? 1 : 0;
        public override float CaptureAuxiliary() => angle;
        public override void RestoreSnapshot(ObjectState state)
        {
            opened = state.value > .5f;
            angle = Mathf.Clamp(state.auxiliary, Mathf.Min(0,openAngle), Mathf.Max(0,openAngle));
            hinge.localRotation = Quaternion.Euler(0,angle,0);
        }
        public override void Restore(float value)
        {
            opened = value > .5f;
            angle = opened ? openAngle : 0;
            hinge.localRotation = Quaternion.Euler(0, angle, 0);
        }

        void Update()
        {
            float next = Mathf.MoveTowards(angle, opened ? openAngle : 0, 85 * Time.deltaTime);
            if (Mathf.Approximately(next, angle)) return;
            var proposedRotation = hinge.parent.rotation * Quaternion.Euler(0, next, 0);
            var center = hinge.position + proposedRotation * Vector3.Scale(leaf.center, hinge.lossyScale);
            var extent = Vector3.Scale(leaf.size / 2, hinge.lossyScale) + Vector3.one * .03f;
            int count = Physics.OverlapBoxNonAlloc(center, extent, contacts, proposedRotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (contacts[i].GetComponentInParent<FirstPersonController>() != null) return;
            if (count == contacts.Length) return;
            angle = next;
            hinge.localRotation = Quaternion.Euler(0, angle, 0);
        }
    }
}
