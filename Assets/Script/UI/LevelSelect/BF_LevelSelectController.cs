using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BF_LevelSelectController : MonoBehaviour
{
    [SerializeField]
    private Button[] _levelButtons;

    [SerializeField]
    private GameObject[] _selectedFrames;

    [SerializeField]
    private TMP_Text[] _stateTexts;

    [SerializeField]
    private Sprite _lockedSprite;

    [SerializeField]
    private Sprite _unlockedSprite;

    [SerializeField]
    private Sprite _completedSprite;

    [SerializeField]
    private Button _backButton;

    [SerializeField]
    private Button _enterButton;

    [SerializeField]
    private TMP_Text _levelInfoText;

    private BF_SceneLoadManager _sceneLoadManager;
    private BF_BattleService _battleService;
    private BF_LevelProgress _levelProgress;
    private int _selectedLevel = 1;

    private void OnEnable()
    {
        _sceneLoadManager = FindFirstObjectByType<BF_SceneLoadManager>();
        _battleService = FindFirstObjectByType<BF_BattleService>();
        _levelProgress = _battleService.LevelProgress;

        _levelButtons[0].onClick.AddListener(OnLevel01Clicked);
        _levelButtons[1].onClick.AddListener(OnLevel02Clicked);
        _levelButtons[2].onClick.AddListener(OnLevel03Clicked);
        _backButton.onClick.AddListener(OnBackClicked);
        _enterButton.onClick.AddListener(OnEnterClicked);
        SelectLevel(_levelProgress.HighestUnlockedLevel);
    }

    private void OnDisable()
    {
        _levelButtons[0].onClick.RemoveListener(OnLevel01Clicked);
        _levelButtons[1].onClick.RemoveListener(OnLevel02Clicked);
        _levelButtons[2].onClick.RemoveListener(OnLevel03Clicked);
        _backButton.onClick.RemoveListener(OnBackClicked);
        _enterButton.onClick.RemoveListener(OnEnterClicked);
    }

    private void OnLevel01Clicked()
    {
        SelectLevel(1);
    }

    private void OnLevel02Clicked()
    {
        SelectLevel(2);
    }

    private void OnLevel03Clicked()
    {
        SelectLevel(3);
    }

    private void OnBackClicked()
    {
        _backButton.interactable = false;
        _sceneLoadManager.LoadMenu();
    }

    private void OnEnterClicked()
    {
        _enterButton.interactable = false;
        _battleService.StartLevel(_selectedLevel);
    }

    private void SelectLevel(int level)
    {
        if (!_levelProgress.IsUnlocked(level))
        {
            return;
        }

        _selectedLevel = level;
        Refresh();
        EventSystem.current.SetSelectedGameObject(_levelButtons[level - 1].gameObject);
    }

    private void Refresh()
    {
        for (int i = 0; i < _levelButtons.Length; i++)
        {
            int level = i + 1;
            bool isUnlocked = _levelProgress.IsUnlocked(level);
            bool isCompleted = _levelProgress.IsCompleted(level);

            Image image = _levelButtons[i].targetGraphic as Image;
            if (image != null)
            {
                image.sprite = isCompleted ? _completedSprite : isUnlocked ? _unlockedSprite : _lockedSprite;
            }

            _levelButtons[i].interactable = isUnlocked;
            if (image != null)
            {
                image.color = isUnlocked ? Color.white : new Color(0.43f, 0.43f, 0.43f, 1f);
            }

            _selectedFrames[i].SetActive(level == _selectedLevel);
            _stateTexts[i].text = isCompleted ? "COMPLETED" : isUnlocked ? "UNLOCKED" : "LOCKED";
        }

        string state = _levelProgress.IsCompleted(_selectedLevel) ? "已完成" : "已解锁";
        _levelInfoText.text = $"第{_selectedLevel}关\n状态：{state} / 当前选择";
        _enterButton.interactable = true;
    }
}
