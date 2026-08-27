using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 处理玩家单位选择、移动预览、基础攻击目标和行动提交。
/// </summary>
public class BF_UnitMoveController : MonoBehaviour
{
    [SerializeField]
    private BF_BoardManager _board;

    [SerializeField]
    private LineRenderer _pathLine;

    private readonly Dictionary<Vector2Int, Vector2Int> _cameFrom = new();
    private readonly Dictionary<Vector2Int, int> _cost = new();
    private readonly HashSet<Vector2Int> _attackable = new();
    private HashSet<Vector2Int> _reachable = new();
    private List<Vector2Int> _path = new();
    private Camera _camera;
    private Material _pathMaterial;
    private BF_BattleController _battleController;
    private BF_BattleUnit _unit;
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
            EnterAttackMode();
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ClearPathPreview();
            return;
        }

        Vector3 mousePos = BF_InputManager.Instance.Point;
        Vector3 worldPos = _camera.ScreenToWorldPoint(mousePos);
        Vector2Int gridPos = _board.WorldToGrid(worldPos);

        if (_isSelected && Mode == BF_PlayerActionMode.Move)
        {
            UpdatePath(gridPos);
        }

        if (BF_InputManager.Instance.MovePressed)
        {
            if (Mode == BF_PlayerActionMode.Attack)
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

        if (Mode == BF_PlayerActionMode.Attack)
        {
            TryAttack(gridPos);
            return;
        }

        if (_board.TryGetOccupant(gridPos, out GameObject occupant)
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
        Mode = BF_PlayerActionMode.Move;
        ActionDone = false;
        GameEventBus.Instance?.Publish(new BF_PathCostChangedEvent(0, 0));
    }

    public bool EnterAttackMode()
    {
        BF_SkillConfigSO skill = _unit != null ? _unit.Config.BasicAttack : null;
        if (skill == null || !_unit.CanPay(skill.APCost))
        {
            return false;
        }

        ClearSelection();
        Mode = BF_PlayerActionMode.Attack;
        ShowAttackRange(skill.Range);
        _isSelected = true;
        return true;
    }

    public bool CancelActionMode()
    {
        if (Mode != BF_PlayerActionMode.Attack)
        {
            return false;
        }

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
        int budget = _unit.CurrentAP;
        _reachable = BF_Pathfinder.FindReachable(
            _board,
            _unit.GridPos,
            budget,
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

    private void UpdatePath(Vector2Int gridPos)
    {
        if (_hasHoverPos && gridPos == _hoverPos)
        {
            return;
        }

        _hoverPos = gridPos;
        _hasHoverPos = true;
        ClearTarget();

        if (!_reachable.Contains(gridPos))
        {
            _path.Clear();
            HidePath();
            GameEventBus.Instance?.Publish(new BF_PathCostChangedEvent(0, _unit.CurrentAP));
            return;
        }

        if (_board.TryGetCell(gridPos, out _targetCell))
        {
            _targetCell.SetSelected(true);
        }

        _path = BF_Pathfinder.BuildPath(_unit.GridPos, gridPos, _cameFrom);
        ShowPath();

        int pathCost = _cost[gridPos];
        GameEventBus.Instance?.Publish(new BF_PathCostChangedEvent(pathCost, _unit.CurrentAP - pathCost));
    }

    private void TryAttack(Vector2Int pos)
    {
        if (!_attackable.Contains(pos)
            || !_board.TryGetOccupant(pos, out GameObject occupant)
            || !occupant.TryGetComponent(out BF_BattleUnit target)
            || !target.IsAlive
            || target.Team == _unit.Team)
        {
            return;
        }

        BF_BattleUnit unit = _unit;
        ClearSelection();
        Mode = BF_PlayerActionMode.Executing;
        StartCoroutine(AttackUnit(unit, target));
    }

    private IEnumerator MoveUnit(List<Vector2Int> path)
    {
        BF_BattleUnit unit = _unit;
        Vector2Int target = path[path.Count - 1];

        yield return unit.Move(path);

        if (unit.GridPos == target)
        {
            unit.SpendAP(path.Count);
        }

        ActionDone = true;
        _battleController.OnUnitActionFinished(unit);
    }

    private IEnumerator AttackUnit(BF_BattleUnit unit, BF_BattleUnit target)
    {
        yield return unit.Attack(target);
        ActionDone = true;
        _battleController.OnUnitActionFinished(unit);
    }

    private void ShowAttackRange(int range)
    {
        _attackable.Clear();

        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) == 0
                    || Mathf.Abs(x) + Mathf.Abs(y) > range)
                {
                    continue;
                }

                Vector2Int pos = _unit.GridPos + new Vector2Int(x, y);
                if (_board.TryGetCell(pos, out BF_BoardCell cell))
                {
                    _attackable.Add(pos);
                    cell.SetAttackable(true);
                }
            }
        }

        if (_board.TryGetCell(_unit.GridPos, out BF_BoardCell unitCell))
        {
            unitCell.SetSelected(true);
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

        foreach (Vector2Int pos in _attackable)
        {
            if (_board.TryGetCell(pos, out BF_BoardCell cell))
            {
                cell.SetAttackable(false);
            }
        }

        _reachable.Clear();
        _attackable.Clear();

        if (_unit != null && _board.TryGetCell(_unit.GridPos, out BF_BoardCell unitCell))
        {
            unitCell.SetSelected(false);
        }

        ClearPathPreview();
        _isSelected = false;
    }

    private void ClearPathPreview()
    {
        _hasHoverPos = false;
        ClearTarget();
        _path.Clear();
        HidePath();

        if (_unit != null)
        {
            GameEventBus.Instance?.Publish(new BF_PathCostChangedEvent(0, _unit.CurrentAP));
        }
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
