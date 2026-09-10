using UnityEngine;

/// <summary>
/// 单个逻辑棋盘格的轻量运行时组件。
///
/// 负责保存逻辑坐标、静态地形和动态占用者；不负责寻路、输入或单位移动。
/// </summary>
public class BF_BoardCell : MonoBehaviour
{
    #region 序列化配置与引用

    [Header("选择高亮渲染")]
    [SerializeField]
    private SpriteRenderer _selectionSpriteRenderer;

    [SerializeField]
    private SpriteRenderer _highlightSpriteRenderer;

    [Header("高亮颜色")]
    [SerializeField]
    private Color _reachableColor;

    [SerializeField]
    private Color _attackableColor = new(1f, 0.2f, 0.2f, 0.35f);

    [SerializeField]
    private Color _affectedColor = new(1f, 0.65f, 0.1f, 0.6f);

    [Header("地形基础表现")]
    [SerializeField]
    private Color _normalColor = new(1f, 1f, 1f, 0.1f);

    [SerializeField]
    private Color _difficultColor = new(0.9f, 0.75f, 0.5f, 0.5f);

    [SerializeField]
    private Color _swampColor = new(0.2f, 0.45f, 0.3f, 0.6f);

    [SerializeField]
    private Color _blockedColor = new(0.4f, 0.4f, 0.4f, 0.8f);

    #endregion

    #region 运行时数据

    // 主精灵渲染缓存
    private SpriteRenderer _defaultSpriteRenderer;

    // 地形基础表现色；高亮状态优先于基础色。
    private Color _baseColor = Color.clear;

    // 高亮状态标记
    private bool _isReachable;
    private bool _isTargetable;
    private bool _isAffected;

    #endregion

    #region 对外接口

    // 逻辑状态
    public Vector2Int GridPos { get; private set; }
    public TerrainType TerrainType { get; private set; }
    public bool Passable { get; private set; }
    public int MoveCost { get; private set; }
    public GameObject Occupant { get; private set; }

    // 派生状态
    public bool IsOccupied => Occupant != null;
    public bool CanEnter => Passable && !IsOccupied;

    #endregion

    #region 生命周期与初始化

    private void Awake()
    {
        _defaultSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    internal void Init(Vector2Int pos, TerrainType terrainType, bool passable, int moveCost)
    {
        GridPos = pos;
        TerrainType = terrainType;
        Passable = passable;
        MoveCost = moveCost;
        _baseColor = ResolveBaseColor(terrainType);
        Occupant = null;
        _isReachable = false;
        _isTargetable = false;
        _isAffected = false;
        SetSelected(false);
        RefreshColor();
    }

    // 地形基础表现色由 Cell 序列化配置提供，仅用于展示，不参与规则。
    private Color ResolveBaseColor(TerrainType terrainType)
    {
        return terrainType switch
        {
            TerrainType.Difficult => _difficultColor,
            TerrainType.Swamp => _swampColor,
            TerrainType.Blocked => _blockedColor,
            _ => _normalColor
        };
    }

    #endregion

    #region 占用变更

    internal void SetOccupant(GameObject occupant)
    {
        Occupant = occupant;
    }

    #endregion

    #region 高亮控制

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

    // 地形底色始终由默认渲染器显示；战术高亮使用独立层，不覆盖地形语义。
    private void RefreshColor()
    {
        if (_defaultSpriteRenderer != null)
        {
            _defaultSpriteRenderer.color = _baseColor;
        }

        if (_highlightSpriteRenderer != null)
        {
            if (_isAffected)
            {
                _highlightSpriteRenderer.color = _affectedColor;
                _highlightSpriteRenderer.enabled = true;
            }
            else if (_isTargetable)
            {
                _highlightSpriteRenderer.color = _attackableColor;
                _highlightSpriteRenderer.enabled = true;
            }
            else if (_isReachable)
            {
                _highlightSpriteRenderer.color = _reachableColor;
                _highlightSpriteRenderer.enabled = true;
            }
            else
            {
                _highlightSpriteRenderer.enabled = false;
            }
        }
    }

    #endregion
}
