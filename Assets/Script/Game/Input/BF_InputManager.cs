using System;
using UnityEngine;

/// <summary>
/// Persistent 场景中的全局输入入口。
/// </summary>
public class BF_InputManager : Singleton<BF_InputManager>
{
    #region 运行时数据

    // 输入资产与订阅句柄
    private InputSystem_Actions _actions;
    private IDisposable _gameModeSubscription;

    #endregion

    #region 对外接口

    // 指针与相机
    public Vector2 Point => _actions.Player.Point.ReadValue<Vector2>();
    public Vector2 CameraMove => _actions.Player.CameraMove.ReadValue<Vector2>();
    public float CameraZoom => _actions.Player.CameraZoom.ReadValue<Vector2>().y;

    // 点击
    public bool ClickPressed => _actions.Player.Click.WasPressedThisFrame();
    public bool ClickHeld => _actions.Player.Click.IsPressed();
    public bool ClickReleased => _actions.Player.Click.WasReleasedThisFrame();

    // 战斗操作
    public bool MovePressed => _actions.Player.Move.WasPressedThisFrame();
    public bool AttackPressed => _actions.Player.Attack.WasPressedThisFrame();
    public bool NextUnitPressed => _actions.Player.NextUnit.WasPressedThisFrame();
    public bool EndPlayerPhasePressed => _actions.Player.EndPlayerPhase.WasPressedThisFrame();

    // 全局
    public bool PausePressed => _actions.Global.Pause.WasPressedThisFrame();

    #endregion

    #region 生命周期

    private void OnEnable()
    {
        _actions ??= new InputSystem_Actions();
        _gameModeSubscription = GameEventBus.Instance.Subscribe<BF_GameModeChangedEvent>(OnGameModeChanged);

        BF_GameMode gameMode = BF_GameModeManager.Instance != null
            ? BF_GameModeManager.Instance.CurrentGameMode
            : BF_GameMode.Battle;

        _actions.Global.Enable();
        SetPlayerInput(gameMode == BF_GameMode.Battle);
    }

    private void OnDisable()
    {
        _gameModeSubscription?.Dispose();
        _gameModeSubscription = null;
        _actions?.Global.Disable();
        SetPlayerInput(false);
    }

    protected override void OnDestroy()
    {
        _actions?.Dispose();
        _actions = null;
        base.OnDestroy();
    }

    #endregion

    #region 输入开关

    private void OnGameModeChanged(BF_GameModeChangedEvent gameEvent)
    {
        SetPlayerInput(gameEvent.CurrentMode == BF_GameMode.Battle);
    }

    private void SetPlayerInput(bool isEnabled)
    {
        if (_actions == null)
        {
            return;
        }

        if (isEnabled)
        {
            _actions.Player.Enable();
        }
        else
        {
            _actions.Player.Disable();
        }
    }

    #endregion
}
