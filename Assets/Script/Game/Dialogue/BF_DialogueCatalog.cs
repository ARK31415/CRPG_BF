using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 纯 C# 对话数据目录：解析、整份校验、索引和按 ID 查询。
/// 不是 MonoBehaviour、不是 Singleton；由 BF_DialogueManager 创建和持有。
/// 校验失败时整份不可用，不生成部分可用的静默结果。
/// </summary>
public class BF_DialogueCatalog
{
    #region 索引数据

    private readonly Dictionary<string, BF_DialogueDefinition> _byDialogueId = new();

    #endregion

    #region 对外状态

    public bool IsAvailable { get; private set; }

    #endregion

    #region 初始化

    /// <summary>
    /// 解析并校验整份 Catalog；任何一条失败都会使整份不可用。
    /// </summary>
    public bool TryInitialize(TextAsset catalogAsset, out string failureReason)
    {
        failureReason = string.Empty;
        IsAvailable = false;
        _byDialogueId.Clear();

        if (catalogAsset == null || string.IsNullOrWhiteSpace(catalogAsset.text))
        {
            failureReason = "Dialogue Catalog TextAsset 为空";
            return false;
        }

        BF_DialogueCatalogData catalog;
        try
        {
            catalog = JsonUtility.FromJson<BF_DialogueCatalogData>(catalogAsset.text);
        }
        catch (System.Exception exception)
        {
            failureReason = $"JSON 解析失败：{exception.Message}";
            return false;
        }

        if (catalog == null)
        {
            failureReason = "JSON 解析结果为 null";
            return false;
        }

        if (catalog.schemaVersion != 1)
        {
            failureReason = $"不支持的 schemaVersion：{catalog.schemaVersion}";
            return false;
        }

        BF_DialogueDefinition[] dialogues = catalog.dialogues;
        if (dialogues == null || dialogues.Length == 0)
        {
            failureReason = "Catalog 没有对话条目";
            return false;
        }

        for (int i = 0; i < dialogues.Length; i++)
        {
            if (!ValidateDialogue(dialogues[i], i, out failureReason))
            {
                return false;
            }

            _byDialogueId.Add(dialogues[i].dialogueId, dialogues[i]);
        }

        IsAvailable = true;
        return true;
    }

    #endregion

    #region 查询

    public bool TryGetDialogue(string dialogueId, out BF_DialogueDefinition definition)
    {
        if (!IsAvailable || string.IsNullOrEmpty(dialogueId))
        {
            definition = null;
            return false;
        }

        return _byDialogueId.TryGetValue(dialogueId, out definition);
    }

    #endregion

    #region 校验

    private bool ValidateDialogue(BF_DialogueDefinition dialogue, int index, out string failureReason)
    {
        failureReason = string.Empty;

        if (dialogue == null)
        {
            failureReason = $"第 {index} 段对话为 null";
            return false;
        }

        if (string.IsNullOrWhiteSpace(dialogue.dialogueId))
        {
            failureReason = $"第 {index} 段对话的 dialogueId 为空";
            return false;
        }

        if (_byDialogueId.ContainsKey(dialogue.dialogueId))
        {
            failureReason = $"dialogueId 重复：{dialogue.dialogueId}";
            return false;
        }

        BF_DialogueNodeDefinition[] nodes = dialogue.nodes;
        if (nodes == null || nodes.Length == 0)
        {
            failureReason = $"对话 {dialogue.dialogueId} 没有节点";
            return false;
        }

        var nodeIds = new HashSet<int>();
        string[] participants = dialogue.participantUnitIds ?? System.Array.Empty<string>();
        if (participants.Length > 2)
        {
            failureReason = $"对话 {dialogue.dialogueId} 的参与者超过第一版上限 2 个";
            return false;
        }

        var participantIds = new HashSet<string>();
        for (int p = 0; p < participants.Length; p++)
        {
            if (string.IsNullOrWhiteSpace(participants[p]))
            {
                failureReason = $"对话 {dialogue.dialogueId} 的 participantUnitIds[{p}] 为空";
                return false;
            }

            if (!participantIds.Add(participants[p]))
            {
                failureReason = $"对话 {dialogue.dialogueId} 的参与者重复：{participants[p]}";
                return false;
            }
        }

        for (int n = 0; n < nodes.Length; n++)
        {
            BF_DialogueNodeDefinition node = nodes[n];
            if (node == null)
            {
                failureReason = $"对话 {dialogue.dialogueId} 第 {n} 个节点为 null";
                return false;
            }

            if (!nodeIds.Add(node.nodeId))
            {
                failureReason = $"对话 {dialogue.dialogueId} 的 nodeId 重复：{node.nodeId}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(node.text))
            {
                failureReason = $"对话 {dialogue.dialogueId} 节点 {node.nodeId} 的 text 为空";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(node.speakerUnitId)
                && !participantIds.Contains(node.speakerUnitId))
            {
                failureReason = $"对话 {dialogue.dialogueId} 节点 {node.nodeId} 的说话人 {node.speakerUnitId} 不属于参与者";
                return false;
            }
        }

        if (!nodeIds.Contains(dialogue.startNodeId))
        {
            failureReason = $"对话 {dialogue.dialogueId} 的 startNodeId {dialogue.startNodeId} 不存在";
            return false;
        }

        // 跳转、选项与 Outcome 校验：一段对话最多配置一个非空 OutcomeId。
        string firstOutcomeId = string.Empty;
        for (int n = 0; n < nodes.Length; n++)
        {
            BF_DialogueNodeDefinition node = nodes[n];
            BF_DialogueChoiceDefinition[] choices = node.choices;

            if (choices == null || choices.Length == 0)
            {
                if (node.nextNodeId != -1 && !nodeIds.Contains(node.nextNodeId))
                {
                    failureReason = $"对话 {dialogue.dialogueId} 节点 {node.nodeId} 的 nextNodeId {node.nextNodeId} 不存在";
                    return false;
                }

                continue;
            }

            var choiceIds = new HashSet<string>();
            for (int c = 0; c < choices.Length; c++)
            {
                BF_DialogueChoiceDefinition choice = choices[c];
                if (choice == null)
                {
                    failureReason = $"对话 {dialogue.dialogueId} 节点 {node.nodeId} 第 {c} 个选项为 null";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(choice.choiceId))
                {
                    failureReason = $"对话 {dialogue.dialogueId} 节点 {node.nodeId} 的 choiceId 为空";
                    return false;
                }

                if (!choiceIds.Add(choice.choiceId))
                {
                    failureReason = $"对话 {dialogue.dialogueId} 节点 {node.nodeId} 的 choiceId 重复：{choice.choiceId}";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(choice.text))
                {
                    failureReason = $"对话 {dialogue.dialogueId} 选项 {choice.choiceId} 的 text 为空";
                    return false;
                }

                if (!nodeIds.Contains(choice.targetNodeId))
                {
                    failureReason = $"对话 {dialogue.dialogueId} 选项 {choice.choiceId} 的 targetNodeId {choice.targetNodeId} 不存在";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(choice.outcomeId))
                {
                    if (firstOutcomeId.Length == 0)
                    {
                        firstOutcomeId = choice.outcomeId;
                    }
                    else
                    {
                        failureReason = $"对话 {dialogue.dialogueId} 出现重复的非空 OutcomeId：{firstOutcomeId} 与 {choice.outcomeId}";
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>在开始会话前，用调用方提供的单位目录校验所有参与者是否可解析。</summary>
    public bool TryValidateParticipantUnits(
        BF_DialogueDefinition dialogue,
        System.Func<string, bool> canResolveUnit,
        out string failureReason)
    {
        failureReason = string.Empty;
        if (dialogue == null || canResolveUnit == null)
        {
            failureReason = "参与者单位校验器缺失";
            return false;
        }

        string[] participants = dialogue.participantUnitIds ?? System.Array.Empty<string>();
        for (int i = 0; i < participants.Length; i++)
        {
            if (!canResolveUnit(participants[i]))
            {
                failureReason = $"对话 {dialogue.dialogueId} 的参与者无法解析：{participants[i]}";
                return false;
            }
        }

        return true;
    }

    #endregion
}
