using System;
using UnityEngine;

/// <summary>
/// Battle 场景局部入场流程：接收战斗运行时就绪事实，按 对话 → 标题卡 → 教程 → 放行 的顺序
/// 协调开场流程。无配置、对话跳过或失败都必须继续后续流程，不能永久阻塞 SetupPhase。
/// 只协调业务顺序，不推进对话节点、不直接操作 BF.UI。
/// </summary>
public class BF_BattleIntroFlow : MonoBehaviour
{
    #region 序列化配置与引用

    [SerializeField]
    private BF_BattleController _battleController;

    #endregion

    #region 运行时数据

    private IDisposable _readySubscription;
    private IDisposable _dialogueEndedSubscription;
    private IDisposable _titleCompletedSubscription;
    private IDisposable _tutorialCompletedSubscription;
    private bool _flowStarted;
    private bool _dialogueFinished;
    private bool _titleFinished;
    private bool _tutorialFinished;

    #endregion

    #region 生命周期

    private void OnEnable()
    {
        _readySubscription = GameEventBus.Instance.Subscribe<BF_BattleRuntimeReadyEvent>(OnRuntimeReady);
        _dialogueEndedSubscription = GameEventBus.Instance.Subscribe<BF_DialogueEndedEvent>(OnDialogueEnded);
        _titleCompletedSubscription = GameEventBus.Instance.Subscribe<BF_LevelTitleCompletedEvent>(OnTitleCompleted);
        _tutorialCompletedSubscription = GameEventBus.Instance.Subscribe<BF_IntroTutorialCompletedEvent>(OnTutorialCompleted);
    }

    private void OnDisable()
    {
        _readySubscription?.Dispose();
        _readySubscription = null;
        _dialogueEndedSubscription?.Dispose();
        _dialogueEndedSubscription = null;
        _titleCompletedSubscription?.Dispose();
        _titleCompletedSubscription = null;
        _tutorialCompletedSubscription?.Dispose();
        _tutorialCompletedSubscription = null;
    }

    #endregion

    #region 流程

    private void OnRuntimeReady(BF_BattleRuntimeReadyEvent readyEvent)
    {
        if (_flowStarted)
        {
            return;
        }

        _flowStarted = true;

        if (TryStartIntroDialogue())
        {
            return;
        }

        RequestLevelTitle();
    }

    private void OnDialogueEnded(BF_DialogueEndedEvent endedEvent)
    {
        if (!_flowStarted || _dialogueFinished)
        {
            return;
        }

        _dialogueFinished = true;
        RequestLevelTitle();
    }

    private void OnTitleCompleted(BF_LevelTitleCompletedEvent completedEvent)
    {
        if (!_flowStarted || _titleFinished)
        {
            return;
        }

        _titleFinished = true;
        GameEventBus.Instance.Publish(new BF_IntroTutorialRequestEvent());
    }

    private void OnTutorialCompleted(BF_IntroTutorialCompletedEvent completedEvent)
    {
        if (!_flowStarted || !_titleFinished || _tutorialFinished)
        {
            return;
        }

        _tutorialFinished = true;
        _battleController?.ReleaseIntroGate();
    }

    private bool TryStartIntroDialogue()
    {
        BF_BattleService battleService = BF_BattleService.Instance;
        BF_DialogueManager dialogueManager = BF_DialogueManager.Instance;

        string dialogueId = battleService != null && battleService.CurrentLevelConfig != null
            ? battleService.CurrentLevelConfig.IntroDialogueId
            : string.Empty;

        if (string.IsNullOrWhiteSpace(dialogueId) || dialogueManager == null)
        {
            return false;
        }

        return dialogueManager.TryStartDialogue(
            dialogueId,
            unitId => battleService.GetUnitConfig(unitId) != null);
    }

    private void RequestLevelTitle()
    {
        int level = BF_BattleService.Instance != null ? BF_BattleService.Instance.CurrentLevel : 1;
        GameEventBus.Instance.Publish(new BF_LevelTitleRequestEvent(level));
    }

    #endregion
}
