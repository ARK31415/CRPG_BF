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

    private BF_BattleService _battleService;
    private BF_SceneLoadManager _sceneLoadManager;
    private BF_UnitRuntimeService _runtime;
    private BF_SaveService _saveService;
    private int _unitIndex;
    private System.IDisposable _unitSubscription;

    private BF_UnitRuntimeData CurrentData => _runtime != null
        && _runtime.Units.Count > 0
        && _unitIndex >= 0
        && _unitIndex < _runtime.Units.Count
        ? _runtime.Units[_unitIndex]
        : null;

    private BF_UnitConfigSO CurrentConfig => CurrentData != null && _battleService != null
        ? _battleService.GetUnitConfig(CurrentData.ConfigId)
        : null;

    private void OnEnable()
    {
        _battleService = FindFirstObjectByType<BF_BattleService>();
        _sceneLoadManager = FindFirstObjectByType<BF_SceneLoadManager>();
        _runtime = FindFirstObjectByType<BF_UnitRuntimeService>();
        _saveService = FindFirstObjectByType<BF_SaveService>();

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

    private void Start()
    {
        // BattlePrepare 通过 Addressables 异步加载时，OnEnable 可能早于 Persistent 服务初始化。
        // 在 Start 再获取一次服务，确保默认角色和出战人数显示正常。
        _battleService ??= FindFirstObjectByType<BF_BattleService>();
        _sceneLoadManager ??= FindFirstObjectByType<BF_SceneLoadManager>();
        _runtime ??= FindFirstObjectByType<BF_UnitRuntimeService>();
        _saveService ??= FindFirstObjectByType<BF_SaveService>();

        if (_unitSubscription == null && GameEventBus.Instance != null)
        {
            _unitSubscription = GameEventBus.Instance.Subscribe<BF_UnitRuntimeChangedEvent>(OnUnitChanged);
        }

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
        int count = _runtime != null ? _runtime.Units.Count : 0;
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
        _sceneLoadManager.LoadLevelSelect();
    }

    private void StartBattle()
    {
        _startButton.interactable = false;
        SaveCurrentSlot();
        _battleService.StartPreparedLevel();
    }

    private void SaveCurrentSlot()
    {
        if (_saveService != null && _saveService.CurrentSlot > 0)
        {
            _saveService.Save();
        }
    }

    private void OnUnitChanged(BF_UnitRuntimeChangedEvent gameEvent)
    {
        string selectedId = CurrentData != null ? CurrentData.UnitId : string.Empty;
        if (_runtime == null || _runtime.Units.Count == 0)
        {
            SelectUnit(0);
            return;
        }

        for (int i = 0; i < _runtime.Units.Count; i++)
        {
            if (_runtime.Units[i].UnitId == selectedId)
            {
                _unitIndex = i;
                break;
            }
        }

        SelectUnit(_unitIndex);
    }

    private void RefreshRoster()
    {
        int count = _runtime != null ? _runtime.DeployedCount : 0;
        int limit = _battleService != null && _battleService.CurrentLevelConfig != null
            ? _battleService.CurrentLevelConfig.PlayerSpawns.Count
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
