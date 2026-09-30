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

    [SerializeField]
    private BF_DialoguePanel _dialoguePanel;

    [SerializeField]
    private BF_LevelTitlePanel _levelTitlePanel;

    #endregion

    #region 运行时数据

    // 订阅句柄
    private IDisposable _gameModeSubscription;
    private IDisposable _dialogueStartedSubscription;
    private IDisposable _dialogueEndedSubscription;
    private IDisposable _levelTitleRequestSubscription;
    private IDisposable _battlePhaseSubscription;
    private IDisposable _blockingPresentationSubscription;
    private bool _dialoguePresentationOpen;
    private bool _pendingDialoguePresentation;
    private int? _pendingLevelTitle;
    private BF_TutorialViewData _pendingTutorial;
    private BF_BattlePhase _currentBattlePhase = BF_BattlePhase.SetupPhase;
    private bool _phasePauseLockOpen;

    #endregion

    #region 生命周期

    private void OnEnable()
    {
        if (Instance != this)
        {
            return;
        }

        _gameModeSubscription = GameEventBus.Instance.Subscribe<BF_GameModeChangedEvent>(OnGameModeChanged);
        _dialogueStartedSubscription = GameEventBus.Instance.Subscribe<BF_DialogueStartedEvent>(OnDialogueStarted);
        _dialogueEndedSubscription = GameEventBus.Instance.Subscribe<BF_DialogueEndedEvent>(OnDialogueEnded);
        _levelTitleRequestSubscription = GameEventBus.Instance.Subscribe<BF_LevelTitleRequestEvent>(OnLevelTitleRequested);
        _battlePhaseSubscription = GameEventBus.Instance.Subscribe<BF_BattlePhaseChangeEvent>(OnBattlePhaseChanged);
        _blockingPresentationSubscription = GameEventBus.Instance.Subscribe<BF_BlockingPresentationChangedEvent>(
            OnBlockingPresentationChanged);
        BF_EscapeRouter.Instance?.Register(this);

        BF_GameMode gameMode = BF_GameModeManager.Instance != null
            ? BF_GameModeManager.Instance.CurrentGameMode
            : BF_GameMode.Loading;

        Refresh(gameMode, BF_GameMode.None);
    }

    private void OnDisable()
    {
        SetPhasePauseLock(false);
        _gameModeSubscription?.Dispose();
        _gameModeSubscription = null;
        _dialogueStartedSubscription?.Dispose();
        _dialogueStartedSubscription = null;
        _dialogueEndedSubscription?.Dispose();
        _dialogueEndedSubscription = null;
        _levelTitleRequestSubscription?.Dispose();
        _levelTitleRequestSubscription = null;
        _battlePhaseSubscription?.Dispose();
        _battlePhaseSubscription = null;
        _blockingPresentationSubscription?.Dispose();
        _blockingPresentationSubscription = null;
        BF_EscapeRouter.Instance?.Unregister(this);
    }

    #endregion

    #region Esc 消费

    public int EscapePriority => BF_EscapePriorities.UI;

    // 关闭 UI 管理的最上层内容；不改变 GameMode，不处理战斗取消与场景导航。
    public bool TryConsumeEscape()
    {
        // 对话是最高层 UI 上下文：Esc 只打开/关闭跳过确认，不直接结束业务会话。
        if (_dialoguePanel != null && _dialoguePanel.IsOpen)
        {
            return _dialoguePanel.TryHandleCancel();
        }

        // 标题卡：Esc 跳过，避免在入场流程中误触发暂停。
        if (_levelTitlePanel != null && _levelTitlePanel.IsOpen)
        {
            _levelTitlePanel.Skip();
            return true;
        }

        _tutorialPanel ??= FindFirstObjectByType<BF_TutorialPanel>();
        if (_tutorialPanel != null && _tutorialPanel.IsOpen)
        {
            CloseTutorial();
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

        if (gameEvent.CurrentMode == BF_GameMode.Battle
            && gameEvent.PreviousMode == BF_GameMode.Paused)
        {
            FlushPendingPresentation();
        }
    }

    // 对话窗口：打开时关闭教程并隐藏战斗 HUD；绑定缺失时安全跳过。
    private void OnDialogueStarted(BF_DialogueStartedEvent startedEvent)
    {
        CloseTutorial();
        _battleUI?.SetActive(false);

        if (_dialoguePanel == null)
        {
            Debug.LogError("[BF_UIManager] DialoguePanel 缺失，对话安全跳过。");
            GameEventBus.Instance.Publish(new BF_DialogueSkipRequestEvent());
            return;
        }

        if (IsPaused())
        {
            _pendingDialoguePresentation = true;
            return;
        }

        OpenDialoguePresentation();
    }

    private void OnDialogueEnded(BF_DialogueEndedEvent endedEvent)
    {
        _pendingDialoguePresentation = false;
        CloseDialoguePresentation();
        RestoreBattleUiIfAllowed();
    }

    // 标题卡：面板缺失时立即发布完成事实，不阻塞入场流程。
    private void OnLevelTitleRequested(BF_LevelTitleRequestEvent requestEvent)
    {
        if (IsPaused())
        {
            _pendingLevelTitle = requestEvent.Level;
            return;
        }

        if (_levelTitlePanel == null)
        {
            Debug.LogError("[BF_UIManager] LevelTitlePanel 缺失，标题卡安全跳过。");
            GameEventBus.Instance.Publish(new BF_LevelTitleCompletedEvent());
            return;
        }

        _battleUI?.SetActive(false);
        _levelTitlePanel.Play(requestEvent.Level);
    }

    // BattleUI 显隐：Setup 与 BattleEnd 期间隐藏，正式阶段恢复。
    private void OnBattlePhaseChanged(BF_BattlePhaseChangeEvent phaseEvent)
    {
        _currentBattlePhase = phaseEvent.Phase;
        RefreshPhasePauseLock();
        RestoreBattleUiIfAllowed();
    }

    private void OnBlockingPresentationChanged(BF_BlockingPresentationChangedEvent presentationEvent)
    {
        if (!presentationEvent.IsOpen)
        {
            RestoreBattleUiIfAllowed();
        }
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

    #region 教程面板开关

    /// <summary>
    /// 显示教程面板；ViewData 有效、面板引用存在且成功进入显示状态时返回 true。
    /// </summary>
    public bool TryShowTutorial(BF_TutorialViewData data)
    {
        if (data == null)
        {
            return false;
        }

        if (IsPaused())
        {
            _pendingTutorial = data;
            return true;
        }

        _tutorialPanel ??= FindFirstObjectByType<BF_TutorialPanel>();
        if (_tutorialPanel == null)
        {
            return false;
        }

        _tutorialPanel.Show(data);
        _battleUI?.SetActive(false);
        return _tutorialPanel.IsOpen;
    }

    public void CloseTutorial()
    {
        _pendingTutorial = null;
        _tutorialPanel ??= FindFirstObjectByType<BF_TutorialPanel>();
        _tutorialPanel?.Close();
        RestoreBattleUiIfAllowed();
    }

    #endregion

    #region GameMode 刷新

    private void Refresh(BF_GameMode gameMode, BF_GameMode previousMode)
    {
        RefreshPhasePauseLock();
        if (gameMode == BF_GameMode.Battle
            && previousMode != BF_GameMode.Paused)
        {
            _battleHUD?.ResetView();
        }

        if (gameMode == BF_GameMode.Paused)
        {
            _battleUI?.SetActive(true);
        }
        else
        {
            RestoreBattleUiIfAllowed();
        }
        _resultUI?.SetActive(gameMode == BF_GameMode.Result);
        _pauseUI?.SetActive(gameMode == BF_GameMode.Paused);

        if (gameMode == BF_GameMode.Loading || gameMode == BF_GameMode.Result)
        {
            _settingsPanel?.Close();
            CloseTutorial();
            _pendingDialoguePresentation = false;
            _pendingLevelTitle = null;
            CloseDialoguePresentation();
            _levelTitlePanel?.Close();
        }
    }

    private void OpenDialoguePresentation()
    {
        if (_dialoguePresentationOpen || _dialoguePanel == null)
        {
            return;
        }

        _pendingDialoguePresentation = false;
        _dialoguePanel.Open();
        _dialoguePresentationOpen = true;
        GameEventBus.Instance.Publish(
            new BF_BlockingPresentationChangedEvent(BF_BlockingPresentation.Dialogue, true));
    }

    private void CloseDialoguePresentation()
    {
        _dialoguePanel?.Close();
        if (!_dialoguePresentationOpen)
        {
            return;
        }

        _dialoguePresentationOpen = false;
        GameEventBus.Instance.Publish(
            new BF_BlockingPresentationChangedEvent(BF_BlockingPresentation.Dialogue, false));
    }

    private void FlushPendingPresentation()
    {
        if (_pendingDialoguePresentation)
        {
            OpenDialoguePresentation();
            return;
        }

        if (_pendingLevelTitle.HasValue)
        {
            int level = _pendingLevelTitle.Value;
            _pendingLevelTitle = null;
            OnLevelTitleRequested(new BF_LevelTitleRequestEvent(level));
            return;
        }

        if (_pendingTutorial != null)
        {
            BF_TutorialViewData tutorial = _pendingTutorial;
            _pendingTutorial = null;
            TryShowTutorial(tutorial);
        }
    }

    private void RestoreBattleUiIfAllowed()
    {
        BF_GameMode gameMode = BF_GameModeManager.Instance != null
            ? BF_GameModeManager.Instance.CurrentGameMode
            : BF_GameMode.None;
        bool phaseAllowsBattleUi = _currentBattlePhase == BF_BattlePhase.PlayerPhase
            || _currentBattlePhase == BF_BattlePhase.EnemyPhase;
        bool blockingUiOpen = _dialoguePresentationOpen
            || (_levelTitlePanel != null && _levelTitlePanel.IsOpen)
            || (_tutorialPanel != null && _tutorialPanel.IsOpen);
        _battleUI?.SetActive(gameMode == BF_GameMode.Battle && phaseAllowsBattleUi && !blockingUiOpen);
    }

    private void RefreshPhasePauseLock()
    {
        BF_GameMode gameMode = BF_GameModeManager.Instance != null
            ? BF_GameModeManager.Instance.CurrentGameMode
            : BF_GameMode.None;
        bool shouldBlock = gameMode == BF_GameMode.Battle
            && (_currentBattlePhase == BF_BattlePhase.SetupPhase
                || _currentBattlePhase == BF_BattlePhase.BattleEnd);
        SetPhasePauseLock(shouldBlock);
    }

    private void SetPhasePauseLock(bool isOpen)
    {
        if (_phasePauseLockOpen == isOpen)
        {
            return;
        }

        _phasePauseLockOpen = isOpen;
        GameEventBus.Instance?.Publish(
            new BF_BlockingPresentationChangedEvent(BF_BlockingPresentation.BattleTransition, isOpen));
    }

    private static bool IsPaused()
    {
        return BF_GameModeManager.Instance != null
            && BF_GameModeManager.Instance.CurrentGameMode == BF_GameMode.Paused;
    }

    #endregion
}
