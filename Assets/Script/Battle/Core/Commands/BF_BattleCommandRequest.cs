using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 描述一次移动、普通攻击或结束行动。
/// </summary>
public class BF_BattleCommandRequest
{
    private BF_BattleCommandRequest(
        BF_BattleCommandType type,
        BF_BattleUnit actor,
        BF_BattleUnit target = null,
        BF_SkillConfigSO skill = null,
        IReadOnlyList<Vector2Int> path = null)
    {
        Type = type;
        Actor = actor;
        Target = target;
        Skill = skill;
        Path = path;
    }

    public BF_BattleCommandType Type { get; }
    public BF_BattleUnit Actor { get; }
    public BF_BattleUnit Target { get; }
    public BF_SkillConfigSO Skill { get; }
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

    public static BF_BattleCommandRequest CreateBasicAttack(
        BF_BattleUnit actor,
        BF_BattleUnit target)
    {
        return new BF_BattleCommandRequest(
            BF_BattleCommandType.BasicAttack,
            actor,
            target,
            actor.Config.BasicAttack);
    }

    public static BF_BattleCommandRequest CreateEndTurn(BF_BattleUnit actor)
    {
        return new BF_BattleCommandRequest(BF_BattleCommandType.EndTurn, actor);
    }
}
