public class BF_GameModeChangedEvent : IGameEvent
{
    public BF_GameMode PreviousMode { get; }
    public BF_GameMode CurrentMode { get; }

    public BF_GameModeChangedEvent(BF_GameMode previousMode, BF_GameMode currentMode)
    {
        PreviousMode = previousMode;
        CurrentMode = currentMode;
    }
}
