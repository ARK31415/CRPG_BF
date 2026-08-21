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

    public IReadOnlyList<BF_BattleUnit> Units => _units;
    public BF_UnitMoveController MoveController => _moveController;
    public BF_BattleUnit CurrentUnit { get; set; }
    public int Round { get; private set; }

    private void Start() {
        StartCoroutine(StartBattle());
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
    }

    public void StartPlayerRound() {
        Round++;

        foreach (BF_BattleUnit unit in _units) {
            unit.ResetTurn();
        }
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
        _running = false;

        if (_battleLoop != null) {
            StopCoroutine(_battleLoop);
        }
    }
}
