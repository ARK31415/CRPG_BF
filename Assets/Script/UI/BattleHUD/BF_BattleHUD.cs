using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BF_BattleHUD : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _phaseText;
    [SerializeField]
    private TMP_Text _roundText;
    [SerializeField]
    private Button _endTurnButton;

    [Header("Unit")]
    [SerializeField]
    private TMP_Text _unitNameText;

    [SerializeField]
    private TMP_Text _hpText;

    [SerializeField]
    private TMP_Text _apText;

    [SerializeField]
    private TMP_Text _pathCostText;

    [SerializeField]
    private Button _attackButton;

    [SerializeField]
    private Button _endUnitButton;

    [Header("Result")]
    [SerializeField]
    private GameObject _resultPanel;

    [SerializeField]
    private TMP_Text _resultText;

    private BF_BattleUnit _unit;

    private void Awake()
    {
        GameEventBus.Instance?.Subscribe<BF_BattlePhaseChangeEvent>(OnPhaseChanged).UnRegisterWhenGameObjectDestroyed(gameObject);
        GameEventBus.Instance?.Subscribe<BF_UnitSelectedEvent>(OnUnitSelected).UnRegisterWhenGameObjectDestroyed(gameObject);
        GameEventBus.Instance?.Subscribe<BF_UnitStatsChangedEvent>(OnUnitStatsChanged).UnRegisterWhenGameObjectDestroyed(gameObject);
        GameEventBus.Instance?.Subscribe<BF_PathCostChangedEvent>(OnPathCostChanged).UnRegisterWhenGameObjectDestroyed(gameObject);
        GameEventBus.Instance?.Subscribe<BF_BattleResultEvent>(OnBattleResult).UnRegisterWhenGameObjectDestroyed(gameObject);
        _resultPanel.SetActive(false);
        RefreshUnit();
    }

    private void OnEnable()
    {
        _endTurnButton.onClick.AddListener(OnEndTurnClicked);
        _attackButton.onClick.AddListener(OnAttackClicked);
        _endUnitButton.onClick.AddListener(OnEndUnitClicked);
    }
    private void OnDisable()
    {
        _endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
        _attackButton.onClick.RemoveListener(OnAttackClicked);
        _endUnitButton.onClick.RemoveListener(OnEndUnitClicked);
    }

    private void OnPhaseChanged(BF_BattlePhaseChangeEvent gameEvent)
    {
        _roundText.text = gameEvent.Round > 0 ? $"第{gameEvent.Round}回合" : string.Empty;

        _phaseText.text = gameEvent.Phase switch
        {
            BF_BattlePhase.SetupPhase => "战斗准备",
            BF_BattlePhase.PlayerPhase => "玩家回合",
            BF_BattlePhase.EnemyPhase => "敌方回合",
            BF_BattlePhase.BattleEnd => "战斗结束",
            _ => string.Empty
        };

        bool isPlayerPhase = gameEvent.Phase == BF_BattlePhase.PlayerPhase;
        _endTurnButton.interactable = isPlayerPhase;
        _attackButton.interactable = isPlayerPhase && CanAttack();
        _endUnitButton.interactable = isPlayerPhase && _unit != null;
        Debug.Log($"[BF] HUD Phase: {gameEvent.Phase}");
    }

    private void OnUnitSelected(BF_UnitSelectedEvent gameEvent)
    {
        _unit = gameEvent.Unit;
        RefreshUnit();
    }

    private void OnUnitStatsChanged(BF_UnitStatsChangedEvent gameEvent)
    {
        if (gameEvent.Unit == _unit)
        {
            RefreshUnit();
        }
    }

    private void OnPathCostChanged(BF_PathCostChangedEvent gameEvent)
    {
        _pathCostText.text = gameEvent.Cost > 0
            ? $"Move -{gameEvent.Cost} AP  |  Left {gameEvent.RemainingAP}"
            : string.Empty;
    }

    private void OnBattleResult(BF_BattleResultEvent gameEvent)
    {
        _resultPanel.SetActive(true);
        _resultText.text = gameEvent.Result == BF_BattleResult.Victory ? "VICTORY" : "DEFEAT";
        _attackButton.interactable = false;
        _endUnitButton.interactable = false;
        _endTurnButton.interactable = false;
    }

    private void OnEndTurnClicked()
    {
        GameEventBus.Instance.Publish(new BF_EndPlayerPhaseRequestEvent());
    }

    private void OnAttackClicked()
    {
        GameEventBus.Instance.Publish(new BF_AttackRequestEvent());
    }

    private void OnEndUnitClicked()
    {
        GameEventBus.Instance.Publish(new BF_EndUnitRequestEvent());
    }

    private void RefreshUnit()
    {
        bool hasUnit = _unit != null;
        _unitNameText.text = hasUnit ? _unit.DisplayName : string.Empty;
        _hpText.text = hasUnit ? $"HP {_unit.CurrentHP} / {_unit.MaxHP}" : string.Empty;
        _apText.text = hasUnit ? $"AP {_unit.CurrentAP} / {_unit.MaxAP}" : string.Empty;
        _pathCostText.text = string.Empty;
        _attackButton.interactable = hasUnit && CanAttack();
        _endUnitButton.interactable = hasUnit;
    }

    private bool CanAttack()
    {
        return _unit != null
            && _unit.Config.BasicAttack != null
            && _unit.CanPay(_unit.Config.BasicAttack.APCost);
    }
}
