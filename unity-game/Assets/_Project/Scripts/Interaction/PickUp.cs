namespace Ataka
{
    public sealed class PickUp : Interactable
    {
        public string itemId;
        public bool collected;
        public override bool Available => base.Available && !collected;
        public override string Prompt => "Взять: " + label;
        public override void Interact(GameSession session)
        {
            if (collected || session == null) return;
            session.inventory.Add(itemId);
            Restore(1);
            session.Notify("Взято: " + label);
        }
        public override float Capture() => collected ? 1 : 0;
        public override void Restore(float value)
        {
            collected = value > .5f;
            gameObject.SetActive(!collected);
        }
    }
}
