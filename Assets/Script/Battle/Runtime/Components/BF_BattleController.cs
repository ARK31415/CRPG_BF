using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 控制敌我阶段和回合切换，不处理棋盘细节或单位移动规则。
/// </summary>
public class BF_BattleController : MonoBehaviour
{
    #region 序列化配置与引用

    [Header("战斗组件引用")]
    [SerializeField]
    private BF_UnitMoveController _moveController;

    [SerializeField]
    private BF_UnitSpawner _unitSpawner;

    [SerializeField]
    private BF_EnemyController _enemyController;

    #endregion

    #region 运行时数据

    // 战斗单位集合
    private readonly List<BF_BattleUnit> _units = new();

    // 状态机执行：状态、主循环句柄、循环开关。
    private BF_BattleState _state;
    private Coroutine _battleLoop;
    private bool _running;

    // 玩家阶段控制
    private bool _playerPhaseEnded;

    #endregion

    #region 对外接口

    // 场景组件访问
    public BF_UnitMoveController MoveController => _moveController;
    public BF_UnitSpawner UnitSpawner => _unitSpawner;

    // 本控制器创建并持有的命令执行器
    public BF_BattleCommandExecutor CommandExecutor { get; } = new();

    // 单位查询
    public IReadOnlyList<BF_BattleUnit> Units => _units;
    public BF_BattleUnit CurrentUnit { get; private set; }

    // 阶段与回合
    public BF_BattlePhase CurrentPhase { get; private set; } = BF_BattlePhase.None;
    public int Round { get; private set; }
    public bool PlayerPhaseEnded => _playerPhaseEnded;

    // 战斗结果状态
    public bool IsBattleEnded { get; private set; }

    #endregion

    #region 生命周期

    private void Awake()
    {
        GameEventBus.Instance?.Subscribe<BF_EndPlayerPhaseRequestEvent>(OnEndPlayerPhaseRequested).UnRegisterWhenGameObjectDestroyed(gameObject);
        GameEventBus.Instance?.Subscribe<BF_SkillRequestEvent>(OnSkillRequested).UnRegisterWhenGameObjectDestroyed(gameObject);
        GameEventBus.Instance?.Subscribe<BF_EndUnitRequestEvent>(OnEndUnitRequested).UnRegisterWhenGameObjectDestroyed(gameObject);
        GameEventBus.Instance?.Subscribe<BF_ItemRequestEvent>(OnItemRequested).UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void Start()
    {
        BF_CameraManager.Instance?.BindBounds();
        _moveController.SetBattleController(this);
        StartCoroutine(StartBattle());
    }

    private void Update()
    {
        if (IsBattleEnded
            || _playerPhaseEnded
            || _state is not BF_PlayerPhaseState
            || BF_InputManager.Instance == null
            || (CurrentUnit != null && (CurrentUnit.IsMoving || CurrentUnit.IsActing)))
        {
            return;
        }

        if (BF_InputManager.Instance.EndPlayerPhasePressed)
        {
            EndPlayerPhase();
            return;
        }

        if (BF_InputManager.Instance.CancelSelectionPressed)
        {
            CancelSelection();
            return;
        }

        if (BF_InputManager.Instance.NextUnitPressed)
        {
            SelectNextPlayerUnit();
        }
    }

    private void OnDestroy()
    {
        BF_CameraManager.Instance?.ClearBounds();
        _running = false;

        if (_battleLoop != null)
        {
            StopCoroutine(_battleLoop);
        }
    }

    #endregion

    #region 战斗初始化

    private IEnumerator StartBattle()
    {
        yield return null;

        SetState(new BF_BattleSetupState(this));
        _running = true;
        _battleLoop = StartCoroutine(BattleLoopRoutine());
    }

    public void SetUnits(IReadOnlyList<BF_BattleUnit> units)
    {
        _units.Clear();

        for (int i = 0; i < units.Count; i++)
        {
            _units.Add(units[i]);
        }
    }

    #endregion

    #region 阶段切换

    // 状态机执行
    public void SetState(BF_BattleState nextState)
    {
        _state = nextState;
    }

    private IEnumerator BattleLoopRoutine()
    {
        while (_running && _state != null)
        {
            BF_BattleState state = _state;

            yield return StartCoroutine(state.Enter());
            if (state != _state)
            {
                yield return StartCoroutine(state.Exit());
                continue;
            }

            yield return StartCoroutine(state.Execute());
            yield return StartCoroutine(state.Exit());
        }

        _battleLoop = null;
    }

    // 阶段与回合
    public void SetPhase(BF_BattlePhase phase)
    {
        if (CurrentPhase == phase)
        {
            return;
        }

        CurrentPhase = phase;
        Debug.Log($"[BF] Battle Phase Changed: {CurrentPhase }");

        GameEventBus.Instance?.Publish(new BF_BattlePhaseChangeEvent(CurrentPhase, Round));
    }

    public void StartPlayerRound()
    {
        Round++;
        _playerPhaseEnded = false;

        foreach (BF_BattleUnit unit in _units)
        {
            if (unit.Team == BF_UnitTeam.Player)
            {
                unit.ResetTurn();
            }
        }
    }

    public void StartEnemyRound()
    {
        foreach (BF_BattleUnit unit in _units)
        {
            if (unit.Team == BF_UnitTeam.Enemy)
            {
                unit.ResetTurn();
            }
        }
    }

    public void EndPlayerPhase()
    {
        if (_state is not BF_PlayerPhaseState
            || (CurrentUnit != null && (CurrentUnit.IsMoving || CurrentUnit.IsActing)))
        {
            return;
        }

        ClearCurrentUnit();

        foreach (BF_BattleUnit unit in _units)
        {
            if (unit.Team == BF_UnitTeam.Player && unit.IsAlive && !unit.IsTurnEnded)
            {
                unit.FinishTurn();
            }
        }

        _playerPhaseEnded = true;
    }

    private void OnEndPlayerPhaseRequested(BF_EndPlayerPhaseRequestEvent requestEvent)
    {
        EndPlayerPhase();
    }

    public IEnumerator RunEnemyPhase()
    {
        while (!IsBattleEnded && TryGetNextUnit(BF_UnitTeam.Enemy, out BF_BattleUnit enemy))
        {
            yield return _enemyController.RunTurn(
                enemy,
                _units,
                CommandExecutor,
                CheckBattleResult);

            yield return null;
        }
    }

    #endregion

    #region 单位选择

    public bool SelectFirstPlayerUnit()
    {
        return TryGetNextUnit(BF_UnitTeam.Player, out BF_BattleUnit unit)
            && TrySelectPlayerUnit(unit);
    }

    public bool TrySelectPlayerUnit(BF_BattleUnit unit)
    {
        if (_state is not BF_PlayerPhaseState
            || _playerPhaseEnded
            || unit == null
            || unit.Team != BF_UnitTeam.Player
            || !unit.IsAlive
            || unit.IsTurnEnded
            || unit.CurrentAP <= 0
            || (CurrentUnit != null && (CurrentUnit.IsMoving || CurrentUnit.IsActing)))
        {
            return false;
        }

        CurrentUnit = unit;
        _moveController.SetUnit(unit);
        BF_CameraManager.Instance?.Focus(unit.transform);
        GameEventBus.Instance?.Publish(new BF_UnitSelectedEvent(unit));
        Debug.Log($"[BF] Player Unit Selected: {unit.DisplayName}");
        return true;
    }

    public void SelectNextPlayerUnit()
    {
        int start = CurrentUnit == null ? -1 : _units.IndexOf(CurrentUnit);

        for (int offset = 1; offset <= _units.Count; offset++)
        {
            BF_BattleUnit unit = _units[(start + offset) % _units.Count];
            if (unit.Team == BF_UnitTeam.Player
                && unit.IsAlive
                && !unit.IsTurnEnded
                && unit.CurrentAP > 0)
            {
                TrySelectPlayerUnit(unit);
                return;
            }
        }
    }

    public void CancelSelection()
    {
        if (_state is not BF_PlayerPhaseState
            || CurrentUnit == null
            || CurrentUnit.IsMoving
            || CurrentUnit.IsActing)
        {
            return;
        }

        if (_moveController.CancelActionMode())
        {
            return;
        }

        Debug.Log($"[BF] Player Unit Deselected: {CurrentUnit.DisplayName}");
        ClearCurrentUnit();
    }

    public void ClearCurrentUnit()
    {
        _moveController.ClearUnit();
        CurrentUnit = null;
        GameEventBus.Instance?.Publish(new BF_UnitSelectedEvent(null));
    }

    // 单位查询
    public bool TryGetNextUnit(BF_UnitTeam team, out BF_BattleUnit unit)
    {
        for (int i = 0; i < _units.Count; i++)
        {
            BF_BattleUnit next = _units[i];
            if (next.Team == team
                && next.IsAlive
                && !next.IsTurnEnded
                && next.CurrentAP > 0)
            {
                unit = next;
                return true;
            }
        }

        unit = null;
        return false;
    }

    public bool AreAllUnitsDone(BF_UnitTeam team)
    {
        return !TryGetNextUnit(team, out _);
    }

    #endregion

    #region 命令请求与执行

    // 技能与物品请求
    private void OnSkillRequested(BF_SkillRequestEvent requestEvent)
    {
        if (!IsBattleEnded && _state is BF_PlayerPhaseState && CurrentUnit != null)
        {
            _moveController.EnterSkillMode(requestEvent.Skill);
        }
    }

    private void OnItemRequested(BF_ItemRequestEvent requestEvent)
    {
        if (IsBattleEnded || _state is not BF_PlayerPhaseState || CurrentUnit == null)
        {
            return;
        }

        StartCoroutine(UseItemRoutine(CurrentUnit, requestEvent.Slot));
    }

    private IEnumerator UseItemRoutine(BF_BattleUnit unit, int itemSlot)
    {
        yield return CommandExecutor.Execute(BF_BattleCommandRequest.CreateItem(unit, itemSlot));
        OnUnitActionFinished(unit);
    }

    // 行动结束
    private void OnEndUnitRequested(BF_EndUnitRequestEvent requestEvent)
    {
        FinishUnit(CurrentUnit);
    }

    public void FinishUnit(BF_BattleUnit unit)
    {
        if (unit == null || unit != CurrentUnit)
        {
            return;
        }

        StartCoroutine(FinishUnitRoutine(unit));
    }

    private IEnumerator FinishUnitRoutine(BF_BattleUnit unit)
    {
        yield return CommandExecutor.Execute(BF_BattleCommandRequest.CreateEndTurn(unit));
        Debug.Log($"[BF] Player Unit Ended: {unit.DisplayName}");
        ClearCurrentUnit();
        SelectFirstPlayerUnit();
    }

    public void OnUnitActionFinished(BF_BattleUnit unit)
    {
        CheckBattleResult();
        if (IsBattleEnded || unit == null || unit != CurrentUnit)
        {
            return;
        }

        if (unit.IsTurnEnded || unit.CurrentAP <= 0)
        {
            Debug.Log($"[BF] Player Unit AP Empty: {unit.DisplayName}");
            ClearCurrentUnit();
            SelectFirstPlayerUnit();
            return;
        }

        _moveController.RefreshSelection();
        GameEventBus.Instance?.Publish(new BF_UnitSelectedEvent(unit));
    }

    #endregion

    #region 胜负结算

    public void CheckBattleResult()
    {
        bool hasPlayer = HasLivingUnit(BF_UnitTeam.Player);
        bool hasEnemy = HasLivingUnit(BF_UnitTeam.Enemy);

        if (!hasEnemy)
        {
            EndBattle(BF_BattleResult.Victory);
        }
        else if (!hasPlayer)
        {
            EndBattle(BF_BattleResult.Defeat);
        }
    }

    private bool HasLivingUnit(BF_UnitTeam team)
    {
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i].Team == team && _units[i].IsAlive)
            {
                return true;
            }
        }

        return false;
    }

    private void EndBattle(BF_BattleResult result)
    {
        if (IsBattleEnded)
        {
            return;
        }

        IsBattleEnded = true;
        _playerPhaseEnded = true;
        ClearCurrentUnit();
        SetPhase(BF_BattlePhase.BattleEnd);
        GameEventBus.Instance?.Publish(new BF_BattleResultEvent(result));
        Debug.Log($"[BF] Battle End: {result}");
        _running = false;
    }

    #endregion
}
