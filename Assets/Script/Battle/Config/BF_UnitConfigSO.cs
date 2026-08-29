using System.Collections.Generic;
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

    [SerializeField]
    private BF_SkillConfigSO _skill01;

    [SerializeField]
    private BF_SkillConfigSO _skill02;

    [SerializeField]
    private List<BF_SkillConfigSO> _availableSkills = new();

    [Header("Display")]
    [SerializeField]
    private Sprite _portrait;

    [Header("Move")]
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
    public BF_SkillConfigSO Skill01 => _skill01;
    public BF_SkillConfigSO Skill02 => _skill02;
    public IReadOnlyList<BF_SkillConfigSO> AvailableSkills => _availableSkills;
    public Sprite Portrait => _portrait;
    public float MoveSpeed => _moveSpeed;
    public RuntimeAnimatorController AnimatorController => _animatorController;

    public BF_SkillConfigSO GetSkill(string skillId)
    {
        if (_skill01 != null && _skill01.Id == skillId)
        {
            return _skill01;
        }

        if (_skill02 != null && _skill02.Id == skillId)
        {
            return _skill02;
        }

        return _availableSkills.Find(skill => skill != null && skill.Id == skillId);
    }
}
