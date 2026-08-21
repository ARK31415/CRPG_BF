using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 控制敌我阶段和回合切换，不处理棋盘细节或单位移动规则。
/// </summary>
public class BF_BattleController : MonoBehaviour {
    [SerializeField]
    private BF_UnitMoveController _moveController;

    private readonly List<BF_BattleUnit> _units = new();
    private BF_BattleState _state;
    private Coroutine _battleLoop;
    private bool _running;
    private bool _playerPhaseEnded;

    public IReadOnlyList<BF_BattleUnit> Units => _units;
    public BF_UnitMoveController MoveController => _moveController;
    public BF_BattleUnit CurrentUnit { get; private set; }
    public int Round { get; private set; }
    public bool PlayerPhaseEnded => _playerPhaseEnded;

    private void Start() {
        BF_CameraManager.Instance?.BindBounds();
        _moveController.SetBattleController(this);
        StartCoroutine(StartBattle());
    }

    private void Update() {
        if (_playerPhaseEnded
            || _state is not BF_PlayerPhaseState
            || BF_InputManager.Instance == null
            || (CurrentUnit != null && CurrentUnit.IsMoving)) {
            return;
        }

        if (BF_InputManager.Instance.EndPlayerPhasePressed) {
            EndPlayerPhase();
            return;
        }

        if (BF_InputManager.Instance.CancelSelectionPressed) {
            CancelSelection();
            return;
        }

        if (BF_InputManager.Instance.NextUnitPressed) {
            SelectNextPlayerUnit();
        }
    }

    private IEnumerator StartBattle() {
        yield return null;

        SetState(new BF_BattleSetupState(this));
        _running = true;
        _battleLoop = StartCoroutine(BattleLoopRoutine());
    }

    public void SetState(BF_BattleState nextState) {
        _state = nextState;
    }

    public void CacheUnits() {
        _units.Clear();
        _units.AddRange(FindObjectsByType<BF_BattleUnit>(FindObjectsSortMode.None));
        _units.Sort((a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));
    }

    public void StartPlayerRound() {
        Round++;
        _playerPhaseEnded = false;

        foreach (BF_BattleUnit unit in _units) {
            unit.ResetTurn();
        }
    }

    public bool SelectFirstPlayerUnit() {
        return TryGetNextUnit(BF_UnitTeam.Player, out BF_BattleUnit unit)
            && TrySelectPlayerUnit(unit);
    }

    public bool TrySelectPlayerUnit(BF_BattleUnit unit) {
        if (_state is not BF_PlayerPhaseState
            || _playerPhaseEnded
            || unit == null
            || unit.Team != BF_UnitTeam.Player
            || unit.HasActed
            || (CurrentUnit != null && CurrentUnit.IsMoving)) {
            return false;
        }

        CurrentUnit = unit;
        _moveController.SetUnit(unit);
        BF_CameraManager.Instance?.Focus(unit);
        Debug.Log($"[BF] Player Unit Selected: {unit.DisplayName}");
        return true;
    }

    public void SelectNextPlayerUnit() {
        int start = CurrentUnit == null ? -1 : _units.IndexOf(CurrentUnit);

        for (int offset = 1; offset <= _units.Count; offset++) {
            BF_BattleUnit unit = _units[(start + offset) % _units.Count];
            if (unit.Team == BF_UnitTeam.Player && !unit.HasActed) {
                TrySelectPlayerUnit(unit);
                return;
            }
        }
    }

    public void FinishUnit(BF_BattleUnit unit) {
        if (unit == null || unit != CurrentUnit) {
            return;
        }

        unit.FinishTurn();
        Debug.Log($"[BF] Player Unit Acted: {unit.DisplayName}");
        ClearCurrentUnit();
    }

    public bool AreAllUnitsDone(BF_UnitTeam team) {
        return !TryGetNextUnit(team, out _);
    }

    public void EndPlayerPhase() {
        if (_state is not BF_PlayerPhaseState
            || (CurrentUnit != null && CurrentUnit.IsMoving)) {
            return;
        }

        ClearCurrentUnit();
        _playerPhaseEnded = true;
    }

    public void CancelSelection() {
        if (_state is not BF_PlayerPhaseState
            || CurrentUnit == null
            || CurrentUnit.IsMoving) {
            return;
        }

        Debug.Log($"[BF] Player Unit Deselected: {CurrentUnit.DisplayName}");
        ClearCurrentUnit();
    }

    public void ClearCurrentUnit() {
        _moveController.ClearUnit();
        CurrentUnit = null;
    }

    public bool TryGetNextUnit(BF_UnitTeam team, out BF_BattleUnit unit) {
        for (int i = 0; i < _units.Count; i++) {
            BF_BattleUnit next = _units[i];
            if (next.Team == team && !next.HasActed) {
                unit = next;
                return true;
            }
        }

        unit = null;
        return false;
    }

    private IEnumerator BattleLoopRoutine() {
        while (_running && _state != null) {
            BF_BattleState state = _state;

            yield return StartCoroutine(state.Enter());
            if (state != _state) {
                yield return StartCoroutine(state.Exit());
                continue;
            }

            yield return StartCoroutine(state.Execute());
            yield return StartCoroutine(state.Exit());
        }

        _battleLoop = null;
    }

    private void OnDestroy() {
        BF_CameraManager.Instance?.ClearBounds();
        _running = false;

        if (_battleLoop != null) {
            StopCoroutine(_battleLoop);
        }
    }
}
