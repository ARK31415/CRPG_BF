using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 当前关卡的棋盘管理器，负责生成格子、查询阻挡和维护单位占用。
/// </summary>
public class BF_BoardManager : MonoBehaviour
{
    #region 序列化配置与引用

    [Header("棋盘生成配置")]
    [SerializeField]
    private BF_LevelConfigSO _levelConfig;

    [SerializeField]
    private BF_BoardCell _cellPrefab;

    [SerializeField]
    private Vector2 _cellSize = Vector2.one;

    #endregion

    #region 运行时数据

    // 按 (x, y) 索引的格子数组
    private BF_BoardCell[,] _cells;

    #endregion

    #region 对外接口

    // 关卡配置与初始化状态
    public BF_LevelConfigSO LevelConfig => _levelConfig;
    public bool IsInitialized { get; private set; }

    #endregion

    #region 初始化与棋盘生成

    private void Awake()
    {
        InitBoard();
    }

    private void InitBoard()
    {
        if (_levelConfig == null || _cellPrefab == null)
        {
            Debug.LogError("Board config or cell prefab is missing.", this);
            return;
        }

        if (_levelConfig.Width <= 0
            || _levelConfig.Height <= 0
            || _cellSize.x <= 0f
            || _cellSize.y <= 0f)
        {
            Debug.LogError("Board size and cell size must be positive.", this);
            return;
        }

        BF_TerrainRuleSetSO terrainRules = _levelConfig.TerrainRules;
        if (terrainRules == null)
        {
            Debug.LogError("Level config terrain rules are missing.", this);
            return;
        }

        if (!terrainRules.Validate(out string ruleError))
        {
            Debug.LogError($"Terrain rules invalid: {ruleError}", this);
            return;
        }

        if (!HasRulesForAllTerrains(terrainRules))
        {
            Debug.LogError("Terrain rules must cover every TerrainType value.", this);
            return;
        }

        Dictionary<Vector2Int, TerrainType> overrides = new();
        IReadOnlyList<BF_TerrainCellData> terrainCells = _levelConfig.TerrainCells;
        for (int i = 0; i < terrainCells.Count; i++)
        {
            BF_TerrainCellData entry = terrainCells[i];
            if (entry == null)
            {
                Debug.LogError($"Terrain cell entry {i} is null.", this);
                return;
            }

            if (entry.Position.x < 0
                || entry.Position.y < 0
                || entry.Position.x >= _levelConfig.Width
                || entry.Position.y >= _levelConfig.Height)
            {
                Debug.LogError($"Terrain cell {entry.Position} is out of board bounds.", this);
                return;
            }

            if (overrides.ContainsKey(entry.Position))
            {
                Debug.LogError($"Duplicate terrain cell: {entry.Position}.", this);
                return;
            }

            overrides.Add(entry.Position, entry.TerrainType);
        }

        _cells = new BF_BoardCell[_levelConfig.Width, _levelConfig.Height];
        TerrainType defaultTerrain = _levelConfig.DefaultTerrain;

        for (int x = 0; x < _levelConfig.Width; x++)
        {
            for (int y = 0; y < _levelConfig.Height; y++)
            {
                Vector2Int pos = new(x, y);
                TerrainType terrainType = overrides.TryGetValue(pos, out TerrainType overrideType)
                    ? overrideType
                    : defaultTerrain;

                if (!terrainRules.TryGetRule(terrainType, out BF_TerrainRuleData rule))
                {
                    Debug.LogError($"Terrain rule missing for {terrainType} at {pos}.", this);
                    return;
                }

                BF_BoardCell cell = Instantiate(
                    _cellPrefab,
                    GridToWorld(pos),
                    Quaternion.identity,
                    transform);

                cell.name = $"Cell_{x}_{y}";
                cell.Init(pos, terrainType, rule.Passable, rule.MoveCost);

                _cells[x, y] = cell;
            }
        }

        IsInitialized = true;
    }

    private bool HasRulesForAllTerrains(BF_TerrainRuleSetSO terrainRules)
    {
        TerrainType[] values = (TerrainType[])System.Enum.GetValues(typeof(TerrainType));
        for (int i = 0; i < values.Length; i++)
        {
            if (!terrainRules.TryGetRule(values[i], out _))
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region 格子查询

    public bool IsInside(Vector2Int pos)
    {
        return IsInitialized
            && pos.x >= 0
            && pos.y >= 0
            && pos.x < _cells.GetLength(0)
            && pos.y < _cells.GetLength(1);
    }

    public bool TryGetCell(Vector2Int pos, out BF_BoardCell cell)
    {
        if (!IsInside(pos))
        {
            cell = null;
            return false;
        }

        cell = _cells[pos.x, pos.y];
        return true;
    }

    public bool IsBlocked(Vector2Int pos)
    {
        return TryGetCell(pos, out BF_BoardCell cell)
            && cell.TerrainType == TerrainType.Blocked;
    }

    public bool IsOccupied(Vector2Int pos)
    {
        return TryGetCell(pos, out BF_BoardCell cell) && cell.IsOccupied;
    }

    public bool TryGetOccupant(Vector2Int pos, out GameObject occupant)
    {
        if (!TryGetCell(pos, out BF_BoardCell cell) || !cell.IsOccupied)
        {
            occupant = null;
            return false;
        }

        occupant = cell.Occupant;
        return true;
    }

    public bool CanEnter(Vector2Int pos)
    {
        return TryGetCell(pos, out BF_BoardCell cell) && cell.CanEnter;
    }

    /// <summary>
    /// 返回进入某格的移动成本。越界、不可通行或已被占用时返回 false，
    /// 寻路与执行校验都只通过该查询读取地形成本。
    /// </summary>
    public bool TryGetMoveCost(Vector2Int pos, out int moveCost)
    {
        if (TryGetCell(pos, out BF_BoardCell cell) && cell.Passable && !cell.IsOccupied)
        {
            moveCost = cell.MoveCost;
            return true;
        }

        moveCost = 0;
        return false;
    }

    /// <summary>
    /// 验证一条不包含起点的移动路径：逐格四方向连续、界内、可通行且未占用，
    /// 并按当前地形累计真实进入成本。失败时返回第一条不合法原因。
    /// </summary>
    public bool TryValidateMovePath(
        Vector2Int start,
        IReadOnlyList<Vector2Int> path,
        out int totalCost,
        out string failReason)
    {
        totalCost = 0;

        if (path == null || path.Count == 0)
        {
            failReason = "Path is empty";
            return false;
        }

        Vector2Int previous = start;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int step = path[i];

            if (!IsAdjacent(previous, step))
            {
                failReason = $"Step {i} {step} is not adjacent to {previous}";
                return false;
            }

            if (!TryGetCell(step, out BF_BoardCell cell))
            {
                failReason = $"Step {i} {step} is out of board bounds";
                return false;
            }

            if (!cell.Passable)
            {
                failReason = $"Step {i} {step} is blocked";
                return false;
            }

            if (cell.IsOccupied)
            {
                failReason = $"Step {i} {step} is occupied";
                return false;
            }

            totalCost += cell.MoveCost;
            previous = step;
        }

        failReason = string.Empty;
        return true;
    }

    private static bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }

    #endregion

    #region 占用变更

    public bool TryOccupy(Vector2Int pos, GameObject occupant)
    {
        if (occupant == null || !TryGetCell(pos, out BF_BoardCell cell) || !cell.CanEnter)
        {
            return false;
        }

        cell.SetOccupant(occupant);
        return true;
    }

    public bool TryVacate(Vector2Int pos, GameObject occupant)
    {
        if (occupant == null
            || !TryGetCell(pos, out BF_BoardCell cell)
            || cell.Occupant != occupant)
        {
            return false;
        }

        cell.SetOccupant(null);
        return true;
    }

    public bool TryMoveOccupant(Vector2Int from, Vector2Int to, GameObject occupant)
    {
        if (occupant == null || from == to)
        {
            return false;
        }

        if (!TryGetCell(from, out BF_BoardCell fromCell)
            || fromCell.Occupant != occupant
            || !TryGetCell(to, out BF_BoardCell toCell)
            || !toCell.CanEnter)
        {
            return false;
        }

        fromCell.SetOccupant(null);
        toCell.SetOccupant(occupant);
        return true;
    }

    #endregion

    #region 坐标换算

    public Vector3 GridToWorld(Vector2Int pos)
    {
        return GridCornerToWorld(pos) + new Vector3(_cellSize.x * 0.5f, _cellSize.y * 0.5f, 0f);
    }

    /// <summary>
    /// 将世界位置换算为逻辑格坐标；调用方仍需用 IsInside 判断格子是否存在。
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;
        return new Vector2Int(Mathf.FloorToInt(localPos.x / _cellSize.x), Mathf.FloorToInt(localPos.y / _cellSize.y));
    }

    private Vector3 GridCornerToWorld(Vector2Int pos)
    {
        return transform.position + new Vector3(
            pos.x * _cellSize.x,
            pos.y * _cellSize.y,
            0f);
    }

    #endregion

    #region 编辑器辅助

    private void OnDrawGizmos()
    {
        if (_levelConfig == null || _cellSize.x <= 0f || _cellSize.y <= 0f)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        for (int x = 0; x <= _levelConfig.Width; x++)
        {
            Gizmos.DrawLine(
                GridCornerToWorld(new Vector2Int(x, 0)),
                GridCornerToWorld(new Vector2Int(x, _levelConfig.Height)));
        }

        for (int y = 0; y <= _levelConfig.Height; y++)
        {
            Gizmos.DrawLine(
                GridCornerToWorld(new Vector2Int(0, y)),
                GridCornerToWorld(new Vector2Int(_levelConfig.Width, y)));
        }
    }

    #endregion
}
