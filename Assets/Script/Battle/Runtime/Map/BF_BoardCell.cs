using UnityEngine;

/// <summary>
/// 单个逻辑棋盘格的轻量运行时组件。
///
/// 负责保存逻辑坐标、静态地形和动态占用者；不负责寻路、输入或单位移动。
/// </summary>
public class BF_BoardCell : MonoBehaviour
{
    private SpriteRenderer _defaultSpriteRenderer;

    [SerializeField]
    private SpriteRenderer _selectionSpriteRenderer;

    [SerializeField]
    private Color _reachableColor;

    [SerializeField]
    private Color _blockedColor;

    [SerializeField]
    private Color _attackableColor = new(1f, 0.2f, 0.2f, 0.35f);

    [SerializeField]
    private Color _affectedColor = new(1f, 0.65f, 0.1f, 0.6f);

    private bool _isReachable;
    private bool _isTargetable;
    private bool _isAffected;

    public Vector2Int GridPos { get; private set; }
    public TerrainType TerrainType { get; private set; }
    public GameObject Occupant { get; private set; }
    public bool IsOccupied => Occupant != null;
    public bool CanEnter => TerrainType != TerrainType.Blocked && !IsOccupied;

    private void Awake()
    {
        _defaultSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    internal void Init(Vector2Int pos, TerrainType terrainType)
    {
        GridPos = pos;
        TerrainType = terrainType;
        Occupant = null;
        _isReachable = false;
        _isTargetable = false;
        _isAffected = false;
        SetSelected(false);
        RefreshColor();
    }

    internal void SetOccupant(GameObject occupant)
    {
        Occupant = occupant;
    }

    /// <summary>
    /// 显示或隐藏当前格子的可达范围高亮。
    /// </summary>
    public void SetReachable(bool isReachable)
    {
        _isReachable = isReachable;
        RefreshColor();
    }

    public void SetSelected(bool isSelected)
    {
        if (_selectionSpriteRenderer != null)
        {
            _selectionSpriteRenderer.enabled = isSelected;
        }
    }

    public void SetTargetable(bool isTargetable)
    {
        _isTargetable = isTargetable;
        RefreshColor();
    }

    public void SetAffected(bool isAffected)
    {
        _isAffected = isAffected;
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (_defaultSpriteRenderer == null)
        {
            return;
        }

        if (_isAffected)
        {
            _defaultSpriteRenderer.color = _affectedColor;
        }
        else if (_isTargetable)
        {
            _defaultSpriteRenderer.color = _attackableColor;
        }
        else if (_isReachable)
        {
            _defaultSpriteRenderer.color = _reachableColor;
        }
        else if (TerrainType == TerrainType.Blocked)
        {
            _defaultSpriteRenderer.color = _blockedColor;
        }
        else
        {
            _defaultSpriteRenderer.color = Color.clear;
        }
    }
}
