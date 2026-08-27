using UnityEngine;
using UnityEngine.UI;

public class BF_WorldHealthBar : MonoBehaviour
{
    [SerializeField]
    private Image _fill;

    [SerializeField]
    private Color _playerColor = new Color(0.2f, 0.8f, 0.3f);

    [SerializeField]
    private Color _enemyColor = new Color(0.9f, 0.2f, 0.2f);

    private BF_BattleUnit _unit;

    private void Awake()
    {
        GameEventBus.Instance?.Subscribe<BF_UnitStatsChangedEvent>(OnUnitStatsChanged).UnRegisterWhenGameObjectDestroyed(gameObject);
        Bind(GetComponentInParent<BF_BattleUnit>());
    }

    public void Bind(BF_BattleUnit unit)
    {
        _unit = unit;
        Refresh();
    }

    private void OnUnitStatsChanged(BF_UnitStatsChangedEvent gameEvent)
    {
        if (gameEvent.Unit == _unit)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (_unit == null || _unit.MaxHP <= 0)
        {
            return;
        }

        _fill.color = _unit.Team == BF_UnitTeam.Player ? _playerColor : _enemyColor;
        _fill.fillAmount = (float)_unit.CurrentHP / _unit.MaxHP;

        if (!_unit.IsAlive)
        {
            gameObject.SetActive(false);
        }
    }
}
