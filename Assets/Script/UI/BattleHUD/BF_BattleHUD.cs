using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_BattleHUD : MonoBehaviour
{
    [Header("Battle")]
    [SerializeField]
    private TMP_Text _phaseText;

    [SerializeField]
    private TMP_Text _roundText;

    [SerializeField]
    private Button _endTurnButton;

    [Header("Unit")]
    [SerializeField]
    private GameObject _unitPanelRoot;

    [SerializeField]
    private BF_UnitInfoPanel _unitInfoPanel;

    [SerializeField]
    private BF_ActionPanel _actionPanel;

    [SerializeField]
    private TMP_Text _pathCostText;

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
        ShowUnit(null);
    }

    private void OnEnable()
    {
        _endTurnButton.onClick.AddListener(OnEndTurnClicked);
    }

    private void OnDisable()
    {
        _endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
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
        _actionPanel.SetPlayerPhase(isPlayerPhase);
        Debug.Log($"[BF] HUD Phase: {gameEvent.Phase}");
    }

    private void OnUnitSelected(BF_UnitSelectedEvent gameEvent)
    {
        ShowUnit(gameEvent.Unit);
    }

    private void OnUnitStatsChanged(BF_UnitStatsChangedEvent gameEvent)
    {
        if (gameEvent.Unit != _unit)
        {
            return;
        }

        _unitInfoPanel.Refresh();
        _actionPanel.Refresh();
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
        _actionPanel.SetBattleActive(false);
        _endTurnButton.interactable = false;
    }

    private void OnEndTurnClicked()
    {
        GameEventBus.Instance.Publish(new BF_EndPlayerPhaseRequestEvent());
    }

    private void ShowUnit(BF_BattleUnit unit)
    {
        _unit = unit;
        _pathCostText.text = string.Empty;

        if (_unit == null)
        {
            _unitInfoPanel.Hide();
            _actionPanel.Hide();
            _unitPanelRoot.SetActive(false);
            return;
        }

        _unitPanelRoot.SetActive(true);
        _unitInfoPanel.Show(_unit);
        _actionPanel.Show(_unit);
    }
}
