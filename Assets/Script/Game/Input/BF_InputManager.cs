using UnityEngine;

/// <summary>
/// Persistent 场景中的全局输入入口。
/// </summary>
public class BF_InputManager : Singleton<BF_InputManager>
{
    private InputSystem_Actions _actions;

    public Vector2 Point => _actions.Player.Point.ReadValue<Vector2>();
    public Vector2 CameraMove => _actions.Player.CameraMove.ReadValue<Vector2>();
    public float CameraZoom => _actions.Player.CameraZoom.ReadValue<Vector2>().y;
    public bool ClickPressed => _actions.Player.Click.WasPressedThisFrame();
    public bool AttackPressed => _actions.Player.Attack.WasPressedThisFrame();
    public bool NextUnitPressed => _actions.Player.NextUnit.WasPressedThisFrame();
    public bool CancelSelectionPressed => _actions.Player.CancelSelection.WasPressedThisFrame();
    public bool EndPlayerPhasePressed => _actions.Player.EndPlayerPhase.WasPressedThisFrame();

    private void OnEnable()
    {
        _actions ??= new InputSystem_Actions();
        _actions.Player.Enable();
    }

    private void OnDisable()
    {
        _actions?.Player.Disable();
    }

    protected override void OnDestroy()
    {
        _actions?.Dispose();
        _actions = null;
        base.OnDestroy();
    }
}
