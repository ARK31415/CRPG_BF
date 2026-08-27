using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_UnitInfoPanel : MonoBehaviour
{
    [SerializeField]
    private Image _portrait;

    [SerializeField]
    private TMP_Text _nameText;

    [SerializeField]
    private TMP_Text _teamText;

    [SerializeField]
    private Image _hpFill;

    [SerializeField]
    private TMP_Text _hpText;

    [SerializeField]
    private Image _apFill;

    [SerializeField]
    private TMP_Text _apText;

    [SerializeField]
    private TMP_Text _attackText;

    [SerializeField]
    private TMP_Text _defenseText;

    private BF_BattleUnit _unit;

    public void Show(BF_BattleUnit unit)
    {
        _unit = unit;
        Refresh();
    }

    public void Hide()
    {
        _unit = null;
    }

    public void Refresh()
    {
        if (_unit == null || _unit.Config == null)
        {
            return;
        }

        BF_UnitConfigSO config = _unit.Config;
        _portrait.sprite = config.Portrait;
        _portrait.enabled = config.Portrait != null;
        _nameText.text = _unit.DisplayName;
        _teamText.text = _unit.Team == BF_UnitTeam.Player ? "PLAYER" : "ENEMY";
        _hpFill.fillAmount = _unit.MaxHP > 0 ? (float)_unit.CurrentHP / _unit.MaxHP : 0f;
        _hpText.text = $"HP  {_unit.CurrentHP} / {_unit.MaxHP}";
        _apFill.fillAmount = _unit.MaxAP > 0 ? (float)_unit.CurrentAP / _unit.MaxAP : 0f;
        _apText.text = $"AP  {_unit.CurrentAP} / {_unit.MaxAP}";
        _attackText.text = $"ATK  {config.Attack}";
        _defenseText.text = $"DEF  {config.Defense}";
    }
}
