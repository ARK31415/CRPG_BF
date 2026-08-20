using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个战棋单位的逻辑格位置和逐格移动表现。
/// </summary>
public class BF_BattleUnit : MonoBehaviour {
    [SerializeField]
    private BF_BoardManager _board;

    [SerializeField]
    private Vector2Int _startPos = new(1, 1);

    [SerializeField]
    [Min(1)]
    private int _moveRange = 4;

    [SerializeField]
    [Min(0.1f)]
    private float _moveSpeed = 4f;

    public Vector2Int GridPos { get; private set; }
    public int MoveRange => _moveRange;
    public bool IsMoving { get; private set; }

    private void Start() {
        GridPos = _startPos;

        if (_board == null || !_board.IsInitialized || !_board.TryOccupy(GridPos, gameObject)) {
            Debug.LogError($"Cannot place battle unit at {GridPos}.", this);
            return;
        }

        transform.position = _board.GridToWorld(GridPos);
    }

    /// <summary>
    /// 沿不包含起点的逻辑路径逐格移动，完成后提交棋盘占用。
    /// </summary>
    public IEnumerator Move(IReadOnlyList<Vector2Int> path) {
        if (IsMoving || path == null || path.Count == 0) {
            yield break;
        }

        IsMoving = true;
        Vector2Int from = GridPos;

        for (int i = 0; i < path.Count; i++) {
            Vector3 target = _board.GridToWorld(path[i]);

            while (transform.position != target) {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target,
                    _moveSpeed * Time.deltaTime);
                yield return null;
            }
        }

        Vector2Int to = path[path.Count - 1];
        if (_board.TryMoveOccupant(from, to, gameObject)) {
            GridPos = to;
        } else {
            transform.position = _board.GridToWorld(from);
        }

        IsMoving = false;
    }
}
