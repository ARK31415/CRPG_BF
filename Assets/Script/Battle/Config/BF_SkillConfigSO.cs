using UnityEngine;

/// <summary>
/// 普通攻击与后续技能共用的最小静态配置。
/// </summary>
[CreateAssetMenu(fileName = "SO_BF_Skill", menuName = "CRPG BF/Battle/Skill Config")]
public class BF_SkillConfigSO : ScriptableObject
{
    [SerializeField]
    private string _id;

    [SerializeField]
    private string _displayName;

    [TextArea]
    [SerializeField]
    private string _description;

    [SerializeField]
    private Sprite _icon;

    [Min(1)]
    [SerializeField]
    private int _apCost = 2;

    [Min(1)]
    [SerializeField]
    private int _range = 1;

    [Min(0)]
    [SerializeField]
    private int _power = 2;

    [Min(0f)]
    [SerializeField]
    private float _hitDelay = 0.25f;

    [Min(0f)]
    [SerializeField]
    private float _duration = 0.6f;

    public string Id => _id;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public int APCost => _apCost;
    public int Range => _range;
    public int Power => _power;
    public float HitDelay => _hitDelay;
    public float Duration => _duration;
}
