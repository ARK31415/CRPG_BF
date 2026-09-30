using System;
using UnityEngine;

/// <summary>
/// Persistent 对话业务协调器：持有唯一活动会话，负责节点推进、Choice、Outcome、
/// 结束原因与失败回退；订阅用户请求事件，发布节点快照与结束事实。
/// 不决定播放时机、不直接操作 BattleUnit、不直接发奖励。
/// </summary>
public class BF_DialogueManager : Singleton<BF_DialogueManager>
{
    #region 序列化配置

    [Header("对话数据")]
    [SerializeField]
    private TextAsset _catalogAsset;

    #endregion

    #region 运行时数据

    private readonly BF_DialogueCatalog _catalog = new();
    private IDisposable _advanceSubscription;
    private IDisposable _choiceSubscription;
    private IDisposable _skipSubscription;
    private BF_DialogueDefinition _activeDialogue;
    private BF_DialogueNodeDefinition _activeNode;
    private string _pendingOutcomeId = string.Empty;
    private string _selectedChoiceId = string.Empty;
    private bool _endPublished;

    #endregion

    #region 对外状态

    public bool IsActive => _activeDialogue != null;

    #endregion

    #region 生命周期

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        if (!_catalog.TryInitialize(_catalogAsset, out string failureReason))
        {
            // 不阻断启动：只停用本次运行的对话能力。
            Debug.LogError($"[BF_DialogueManager] 对话 Catalog 不可用，本次运行停用对话功能：{failureReason}");
        }
    }

    private void OnEnable()
    {
        if (Instance != this)
        {
            return;
        }

        _advanceSubscription = GameEventBus.Instance.Subscribe<BF_DialogueAdvanceRequestEvent>(OnAdvanceRequested);
        _choiceSubscription = GameEventBus.Instance.Subscribe<BF_DialogueChoiceRequestEvent>(OnChoiceRequested);
        _skipSubscription = GameEventBus.Instance.Subscribe<BF_DialogueSkipRequestEvent>(OnSkipRequested);
    }

    private void OnDisable()
    {
        _advanceSubscription?.Dispose();
        _advanceSubscription = null;
        _choiceSubscription?.Dispose();
        _choiceSubscription = null;
        _skipSubscription?.Dispose();
        _skipSubscription = null;
    }

    #endregion

    #region 对外接口

    /// <summary>
    /// 请求播放一段对话；已有活动会话时拒绝。
    /// 返回 false 时调用方必须继续后续流程（安全回退）。
    /// </summary>
    public bool TryStartDialogue(string dialogueId, Func<string, bool> canResolveUnit)
    {
        if (IsActive)
        {
            Debug.LogWarning($"[BF_DialogueManager] 已有活动会话，拒绝重复开始：{dialogueId}");
            return false;
        }

        if (!_catalog.TryGetDialogue(dialogueId, out BF_DialogueDefinition definition))
        {
            Debug.LogError($"[BF_DialogueManager] 找不到对话：{dialogueId}");
            return false;
        }

        if (!_catalog.TryValidateParticipantUnits(definition, canResolveUnit, out string participantFailure))
        {
            Debug.LogError($"[BF_DialogueManager] 对话参与者配置无效：{participantFailure}");
            return false;
        }

        if (!TryFindNode(definition, definition.startNodeId, out BF_DialogueNodeDefinition startNode))
        {
            Debug.LogError($"[BF_DialogueManager] 对话 {dialogueId} 入口节点 {definition.startNodeId} 无效");
            return false;
        }

        _activeDialogue = definition;
        _activeNode = startNode;
        _pendingOutcomeId = string.Empty;
        _selectedChoiceId = string.Empty;
        _endPublished = false;

        GameEventBus.Instance.Publish(new BF_DialogueStartedEvent(BuildSnapshot()));
        return true;
    }

    #endregion

    #region 请求处理

    private void OnAdvanceRequested(BF_DialogueAdvanceRequestEvent requestEvent)
    {
        if (!IsActive || _endPublished)
        {
            return;
        }

        // Choosing 状态只接受有效 ChoiceId。
        if (HasChoices(_activeNode))
        {
            return;
        }

        if (_activeNode.nextNodeId == -1)
        {
            EndDialogue(BF_DialogueEndReason.Completed, string.Empty);
            return;
        }

        if (!TryFindNode(_activeDialogue, _activeNode.nextNodeId, out BF_DialogueNodeDefinition next))
        {
            EndDialogue(BF_DialogueEndReason.Failed, $"nextNodeId {_activeNode.nextNodeId} 不存在");
            return;
        }

        _activeNode = next;
        GameEventBus.Instance.Publish(new BF_DialogueNodeChangedEvent(BuildSnapshot()));
    }

    private void OnChoiceRequested(BF_DialogueChoiceRequestEvent requestEvent)
    {
        if (!IsActive || _endPublished || !HasChoices(_activeNode))
        {
            return;
        }

        BF_DialogueChoiceDefinition choice = FindChoice(_activeNode, requestEvent.ChoiceId);
        if (choice == null)
        {
            Debug.LogError($"[BF_DialogueManager] 无效选项：{requestEvent.ChoiceId}");
            return;
        }

        _selectedChoiceId = choice.choiceId;
        if (!string.IsNullOrWhiteSpace(choice.outcomeId))
        {
            _pendingOutcomeId = choice.outcomeId;
        }

        if (!TryFindNode(_activeDialogue, choice.targetNodeId, out BF_DialogueNodeDefinition next))
        {
            EndDialogue(BF_DialogueEndReason.Failed, $"targetNodeId {choice.targetNodeId} 不存在");
            return;
        }

        _activeNode = next;
        GameEventBus.Instance.Publish(new BF_DialogueNodeChangedEvent(BuildSnapshot()));
    }

    private void OnSkipRequested(BF_DialogueSkipRequestEvent requestEvent)
    {
        if (!IsActive || _endPublished)
        {
            return;
        }

        EndDialogue(BF_DialogueEndReason.Skipped, string.Empty);
    }

    #endregion

    #region 结束

    private void EndDialogue(BF_DialogueEndReason reason, string failureReason)
    {
        if (_endPublished)
        {
            return;
        }

        _endPublished = true;

        string dialogueId = _activeDialogue != null ? _activeDialogue.dialogueId : string.Empty;
        string choiceId = _selectedChoiceId;
        string outcomeId = _pendingOutcomeId;

        _activeDialogue = null;
        _activeNode = null;
        _selectedChoiceId = string.Empty;
        _pendingOutcomeId = string.Empty;

        if (reason == BF_DialogueEndReason.Failed)
        {
            // 失败必须解除等待：记录一次明确 Error 后按 Completed/Skipped 同样的方式发布结束事实。
            Debug.LogError($"[BF_DialogueManager] 对话 {dialogueId} 失败并安全跳过：{failureReason}");
        }

        GameEventBus.Instance.Publish(
            new BF_DialogueEndedEvent(dialogueId, reason, choiceId, outcomeId, failureReason));
    }

    #endregion

    #region 内部

    private BF_DialogueNodeSnapshot BuildSnapshot()
    {
        BF_DialogueChoiceDefinition[] source = _activeNode.choices;
        int count = source != null ? source.Length : 0;
        var choices = new BF_DialogueChoiceView[count];
        for (int i = 0; i < count; i++)
        {
            choices[i] = new BF_DialogueChoiceView(source[i].choiceId, source[i].text);
        }

        return new BF_DialogueNodeSnapshot(
            _activeDialogue.dialogueId,
            _activeNode.nodeId,
            _activeNode.speakerName,
            _activeNode.speakerUnitId,
            (string[])(_activeDialogue.participantUnitIds ?? Array.Empty<string>()).Clone(),
            _activeNode.text,
            count > 0,
            choices);
    }

    private static bool HasChoices(BF_DialogueNodeDefinition node)
    {
        return node != null && node.choices != null && node.choices.Length > 0;
    }

    private static bool TryFindNode(BF_DialogueDefinition dialogue, int nodeId, out BF_DialogueNodeDefinition node)
    {
        for (int i = 0; i < dialogue.nodes.Length; i++)
        {
            if (dialogue.nodes[i].nodeId == nodeId)
            {
                node = dialogue.nodes[i];
                return true;
            }
        }

        node = null;
        return false;
    }

    private static BF_DialogueChoiceDefinition FindChoice(BF_DialogueNodeDefinition node, string choiceId)
    {
        for (int i = 0; i < node.choices.Length; i++)
        {
            if (node.choices[i].choiceId == choiceId)
            {
                return node.choices[i];
            }
        }

        return null;
    }

    #endregion
}
