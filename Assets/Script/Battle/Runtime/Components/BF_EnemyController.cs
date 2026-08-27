using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 为单个敌人选择目标，并按剩余 AP 连续产生战斗命令。
/// </summary>
public class BF_EnemyController : MonoBehaviour
{
    [SerializeField]
    private BF_BoardManager _board;

    private readonly Dictionary<Vector2Int, Vector2Int> _cameFrom = new();
    private readonly Dictionary<Vector2Int, int> _cost = new();

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

    private BF_BattleCommandRequest BuildCommand(BF_BattleUnit enemy, BF_BattleUnit target)
    {
        BF_SkillConfigSO skill = enemy.Config.BasicAttack;
        if (target == null || skill == null)
        {
            return BF_BattleCommandRequest.CreateEndTurn(enemy);
        }

        int distance = GetDistance(enemy.GridPos, target.GridPos);
        if (distance <= skill.Range)
        {
            return enemy.CanPay(skill.APCost)
                ? BF_BattleCommandRequest.CreateBasicAttack(enemy, target)
                : BF_BattleCommandRequest.CreateEndTurn(enemy);
        }

        List<Vector2Int> path = FindMovePath(enemy, target, skill);
        return path.Count > 0
            ? BF_BattleCommandRequest.CreateMove(enemy, path)
            : BF_BattleCommandRequest.CreateEndTurn(enemy);
    }

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
        int bestCost = int.MaxValue;
        bool canAttack = false;

        foreach (Vector2Int pos in reachable)
        {
            int distance = GetDistance(pos, target.GridPos);
            int cost = _cost[pos];
            bool attackFromPos = distance <= skill.Range
                && cost + skill.APCost <= enemy.CurrentAP;

            if (attackFromPos)
            {
                if (!canAttack || cost < bestCost)
                {
                    bestPos = pos;
                    bestCost = cost;
                    canAttack = true;
                }

                continue;
            }

            if (!canAttack
                && (distance < bestDistance
                    || (bestPos != enemy.GridPos
                        && distance == bestDistance
                        && cost < bestCost)))
            {
                bestPos = pos;
                bestDistance = distance;
                bestCost = cost;
            }
        }

        return BF_Pathfinder.BuildPath(enemy.GridPos, bestPos, _cameFrom);
    }

    private void LogCommand(BF_BattleCommandRequest request)
    {
        string target = request.Target != null ? $" -> {request.Target.DisplayName}" : string.Empty;
        int pathCount = request.Path != null ? request.Path.Count : 0;
        string path = pathCount > 0 ? $", Path={pathCount}" : string.Empty;
        Debug.Log($"[BF] Enemy Command: {request.Actor.DisplayName} {request.Type}{target}{path}, AP={request.Actor.CurrentAP}");
    }

    private int GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
