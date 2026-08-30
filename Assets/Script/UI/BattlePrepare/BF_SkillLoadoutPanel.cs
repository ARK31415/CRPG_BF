using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_SkillLoadoutPanel : MonoBehaviour
{
    [SerializeField] private BF_SkillSlot _basicAttackSlot;
    [SerializeField] private BF_SkillSlot[] _skillSlots = new BF_SkillSlot[2];
    [SerializeField] private BF_SkillSlot _availableSlotPrefab;
    [SerializeField] private Transform _availableContent;
    [SerializeField] private Image _detailIcon;
    [SerializeField] private TMP_Text _detailNameText;
    [SerializeField] private TMP_Text _detailInfoText;
    [SerializeField] private TMP_Text _detailDescriptionText;

    private readonly List<BF_SkillSlot> _availableSlots = new();
    private BF_UnitRuntimeService _runtime;
    private BF_UnitConfigSO _unit;
    private int _selectedSlot = -2;
    private IDisposable _subscription;

    private BF_UnitRuntimeData Data => _unit != null && _runtime != null
        ? _runtime.Get(_unit.Id)
        : null;

    private void OnEnable()
    {
        _runtime = FindFirstObjectByType<BF_UnitRuntimeService>();
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

    public void ShowUnit(BF_UnitConfigSO unit)
    {
        _runtime ??= FindFirstObjectByType<BF_UnitRuntimeService>();
        _unit = unit;
        _selectedSlot = -2;
        Refresh();
        ShowDetail(_unit != null ? _unit.BasicAttack : null);
    }

    public void Refresh()
    {
        if (_unit == null || Data == null)
        {
            return;
        }

        _basicAttackSlot.Setup(_unit.BasicAttack, "普通攻击", SelectBasicAttack);
        _basicAttackSlot.SetSelected(_selectedSlot == -2);

        for (int i = 0; i < _skillSlots.Length; i++)
        {
            int slot = i;
            string skillId = i == 0 ? Data.Skill01Id : Data.Skill02Id;
            BF_SkillConfigSO skill = _unit.GetSkill(skillId);
            _skillSlots[i].Setup(skill, $"技能 {i + 1}", () => SelectSlot(slot));
            _skillSlots[i].SetSelected(_selectedSlot == i);
        }

        RefreshAvailable();
    }

    private void SelectBasicAttack()
    {
        _selectedSlot = -2;
        ShowDetail(_unit.BasicAttack);
        Refresh();
    }

    private void SelectSlot(int slot)
    {
        _selectedSlot = slot;
        string skillId = slot == 0 ? Data.Skill01Id : Data.Skill02Id;
        ShowDetail(_unit.GetSkill(skillId));
        Refresh();
    }

    private void SelectAvailable(BF_SkillConfigSO skill)
    {
        ShowDetail(skill);
        if (_selectedSlot < 0 || skill == null)
        {
            return;
        }

        string current = _selectedSlot == 0 ? Data.Skill01Id : Data.Skill02Id;
        _runtime.SetSkill(_unit.Id, _selectedSlot, current == skill.Id ? string.Empty : skill.Id);
    }

    private void RefreshAvailable()
    {
        List<BF_SkillConfigSO> skills = GetSkills();
        string current = _selectedSlot switch
        {
            0 => Data.Skill01Id,
            1 => Data.Skill02Id,
            _ => string.Empty
        };
        string usedByOtherSlot = _selectedSlot switch
        {
            0 => Data.Skill02Id,
            1 => Data.Skill01Id,
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
        AddSkill(skills, _unit.Skill01);
        AddSkill(skills, _unit.Skill02);
        foreach (BF_SkillConfigSO skill in _unit.AvailableSkills)
        {
            AddSkill(skills, skill);
        }

        return skills;
    }

    private void AddSkill(List<BF_SkillConfigSO> skills, BF_SkillConfigSO skill)
    {
        if (skill != null && !skills.Contains(skill) && skill != _unit.BasicAttack)
        {
            skills.Add(skill);
        }
    }

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

    private void OnUnitChanged(BF_UnitRuntimeChangedEvent evt)
    {
        if (_unit != null && evt.UnitId == _unit.Id)
        {
            Refresh();
        }
    }
}
