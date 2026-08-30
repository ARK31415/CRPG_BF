using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BF_MenuController : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _exitButton;

    [Header("Save Slots")]
    [SerializeField] private GameObject _slotPanel;
    [SerializeField] private BF_SaveSlotView[] _slotViews;
    [SerializeField] private Button _backButton;

    [Header("Confirm")]
    [SerializeField] private GameObject _confirmPanel;
    [SerializeField] private TMP_Text _confirmText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    private BF_SceneLoadManager _sceneLoadManager;
    private BF_SaveService _saveService;
    private BF_BattleService _battleService;
    private SlotAction _slotAction;
    private ConfirmAction _confirmAction;
    private int _selectedSlot;

    private void OnEnable()
    {
        _sceneLoadManager = FindFirstObjectByType<BF_SceneLoadManager>();
        _saveService = FindFirstObjectByType<BF_SaveService>();
        _battleService = FindFirstObjectByType<BF_BattleService>();
        _newGameButton.onClick.AddListener(ShowNewGameSlots);
        _continueButton.onClick.AddListener(ShowContinueSlots);
        _exitButton.onClick.AddListener(OnExitClicked);
        _backButton.onClick.AddListener(ShowMain);
        _confirmButton.onClick.AddListener(Confirm);
        _cancelButton.onClick.AddListener(CancelConfirm);
        ShowMain();
    }

    private void OnDisable()
    {
        _newGameButton.onClick.RemoveListener(ShowNewGameSlots);
        _continueButton.onClick.RemoveListener(ShowContinueSlots);
        _exitButton.onClick.RemoveListener(OnExitClicked);
        _backButton.onClick.RemoveListener(ShowMain);
        _confirmButton.onClick.RemoveListener(Confirm);
        _cancelButton.onClick.RemoveListener(CancelConfirm);
    }

    private void ShowMain()
    {
        _slotAction = SlotAction.None;
        _confirmAction = ConfirmAction.None;
        _slotPanel.SetActive(false);
        _confirmPanel.SetActive(false);
        SetMainVisible(true);

        bool hasSave = false;
        if (_saveService != null)
        {
            for (int slot = 1; slot <= 3; slot++)
            {
                hasSave |= _saveService.HasSave(slot);
            }
        }

        _continueButton.interactable = hasSave;
        SelectButton(_newGameButton);
    }

    private void ShowNewGameSlots()
    {
        ShowSlots(SlotAction.NewGame);
    }

    private void ShowContinueSlots()
    {
        ShowSlots(SlotAction.Continue);
    }

    private void ShowSlots(SlotAction action)
    {
        _slotAction = action;
        SetMainVisible(false);
        _slotPanel.SetActive(true);
        _confirmPanel.SetActive(false);
        RefreshSlots();
        SelectButton(_backButton);
    }

    private void RefreshSlots()
    {
        if (_saveService == null)
        {
            return;
        }

        for (int i = 0; i < _slotViews.Length; i++)
        {
            BF_SaveSlotInfo info = _saveService.GetSlotInfo(i + 1);
            bool canSelect = _slotAction == SlotAction.NewGame || info.IsValid;
            _slotViews[i].Show(info, canSelect, SelectSlot, RequestDelete);
        }
    }

    private void SelectSlot(int slot)
    {
        BF_SaveSlotInfo info = _saveService.GetSlotInfo(slot);
        if (_slotAction == SlotAction.NewGame && info.HasData)
        {
            ShowConfirm(slot, ConfirmAction.Overwrite, $"覆盖存档 {slot}？\n原有进度将被替换。");
            return;
        }

        if (_slotAction == SlotAction.NewGame)
        {
            StartNewGame(slot);
        }
        else if (_slotAction == SlotAction.Continue && _saveService.Load(slot))
        {
            EnterLevelSelect();
        }
    }

    private void RequestDelete(int slot)
    {
        ShowConfirm(slot, ConfirmAction.Delete, $"删除存档 {slot}？\n此操作无法撤销。");
    }

    private void ShowConfirm(int slot, ConfirmAction action, string message)
    {
        _selectedSlot = slot;
        _confirmAction = action;
        _confirmText.text = message;
        _confirmPanel.SetActive(true);
        SelectButton(_cancelButton);
    }

    private void Confirm()
    {
        _confirmPanel.SetActive(false);

        if (_confirmAction == ConfirmAction.Overwrite)
        {
            StartNewGame(_selectedSlot);
        }
        else if (_confirmAction == ConfirmAction.Delete)
        {
            _saveService.Delete(_selectedSlot);
            RefreshSlots();
            SelectButton(_backButton);
        }

        _confirmAction = ConfirmAction.None;
    }

    private void CancelConfirm()
    {
        _confirmAction = ConfirmAction.None;
        _confirmPanel.SetActive(false);
        SelectButton(_backButton);
    }

    private void StartNewGame(int slot)
    {
        if (_saveService != null
            && _battleService != null
            && _saveService.StartNewGame(slot, _battleService.CreateInitialUnits))
        {
            EnterLevelSelect();
        }
    }

    private void EnterLevelSelect()
    {
        _newGameButton.interactable = false;
        _continueButton.interactable = false;
        _sceneLoadManager.LoadLevelSelect();
    }

    private void SetMainVisible(bool visible)
    {
        _newGameButton.gameObject.SetActive(visible);
        _continueButton.gameObject.SetActive(visible);
        _exitButton.gameObject.SetActive(visible);
    }

    private void SelectButton(Button button)
    {
        if (EventSystem.current != null && button != null && button.isActiveAndEnabled)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    private void OnExitClicked()
    {
#if UNITY_EDITOR
        Debug.Log("[BF] Exit requested. Application.Quit runs in Player build.");
#else
        Application.Quit();
#endif
    }

    private enum SlotAction
    {
        None,
        NewGame,
        Continue
    }

    private enum ConfirmAction
    {
        None,
        Overwrite,
        Delete
    }
}
