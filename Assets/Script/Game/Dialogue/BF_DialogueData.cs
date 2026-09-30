using System;

/// <summary>
/// 对话 Catalog 数据根（SchemaVersion 1）。
/// 由工程内 JSON/TextAsset 提供，Player 只读取工程内产物。
/// </summary>
[Serializable]
public class BF_DialogueCatalogData
{
    public int schemaVersion = 1;
    public BF_DialogueDefinition[] dialogues = Array.Empty<BF_DialogueDefinition>();
}

/// <summary>
/// 一段对话的只读定义：入口节点与节点集合。
/// </summary>
[Serializable]
public class BF_DialogueDefinition
{
    public string dialogueId;
    public int startNodeId;

    // 稳定参与者槽位：索引 0 为左槽、索引 1 为右槽；第一版只允许 0～2 个。
    public string[] participantUnitIds = Array.Empty<string>();

    public BF_DialogueNodeDefinition[] nodes = Array.Empty<BF_DialogueNodeDefinition>();
}

/// <summary>
/// 单个对话节点：说话人、正文与跳转；nextNodeId 为 -1 表示对话结束。
/// 有 choices 时由玩家选择决定跳转，nextNodeId 不参与推进。
/// </summary>
[Serializable]
public class BF_DialogueNodeDefinition
{
    public int nodeId;
    public string speakerName;

    // 当前说话人的单位配置 Id；空表示旁白 / 系统，不占槽位。
    public string speakerUnitId;

    public string text;
    public int nextNodeId = -1;
    public BF_DialogueChoiceDefinition[] choices = Array.Empty<BF_DialogueChoiceDefinition>();
}

/// <summary>
/// 分支选项：稳定身份、显示文本、节点跳转与可空的结果语义。
/// OutcomeId 只表达结果语义，不直接发奖励或修改战斗。
/// </summary>
[Serializable]
public class BF_DialogueChoiceDefinition
{
    public string choiceId;
    public string text;
    public int targetNodeId;
    public string outcomeId;
}

/// <summary>
/// 对话结束原因；Completed / Skipped / Failed 都必须解除流程等待。
/// </summary>
public enum BF_DialogueEndReason
{
    Completed,
    Skipped,
    Failed,
}
