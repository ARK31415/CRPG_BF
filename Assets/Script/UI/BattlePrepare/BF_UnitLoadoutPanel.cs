using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_UnitLoadoutPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _unitNameText;
    [SerializeField] private TMP_Text _statsText;
    [SerializeField] private Button _prevUnitButton;
    [SerializeField] private Button _nextUnitButton;
    [SerializeField] private Button _equipmentButton;
    [SerializeField] private TMP_Text _equipmentText;
    [SerializeField] private Button[] _skillButtons = new Button[2];
    [SerializeField] private TMP_Text[] _skillTexts = new TMP_Text[2];
    [SerializeField] private Button[] _itemButtons = new Button[4];
    [SerializeField] private TMP_Text[] _itemTexts = new TMP_Text[4];
    [SerializeField] private BF_UnitConfigSO[] _playerUnits;

    private BF_InventoryService _inventory;
    private BF_UnitRuntimeService _runtime;
    private int _unitIndex;

    private BF_UnitConfigSO Unit => _playerUnits != null && _playerUnits.Length > 0 ? _playerUnits[_unitIndex] : null;
    private BF_UnitRuntimeData Data => Unit != null ? _runtime.Get(Unit.Id) : null;

    private void OnEnable()
    {
        _inventory = FindFirstObjectByType<BF_InventoryService>();
        _runtime = FindFirstObjectByType<BF_UnitRuntimeService>();
        InitUnits();

        _prevUnitButton.onClick.AddListener(PreviousUnit);
        _nextUnitButton.onClick.AddListener(NextUnit);
        _equipmentButton.onClick.AddListener(NextEquipment);

        for (int i = 0; i < _skillButtons.Length; i++)
        {
            int slot = i;
            _skillButtons[i].onClick.AddListener(() => NextSkill(slot));
        }

        for (int i = 0; i < _itemButtons.Length; i++)
        {
            int slot = i;
            _itemButtons[i].onClick.AddListener(() => NextItem(slot));
        }

        Refresh();
    }

    private void OnDisable()
    {
        _prevUnitButton.onClick.RemoveListener(PreviousUnit);
        _nextUnitButton.onClick.RemoveListener(NextUnit);
        _equipmentButton.onClick.RemoveListener(NextEquipment);

        foreach (Button button in _skillButtons)
        {
            button.onClick.RemoveAllListeners();
        }

        foreach (Button button in _itemButtons)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    private void InitUnits()
    {
        if (_runtime == null || _playerUnits == null)
        {
            return;
        }

        foreach (BF_UnitConfigSO unit in _playerUnits)
        {
            if (unit != null)
            {
                _runtime.GetOrCreate(
                    unit.Id,
                    unit.Skill01 != null ? unit.Skill01.Id : string.Empty,
                    unit.Skill02 != null ? unit.Skill02.Id : string.Empty);
            }
        }
    }

    private void PreviousUnit()
    {
        _unitIndex = (_unitIndex - 1 + _playerUnits.Length) % _playerUnits.Length;
        Refresh();
    }

    private void NextUnit()
    {
        _unitIndex = (_unitIndex + 1) % _playerUnits.Length;
        Refresh();
    }

    private void NextEquipment()
    {
        List<BF_ItemConfigSO> items = GetOwnedItems(BF_ItemType.Equipment);
        string next = GetNextId(items, Data.EquipmentItemId);
        _runtime.SetEquipment(Unit.Id, next);
        Refresh();
    }

    private void NextSkill(int slot)
    {
        List<BF_SkillConfigSO> skills = GetSkills();
        string current = slot == 0 ? Data.Skill01Id : Data.Skill02Id;
        string next = GetNextSkillId(skills, current);
        _runtime.SetSkill(Unit.Id, slot, next);
        Refresh();
    }

    private void NextItem(int slot)
    {
        List<BF_ItemConfigSO> items = GetOwnedItems(BF_ItemType.Consumable);
        string next = GetNextId(items, Data.BattleItemIds[slot]);
        _runtime.SetBattleItem(Unit.Id, slot, next);
        Refresh();
    }

    private void Refresh()
    {
        if (Unit == null || Data == null)
        {
            return;
        }

        BF_ItemConfigSO equipment = _inventory.GetItem(Data.EquipmentItemId);
        _unitNameText.text = Unit.DisplayName;
        _statsText.text = $"HP {Unit.MaxHP}  ATK {Unit.Attack}  DEF {Unit.Defense}  AP {Unit.MaxAP}";
        _equipmentText.text = equipment != null ? $"装备：{equipment.DisplayName}" : "装备：无";

        _skillTexts[0].text = $"技能 1：{GetSkillName(Data.Skill01Id)}";
        _skillTexts[1].text = $"技能 2：{GetSkillName(Data.Skill02Id)}";

        for (int i = 0; i < _itemTexts.Length; i++)
        {
            BF_ItemConfigSO item = _inventory.GetItem(Data.BattleItemIds[i]);
            _itemTexts[i].text = item != null ? $"{i + 1}. {item.DisplayName} ×{_inventory.GetCount(item.Id)}" : $"{i + 1}. 空";
        }
    }

    private List<BF_ItemConfigSO> GetOwnedItems(BF_ItemType type)
    {
        List<BF_ItemConfigSO> items = new();
        foreach (BF_InventoryEntry entry in _inventory.Items)
        {
            if (entry.Item.ItemType == type && entry.Quantity > 0)
            {
                items.Add(entry.Item);
            }
        }

        return items;
    }

    private List<BF_SkillConfigSO> GetSkills()
    {
        List<BF_SkillConfigSO> skills = new();
        AddSkill(skills, Unit.Skill01);
        AddSkill(skills, Unit.Skill02);
        foreach (BF_SkillConfigSO skill in Unit.AvailableSkills)
        {
            AddSkill(skills, skill);
        }

        return skills;
    }

    private void AddSkill(List<BF_SkillConfigSO> skills, BF_SkillConfigSO skill)
    {
        if (skill != null && !skills.Contains(skill))
        {
            skills.Add(skill);
        }
    }

    private string GetNextId(List<BF_ItemConfigSO> items, string current)
    {
        int index = items.FindIndex(item => item.Id == current);
        return index + 1 < items.Count ? items[index + 1].Id : string.Empty;
    }

    private string GetNextSkillId(List<BF_SkillConfigSO> skills, string current)
    {
        int index = skills.FindIndex(skill => skill.Id == current);
        return skills.Count > 0 ? skills[(index + 1) % skills.Count].Id : string.Empty;
    }

    private string GetSkillName(string skillId)
    {
        BF_SkillConfigSO skill = Unit.GetSkill(skillId);
        return skill != null ? skill.DisplayName : "无";
    }
}
