using UnityEngine;
using UnityEngine.UI;

public class BF_PausePanel : MonoBehaviour
{
    [SerializeField]
    private Button _resumeButton;

    [SerializeField]
    private Button _settingsButton;

    [SerializeField]
    private Button _exitBattleButton;

    [SerializeField]
    private GameObject _exitConfirmPanel;

    [SerializeField]
    private Button _confirmExitButton;

    [SerializeField]
    private Button _cancelExitButton;

    [SerializeField]
    private BF_UIManager _uiManager;

    private void OnEnable()
    {
        _resumeButton?.onClick.AddListener(Resume);
        _settingsButton?.onClick.AddListener(OpenSettings);
        _exitBattleButton?.onClick.AddListener(ShowExitConfirm);
        _confirmExitButton?.onClick.AddListener(ConfirmExit);
        _cancelExitButton?.onClick.AddListener(HideExitConfirm);
        HideExitConfirm();
    }

    private void OnDisable()
    {
        _resumeButton?.onClick.RemoveListener(Resume);
        _settingsButton?.onClick.RemoveListener(OpenSettings);
        _exitBattleButton?.onClick.RemoveListener(ShowExitConfirm);
        _confirmExitButton?.onClick.RemoveListener(ConfirmExit);
        _cancelExitButton?.onClick.RemoveListener(HideExitConfirm);
    }

    private void Resume()
    {
        BF_GameModeManager.Instance?.ResumeBattle();
    }

    private void OpenSettings()
    {
        _uiManager ??= FindFirstObjectByType<BF_UIManager>();
        _uiManager?.OpenSettingsPanel();
    }

    private void ShowExitConfirm()
    {
        _exitConfirmPanel?.SetActive(true);
    }

    private void HideExitConfirm()
    {
        if (_exitConfirmPanel != null)
        {
            _exitConfirmPanel.SetActive(false);
        }
    }

    private void ConfirmExit()
    {
        GameEventBus.Instance.Publish(new BF_AbandonBattleRequestEvent());
    }
}
