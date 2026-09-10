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

    [Header("Terrain Info")]
    [SerializeField]
    private GameObject _terrainInfoPanelRoot;

    [SerializeField]
    private TMP_Text _terrainNameText;

    [SerializeField]
    private TMP_Text _terrainCostText;

    [SerializeField]
    private TMP_Text _terrainStatusText;

    private BF_BattleUnit _unit;

    private void Awake()
    {
        GameEventBus.Instance?.Subscribe<BF_BattlePhaseChangeEvent>(OnPhaseChanged).UnRegisterWhenGameObjectDestroyed(gameObject);
        GameEventBus.Instance?.Subscribe<BF_UnitSelectedEvent>(OnUnitSelected).UnRegisterWhenGameObjectDestroyed(gameObject);
        GameEventBus.Instance?.Subscribe<BF_UnitStatsChangedEvent>(OnUnitStatsChanged).UnRegisterWhenGameObjectDestroyed(gameObject);
        GameEventBus.Instance?.Subscribe<BF_PathCostChangedEvent>(OnPathCostChanged).UnRegisterWhenGameObjectDestroyed(gameObject);
        GameEventBus.Instance?.Subscribe<BF_BoardCellHoveredEvent>(OnCellHovered).UnRegisterWhenGameObjectDestroyed(gameObject);
        GameEventBus.Instance?.Subscribe<BF_InventoryChangedEvent>(_ => _actionPanel.Refresh()).UnRegisterWhenGameObjectDestroyed(gameObject);
        ResetView();
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

    private void OnCellHovered(BF_BoardCellHoveredEvent gameEvent)
    {
        if (_terrainInfoPanelRoot == null)
        {
            return;
        }

        if (!gameEvent.HasCell)
        {
            _terrainInfoPanelRoot.SetActive(false);
            return;
        }

        _terrainInfoPanelRoot.SetActive(true);

        if (_terrainNameText != null)
        {
            _terrainNameText.text = $"{TerrainName(gameEvent.Terrain)}  ({gameEvent.Position.x},{gameEvent.Position.y})";
        }

        if (_terrainCostText != null)
        {
            _terrainCostText.text = gameEvent.Passable ? $"进入 {gameEvent.MoveCost} AP" : "进入 — AP";
        }

        if (_terrainStatusText != null)
        {
            _terrainStatusText.text = gameEvent.IsOccupied ? "已占用" : gameEvent.Passable ? "可通行" : "阻挡";
        }
    }

    private static string TerrainName(TerrainType terrainType)
    {
        return terrainType switch
        {
            TerrainType.Normal => "普通地面",
            TerrainType.Difficult => "复杂地形",
            TerrainType.Swamp => "沼泽",
            TerrainType.Blocked => "阻挡",
            _ => "未知地形"
        };
    }

    public void ResetView()
    {
        _phaseText.text = string.Empty;
        _roundText.text = string.Empty;
        _pathCostText.text = string.Empty;
        if (_terrainInfoPanelRoot != null)
        {
            _terrainInfoPanelRoot.SetActive(false);
        }

        _endTurnButton.interactable = false;
        _actionPanel.SetBattleActive(true);
        _actionPanel.SetPlayerPhase(false);
        ShowUnit(null);
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
