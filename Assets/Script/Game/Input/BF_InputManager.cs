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
    private InputAction _fastForwardAction;
    private InputDevice _lastUiDevice;
    private IDisposable _gameModeSubscription;

    public event Action FastForwardStarted;
    public event Action FastForwardCanceled;
    public event Action BindingDisplayChanged;

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

    // 离散动作改为 performed 回调发布请求事件；UI/Cancel 是唯一的跨设备 Esc 语义入口。

    #endregion

    #region 生命周期

    private void OnEnable()
    {
        _actions ??= new InputSystem_Actions();
        _actions.Player.EndPlayerPhase.performed += OnEndPlayerPhasePerformed;
        _actions.Player.NextUnit.performed += OnNextUnitPerformed;
        _fastForwardAction = _actions.asset.FindAction("UI/FastForward", false);
        if (_fastForwardAction != null)
        {
            _fastForwardAction.started += OnFastForwardStarted;
            _fastForwardAction.canceled += OnFastForwardCanceled;
        }
        _actions.UI.Submit.performed += OnUiDevicePerformed;
        _actions.UI.Cancel.performed += OnCancelPerformed;
        _actions.UI.Navigate.performed += OnUiDevicePerformed;
        _actions.UI.Click.performed += OnUiDevicePerformed;
        _gameModeSubscription = GameEventBus.Instance.Subscribe<BF_GameModeChangedEvent>(OnGameModeChanged);

        BF_GameMode gameMode = BF_GameModeManager.Instance != null
            ? BF_GameModeManager.Instance.CurrentGameMode
            : BF_GameMode.Battle;

        _actions.UI.Enable();
        _lastUiDevice = Keyboard.current != null ? Keyboard.current : Gamepad.current;
        SetPlayerInput(gameMode == BF_GameMode.Battle);
    }

    private void OnDisable()
    {
        _actions?.UI.Disable();
        SetPlayerInput(false);

        if (_actions != null)
        {
            _actions.Player.EndPlayerPhase.performed -= OnEndPlayerPhasePerformed;
            _actions.Player.NextUnit.performed -= OnNextUnitPerformed;
            _actions.UI.Submit.performed -= OnUiDevicePerformed;
            _actions.UI.Cancel.performed -= OnCancelPerformed;
            _actions.UI.Navigate.performed -= OnUiDevicePerformed;
            _actions.UI.Click.performed -= OnUiDevicePerformed;
            if (_fastForwardAction != null)
            {
                _fastForwardAction.started -= OnFastForwardStarted;
                _fastForwardAction.canceled -= OnFastForwardCanceled;
            }
        }

        _gameModeSubscription?.Dispose();
        _gameModeSubscription = null;
        _fastForwardAction = null;
        _lastUiDevice = null;
    }

    protected override void OnDestroy()
    {
        _actions?.Dispose();
        _actions = null;
        base.OnDestroy();
    }

    #endregion

    #region UI 输入提示

    public string GetSubmitBindingDisplayString()
    {
        return GetBindingDisplayString(_actions?.UI.Submit);
    }

    public string GetCancelBindingDisplayString()
    {
        return GetBindingDisplayString(_actions?.UI.Cancel);
    }

    public string GetFastForwardBindingDisplayString()
    {
        return GetBindingDisplayString(_fastForwardAction);
    }

    private string GetBindingDisplayString(InputAction action)
    {
        if (action == null)
        {
            return string.Empty;
        }

        if (_lastUiDevice != null)
        {
            for (int i = 0; i < action.controls.Count; i++)
            {
                InputControl control = action.controls[i];
                if (control.device == _lastUiDevice && !string.IsNullOrWhiteSpace(control.displayName))
                {
                    return control.displayName;
                }
            }
        }

        string display = action.GetBindingDisplayString();
        return string.IsNullOrWhiteSpace(display) ? action.name : display;
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

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        TrackUiDevice(context.control?.device);
        GameEventBus.Instance?.Publish(new BF_EscapeRequestEvent());
    }

    private void OnFastForwardStarted(InputAction.CallbackContext context)
    {
        TrackUiDevice(context.control?.device);
        FastForwardStarted?.Invoke();
    }

    private void OnFastForwardCanceled(InputAction.CallbackContext context)
    {
        FastForwardCanceled?.Invoke();
    }

    private void OnUiDevicePerformed(InputAction.CallbackContext context)
    {
        TrackUiDevice(context.control?.device);
    }

    private void TrackUiDevice(InputDevice device)
    {
        if (device == null || device == _lastUiDevice)
        {
            return;
        }

        _lastUiDevice = device;
        BindingDisplayChanged?.Invoke();
    }

    #endregion
}
