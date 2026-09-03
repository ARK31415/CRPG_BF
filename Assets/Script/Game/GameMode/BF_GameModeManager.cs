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
        Time.timeScale = gameMode == BF_GameMode.Paused ? 0f : 1f;
        GameEventBus.Instance.Publish(new BF_GameModeChangedEvent(previousMode, gameMode));
        Debug.Log($"[BF] GameMode: {previousMode} -> {gameMode}");
    }

    public void PauseBattle()
    {
        if (CurrentGameMode == BF_GameMode.Battle)
        {
            SetGameMode(BF_GameMode.Paused);
        }
    }

    public void ResumeBattle()
    {
        if (CurrentGameMode == BF_GameMode.Paused)
        {
            SetGameMode(BF_GameMode.Battle);
        }
    }

    public void NormalizeTimeScale()
    {
        Time.timeScale = 1f;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && CurrentGameMode == BF_GameMode.Battle)
        {
            PauseBattle();
        }
    }

    protected override void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
        }

        base.OnDestroy();
    }
}
