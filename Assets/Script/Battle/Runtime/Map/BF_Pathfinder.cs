using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 为当前矩形棋盘计算四方向可达格，并通过前驱表还原最短路径。
/// </summary>
public static class BF_Pathfinder {
    private static readonly Vector2Int[] Directions = {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    public static HashSet<Vector2Int> FindReachable(
        BF_BoardManager board,
        Vector2Int start,
        int moveRange,
        Dictionary<Vector2Int, Vector2Int> cameFrom) {
        HashSet<Vector2Int> reachable = new();
        Queue<Vector2Int> open = new();
        Dictionary<Vector2Int, int> distance = new();

        cameFrom.Clear();
        open.Enqueue(start);
        distance[start] = 0;

        while (open.Count > 0) {
            Vector2Int current = open.Dequeue();
            int nextDistance = distance[current] + 1;

            if (nextDistance > moveRange) {
                continue;
            }

            for (int i = 0; i < Directions.Length; i++) {
                Vector2Int next = current + Directions[i];

                if (distance.ContainsKey(next) || !board.CanEnter(next)) {
                    continue;
                }

                distance[next] = nextDistance;
                cameFrom[next] = current;
                reachable.Add(next);
                open.Enqueue(next);
            }
        }

        return reachable;
    }

    public static List<Vector2Int> BuildPath(
        Vector2Int start,
        Vector2Int target,
        Dictionary<Vector2Int, Vector2Int> cameFrom) {
        List<Vector2Int> path = new();

        if (target == start || !cameFrom.ContainsKey(target)) {
            return path;
        }

        Vector2Int current = target;
        while (current != start) {
            path.Add(current);

            if (!cameFrom.TryGetValue(current, out current)) {
                path.Clear();
                return path;
            }
        }

        path.Reverse();
        return path;
    }
}
