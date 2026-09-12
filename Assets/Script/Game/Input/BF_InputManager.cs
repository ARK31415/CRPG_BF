using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

    // 指针与相机（持续值，保持轮询）
    public Vector2 Point => _actions.Player.Point.ReadValue<Vector2>();
    public Vector2 CameraMove => _actions.Player.CameraMove.ReadValue<Vector2>();
    public float CameraZoom => _actions.Player.CameraZoom.ReadValue<Vector2>().y;

    // 上下文动作（依赖指针 / 选中 / UI 状态，保持轮询）
    public bool ClickPressed => _actions.Player.Click.WasPressedThisFrame();
    public bool MovePressed => _actions.Player.Move.WasPressedThisFrame();
    public bool AttackPressed => _actions.Player.Attack.WasPressedThisFrame();

    // 全局离散动作（EndPlayerPhase / NextUnit / Pause）改为 performed 回调发布请求事件，不再暴露轮询属性。

    #endregion

    #region 生命周期

    private void OnEnable()
    {
        _actions ??= new InputSystem_Actions();
        _actions.Player.EndPlayerPhase.performed += OnEndPlayerPhasePerformed;
        _actions.Player.NextUnit.performed += OnNextUnitPerformed;
        _actions.Global.Pause.performed += OnPausePerformed;
        _gameModeSubscription = GameEventBus.Instance.Subscribe<BF_GameModeChangedEvent>(OnGameModeChanged);

        BF_GameMode gameMode = BF_GameModeManager.Instance != null
            ? BF_GameModeManager.Instance.CurrentGameMode
            : BF_GameMode.Battle;

        _actions.Global.Enable();
        SetPlayerInput(gameMode == BF_GameMode.Battle);
    }

    private void OnDisable()
    {
        _actions?.Global.Disable();
        SetPlayerInput(false);

        if (_actions != null)
        {
            _actions.Player.EndPlayerPhase.performed -= OnEndPlayerPhasePerformed;
            _actions.Player.NextUnit.performed -= OnNextUnitPerformed;
            _actions.Global.Pause.performed -= OnPausePerformed;
        }

        _gameModeSubscription?.Dispose();
        _gameModeSubscription = null;
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

    #region 输入回调

    // 全局离散动作只订阅 performed；回调内只发布请求，不判断业务上下文。
    private void OnEndPlayerPhasePerformed(InputAction.CallbackContext context)
    {
        GameEventBus.Instance?.Publish(new BF_EndPlayerPhaseRequestEvent());
    }

    private void OnNextUnitPerformed(InputAction.CallbackContext context)
    {
        GameEventBus.Instance?.Publish(new BF_NextUnitRequestEvent());
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        GameEventBus.Instance?.Publish(new BF_EscapeRequestEvent());
    }

    #endregion
}
