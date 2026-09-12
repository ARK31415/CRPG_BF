using System;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class BF_UIManager : Singleton<BF_UIManager>, IBF_EscapeHandler
{
    #region 序列化配置与引用

    [Header("全局界面节点")]
    [SerializeField]
    private GameObject _battleUI;

    [SerializeField]
    private GameObject _resultUI;

    [SerializeField]
    private GameObject _pauseUI;

    [Header("全局面板引用")]
    [SerializeField]
    private BF_SettingsPanel _settingsPanel;

    [SerializeField]
    private BF_BattleHUD _battleHUD;

    [SerializeField]
    private BF_TutorialPanel _tutorialPanel;

    #endregion

    #region 运行时数据

    // 订阅句柄
    private IDisposable _gameModeSubscription;

    #endregion

    #region 生命周期

    private void OnEnable()
    {
        if (Instance != this)
        {
            return;
        }

        _gameModeSubscription = GameEventBus.Instance.Subscribe<BF_GameModeChangedEvent>(OnGameModeChanged);
        BF_EscapeRouter.Instance?.Register(this);

        BF_GameMode gameMode = BF_GameModeManager.Instance != null
            ? BF_GameModeManager.Instance.CurrentGameMode
            : BF_GameMode.Loading;

        Refresh(gameMode, BF_GameMode.None);
    }

    private void OnDisable()
    {
        _gameModeSubscription?.Dispose();
        _gameModeSubscription = null;
        BF_EscapeRouter.Instance?.Unregister(this);
    }

    #endregion

    #region Esc 消费

    public int EscapePriority => BF_EscapePriorities.UI;

    // 关闭 UI 管理的最上层内容；不改变 GameMode，不处理战斗取消与场景导航。
    public bool TryConsumeEscape()
    {
        _tutorialPanel ??= FindFirstObjectByType<BF_TutorialPanel>();
        if (_tutorialPanel != null && _tutorialPanel.IsOpen)
        {
            _tutorialPanel.Close();
            return true;
        }

        if (_settingsPanel != null && _settingsPanel.IsOpen)
        {
            _settingsPanel.Close();
            return true;
        }

        BF_ItemContextMenu itemMenu = FindFirstObjectByType<BF_ItemContextMenu>();
        if (itemMenu != null && itemMenu.IsOpen)
        {
            itemMenu.Hide();
            return true;
        }

        BF_MenuController menu = FindFirstObjectByType<BF_MenuController>();
        if (menu != null && menu.IsConfirmOpen)
        {
            menu.CloseConfirm();
            return true;
        }

        BF_PausePanel pausePanel = FindFirstObjectByType<BF_PausePanel>();
        if (pausePanel != null && pausePanel.IsExitConfirmOpen)
        {
            pausePanel.CloseExitConfirm();
            return true;
        }

        return false;
    }

    #endregion

    #region 事件处理

    private void OnGameModeChanged(BF_GameModeChangedEvent gameEvent)
    {
        Refresh(gameEvent.CurrentMode, gameEvent.PreviousMode);
    }

    #endregion

    #region 设置面板开关

    public void OpenSettingsPanel()
    {
        _settingsPanel?.Open();
    }

    public void CloseSettingsPanel()
    {
        _settingsPanel?.Close();
    }

    #endregion

    #region GameMode 刷新

    private void Refresh(BF_GameMode gameMode, BF_GameMode previousMode)
    {
        if (gameMode == BF_GameMode.Battle
            && previousMode != BF_GameMode.Paused)
        {
            _battleHUD?.ResetView();
        }

        _battleUI?.SetActive(gameMode == BF_GameMode.Battle || gameMode == BF_GameMode.Paused);
        _resultUI?.SetActive(gameMode == BF_GameMode.Result);
        _pauseUI?.SetActive(gameMode == BF_GameMode.Paused);

        if (gameMode == BF_GameMode.Loading || gameMode == BF_GameMode.Result)
        {
            _settingsPanel?.Close();
        }
    }

    #endregion
}
