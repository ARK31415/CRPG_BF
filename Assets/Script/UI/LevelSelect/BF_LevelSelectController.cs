using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BF_LevelSelectController : MonoBehaviour
{
    [SerializeField]
    private Button _level01Button;

    [SerializeField]
    private Button _backButton;

    [SerializeField]
    private TMP_Text _levelInfoText;

    private BF_SceneLoadManager _sceneLoadManager;

    private void OnEnable()
    {
        _sceneLoadManager = FindFirstObjectByType<BF_SceneLoadManager>();
        _level01Button.onClick.AddListener(OnLevel01Clicked);
        _backButton.onClick.AddListener(OnBackClicked);
        SelectLevel01();
    }

    private void OnDisable()
    {
        _level01Button.onClick.RemoveListener(OnLevel01Clicked);
        _backButton.onClick.RemoveListener(OnBackClicked);
    }

    private void OnLevel01Clicked()
    {
        SelectLevel01();
    }

    private void OnBackClicked()
    {
        _backButton.interactable = false;
        _sceneLoadManager.LoadMenu();
    }

    private void SelectLevel01()
    {
        _levelInfoText.text = "第一关  边境遭遇\n状态：已解锁 / 当前选择";
        EventSystem.current.SetSelectedGameObject(_level01Button.gameObject);
    }
}
