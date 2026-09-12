using UnityEngine;

/// <summary>
/// 全局游戏模式入口：维护当前模式、按模式切换时间缩放并广播模式变更事件。
/// </summary>
public class BF_GameModeManager : Singleton<BF_GameModeManager>
{
    #region 序列化配置与引用

    [Header("默认模式")]
    [SerializeField]
    private BF_GameMode _defaultGameMode = BF_GameMode.Battle;

    #endregion

    #region 对外接口

    public BF_GameMode CurrentGameMode { get; private set; }

    #endregion

    #region 生命周期

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        SetGameMode(_defaultGameMode);
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

    #endregion

    #region 游戏模式切换

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

    #endregion
}
