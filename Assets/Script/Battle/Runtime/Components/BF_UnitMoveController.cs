using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 处理测试单位的选择、可达高亮、路径预览和点击移动。
/// </summary>
public class BF_UnitMoveController : MonoBehaviour {
    [SerializeField]
    private BF_BoardManager _board;

    private BF_BattleUnit _unit;

    [SerializeField]
    private LineRenderer _pathLine;

    private readonly Dictionary<Vector2Int, Vector2Int> _cameFrom = new();
    private HashSet<Vector2Int> _reachable = new();
    private List<Vector2Int> _path = new();
    private Camera _camera;
    private Material _pathMaterial;
    private BF_BattleController _battleController;
    private BF_BoardCell _targetCell;
    private Vector2Int _hoverPos;
    private bool _isSelected;
    private bool _hasHoverPos;

    public BF_BattleUnit Unit => _unit;
    public bool ActionDone { get; private set; }

    private void Start() {
        _camera = Camera.main;
        SetupPathLine();
        HidePath();
    }

    private void OnDestroy() {
        if (_pathMaterial != null) {
            Destroy(_pathMaterial);
        }
    }

    private void Update() {
        if (_camera == null
            || BF_InputManager.Instance == null) {
            return;
        }

        if (_unit != null && _unit.IsMoving) {
            return;
        }

        Vector3 mousePos = BF_InputManager.Instance.Point;
        Vector3 worldPos = _camera.ScreenToWorldPoint(mousePos);
        Vector2Int gridPos = _board.WorldToGrid(worldPos);

        if (_unit != null && _isSelected) {
            UpdatePath(gridPos);
        }

        if (!BF_InputManager.Instance.ClickPressed) {
            return;
        }

        if (_board.TryGetOccupant(gridPos, out GameObject occupant)
            && occupant.TryGetComponent(out BF_BattleUnit unit)) {
            _battleController.TrySelectPlayerUnit(unit);
            return;
        }

        if (_unit == null) {
            return;
        }

        if (_isSelected && _path.Count > 0) {
            List<Vector2Int> movePath = new(_path);
            ClearSelection();
            StartCoroutine(MoveUnit(movePath));
        }
    }

    public void SetBattleController(BF_BattleController battleController) {
        _battleController = battleController;
    }

    public void SetUnit(BF_BattleUnit unit) {
        if (_unit == unit) {
            return;
        }

        ClearSelection();
        _unit = unit;
        ActionDone = false;
        _hasHoverPos = false;

        if (_unit != null) {
            SelectUnit();
        }
    }

    public void ClearUnit() {
        ClearSelection();
        _unit = null;
        ActionDone = false;
    }

    private void SelectUnit() {
        _reachable = BF_Pathfinder.FindReachable(
            _board,
            _unit.GridPos,
            _unit.MoveRange,
            _cameFrom);

        foreach (Vector2Int pos in _reachable) {
            if (_board.TryGetCell(pos, out BF_BoardCell cell)) {
                cell.SetReachable(true);
            }
        }

        if (_board.TryGetCell(_unit.GridPos, out BF_BoardCell unitCell)) {
            unitCell.SetSelected(true);
        }

        _isSelected = true;
        _hasHoverPos = false;
    }

    private void UpdatePath(Vector2Int gridPos) {
        if (_hasHoverPos && gridPos == _hoverPos) {
            return;
        }

        _hoverPos = gridPos;
        _hasHoverPos = true;
        ClearTarget();

        if (!_reachable.Contains(gridPos)) {
            _path.Clear();
            HidePath();
            return;
        }

        if (_board.TryGetCell(gridPos, out _targetCell)) {
            _targetCell.SetSelected(true);
        }

        _path = BF_Pathfinder.BuildPath(_unit.GridPos, gridPos, _cameFrom);
        ShowPath();
    }

    private void ShowPath() {
        _pathLine.positionCount = _path.Count + 1;
        _pathLine.SetPosition(0, _board.GridToWorld(_unit.GridPos));

        for (int i = 0; i < _path.Count; i++) {
            _pathLine.SetPosition(i + 1, _board.GridToWorld(_path[i]));
        }

        _pathLine.enabled = true;
    }

    private void SetupPathLine() {
        _pathLine.useWorldSpace = true;
        _pathLine.startWidth = 0.08f;
        _pathLine.endWidth = 0.08f;
        _pathLine.startColor = Color.yellow;
        _pathLine.endColor = Color.yellow;
        _pathLine.sortingLayerName = "Middle";
        _pathLine.sortingOrder = 4;

        if (_pathLine.sharedMaterial != null) {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
            ?? Shader.Find("Sprites/Default");

        if (shader != null) {
            _pathMaterial = new Material(shader);
            _pathLine.sharedMaterial = _pathMaterial;
        }
    }

    private void HidePath() {
        _pathLine.enabled = false;
        _pathLine.positionCount = 0;
    }

    private void ClearSelection() {
        foreach (Vector2Int pos in _reachable) {
            if (_board.TryGetCell(pos, out BF_BoardCell cell)) {
                cell.SetReachable(false);
            }
        }

        if (_unit != null && _board.TryGetCell(_unit.GridPos, out BF_BoardCell unitCell)) {
            unitCell.SetSelected(false);
        }

        ClearTarget();

        _isSelected = false;
        _hasHoverPos = false;
        _path.Clear();
        HidePath();
    }

    private void ClearTarget() {
        if (_targetCell != null) {
            _targetCell.SetSelected(false);
            _targetCell = null;
        }
    }

    private IEnumerator MoveUnit(List<Vector2Int> path) {
        BF_BattleUnit unit = _unit;
        yield return unit.Move(path);
        ActionDone = true;
        _battleController.FinishUnit(unit);
    }
}
