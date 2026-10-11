using UnityEngine;

namespace Ataka
{
    public abstract class Interactable : MonoBehaviour
    {
        public string persistentId;
        public string label;
        public int priority;
        public virtual string Prompt => label;
        public virtual bool Available => isActiveAndEnabled;
        public abstract void Interact(GameSession session);
        public abstract float Capture();
        public virtual float CaptureAuxiliary() => 0;
        public abstract void Restore(float value);
        public virtual void RestoreSnapshot(ObjectState state) => Restore(state.value);
    }
}
