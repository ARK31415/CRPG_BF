using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个战棋单位的逻辑格位置和逐格移动表现。
/// </summary>
public class BF_BattleUnit : MonoBehaviour
{
    private static readonly int IsMovingId = Animator.StringToHash("IsMoving");
    private static readonly int AttackId = Animator.StringToHash("Attack");
    private static readonly int Skill01Id = Animator.StringToHash("Skill01");
    private static readonly int Skill02Id = Animator.StringToHash("Skill02");
    private static readonly int HurtId = Animator.StringToHash("Hurt");
    private static readonly int IsDeadId = Animator.StringToHash("IsDead");

    private BF_BoardManager _board;
    private BF_UnitConfigSO _config;

    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private SpriteRenderer _sprite;

    [SerializeField]
    private Transform _worldUIAnchor;

    [SerializeField]
    private Transform _damagePopupAnchor;

    public Vector2Int GridPos { get; private set; }
    public BF_UnitTeam Team { get; private set; }
    public BF_UnitConfigSO Config => _config;
    public string UnitId => _config != null ? _config.Id : string.Empty;
    public string DisplayName => _config != null && !string.IsNullOrEmpty(_config.DisplayName) ? _config.DisplayName : gameObject.name;
    public int MaxHP => _config != null ? _config.MaxHP : 0;
    public int MaxAP => _config != null ? _config.MaxAP : 0;
    public Transform WorldUIAnchor => _worldUIAnchor;
    public Transform DamagePopupAnchor => _damagePopupAnchor;
    public int CurrentHP { get; private set; }
    public int CurrentAP { get; private set; }
    public bool IsAlive => CurrentHP > 0;
    public bool IsMoving { get; private set; }
    public bool IsActing { get; private set; }
    public bool IsTurnEnded { get; private set; }
    public bool HasActed { get; private set; }

    public void Init(
        BF_BoardManager board,
        BF_UnitConfigSO config,
        BF_UnitTeam team,
        Vector2Int pos)
    {
        _board = board;
        _config = config;
        Team = team;
        GridPos = pos;
        HasActed = false;

        if (_config == null)
        {
            Debug.LogError("Unit config is missing.", this);
            return;
        }

        CurrentHP = _config.MaxHP;
        CurrentAP = 0;
        IsTurnEnded = false;

        SetFacing(Team == BF_UnitTeam.Player);

        if (_animator != null && _config.AnimatorController != null)
        {
            _animator.runtimeAnimatorController = _config.AnimatorController;
            _animator.SetBool(IsMovingId, false);
            _animator.SetBool(IsDeadId, false);
        }

        if (_board == null || !_board.IsInitialized || !_board.TryOccupy(GridPos, gameObject))
        {
            Debug.LogError($"Cannot place battle unit at {GridPos}.", this);
            return;
        }

        transform.position = _board.GridToWorld(GridPos);
        PublishStats();
    }

    public void ResetTurn()
    {
        if (!IsAlive)
        {
            return;
        }

        CurrentAP = MaxAP;
        IsTurnEnded = false;
        HasActed = false;
        PublishStats();
    }

    public void FinishTurn()
    {
        CurrentAP = 0;
        IsTurnEnded = true;
        HasActed = true;
        PublishStats();
    }

    public bool CanPay(int cost)
    {
        return IsAlive && !IsTurnEnded && !IsMoving && !IsActing && CurrentAP >= cost;
    }

    public bool SpendAP(int cost)
    {
        if (cost < 0 || !CanPay(cost))
        {
            return false;
        }

        CurrentAP -= cost;
        if (CurrentAP == 0)
        {
            IsTurnEnded = true;
            HasActed = true;
        }

        PublishStats();
        return true;
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
            _animator.SetBool(IsMovingId, true);
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
            _animator.SetBool(IsMovingId, false);
        }
    }

    public IEnumerator UseSkill(BF_SkillConfigSO skill, Vector2Int targetPos)
    {
        if (skill == null || _board == null || !CanPay(skill.APCost))
        {
            yield break;
        }

        SpendAP(skill.APCost);
        IsActing = true;
        UpdateFacing(targetPos.x - GridPos.x);

        if (_animator != null)
        {
            _animator.SetTrigger(GetAnimId(skill.Anim));
        }

        List<Vector2Int> area = BF_SkillRange.GetAreaCells(_board, this, targetPos, skill);
        ShowEffect(skill, targetPos, area);

        if (skill.HitDelay > 0f)
        {
            yield return new WaitForSeconds(skill.HitDelay);
        }

        for (int i = 0; i < area.Count; i++)
        {
            if (_board.IsBlocked(area[i])
                || !_board.TryGetOccupant(area[i], out GameObject occupant)
                || !occupant.TryGetComponent(out BF_BattleUnit target)
                || !target.IsAlive
                || !CanTarget(target, skill.TargetGroup))
            {
                continue;
            }

            int damage = Mathf.Max(1, Mathf.RoundToInt(_config.Attack * skill.Rate) - target.Config.Defense);
            target.TakeDamage(damage);
        }

        float remaining = Mathf.Max(0f, skill.Duration - skill.HitDelay);
        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }

        IsActing = false;
        PublishStats();
    }

    public bool CanTarget(BF_BattleUnit target, BF_SkillTargetGroup group)
    {
        if (target == this)
        {
            return (group & BF_SkillTargetGroup.Self) != 0;
        }

        BF_SkillTargetGroup targetGroup = target.Team == Team
            ? BF_SkillTargetGroup.Ally
            : BF_SkillTargetGroup.Enemy;
        return (group & targetGroup) != 0;
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive)
        {
            return;
        }

        CurrentHP = Mathf.Max(0, CurrentHP - Mathf.Max(0, damage));

        if (CurrentHP == 0)
        {
            Die();
        }
        else if (_animator != null)
        {
            _animator.SetTrigger(HurtId);
        }

        PublishStats();
    }

    // 迁移来的动画保留了旧事件；当前伤害时点由 SkillSO.HitDelay 统一控制。
    public void OnAnimationAttackHit()
    {
    }

    public void OnAnimationDeathFinished()
    {
        gameObject.SetActive(false);
    }

    private void SetFacing(bool faceRight)
    {
        if (_sprite != null)
        {
            _sprite.flipX = !faceRight;
        }
    }

    private void UpdateFacing(int x)
    {
        if (x > 0)
        {
            SetFacing(true);
        }
        else if (x < 0)
        {
            SetFacing(false);
        }
    }

    private int GetAnimId(BF_SkillAnim anim)
    {
        return anim switch
        {
            BF_SkillAnim.Skill01 => Skill01Id,
            BF_SkillAnim.Skill02 => Skill02Id,
            _ => AttackId
        };
    }

    private void ShowEffect(
        BF_SkillConfigSO skill,
        Vector2Int targetPos,
        IReadOnlyList<Vector2Int> area)
    {
        if (skill.EffectPrefab == null)
        {
            return;
        }

        Vector2Int effectPos = skill.AreaType == BF_SkillAreaType.ProjectileLine && area.Count > 0
            ? area[area.Count - 1]
            : targetPos;
        GameObject effect = Instantiate(skill.EffectPrefab, _board.GridToWorld(effectPos), Quaternion.identity);

        if (effect.TryGetComponent(out BF_SkillEffect skillEffect))
        {
            skillEffect.Play(_board, GridPos, area, skill.Duration);
        }
        else
        {
            Destroy(effect, skill.Duration);
        }
    }

    private void Die()
    {
        CurrentAP = 0;
        IsTurnEnded = true;
        HasActed = true;
        _board?.TryVacate(GridPos, gameObject);

        if (_animator != null)
        {
            _animator.SetBool(IsMovingId, false);
            _animator.SetBool(IsDeadId, true);
        }
    }

    private void PublishStats()
    {
        GameEventBus.Instance?.Publish(new BF_UnitStatsChangedEvent(this));
    }
}
