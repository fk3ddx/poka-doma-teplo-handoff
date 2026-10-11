using UnityEngine;

namespace Ataka
{
    public sealed class LampSwitch : Interactable
    {
        public Light controlledLight;
        public float intensity = 1.5f;
        public bool powered = true;
        public override string Prompt => (powered ? "Выключить: " : "Включить: ") + label;
        public override void Interact(GameSession session) => Restore(powered ? 0 : 1);
        public override float Capture() => powered ? 1 : 0;
        public override void Restore(float value)
        {
            powered = value > .5f;
            // Keep the light and its shadow mode registered; never rebuild the scene's light set.
            if (controlledLight != null) controlledLight.intensity = powered ? intensity : 0;
        }
    }
}
