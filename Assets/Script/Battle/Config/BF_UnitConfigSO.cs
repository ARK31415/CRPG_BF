using UnityEngine;

/// <summary>
/// 单位静态描述、移动参数和基础动画控制器配置。
/// </summary>
[CreateAssetMenu(fileName = "SO_BF_UnitConfig", menuName = "CRPG BF/Battle/Unit Config")]
public class BF_UnitConfigSO : ScriptableObject
{
    [SerializeField]
    private string _id;

    [SerializeField]
    private string _displayName;

    [TextArea]
    [SerializeField]
    private string _description;

    [Header("Battle")]
    [Min(1)]
    [SerializeField]
    private int _maxHP = 10;

    [Min(0)]
    [SerializeField]
    private int _attack = 2;

    [Min(0)]
    [SerializeField]
    private int _defense;

    [Min(1)]
    [SerializeField]
    private int _maxAP = 6;

    [SerializeField]
    private BF_SkillConfigSO _basicAttack;

    [Header("Move")]
    [Min(1)]
    [SerializeField]
    private int _moveRange = 4;

    [Min(0.1f)]
    [SerializeField]
    private float _moveSpeed = 4f;

    [SerializeField]
    private RuntimeAnimatorController _animatorController;

    public string Id => _id;
    public string DisplayName => _displayName;
    public string Description => _description;
    public int MaxHP => _maxHP;
    public int Attack => _attack;
    public int Defense => _defense;
    public int MaxAP => _maxAP;
    public BF_SkillConfigSO BasicAttack => _basicAttack;
    public int MoveRange => _moveRange;
    public float MoveSpeed => _moveSpeed;
    public RuntimeAnimatorController AnimatorController => _animatorController;
}
