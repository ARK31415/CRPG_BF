using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 描述一次移动、技能或结束行动。
/// </summary>
public class BF_BattleCommandRequest
{
    private BF_BattleCommandRequest(
        BF_BattleCommandType type,
        BF_BattleUnit actor,
        BF_SkillConfigSO skill = null,
        Vector2Int targetPos = default,
        IReadOnlyList<Vector2Int> path = null)
    {
        Type = type;
        Actor = actor;
        Skill = skill;
        TargetPos = targetPos;
        Path = path;
    }

    public BF_BattleCommandType Type { get; }
    public BF_BattleUnit Actor { get; }
    public BF_SkillConfigSO Skill { get; }
    public Vector2Int TargetPos { get; }
    public IReadOnlyList<Vector2Int> Path { get; }

    public static BF_BattleCommandRequest CreateMove(
        BF_BattleUnit actor,
        IReadOnlyList<Vector2Int> path)
    {
        return new BF_BattleCommandRequest(
            BF_BattleCommandType.Move,
            actor,
            path: new List<Vector2Int>(path));
    }

    public static BF_BattleCommandRequest CreateSkill(
        BF_BattleUnit actor,
        BF_SkillConfigSO skill,
        Vector2Int targetPos)
    {
        return new BF_BattleCommandRequest(
            BF_BattleCommandType.Skill,
            actor,
            skill,
            targetPos);
    }

    public static BF_BattleCommandRequest CreateEndTurn(BF_BattleUnit actor)
    {
        return new BF_BattleCommandRequest(BF_BattleCommandType.EndTurn, actor);
    }
}
