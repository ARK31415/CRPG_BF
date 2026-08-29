using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 普通攻击与角色技能共用的静态配置。
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

    [Header("Range")]
    [SerializeField]
    private BF_SkillTargetType _targetType;

    [SerializeField]
    private BF_SkillAreaType _areaType;

    [SerializeField]
    private BF_SkillTargetGroup _targetGroup = BF_SkillTargetGroup.Enemy;

    [Min(1)]
    [SerializeField]
    private int _apCost = 2;

    [Min(1)]
    [SerializeField]
    private int _targetRange = 1;

    [Min(1)]
    [SerializeField]
    private int _areaSize = 1;

    [SerializeField]
    [FormerlySerializedAs("_power")]
    private float _rate = 1f;

    [Header("Display")]
    [SerializeField]
    private BF_SkillAnim _anim;

    [SerializeField]
    private GameObject _effectPrefab;

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
    public BF_SkillTargetType TargetType => _targetType;
    public BF_SkillAreaType AreaType => _areaType;
    public BF_SkillTargetGroup TargetGroup => _targetGroup;
    public int TargetRange => _targetRange;
    public int AreaSize => _areaSize;
    public float Rate => _rate;
    public BF_SkillAnim Anim => _anim;
    public GameObject EffectPrefab => _effectPrefab;
    public float HitDelay => _hitDelay;
    public float Duration => _duration;
}
