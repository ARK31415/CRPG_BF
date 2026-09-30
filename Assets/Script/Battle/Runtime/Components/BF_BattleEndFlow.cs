using System;
using UnityEngine;

/// <summary>
/// Battle 场景局部结尾流程：接收战斗结果事实，按配置播放 Victory / Defeat 对话；
/// 对话完成、跳过、失败或无配置后调用 BF_BattleService.FinalizePendingResult 完成结算。
/// 不直接发放奖励、不直接操作 UI。
/// </summary>
public class BF_BattleEndFlow : MonoBehaviour
{
    #region 运行时数据

    private IDisposable _resultSubscription;
    private IDisposable _dialogueEndedSubscription;
    private bool _flowStarted;
    private bool _finalized;

    #endregion

    #region 生命周期

    private void OnEnable()
    {
        _resultSubscription = GameEventBus.Instance.Subscribe<BF_BattleResultEvent>(OnBattleResult);
        _dialogueEndedSubscription = GameEventBus.Instance.Subscribe<BF_DialogueEndedEvent>(OnDialogueEnded);
    }

    private void OnDisable()
    {
        _resultSubscription?.Dispose();
        _resultSubscription = null;
        _dialogueEndedSubscription?.Dispose();
        _dialogueEndedSubscription = null;
    }

    #endregion

    #region 流程

    private void OnBattleResult(BF_BattleResultEvent resultEvent)
    {
        if (_flowStarted || resultEvent.Result == BF_BattleResult.None)
        {
            return;
        }

        _flowStarted = true;

        if (TryStartEndDialogue(resultEvent.Result))
        {
            return;
        }

        FinalizeResult();
    }

    private void OnDialogueEnded(BF_DialogueEndedEvent endedEvent)
    {
        if (!_flowStarted || _finalized)
        {
            return;
        }

        FinalizeResult();
    }

    private bool TryStartEndDialogue(BF_BattleResult result)
    {
        BF_BattleService battleService = BF_BattleService.Instance;
        BF_DialogueManager dialogueManager = BF_DialogueManager.Instance;
        if (battleService == null || battleService.CurrentLevelConfig == null || dialogueManager == null)
        {
            return false;
        }

        string dialogueId = result == BF_BattleResult.Victory
            ? battleService.CurrentLevelConfig.VictoryDialogueId
            : battleService.CurrentLevelConfig.DefeatDialogueId;

        if (string.IsNullOrWhiteSpace(dialogueId))
        {
            return false;
        }

        return dialogueManager.TryStartDialogue(
            dialogueId,
            unitId => battleService.GetUnitConfig(unitId) != null);
    }

    private void FinalizeResult()
    {
        if (_finalized)
        {
            return;
        }

        _finalized = true;
        BF_BattleService.Instance?.FinalizePendingResult();
    }

    #endregion
}
