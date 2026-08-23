using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个战棋单位的逻辑格位置和逐格移动表现。
/// </summary>
public class BF_BattleUnit : MonoBehaviour
{
    [SerializeField]
    private BF_BoardManager _board;

    [SerializeField]
    private BF_UnitConfigSO _config;

    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private SpriteRenderer _sprite;

    [SerializeField]
    private Vector2Int _startPos = new(1, 1);

    [SerializeField]
    private BF_UnitTeam _team = BF_UnitTeam.Player;

    public Vector2Int GridPos { get; private set; }
    public BF_UnitTeam Team => _team;
    public BF_UnitConfigSO Config => _config;
    public string UnitId => _config != null ? _config.Id : string.Empty;
    public string DisplayName => _config != null && !string.IsNullOrEmpty(_config.DisplayName) ? _config.DisplayName : gameObject.name;
    public int MoveRange => _config != null ? _config.MoveRange : 0;
    public bool IsMoving { get; private set; }
    public bool HasActed { get; private set; }

    private void Start()
    {
        GridPos = _startPos;
        HasActed = false;

        if (_config == null)
        {
            Debug.LogError("Unit config is missing.", this);
            return;
        }

        if(_sprite == null)
        {
            _sprite = GetComponent<SpriteRenderer>();
        }
        SetFacing(_team == BF_UnitTeam.Player);

        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_animator != null && _config.AnimatorController != null)
        {
            _animator.runtimeAnimatorController = _config.AnimatorController;
            _animator.SetBool("IsMoving", false);
        }

        if (_board == null || !_board.IsInitialized || !_board.TryOccupy(GridPos, gameObject))
        {
            Debug.LogError($"Cannot place battle unit at {GridPos}.", this);
            return;
        }

        transform.position = _board.GridToWorld(GridPos);
    }

    public void ResetTurn()
    {
        HasActed = false;
    }

    public void FinishTurn()
    {
        HasActed = true;
    }

    /// <summary>
    /// 沿不包含起点的逻辑路径逐格移动，完成后提交棋盘占用。
    /// </summary>
    public IEnumerator Move(IReadOnlyList<Vector2Int> path)
    {
        if (IsMoving || path == null || path.Count == 0 || _board == null || _config == null)
        {
            yield break;
        }

        IsMoving = true;
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", true);
        }

        Vector2Int from = GridPos;
        float speed = _config.MoveSpeed;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int prev = i == 0 ? GridPos : path[i - 1];
            Vector2Int step = path[i] - prev;

            UpdateFacing(step.x);

            Vector3 target = _board.GridToWorld(path[i]);

            while (transform.position != target)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target,
                    speed * Time.deltaTime);
                yield return null;
            }
        }

        Vector2Int to = path[path.Count - 1];
        if (_board.TryMoveOccupant(from, to, gameObject))
        {
            GridPos = to;
        }
        else
        {
            transform.position = _board.GridToWorld(from);
        }

        IsMoving = false;
        if (_animator != null)
        {
            _animator.SetBool("IsMoving", false);
        }
    }

    private void SetFacing(bool faceRight)
    {
        _sprite.flipX = !faceRight;
    }

    private void UpdateFacing(int x)
    {
        if(x > 0)
        {
            SetFacing(true);
        }
        else if(x < 0)
        {
            SetFacing(false);
        }
    }
}
