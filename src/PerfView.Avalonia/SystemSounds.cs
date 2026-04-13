namespace System.Media;

internal static class SystemSounds
{
    public static SystemSound Beep { get; } = new();

    public sealed class SystemSound
    {
        public void Play()
        {
            // TODO_AVALONIA: Actually play something
        }
    }
}