namespace PodBoy.Event
{
    public class ShortcutEvent
    {
        public ShortcutCommandType Type { get; }

        public ShortcutEvent(ShortcutCommandType type)
        {
            Type = type;
        }
    }
}