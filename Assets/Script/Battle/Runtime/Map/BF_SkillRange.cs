using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家预览、实际命中和 AI 共用的技能格子规则。
/// </summary>
public static class BF_SkillRange
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    public static HashSet<Vector2Int> GetTargetCells(
        BF_BoardManager board,
        Vector2Int actorPos,
        BF_SkillConfigSO skill)
    {
        HashSet<Vector2Int> cells = new();

        if (skill.TargetType == BF_SkillTargetType.Direction)
        {
            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int pos = actorPos + Directions[i];
                if (board.IsInside(pos))
                {
                    cells.Add(pos);
                }
            }

            return cells;
        }

        int range = skill.TargetRange;
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(y);
                if (distance == 0 || distance > range)
                {
                    continue;
                }

                Vector2Int pos = actorPos + new Vector2Int(x, y);
                if (board.IsInside(pos))
                {
                    cells.Add(pos);
                }
            }
        }

        return cells;
    }

    public static List<Vector2Int> GetAreaCells(
        BF_BoardManager board,
        BF_BattleUnit actor,
        Vector2Int targetPos,
        BF_SkillConfigSO skill)
    {
        return skill.AreaType switch
        {
            BF_SkillAreaType.ProjectileLine => GetProjectilePath(board, actor, targetPos, skill.TargetRange),
            BF_SkillAreaType.Square => GetSquare(board, targetPos, skill.AreaSize),
            BF_SkillAreaType.FrontT => GetFrontT(board, actor.GridPos, targetPos),
            _ => board.IsInside(targetPos) ? new List<Vector2Int> { targetPos } : new List<Vector2Int>()
        };
    }

    public static List<Vector2Int> GetProjectilePath(
        BF_BoardManager board,
        BF_BattleUnit actor,
        Vector2Int targetPos,
        int range)
    {
        List<Vector2Int> path = new();
        Vector2Int direction = GetDirection(actor.GridPos, targetPos);

        for (int i = 1; i <= range; i++)
        {
            Vector2Int pos = actor.GridPos + direction * i;
            if (!board.IsInside(pos))
            {
                break;
            }

            path.Add(pos);
            if (board.IsBlocked(pos))
            {
                break;
            }

            if (!board.TryGetOccupant(pos, out GameObject occupant))
            {
                continue;
            }

            if (!occupant.TryGetComponent(out BF_BattleUnit unit) || unit.Team != actor.Team)
            {
                break;
            }
        }

        return path;
    }

    private static List<Vector2Int> GetSquare(BF_BoardManager board, Vector2Int center, int size)
    {
        List<Vector2Int> cells = new();
        int radius = Mathf.Max(0, size / 2);

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);
                if (board.IsInside(pos))
                {
                    cells.Add(pos);
                }
            }
        }

        return cells;
    }

    private static List<Vector2Int> GetFrontT(
        BF_BoardManager board,
        Vector2Int actorPos,
        Vector2Int targetPos)
    {
        List<Vector2Int> cells = new();
        Vector2Int direction = GetDirection(actorPos, targetPos);
        Vector2Int center = actorPos + direction;
        Vector2Int side = new(-direction.y, direction.x);

        AddIfInside(board, cells, center);
        AddIfInside(board, cells, center + side);
        AddIfInside(board, cells, center - side);
        return cells;
    }

    private static Vector2Int GetDirection(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
        {
            return delta.x >= 0 ? Vector2Int.right : Vector2Int.left;
        }

        return delta.y >= 0 ? Vector2Int.up : Vector2Int.down;
    }

    private static void AddIfInside(
        BF_BoardManager board,
        List<Vector2Int> cells,
        Vector2Int pos)
    {
        if (board.IsInside(pos))
        {
            cells.Add(pos);
        }
    }
}
