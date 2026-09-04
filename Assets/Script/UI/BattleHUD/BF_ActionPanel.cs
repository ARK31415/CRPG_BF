using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_ActionPanel : MonoBehaviour
{
    [SerializeField]
    private Button _attackButton;

    [SerializeField]
    private Image _attackIcon;

    [SerializeField]
    private TMP_Text _attackCostText;

    [SerializeField]
    private Button _skillSlot01Button;

    [SerializeField]
    private Image _skillSlot01Icon;

    [SerializeField]
    private TMP_Text _skillSlot01NameText;

    [SerializeField]
    private TMP_Text _skillSlot01CostText;

    [SerializeField]
    private Button _skillSlot02Button;

    [SerializeField]
    private Image _skillSlot02Icon;

    [SerializeField]
    private TMP_Text _skillSlot02NameText;

    [SerializeField]
    private TMP_Text _skillSlot02CostText;

    [SerializeField]
    private Button _endUnitButton;

    [Header("Items")]
    [SerializeField] private Button[] _itemButtons = new Button[4];
    [SerializeField] private Image[] _itemIcons = new Image[4];
    [SerializeField] private TMP_Text[] _itemCountTexts = new TMP_Text[4];

    private BF_BattleUnit _unit;
    private bool _isPlayerPhase;
    private bool _isBattleActive = true;

    private void OnEnable()
    {
        _attackButton.onClick.AddListener(OnAttackClicked);
        _skillSlot01Button.onClick.AddListener(OnSkill01Clicked);
        _skillSlot02Button.onClick.AddListener(OnSkill02Clicked);
        _endUnitButton.onClick.AddListener(OnEndUnitClicked);
        for (int i = 0; i < _itemButtons.Length; i++)
        {
            if (_itemButtons[i] == null)
            {
                continue;
            }

            int slot = i;
            _itemButtons[i].onClick.AddListener(() => OnItemClicked(slot));
        }
        Refresh();
    }

    private void OnDisable()
    {
        _attackButton.onClick.RemoveListener(OnAttackClicked);
        _skillSlot01Button.onClick.RemoveListener(OnSkill01Clicked);
        _skillSlot02Button.onClick.RemoveListener(OnSkill02Clicked);
        _endUnitButton.onClick.RemoveListener(OnEndUnitClicked);
        for (int i = 0; i < _itemButtons.Length; i++)
        {
            if (_itemButtons[i] != null)
            {
                _itemButtons[i].onClick.RemoveAllListeners();
            }
        }
    }

    public void Show(BF_BattleUnit unit)
    {
        _unit = unit;
        Refresh();
    }

    public void Hide()
    {
        _unit = null;
        Refresh();
    }

    public void SetPlayerPhase(bool isPlayerPhase)
    {
        _isPlayerPhase = isPlayerPhase;
        Refresh();
    }

    public void SetBattleActive(bool isActive)
    {
        _isBattleActive = isActive;
        Refresh();
    }

    public void Refresh()
    {
        BF_SkillConfigSO attack = _unit != null && _unit.Config != null
            ? _unit.Config.BasicAttack
            : null;

        bool canAct = _isBattleActive
            && _isPlayerPhase
            && _unit != null
            && _unit.Team == BF_UnitTeam.Player
            && _unit.IsAlive
            && !_unit.IsTurnEnded;

        _attackButton.interactable = canAct && attack != null && _unit.CanPay(attack.APCost);
        _endUnitButton.interactable = canAct;
        BF_SkillConfigSO skill01 = _unit != null ? _unit.Skill01 : null;
        BF_SkillConfigSO skill02 = _unit != null ? _unit.Skill02 : null;

        _skillSlot01Button.interactable = canAct && skill01 != null && _unit.CanPay(skill01.APCost);
        _skillSlot02Button.interactable = canAct && skill02 != null && _unit.CanPay(skill02.APCost);

        _attackIcon.sprite = attack != null ? attack.Icon : null;
        _attackIcon.enabled = attack != null && attack.Icon != null;
        _attackCostText.text = attack != null ? $"{attack.APCost} AP" : string.Empty;
        RefreshSkill(_skillSlot01Icon, _skillSlot01NameText, _skillSlot01CostText, skill01);
        RefreshSkill(_skillSlot02Icon, _skillSlot02NameText, _skillSlot02CostText, skill02);
        RefreshItems(canAct);
    }

    private void OnAttackClicked()
    {
        GameEventBus.Instance.Publish(new BF_SkillRequestEvent(_unit.Config.BasicAttack));
    }

    private void OnSkill01Clicked()
    {
        GameEventBus.Instance.Publish(new BF_SkillRequestEvent(_unit.Skill01));
    }

    private void OnSkill02Clicked()
    {
        GameEventBus.Instance.Publish(new BF_SkillRequestEvent(_unit.Skill02));
    }

    private void OnItemClicked(int slot)
    {
        GameEventBus.Instance.Publish(new BF_ItemRequestEvent(slot));
    }

    private void RefreshItems(bool canAct)
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        BF_UnitRuntimeData data = _unit != null && runtime != null ? runtime.Get(_unit.UnitId) : null;

        for (int i = 0; i < _itemButtons.Length; i++)
        {
            if (_itemButtons[i] == null || _itemIcons[i] == null || _itemCountTexts[i] == null)
            {
                continue;
            }

            string itemId = data != null && i < data.BattleItemIds.Length ? data.BattleItemIds[i] : string.Empty;
            BF_ItemConfigSO item = inventory != null ? inventory.GetItem(itemId) : null;
            int count = item != null ? inventory.GetCount(item.Id) : 0;
            bool usable = item != null
                && item.ItemType == BF_ItemType.Consumable
                && count > 0
                && _unit.CurrentHP < _unit.MaxHP
                && _unit.CanPay(item.APCost);

            _itemButtons[i].interactable = canAct && usable;
            _itemIcons[i].sprite = item != null ? item.Icon : null;
            _itemIcons[i].enabled = item != null && item.Icon != null;
            _itemCountTexts[i].text = item != null ? $"×{count}\n{item.APCost} AP" : "空";
        }
    }

    private void OnEndUnitClicked()
    {
        GameEventBus.Instance.Publish(new BF_EndUnitRequestEvent());
    }

    private void RefreshSkill(
        Image icon,
        TMP_Text nameText,
        TMP_Text costText,
        BF_SkillConfigSO skill)
    {
        icon.sprite = skill != null ? skill.Icon : null;
        icon.enabled = skill != null && skill.Icon != null;
        nameText.text = skill != null ? skill.DisplayName : string.Empty;
        costText.text = skill != null ? $"{skill.APCost} AP" : string.Empty;
    }
}
