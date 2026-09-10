using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 为矩形棋盘计算四方向加权可达格，并通过前驱表还原最低成本路径。
/// 移动范围搜索使用带预算上限的稳定 Dijkstra：每个格子允许被更低成本路线更新，
/// 前驱只在严格更低时覆盖，保证相同输入得到可重复路径。
/// </summary>
public static class BF_Pathfinder
{
    private static readonly Vector2Int[] Directions = {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    public static HashSet<Vector2Int> FindReachable(
        BF_BoardManager board,
        Vector2Int start,
        int budget,
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Dictionary<Vector2Int, int> cost)
    {
        HashSet<Vector2Int> reachable = new();
        NodeHeap heap = new();

        cameFrom.Clear();
        cost.Clear();
        heap.Push(start, 0);

        while (heap.TryPop(out Vector2Int current, out int currentCost))
        {
            // 跳过已被更低成本更新的过期堆节点。
            if (cost.TryGetValue(current, out int settledCost) && currentCost != settledCost)
            {
                continue;
            }

            // 非负成本下堆按成本单调出队；超过预算后不再有可达新格。
            if (currentCost > budget)
            {
                break;
            }

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int next = current + Directions[i];

                if (!board.TryGetMoveCost(next, out int stepCost))
                {
                    continue;
                }

                int nextCost = currentCost + stepCost;
                if (nextCost > budget)
                {
                    continue;
                }

                if (cost.TryGetValue(next, out int oldCost))
                {
                    // 等成本路线不覆盖已有前驱，保证路径稳定。
                    if (nextCost >= oldCost)
                    {
                        continue;
                    }

                    cost[next] = nextCost;
                    cameFrom[next] = current;
                    heap.Push(next, nextCost);
                }
                else
                {
                    cost[next] = nextCost;
                    cameFrom[next] = current;
                    reachable.Add(next);
                    heap.Push(next, nextCost);
                }
            }
        }

        return reachable;
    }

    public static List<Vector2Int> BuildPath(
        Vector2Int start,
        Vector2Int target,
        Dictionary<Vector2Int, Vector2Int> cameFrom)
    {
        List<Vector2Int> path = new();

        if (target == start || !cameFrom.ContainsKey(target))
        {
            return path;
        }

        Vector2Int current = target;
        while (current != start)
        {
            path.Add(current);

            if (!cameFrom.TryGetValue(current, out current))
            {
                path.Clear();
                return path;
            }
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// 按 (成本, 入队序号) 排序的稳定二叉最小堆；只服务本文件中的加权搜索。
    /// </summary>
    private sealed class NodeHeap
    {
        private struct HeapNode
        {
            public Vector2Int Position;
            public int Cost;
            public int Sequence;
        }

        private readonly List<HeapNode> _nodes = new();
        private int _nextSequence;

        public void Push(Vector2Int position, int cost)
        {
            _nodes.Add(new HeapNode
            {
                Position = position,
                Cost = cost,
                Sequence = _nextSequence++
            });

            int index = _nodes.Count - 1;
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (Compare(_nodes[parent], _nodes[index]) <= 0)
                {
                    break;
                }

                Swap(index, parent);
                index = parent;
            }
        }

        public bool TryPop(out Vector2Int position, out int cost)
        {
            if (_nodes.Count == 0)
            {
                position = default;
                cost = 0;
                return false;
            }

            HeapNode root = _nodes[0];
            position = root.Position;
            cost = root.Cost;

            int last = _nodes.Count - 1;
            _nodes[0] = _nodes[last];
            _nodes.RemoveAt(last);

            int index = 0;
            while (true)
            {
                int left = index * 2 + 1;
                int right = left + 1;

                if (left >= _nodes.Count)
                {
                    break;
                }

                int smaller = left;
                if (right < _nodes.Count && Compare(_nodes[right], _nodes[left]) < 0)
                {
                    smaller = right;
                }

                if (Compare(_nodes[index], _nodes[smaller]) <= 0)
                {
                    break;
                }

                Swap(index, smaller);
                index = smaller;
            }

            return true;
        }

        private static int Compare(HeapNode a, HeapNode b)
        {
            int costCompare = a.Cost.CompareTo(b.Cost);
            return costCompare != 0 ? costCompare : a.Sequence.CompareTo(b.Sequence);
        }

        private void Swap(int a, int b)
        {
            (_nodes[a], _nodes[b]) = (_nodes[b], _nodes[a]);
        }
    }
}
