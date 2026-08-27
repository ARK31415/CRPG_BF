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
    private Button _skillSlot02Button;

    [SerializeField]
    private Button _endUnitButton;

    private BF_BattleUnit _unit;
    private bool _isPlayerPhase;
    private bool _isBattleActive = true;

    private void OnEnable()
    {
        _attackButton.onClick.AddListener(OnAttackClicked);
        _endUnitButton.onClick.AddListener(OnEndUnitClicked);
        Refresh();
    }

    private void OnDisable()
    {
        _attackButton.onClick.RemoveListener(OnAttackClicked);
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
        _skillSlot01Button.interactable = false;
        _skillSlot02Button.interactable = false;

        _attackIcon.sprite = attack != null ? attack.Icon : null;
        _attackIcon.enabled = attack != null && attack.Icon != null;
        _attackCostText.text = attack != null ? $"{attack.APCost} AP" : string.Empty;
    }

    private void OnAttackClicked()
    {
        GameEventBus.Instance.Publish(new BF_AttackRequestEvent());
    }

    private void OnEndUnitClicked()
    {
        GameEventBus.Instance.Publish(new BF_EndUnitRequestEvent());
    }
}
