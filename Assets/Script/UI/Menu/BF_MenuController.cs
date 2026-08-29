using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BF_MenuController : MonoBehaviour
{
    [SerializeField]
    private Button _startButton;

    [SerializeField]
    private Button _exitButton;

    private BF_SceneLoadManager _sceneLoadManager;

    private void OnEnable()
    {
        _sceneLoadManager = FindFirstObjectByType<BF_SceneLoadManager>();
        _startButton.onClick.AddListener(OnStartClicked);
        _exitButton.onClick.AddListener(OnExitClicked);
        EventSystem.current.SetSelectedGameObject(_startButton.gameObject);
    }

    private void OnDisable()
    {
        _startButton.onClick.RemoveListener(OnStartClicked);
        _exitButton.onClick.RemoveListener(OnExitClicked);
    }

    private void OnStartClicked()
    {
        _startButton.interactable = false;
        _sceneLoadManager.LoadLevelSelect();
    }

    private void OnExitClicked()
    {
#if UNITY_EDITOR
        Debug.Log("[BF] Exit requested. Application.Quit runs in Player build.");
#else
        Application.Quit();
#endif
    }
}
