using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 处理玩家单位选择、移动预览和技能目标提交。
/// </summary>
public class BF_UnitMoveController : MonoBehaviour
{
    #region 序列化配置与引用

    [Header("棋盘引用")]
    [SerializeField]
    private BF_BoardManager _board;

    [Header("路径线")]
    [SerializeField]
    private LineRenderer _pathLine;

    #endregion

    #region 运行时数据

    // 运行时引用
    private BF_BattleController _battleController;
    private Camera _camera;

    // 当前选中状态
    private BF_BattleUnit _unit;
    private BF_SkillConfigSO _skill;
    private BF_BoardCell _targetCell;
    private Vector2Int _hoverPos;
    private bool _isSelected;
    private bool _hasHoverPos;

    // 寻路缓存
    private readonly Dictionary<Vector2Int, Vector2Int> _cameFrom = new();
    private readonly Dictionary<Vector2Int, int> _cost = new();
    private HashSet<Vector2Int> _reachable = new();
    private List<Vector2Int> _path = new();

    // 手动绘制状态
    private int _manualPathCost;

    // 格子悬停信息状态
    private bool _hoverInfoHidden = true;
    private Vector2Int _lastHoverInfoPos;

    // 高亮缓存
    private readonly HashSet<Vector2Int> _targetable = new();
    private readonly HashSet<Vector2Int> _affected = new();

    // 运行时创建的路径线材质；OnDestroy 中销毁。
    private Material _pathMaterial;

    #endregion

    #region 对外接口

    // 选中单位
    public BF_BattleUnit Unit => _unit;

    // 行动模式与完成状态
    public BF_PlayerActionMode Mode { get; private set; } = BF_PlayerActionMode.Move;
    public bool ActionDone { get; private set; }

    #endregion

    #region 生命周期

    private void Start()
    {
        _camera = Camera.main;
        SetupPathLine();
        HidePath();
        GameEventBus.Instance?.Subscribe<BF_SettingsChangedEvent>(OnSettingsChanged)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void Update()
    {
        if (_camera == null
            || _board == null
            || BF_InputManager.Instance == null
            || Mode == BF_PlayerActionMode.Executing)
        {
            return;
        }

        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (isPointerOverUI)
        {
            PublishCellHover(null);

            if (_unit != null && !_unit.IsMoving && !_unit.IsActing)
            {
                if (Mode != BF_PlayerActionMode.Move || !IsManualPathPlanning())
                {
                    ClearPreview();
                }
            }

            return;
        }

        Vector3 worldPos = _camera.ScreenToWorldPoint(BF_InputManager.Instance.Point);
        Vector2Int pos = _board.WorldToGrid(worldPos);
        PublishCellHover(pos);

        if (_unit == null || _unit.IsMoving || _unit.IsActing)
        {
            // 无选中单位时仍允许左键点击重新选人。
            if (_unit == null && BF_InputManager.Instance.ClickPressed)
            {
                TrySelectUnitAt(pos);
            }

            return;
        }

        if (BF_InputManager.Instance.AttackPressed)
        {
            EnterSkillMode(_unit.Config.BasicAttack);
        }

        if (_isSelected && Mode == BF_PlayerActionMode.Move)
        {
            if (IsManualPathPlanning())
            {
                UpdateManualInput(pos);
            }
            else
            {
                UpdatePath(pos);
            }
        }
        else if (_isSelected && Mode == BF_PlayerActionMode.Skill)
        {
            UpdateSkillPreview(pos);
        }

        if (BF_InputManager.Instance.MovePressed)
        {
            if (Mode == BF_PlayerActionMode.Skill)
            {
                CancelActionMode();
                return;
            }

            TryMove();
            return;
        }

        if (!BF_InputManager.Instance.ClickPressed)
        {
            return;
        }

        if (Mode == BF_PlayerActionMode.Skill)
        {
            TrySkill(pos);
            return;
        }

        TrySelectUnitAt(pos);
    }

    private void OnDestroy()
    {
        GameEventBus.Instance?.Publish(new BF_BoardCellHoveredEvent());

        if (_pathMaterial != null)
        {
            Destroy(_pathMaterial);
        }
    }

    #endregion

    #region 单位选择与模式

    // 单位绑定与刷新
    public void SetBattleController(BF_BattleController battleController)
    {
        _battleController = battleController;
    }

    public void SetUnit(BF_BattleUnit unit)
    {
        if (_unit == unit)
        {
            RefreshSelection();
            return;
        }

        ClearSelection();
        _unit = unit;
        Mode = BF_PlayerActionMode.Move;
        ActionDone = false;

        if (_unit != null)
        {
            SelectUnit();
        }
    }

    public void ClearUnit()
    {
        ClearSelection();
        _unit = null;
        _skill = null;
        Mode = BF_PlayerActionMode.Move;
        ActionDone = false;
        GameEventBus.Instance?.Publish(new BF_PathCostChangedEvent(0, 0));
    }

    public void RefreshSelection()
    {
        ClearSelection();

        if (_unit == null || !_unit.IsAlive || _unit.IsTurnEnded)
        {
            return;
        }

        _skill = null;
        Mode = BF_PlayerActionMode.Move;
        SelectUnit();
    }

    // 技能模式切换
    public bool EnterSkillMode(BF_SkillConfigSO skill)
    {
        if (_unit == null || skill == null || !_unit.CanPay(skill.APCost))
        {
            return false;
        }

        ClearSelection();
        _skill = skill;
        Mode = BF_PlayerActionMode.Skill;

        foreach (Vector2Int pos in BF_SkillRange.GetTargetCells(_board, _unit.GridPos, skill))
        {
            if (skill.TargetType != BF_SkillTargetType.Unit || IsValidUnitTarget(pos, skill))
            {
                _targetable.Add(pos);
            }
        }

        foreach (Vector2Int pos in _targetable)
        {
            if (_board.TryGetCell(pos, out BF_BoardCell cell))
            {
                cell.SetTargetable(true);
            }
        }

        if (_board.TryGetCell(_unit.GridPos, out BF_BoardCell unitCell))
        {
            unitCell.SetSelected(true);
        }

        _isSelected = true;
        return true;
    }

    public bool CancelActionMode()
    {
        if (Mode != BF_PlayerActionMode.Skill)
        {
            return false;
        }

        _skill = null;
        Mode = BF_PlayerActionMode.Move;
        RefreshSelection();
        return true;
    }

    #endregion

    #region 移动

    private void TryMove()
    {
        if (!_isSelected || Mode != BF_PlayerActionMode.Move || _path.Count == 0)
        {
            return;
        }

        List<Vector2Int> movePath = new(_path);
        ClearSelection();
        Mode = BF_PlayerActionMode.Executing;
        StartCoroutine(MoveUnit(movePath));
    }

    private IEnumerator MoveUnit(List<Vector2Int> path)
    {
        BF_BattleUnit unit = _unit;
        yield return _battleController.CommandExecutor.Execute(BF_BattleCommandRequest.CreateMove(unit, path));
        ActionDone = true;
        _battleController.OnUnitActionFinished(unit);
    }

    // 路径预览
    private void UpdatePath(Vector2Int pos)
    {
        if (_hasHoverPos && pos == _hoverPos)
        {
            return;
        }

        _hoverPos = pos;
        _hasHoverPos = true;
        ClearTarget();

        if (!_reachable.Contains(pos))
        {
            _path.Clear();
            HidePath();
            GameEventBus.Instance?.Publish(new BF_PathCostChangedEvent(0, _unit.CurrentAP));
            return;
        }

        if (_board.TryGetCell(pos, out _targetCell))
        {
            _targetCell.SetSelected(true);
        }

        _path = BF_Pathfinder.BuildPath(_unit.GridPos, pos, _cameFrom);
        ShowPath();
        int pathCost = _cost[pos];
        GameEventBus.Instance?.Publish(new BF_PathCostChangedEvent(pathCost, _unit.CurrentAP - pathCost));
    }

    // 路径线表现
    private void SetupPathLine()
    {
        _pathLine.useWorldSpace = true;
        _pathLine.startWidth = 0.08f;
        _pathLine.endWidth = 0.08f;
        _pathLine.startColor = Color.yellow;
        _pathLine.endColor = Color.yellow;
        _pathLine.sortingLayerName = "Middle";
        _pathLine.sortingOrder = 4;

        if (_pathLine.sharedMaterial != null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
            ?? Shader.Find("Sprites/Default");

        if (shader != null)
        {
            _pathMaterial = new Material(shader);
            _pathLine.sharedMaterial = _pathMaterial;
        }
    }

    private void ShowPath()
    {
        _pathLine.positionCount = _path.Count + 1;
        _pathLine.SetPosition(0, _board.GridToWorld(_unit.GridPos));

        for (int i = 0; i < _path.Count; i++)
        {
            _pathLine.SetPosition(i + 1, _board.GridToWorld(_path[i]));
        }

        _pathLine.enabled = true;
    }

    private void HidePath()
    {
        _pathLine.enabled = false;
        _pathLine.positionCount = 0;
    }

    #endregion

    #region 手动绘制

    // 手动模式按格子悬停追加，右键统一走 TryMove 确认。
    private void UpdateManualInput(Vector2Int pos)
    {
        if (_hasHoverPos && pos == _hoverPos)
        {
            return;
        }

        _hoverPos = pos;
        _hasHoverPos = true;

        if (pos == _unit.GridPos)
        {
            if (_path.Count > 0)
            {
                _path.Clear();
                _manualPathCost = 0;
                HidePath();
                PublishManualPathCost();
            }

            return;
        }

        TryAppendManualCell(pos);
    }

    // 仅在指针进入新格时处理一次；不自动补齐非相邻路径。




    private void TryAppendManualCell(Vector2Int pos)
    {
        Vector2Int anchor = _path.Count > 0 ? _path[_path.Count - 1] : _unit.GridPos;
        if (Mathf.Abs(pos.x - anchor.x) + Mathf.Abs(pos.y - anchor.y) != 1)
        {
            return;
        }

        int nodeIndex = _path.IndexOf(pos);
        if (nodeIndex >= 0)
        {
            if (nodeIndex != _path.Count - 1)
            {
                // 回到路径中段：裁掉该格之后的尾部，继续保持绘制。
                TruncateManualPath(nodeIndex);
            }

            return;
        }

        if (!_board.TryGetMoveCost(pos, out int stepCost))
        {
            return;
        }

        if (_manualPathCost + stepCost > _unit.CurrentAP)
        {
            return;
        }

        _path.Add(pos);
        _manualPathCost += stepCost;
        ShowPath();
        PublishManualPathCost();
    }

    private void TruncateManualPath(int keepCount)
    {
        if (keepCount < 0 || keepCount >= _path.Count)
        {
            return;
        }

        _path.RemoveRange(keepCount + 1, _path.Count - keepCount - 1);
        RecomputeManualCost();
        ShowPath();
        PublishManualPathCost();
    }

    private void RecomputeManualCost()
    {
        int total = 0;
        for (int i = 0; i < _path.Count; i++)
        {
            if (_board.TryGetMoveCost(_path[i], out int stepCost))
            {
                total += stepCost;
            }
        }

        _manualPathCost = total;
    }

    private void PublishManualPathCost()
    {
        GameEventBus.Instance?.Publish(new BF_PathCostChangedEvent(
            _manualPathCost,
            Mathf.Max(0, _unit.CurrentAP - _manualPathCost)));
    }

    // 路线模式读取：属于全局操作偏好，自动模式保持原有悬停行为。
    private bool IsManualPathPlanning()
    {
        BF_SettingsService settings = BF_SettingsService.Instance;
        return settings != null && settings.PathPlanningMode == BF_PathPlanningMode.Manual;
    }

    // 战斗中修改路线模式：清理旧路径预览，保留当前选中单位并按新模式刷新可达范围。
    private void OnSettingsChanged(BF_SettingsChangedEvent gameEvent)
    {
        if (_unit == null
            || _battleController == null
            || _battleController.IsBattleEnded
            || _battleController.CurrentUnit != _unit)
        {
            return;
        }

        RefreshSelection();
    }

    #endregion

    #region 悬停信息与取消

    // 指针进入新格子时发布一次地形悬停事件，离开棋盘或进入 UI 时发布隐藏事件。
    private void PublishCellHover(Vector2Int? pos)
    {
        if (pos.HasValue && _board.TryGetCell(pos.Value, out BF_BoardCell cell))
        {
            if (!_hoverInfoHidden && _lastHoverInfoPos == pos.Value)
            {
                return;
            }

            _hoverInfoHidden = false;
            _lastHoverInfoPos = pos.Value;
            GameEventBus.Instance?.Publish(new BF_BoardCellHoveredEvent(
                pos.Value,
                cell.TerrainType,
                cell.MoveCost,
                cell.Passable,
                cell.IsOccupied));
            return;
        }

        if (_hoverInfoHidden)
        {
            return;
        }

        _hoverInfoHidden = true;
        GameEventBus.Instance?.Publish(new BF_BoardCellHoveredEvent());
    }

    // 左键点击玩家单位进行选择；无选中单位时也走同一入口。
    private void TrySelectUnitAt(Vector2Int pos)
    {
        if (_battleController != null
            && _board.TryGetOccupant(pos, out GameObject occupant)
            && occupant.TryGetComponent(out BF_BattleUnit unit))
        {
            _battleController.TrySelectPlayerUnit(unit);
        }
    }

    // Esc 上下文路由使用：清除路径预览并保留单位选择与可达范围。
    public bool TryCancelPathPreview()
    {
        if (_path.Count == 0)
        {
            return false;
        }

        ClearPreview();
        return true;
    }

    #endregion

    #region 技能

    private void TrySkill(Vector2Int pos)
    {
        if (!_targetable.Contains(pos) || !IsValidTarget(pos))
        {
            return;
        }

        BF_BattleUnit unit = _unit;
        BF_SkillConfigSO skill = _skill;
        ClearSelection();
        Mode = BF_PlayerActionMode.Executing;
        StartCoroutine(UseSkill(unit, skill, pos));
    }

    private IEnumerator UseSkill(BF_BattleUnit unit, BF_SkillConfigSO skill, Vector2Int pos)
    {
        yield return _battleController.CommandExecutor.Execute(BF_BattleCommandRequest.CreateSkill(unit, skill, pos));
        ActionDone = true;
        _battleController.OnUnitActionFinished(unit);
    }

    // 预览与目标校验
    private void UpdateSkillPreview(Vector2Int pos)
    {
        if (_hasHoverPos && pos == _hoverPos)
        {
            return;
        }

        _hoverPos = pos;
        _hasHoverPos = true;
        ClearAffected();
        ClearTarget();

        if (!_targetable.Contains(pos))
        {
            return;
        }

        if (_board.TryGetCell(pos, out _targetCell))
        {
            _targetCell.SetSelected(true);
        }

        List<Vector2Int> cells = BF_SkillRange.GetAreaCells(_board, _unit, pos, _skill);
        for (int i = 0; i < cells.Count; i++)
        {
            if (_board.TryGetCell(cells[i], out BF_BoardCell cell))
            {
                _affected.Add(cells[i]);
                cell.SetAffected(true);
            }
        }
    }

    private bool IsValidTarget(Vector2Int pos)
    {
        if (_skill.TargetType != BF_SkillTargetType.Unit)
        {
            return true;
        }

        return IsValidUnitTarget(pos, _skill);
    }

    private bool IsValidUnitTarget(Vector2Int pos, BF_SkillConfigSO skill)
    {
        return _board.TryGetOccupant(pos, out GameObject occupant)
            && occupant.TryGetComponent(out BF_BattleUnit target)
            && target.IsAlive
            && _unit.CanTarget(target, skill.TargetGroup);
    }

    #endregion

    #region 棋盘高亮

    // 高亮建立
    private void SelectUnit()
    {
        _reachable = BF_Pathfinder.FindReachable(
            _board,
            _unit.GridPos,
            _unit.CurrentAP,
            _cameFrom,
            _cost);

        foreach (Vector2Int pos in _reachable)
        {
            if (_board.TryGetCell(pos, out BF_BoardCell cell))
            {
                cell.SetReachable(true);
            }
        }

        if (_board.TryGetCell(_unit.GridPos, out BF_BoardCell unitCell))
        {
            unitCell.SetSelected(true);
        }

        _isSelected = true;
        _hasHoverPos = false;
        GameEventBus.Instance?.Publish(new BF_PathCostChangedEvent(0, _unit.CurrentAP));
    }

    // 高亮清理
    private void ClearSelection()
    {
        foreach (Vector2Int pos in _reachable)
        {
            if (_board.TryGetCell(pos, out BF_BoardCell cell))
            {
                cell.SetReachable(false);
            }
        }

        foreach (Vector2Int pos in _targetable)
        {
            if (_board.TryGetCell(pos, out BF_BoardCell cell))
            {
                cell.SetTargetable(false);
            }
        }

        _reachable.Clear();
        _targetable.Clear();
        ClearAffected();

        if (_unit != null && _board.TryGetCell(_unit.GridPos, out BF_BoardCell unitCell))
        {
            unitCell.SetSelected(false);
        }

        ClearPreview();
        _isSelected = false;
    }

    private void ClearPreview()
    {
        _hasHoverPos = false;
        _manualPathCost = 0;
        ClearAffected();
        ClearTarget();
        _path.Clear();
        HidePath();

        if (_unit != null)
        {
            GameEventBus.Instance?.Publish(new BF_PathCostChangedEvent(0, _unit.CurrentAP));
        }
    }

    private void ClearAffected()
    {
        foreach (Vector2Int pos in _affected)
        {
            if (_board.TryGetCell(pos, out BF_BoardCell cell))
            {
                cell.SetAffected(false);
            }
        }

        _affected.Clear();
    }

    private void ClearTarget()
    {
        if (_targetCell != null)
        {
            _targetCell.SetSelected(false);
            _targetCell = null;
        }
    }

    #endregion
}
