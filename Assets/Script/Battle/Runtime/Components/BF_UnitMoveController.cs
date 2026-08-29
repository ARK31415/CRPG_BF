using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 处理玩家单位选择、移动预览和技能目标提交。
/// </summary>
public class BF_UnitMoveController : MonoBehaviour
{
    [SerializeField] private BF_BoardManager _board;
    [SerializeField] private LineRenderer _pathLine;

    private readonly Dictionary<Vector2Int, Vector2Int> _cameFrom = new();
    private readonly Dictionary<Vector2Int, int> _cost = new();
    private readonly HashSet<Vector2Int> _targetable = new();
    private readonly HashSet<Vector2Int> _affected = new();
    private HashSet<Vector2Int> _reachable = new();
    private List<Vector2Int> _path = new();
    private Camera _camera;
    private Material _pathMaterial;
    private BF_BattleController _battleController;
    private BF_BattleUnit _unit;
    private BF_SkillConfigSO _skill;
    private BF_BoardCell _targetCell;
    private Vector2Int _hoverPos;
    private bool _isSelected;
    private bool _hasHoverPos;

    public BF_BattleUnit Unit => _unit;
    public BF_PlayerActionMode Mode { get; private set; } = BF_PlayerActionMode.Move;
    public bool ActionDone { get; private set; }

    private void Start()
    {
        _camera = Camera.main;
        SetupPathLine();
        HidePath();
    }

    private void Update()
    {
        if (_camera == null
            || _board == null
            || BF_InputManager.Instance == null
            || _unit == null
            || _unit.IsMoving
            || _unit.IsActing
            || Mode == BF_PlayerActionMode.Executing)
        {
            return;
        }

        if (BF_InputManager.Instance.AttackPressed)
        {
            EnterSkillMode(_unit.Config.BasicAttack);
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ClearPreview();
            return;
        }

        Vector3 worldPos = _camera.ScreenToWorldPoint(BF_InputManager.Instance.Point);
        Vector2Int pos = _board.WorldToGrid(worldPos);

        if (_isSelected && Mode == BF_PlayerActionMode.Move)
        {
            UpdatePath(pos);
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

        if (_board.TryGetOccupant(pos, out GameObject occupant)
            && occupant.TryGetComponent(out BF_BattleUnit unit))
        {
            _battleController.TrySelectPlayerUnit(unit);
        }
    }

    private void OnDestroy()
    {
        if (_pathMaterial != null)
        {
            Destroy(_pathMaterial);
        }
    }

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

    private IEnumerator MoveUnit(List<Vector2Int> path)
    {
        BF_BattleUnit unit = _unit;
        yield return _battleController.CommandExecutor.Execute(BF_BattleCommandRequest.CreateMove(unit, path));
        ActionDone = true;
        _battleController.OnUnitActionFinished(unit);
    }

    private IEnumerator UseSkill(BF_BattleUnit unit, BF_SkillConfigSO skill, Vector2Int pos)
    {
        yield return _battleController.CommandExecutor.Execute(BF_BattleCommandRequest.CreateSkill(unit, skill, pos));
        ActionDone = true;
        _battleController.OnUnitActionFinished(unit);
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

    private void HidePath()
    {
        _pathLine.enabled = false;
        _pathLine.positionCount = 0;
    }
}
