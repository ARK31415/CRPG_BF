using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_BattlePrepareController : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject _warehousePage;
    [SerializeField] private GameObject _skillPage;
    [SerializeField] private GameObject _shopPage;
    [SerializeField] private Button _warehouseButton;
    [SerializeField] private Button _skillButton;
    [SerializeField] private Button _shopButton;

    [Header("Unit")]
    [SerializeField] private TMP_Text _unitNameText;
    [SerializeField] private TMP_Text _rosterCountText;
    [SerializeField] private TMP_Text _rosterMessageText;
    [SerializeField] private Button _prevUnitButton;
    [SerializeField] private Button _nextUnitButton;
    [SerializeField] private BF_UnitLoadoutPanel _unitLoadoutPanel;
    [SerializeField] private BF_SkillLoadoutPanel _skillLoadoutPanel;
    [SerializeField] private BF_WarehousePanel _warehousePanel;
    [SerializeField] private BF_ItemContextMenu _itemContextMenu;

    [Header("Flow")]
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _startButton;

    private int _unitIndex;
    private System.IDisposable _unitSubscription;

    private BF_UnitRuntimeData CurrentData
    {
        get
        {
            BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
            return runtime != null
                && _unitIndex >= 0
                && _unitIndex < runtime.Units.Count
                ? runtime.Units[_unitIndex]
                : null;
        }
    }

    private BF_UnitConfigSO CurrentConfig => CurrentData != null && BF_BattleService.Instance != null
        ? BF_BattleService.Instance.GetUnitConfig(CurrentData.ConfigId)
        : null;

    private void OnEnable()
    {
        _warehouseButton.onClick.AddListener(ShowWarehouse);
        _skillButton.onClick.AddListener(ShowSkill);
        _shopButton.onClick.AddListener(ShowShop);
        _prevUnitButton.onClick.AddListener(PreviousUnit);
        _nextUnitButton.onClick.AddListener(NextUnit);
        _backButton.onClick.AddListener(Back);
        _startButton.onClick.AddListener(StartBattle);
        _warehousePanel.SetRightClick(OpenItemMenu);

        _unitSubscription = GameEventBus.Instance.Subscribe<BF_UnitRuntimeChangedEvent>(OnUnitChanged);
        SelectUnit(0);
        ShowWarehouse();
    }

    private void OnDisable()
    {
        _warehouseButton.onClick.RemoveListener(ShowWarehouse);
        _skillButton.onClick.RemoveListener(ShowSkill);
        _shopButton.onClick.RemoveListener(ShowShop);
        _prevUnitButton.onClick.RemoveListener(PreviousUnit);
        _nextUnitButton.onClick.RemoveListener(NextUnit);
        _backButton.onClick.RemoveListener(Back);
        _startButton.onClick.RemoveListener(StartBattle);
        _warehousePanel.SetRightClick(null);
        _unitSubscription?.Dispose();
        _unitSubscription = null;
    }

    private void SelectUnit(int index)
    {
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        int count = runtime != null ? runtime.Units.Count : 0;
        if (count == 0)
        {
            _unitIndex = 0;
            _unitNameText.text = string.Empty;
            _unitLoadoutPanel.ShowUnit(null, null);
            _skillLoadoutPanel.ShowUnit(null, null);
            RefreshRoster();
            return;
        }

        _unitIndex = (index + count) % count;
        BF_UnitRuntimeData data = CurrentData;
        BF_UnitConfigSO config = CurrentConfig;
        _unitNameText.text = data != null && config != null
            ? $"{config.DisplayName}  ({data.UnitId})"
            : string.Empty;
        _unitLoadoutPanel.ShowUnit(data, config);
        _skillLoadoutPanel.ShowUnit(data, config);
        _itemContextMenu.Hide();
        RefreshRoster();
    }

    private void PreviousUnit()
    {
        SelectUnit(_unitIndex - 1);
    }

    private void NextUnit()
    {
        SelectUnit(_unitIndex + 1);
    }

    private void ShowWarehouse()
    {
        _warehousePage.SetActive(true);
        _skillPage.SetActive(false);
        _shopPage.SetActive(false);
        _itemContextMenu.Hide();
    }

    private void ShowSkill()
    {
        _warehousePage.SetActive(false);
        _skillPage.SetActive(true);
        _shopPage.SetActive(false);
        _itemContextMenu.Hide();
    }

    private void ShowShop()
    {
        _warehousePage.SetActive(false);
        _skillPage.SetActive(false);
        _shopPage.SetActive(true);
        _itemContextMenu.Hide();
    }

    private void OpenItemMenu(BF_ItemConfigSO item, Vector2 screenPos)
    {
        _itemContextMenu.Show(
            item,
            CurrentData,
            CurrentConfig,
            _unitLoadoutPanel.SelectedBattleItemSlot,
            screenPos);
    }

    private void Back()
    {
        _backButton.interactable = false;
        SaveCurrentSlot();
        BF_SceneLoadManager.Instance?.LoadLevelSelect();
    }

    private void StartBattle()
    {
        _startButton.interactable = false;
        SaveCurrentSlot();
        BF_BattleService.Instance?.StartPreparedLevel();
    }

    private void SaveCurrentSlot()
    {
        BF_SaveService saveService = BF_SaveService.Instance;
        if (saveService != null && saveService.CurrentSlot > 0)
        {
            saveService.Save();
        }
    }

    private void OnUnitChanged(BF_UnitRuntimeChangedEvent gameEvent)
    {
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        string selectedId = CurrentData != null ? CurrentData.UnitId : string.Empty;
        if (runtime == null || runtime.Units.Count == 0)
        {
            SelectUnit(0);
            return;
        }

        for (int i = 0; i < runtime.Units.Count; i++)
        {
            if (runtime.Units[i].UnitId == selectedId)
            {
                _unitIndex = i;
                break;
            }
        }

        SelectUnit(_unitIndex);
    }

    private void RefreshRoster()
    {
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        BF_BattleService battleService = BF_BattleService.Instance;
        int count = runtime != null ? runtime.DeployedCount : 0;
        int limit = battleService != null && battleService.CurrentLevelConfig != null
            ? battleService.CurrentLevelConfig.PlayerSpawns.Count
            : 0;

        if (_rosterCountText != null)
        {
            _rosterCountText.text = $"出战人数：{count} / {limit}";
        }

        if (_rosterMessageText != null)
        {
            _rosterMessageText.text = count == 0
                ? "至少选择一名角色出战"
                : count > limit
                    ? "当前出战人数超过本关上限，请撤下角色"
                    : string.Empty;
        }

        if (_startButton != null)
        {
            _startButton.interactable = count > 0 && count <= limit;
        }
    }
}
