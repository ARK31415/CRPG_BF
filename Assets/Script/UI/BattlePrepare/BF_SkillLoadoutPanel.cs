using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_SkillLoadoutPanel : MonoBehaviour
{
    #region 序列化配置与引用

    [Header("已装备技能")]
    [SerializeField]
    private BF_SkillSlot _basicAttackSlot;

    [SerializeField]
    private BF_SkillSlot[] _skillSlots = new BF_SkillSlot[2];

    [Header("候选技能")]
    [SerializeField]
    private BF_SkillSlot _availableSlotPrefab;

    [SerializeField]
    private Transform _availableContent;

    [Header("技能详情")]
    [SerializeField]
    private Image _detailIcon;

    [SerializeField]
    private TMP_Text _detailNameText;

    [SerializeField]
    private TMP_Text _detailInfoText;

    [SerializeField]
    private TMP_Text _detailDescriptionText;

    #endregion

    #region 运行时数据

    // 生成格缓存
    private readonly List<BF_SkillSlot> _availableSlots = new();

    // 绑定数据
    private BF_UnitRuntimeData _data;
    private BF_UnitConfigSO _config;

    // 选择状态：-2 表示当前查看普通攻击（不可替换），0/1 为技能槽索引。
    private int _selectedSlot = -2;

    // 订阅句柄
    private IDisposable _subscription;

    #endregion

    #region 生命周期

    private void OnEnable()
    {
        if (GameEventBus.Instance != null)
        {
            _subscription = GameEventBus.Instance.Subscribe<BF_UnitRuntimeChangedEvent>(OnUnitChanged);
        }

        Refresh();
    }

    private void OnDisable()
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    #endregion

    #region 绑定与刷新

    public void ShowUnit(BF_UnitRuntimeData data, BF_UnitConfigSO config)
    {
        _data = data;
        _config = config;
        _selectedSlot = -2;
        Refresh();
        ShowDetail(_config != null ? _config.BasicAttack : null);
    }

    public void Refresh()
    {
        if (_config == null || _data == null)
        {
            return;
        }

        _basicAttackSlot.Setup(_config.BasicAttack, "普通攻击", SelectBasicAttack);
        _basicAttackSlot.SetSelected(_selectedSlot == -2);

        for (int i = 0; i < _skillSlots.Length; i++)
        {
            int slot = i;
            string skillId = i == 0 ? _data.Skill01Id : _data.Skill02Id;
            BF_SkillConfigSO skill = _config.GetSkill(skillId);
            _skillSlots[i].Setup(skill, $"技能 {i + 1}", () => SelectSlot(slot));
            _skillSlots[i].SetSelected(_selectedSlot == i);
        }

        RefreshAvailable();
    }

    #endregion

    #region 槽位选择

    private void SelectBasicAttack()
    {
        _selectedSlot = -2;
        ShowDetail(_config.BasicAttack);
        Refresh();
    }

    private void SelectSlot(int slot)
    {
        _selectedSlot = slot;
        string skillId = slot == 0 ? _data.Skill01Id : _data.Skill02Id;
        ShowDetail(_config.GetSkill(skillId));
        Refresh();
    }

    private void SelectAvailable(BF_SkillConfigSO skill)
    {
        ShowDetail(skill);
        if (_selectedSlot < 0 || skill == null)
        {
            return;
        }

        string current = _selectedSlot == 0 ? _data.Skill01Id : _data.Skill02Id;
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        runtime?.SetSkill(_data.UnitId, _selectedSlot, current == skill.Id ? string.Empty : skill.Id);
    }

    #endregion

    #region 候选技能刷新

    private void RefreshAvailable()
    {
        List<BF_SkillConfigSO> skills = GetSkills();
        string current = _selectedSlot switch
        {
            0 => _data.Skill01Id,
            1 => _data.Skill02Id,
            _ => string.Empty
        };
        string usedByOtherSlot = _selectedSlot switch
        {
            0 => _data.Skill02Id,
            1 => _data.Skill01Id,
            _ => string.Empty
        };

        skills.RemoveAll(skill => skill.Id == usedByOtherSlot && skill.Id != current);

        while (_availableSlots.Count < skills.Count)
        {
            _availableSlots.Add(Instantiate(_availableSlotPrefab, _availableContent));
        }

        for (int i = 0; i < _availableSlots.Count; i++)
        {
            bool isVisible = i < skills.Count;
            _availableSlots[i].gameObject.SetActive(isVisible);
            if (!isVisible)
            {
                continue;
            }

            BF_SkillConfigSO skill = skills[i];
            _availableSlots[i].Setup(skill, "可选技能", () => SelectAvailable(skill));
            _availableSlots[i].SetSelected(skill.Id == current);
        }
    }

    private List<BF_SkillConfigSO> GetSkills()
    {
        List<BF_SkillConfigSO> skills = new();
        AddSkill(skills, _config.Skill01);
        AddSkill(skills, _config.Skill02);
        foreach (BF_SkillConfigSO skill in _config.AvailableSkills)
        {
            AddSkill(skills, skill);
        }

        return skills;
    }

    private void AddSkill(List<BF_SkillConfigSO> skills, BF_SkillConfigSO skill)
    {
        if (skill != null && !skills.Contains(skill) && skill != _config.BasicAttack)
        {
            skills.Add(skill);
        }
    }

    #endregion

    #region 技能详情

    private void ShowDetail(BF_SkillConfigSO skill)
    {
        _detailIcon.sprite = skill != null ? skill.Icon : null;
        _detailIcon.enabled = skill != null && skill.Icon != null;
        _detailNameText.text = skill != null ? skill.DisplayName : "请选择技能";
        _detailInfoText.text = skill != null
            ? $"{skill.APCost} AP  |  倍率 {skill.Rate:0.##}  |  距离 {skill.TargetRange}"
            : string.Empty;
        _detailDescriptionText.text = skill != null ? skill.Description : string.Empty;
    }

    #endregion

    #region 事件处理

    private void OnUnitChanged(BF_UnitRuntimeChangedEvent evt)
    {
        if (_data != null && evt.UnitId == _data.UnitId)
        {
            Refresh();
        }
    }

    #endregion
}
