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

    private bool _isReachable;

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

    private void RefreshColor()
    {
        if (_defaultSpriteRenderer == null)
        {
            return;
        }

        if (_isReachable)
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
