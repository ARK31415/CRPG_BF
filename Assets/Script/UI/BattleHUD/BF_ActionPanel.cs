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

    private BF_BattleUnit _unit;
    private bool _isPlayerPhase;
    private bool _isBattleActive = true;

    private void OnEnable()
    {
        _attackButton.onClick.AddListener(OnAttackClicked);
        _skillSlot01Button.onClick.AddListener(OnSkill01Clicked);
        _skillSlot02Button.onClick.AddListener(OnSkill02Clicked);
        _endUnitButton.onClick.AddListener(OnEndUnitClicked);
        Refresh();
    }

    private void OnDisable()
    {
        _attackButton.onClick.RemoveListener(OnAttackClicked);
        _skillSlot01Button.onClick.RemoveListener(OnSkill01Clicked);
        _skillSlot02Button.onClick.RemoveListener(OnSkill02Clicked);
        _endUnitButton.onClick.RemoveListener(OnEndUnitClicked);
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
        BF_SkillConfigSO skill01 = _unit != null ? _unit.Config.Skill01 : null;
        BF_SkillConfigSO skill02 = _unit != null ? _unit.Config.Skill02 : null;

        _skillSlot01Button.interactable = canAct && skill01 != null && _unit.CanPay(skill01.APCost);
        _skillSlot02Button.interactable = canAct && skill02 != null && _unit.CanPay(skill02.APCost);

        _attackIcon.sprite = attack != null ? attack.Icon : null;
        _attackIcon.enabled = attack != null && attack.Icon != null;
        _attackCostText.text = attack != null ? $"{attack.APCost} AP" : string.Empty;
        RefreshSkill(_skillSlot01Icon, _skillSlot01NameText, _skillSlot01CostText, skill01);
        RefreshSkill(_skillSlot02Icon, _skillSlot02NameText, _skillSlot02CostText, skill02);
    }

    private void OnAttackClicked()
    {
        GameEventBus.Instance.Publish(new BF_SkillRequestEvent(_unit.Config.BasicAttack));
    }

    private void OnSkill01Clicked()
    {
        GameEventBus.Instance.Publish(new BF_SkillRequestEvent(_unit.Config.Skill01));
    }

    private void OnSkill02Clicked()
    {
        GameEventBus.Instance.Publish(new BF_SkillRequestEvent(_unit.Config.Skill02));
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
