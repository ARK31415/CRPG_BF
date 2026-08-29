using UnityEngine;

public class BF_GameModeManager : Singleton<BF_GameModeManager>
{
    public BF_GameMode CurrentGameMode { get; private set; }

    [SerializeField]
    private BF_GameMode _defaultGameMode = BF_GameMode.Battle;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        SetGameMode(_defaultGameMode);
    }

    public void SetGameMode(BF_GameMode gameMode)
    {
        if (CurrentGameMode == gameMode)
        {
            return;
        }

        BF_GameMode previousMode = CurrentGameMode;
        CurrentGameMode = gameMode;
        GameEventBus.Instance.Publish(new BF_GameModeChangedEvent(previousMode, gameMode));
        Debug.Log($"[BF] GameMode: {previousMode} -> {gameMode}");
    }
}
