using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Persistent 场景中的摄像头表现入口。
/// </summary>
public class BF_CameraManager : Singleton<BF_CameraManager> {
    [SerializeField]
    private CinemachineCamera _battleCamera;

    [SerializeField]
    private CinemachineConfiner2D _confiner;

    [SerializeField]
    private float _moveSpeed = 6f;

    [SerializeField]
    private float _zoomStep = 0.5f;

    [SerializeField]
    private float _zoomSpeed = 4f;

    [SerializeField]
    private float _minZoom = 3f;

    [SerializeField]
    private float _maxZoom = 5f;

    private float _zoomTarget;
    protected override void Awake() {
        base.Awake();

        if (_battleCamera != null) {
            if (_confiner == null) {
                _confiner = _battleCamera.GetComponent<CinemachineConfiner2D>();
            }

            _zoomTarget = _battleCamera.Lens.OrthographicSize;
        }
    }

    public void Focus(BF_BattleUnit unit) {
        if (_battleCamera == null || unit == null) {
            return;
        }

        Vector3 pos = unit.transform.position;
        pos.z = _battleCamera.transform.position.z;
        _battleCamera.transform.position = pos;
        _battleCamera.ForceCameraPosition(pos, _battleCamera.transform.rotation);
    }

    private void Update() {
        if (_battleCamera == null || BF_InputManager.Instance == null) {
            return;
        }

        Vector2 input = BF_InputManager.Instance.CameraMove;
        if (input.sqrMagnitude > 0f) {
            Vector3 move = new Vector3(input.x, input.y, 0f).normalized;
            _battleCamera.transform.position += move * (_moveSpeed * Time.deltaTime);
        }

        float zoom = BF_InputManager.Instance.CameraZoom;
        if (Mathf.Abs(zoom) > 0.01f) {
            _zoomTarget = Mathf.Clamp(
                _zoomTarget - Mathf.Sign(zoom) * _zoomStep,
                _minZoom,
                _maxZoom);
        }

        LensSettings lens = _battleCamera.Lens;
        float size = Mathf.MoveTowards(
            lens.OrthographicSize,
            _zoomTarget,
            _zoomSpeed * Time.deltaTime);

        if (Mathf.Approximately(lens.OrthographicSize, size)) {
            return;
        }

        lens.OrthographicSize = size;
        _battleCamera.Lens = lens;
        _confiner?.InvalidateLensCache();
    }

    public void BindBounds() {
        GameObject bounds = GameObject.FindGameObjectWithTag("Bounds");
        SetBounds(bounds == null ? null : bounds.GetComponent<Collider2D>());
    }

    public void SetBounds(Collider2D bounds) {
        if (_confiner == null) {
            return;
        }

        _confiner.BoundingShape2D = bounds;
        _confiner.InvalidateBoundingShapeCache();
        _confiner.InvalidateLensCache();
    }

    public void ClearBounds() {
        SetBounds(null);
    }

}
