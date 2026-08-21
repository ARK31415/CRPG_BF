using UnityEngine;

/// <summary>
/// Persistent 场景中的摄像头表现入口。
/// </summary>
public class BF_CameraManager : Singleton<BF_CameraManager> {
    [SerializeField]
    private Camera _camera;

    protected override void Awake() {
        base.Awake();

        if (_camera == null) {
            _camera = Camera.main;
        }
    }

    public void Focus(BF_BattleUnit unit) {
        if (_camera == null || unit == null) {
            return;
        }

        Vector3 pos = unit.transform.position;
        pos.z = _camera.transform.position.z;
        _camera.transform.position = pos;
    }
}
