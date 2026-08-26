using UnityEngine;

/// <summary>
/// 当前关卡的棋盘管理器，负责生成格子、查询阻挡和维护单位占用。
/// </summary>
public class BF_BoardManager : MonoBehaviour
{
    [SerializeField] private BF_LevelConfigSO _levelConfig;
    [SerializeField] private BF_BoardCell _cellPrefab;
    [SerializeField] private Vector2 _cellSize = Vector2.one;

    private BF_BoardCell[,] _cells;

    public BF_LevelConfigSO LevelConfig => _levelConfig;
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        InitBoard();
    }

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

        _cells = new BF_BoardCell[_levelConfig.Width, _levelConfig.Height];

        for (int x = 0; x < _levelConfig.Width; x++)
        {
            for (int y = 0; y < _levelConfig.Height; y++)
            {
                Vector2Int pos = new(x, y);
                BF_BoardCell cell = Instantiate(
                    _cellPrefab,
                    GridToWorld(pos),
                    Quaternion.identity,
                    transform);

                cell.name = $"Cell_{x}_{y}";
                cell.Init(
                    pos,
                    _levelConfig.BlockedCells.Contains(pos)
                        ? TerrainType.Blocked
                        : TerrainType.Normal);

                _cells[x, y] = cell;
            }
        }

        IsInitialized = true;
    }

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

    private Vector3 GridCornerToWorld(Vector2Int pos)
    {
        return transform.position + new Vector3(
            pos.x * _cellSize.x,
            pos.y * _cellSize.y,
            0f);
    }
}
