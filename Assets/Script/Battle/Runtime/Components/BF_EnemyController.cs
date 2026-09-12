using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 为单个敌人选择目标，并按剩余 AP 连续产生战斗命令。
/// </summary>
public class BF_EnemyController : MonoBehaviour
{
    #region 序列化配置与引用

    [Header("棋盘引用")]
    [SerializeField]
    private BF_BoardManager _board;

    #endregion

    #region 运行时数据

    // 寻路缓存
    private readonly Dictionary<Vector2Int, Vector2Int> _cameFrom = new();
    private readonly Dictionary<Vector2Int, int> _cost = new();

    #endregion

    #region 回合执行

    public IEnumerator RunTurn(
        BF_BattleUnit enemy,
        IReadOnlyList<BF_BattleUnit> units,
        BF_BattleCommandExecutor executor,
        Action onCommandDone)
    {
        while (enemy.IsAlive && !enemy.IsTurnEnded && enemy.CurrentAP > 0)
        {
            BF_BattleUnit target = SelectTarget(enemy, units);
            BF_BattleCommandRequest request = BuildCommand(enemy, target);
            int oldAP = enemy.CurrentAP;
            Vector2Int oldPos = enemy.GridPos;

            LogCommand(request);
            yield return executor.Execute(request);
            onCommandDone?.Invoke();

            if (request.Type == BF_BattleCommandType.EndTurn)
            {
                yield break;
            }

            if (enemy.CurrentAP == oldAP && enemy.GridPos == oldPos)
            {
                yield return executor.Execute(BF_BattleCommandRequest.CreateEndTurn(enemy));
                yield break;
            }
        }
    }

    private void LogCommand(BF_BattleCommandRequest request)
    {
        string target = request.Type == BF_BattleCommandType.Skill ? $" -> {request.TargetPos}" : string.Empty;
        int pathCount = request.Path != null ? request.Path.Count : 0;
        string path = pathCount > 0 ? $", Path={pathCount}" : string.Empty;
        Debug.Log($"[BF] Enemy Command: {request.Actor.DisplayName} {request.Type}{target}{path}, AP={request.Actor.CurrentAP}");
    }

    #endregion

    #region 命令构建

    private BF_BattleCommandRequest BuildCommand(BF_BattleUnit enemy, BF_BattleUnit target)
    {
        BF_SkillConfigSO skill = enemy.Config.BasicAttack;
        if (target == null || skill == null)
        {
            return BF_BattleCommandRequest.CreateEndTurn(enemy);
        }

        int distance = GetDistance(enemy.GridPos, target.GridPos);
        if (distance <= skill.TargetRange)
        {
            return enemy.CanPay(skill.APCost)
                ? BF_BattleCommandRequest.CreateSkill(enemy, skill, target.GridPos)
                : BF_BattleCommandRequest.CreateEndTurn(enemy);
        }

        List<Vector2Int> path = FindMovePath(enemy, target, skill);
        return path.Count > 0
            ? BF_BattleCommandRequest.CreateMove(enemy, path)
            : BF_BattleCommandRequest.CreateEndTurn(enemy);
    }

    #endregion

    #region 目标选择

    private BF_BattleUnit SelectTarget(
        BF_BattleUnit enemy,
        IReadOnlyList<BF_BattleUnit> units)
    {
        BF_BattleUnit best = null;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < units.Count; i++)
        {
            BF_BattleUnit unit = units[i];
            if (unit.Team != BF_UnitTeam.Player || !unit.IsAlive)
            {
                continue;
            }

            int distance = GetDistance(enemy.GridPos, unit.GridPos);
            if (distance < bestDistance)
            {
                best = unit;
                bestDistance = distance;
            }
        }

        return best;
    }

    #endregion

    #region 移动寻路

    private List<Vector2Int> FindMovePath(
        BF_BattleUnit enemy,
        BF_BattleUnit target,
        BF_SkillConfigSO skill)
    {
        HashSet<Vector2Int> reachable = BF_Pathfinder.FindReachable(
            _board,
            enemy.GridPos,
            enemy.CurrentAP,
            _cameFrom,
            _cost);

        Vector2Int bestPos = enemy.GridPos;
        int bestDistance = GetDistance(enemy.GridPos, target.GridPos);
        int bestCost = 0;
        bool canAttack = false;

        foreach (Vector2Int pos in reachable)
        {
            int distance = GetDistance(pos, target.GridPos);
            int cost = _cost[pos];
            bool attackFromPos = distance <= skill.TargetRange
                && cost + skill.APCost <= enemy.CurrentAP;

            if (IsBetterCandidate(
                    attackFromPos, distance, cost, pos,
                    canAttack, bestDistance, bestCost, bestPos))
            {
                bestPos = pos;
                bestDistance = distance;
                bestCost = cost;
                canAttack = attackFromPos;
            }
        }

        return BF_Pathfinder.BuildPath(enemy.GridPos, bestPos, _cameFrom);
    }

    // 确定性决胜：可攻击优先，再按距离、累计成本、坐标升序。
    // 只有严格更优才替换，保证相同棋盘输入的候选结果可重复。
    private static bool IsBetterCandidate(
        bool canAttack,
        int distance,
        int cost,
        Vector2Int pos,
        bool bestCanAttack,
        int bestDistance,
        int bestCost,
        Vector2Int bestPos)
    {
        if (canAttack != bestCanAttack)
        {
            return canAttack;
        }

        if (distance != bestDistance)
        {
            return distance < bestDistance;
        }

        if (cost != bestCost)
        {
            return cost < bestCost;
        }

        if (pos.x != bestPos.x)
        {
            return pos.x < bestPos.x;
        }

        return pos.y < bestPos.y;
    }

    #endregion

    #region 距离工具

    private int GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    #endregion
}
