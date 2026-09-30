using System;

/// <summary>
/// 节点快照：跨程序集显示所需的中立数据，不携带 GameObject、Panel 或其他 BF.UI 类型。
/// </summary>
public class BF_DialogueNodeSnapshot
{
    public string DialogueId { get; }
    public int NodeId { get; }
    public string SpeakerName { get; }
    public string SpeakerUnitId { get; }
    public string[] ParticipantUnitIds { get; }
    public string Text { get; }
    public bool HasChoices { get; }
    public BF_DialogueChoiceView[] Choices { get; }

    public BF_DialogueNodeSnapshot(
        string dialogueId, int nodeId,
        string speakerName, string speakerUnitId, string[] participantUnitIds,
        string text, bool hasChoices, BF_DialogueChoiceView[] choices)
    {
        DialogueId = dialogueId;
        NodeId = nodeId;
        SpeakerName = speakerName;
        SpeakerUnitId = speakerUnitId;
        ParticipantUnitIds = participantUnitIds ?? Array.Empty<string>();
        Text = text;
        HasChoices = hasChoices;
        Choices = choices;
    }
}

/// <summary>
/// 选项显示数据：只含身份与文本；点击后由 Panel 回发 ChoiceId。
/// </summary>
public class BF_DialogueChoiceView
{
    public string ChoiceId { get; }
    public string Text { get; }

    public BF_DialogueChoiceView(string choiceId, string text)
    {
        ChoiceId = choiceId;
        Text = text;
    }
}

// ---------------------------------------------------------------
// 事实事件（业务 → UI）
// ---------------------------------------------------------------

/// <summary>对话开始事实，携带入口节点快照。</summary>
public class BF_DialogueStartedEvent : IGameEvent
{
    public BF_DialogueNodeSnapshot Snapshot { get; }

    public BF_DialogueStartedEvent(BF_DialogueNodeSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}

/// <summary>节点变化事实。</summary>
public class BF_DialogueNodeChangedEvent : IGameEvent
{
    public BF_DialogueNodeSnapshot Snapshot { get; }

    public BF_DialogueNodeChangedEvent(BF_DialogueNodeSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}

/// <summary>
/// 对话结束事实；同一会话只发布一次。
/// Completed / Skipped / Failed 都必须解除流程等待。
/// </summary>
public class BF_DialogueEndedEvent : IGameEvent
{
    public string DialogueId { get; }
    public BF_DialogueEndReason EndReason { get; }
    public string ChoiceId { get; }
    public string OutcomeId { get; }
    public string FailureReason { get; }

    public BF_DialogueEndedEvent(
        string dialogueId, BF_DialogueEndReason endReason,
        string choiceId, string outcomeId, string failureReason)
    {
        DialogueId = dialogueId;
        EndReason = endReason;
        ChoiceId = choiceId;
        OutcomeId = outcomeId;
        FailureReason = failureReason;
    }
}

// ---------------------------------------------------------------
// 用户请求事件（UI → 业务）
// ---------------------------------------------------------------

/// <summary>推进一句；Typing 状态由 Panel 本地拦截为显示全文。</summary>
public class BF_DialogueAdvanceRequestEvent : IGameEvent
{
}

/// <summary>选择分支选项。</summary>
public class BF_DialogueChoiceRequestEvent : IGameEvent
{
    public string ChoiceId { get; }

    public BF_DialogueChoiceRequestEvent(string choiceId)
    {
        ChoiceId = choiceId;
    }
}

/// <summary>跳过当前对话。</summary>
public class BF_DialogueSkipRequestEvent : IGameEvent
{
}

// ---------------------------------------------------------------
// 战斗入场与标题卡
// ---------------------------------------------------------------

/// <summary>战斗运行时初始化完成事实；由 BF_BattleController 在 Setup 完成时发布。</summary>
public class BF_BattleRuntimeReadyEvent : IGameEvent
{
}

/// <summary>
/// 请求显示关卡标题卡；项目当前没有独立关卡名数据，显示格式与 LevelSelect 保持一致（第 N 关）。
/// </summary>
public class BF_LevelTitleRequestEvent : IGameEvent
{
    public int Level { get; }

    public BF_LevelTitleRequestEvent(int level)
    {
        Level = level;
    }
}

/// <summary>标题卡完成事实。</summary>
public class BF_LevelTitleCompletedEvent : IGameEvent
{
}

// ---------------------------------------------------------------
// 入场教程与统一阻塞窗口
// ---------------------------------------------------------------

/// <summary>标题卡完成后请求显示本关入场教程；无教程也必须回复完成事实。</summary>
public class BF_IntroTutorialRequestEvent : IGameEvent
{
}

/// <summary>本关入场教程完成、关闭、无需显示或显示失败的统一完成事实。</summary>
public class BF_IntroTutorialCompletedEvent : IGameEvent
{
}

/// <summary>会阻止暂停的顶层展示类型。</summary>
public enum BF_BlockingPresentation
{
    Dialogue,
    LevelTitle,
    Tutorial,
    BattleTransition,
}

/// <summary>跨程序集的统一阻塞窗口状态；GameMode 不依赖任何 BF.UI 类型。</summary>
public class BF_BlockingPresentationChangedEvent : IGameEvent
{
    public BF_BlockingPresentation Presentation { get; }
    public bool IsOpen { get; }

    public BF_BlockingPresentationChangedEvent(BF_BlockingPresentation presentation, bool isOpen)
    {
        Presentation = presentation;
        IsOpen = isOpen;
    }
}
