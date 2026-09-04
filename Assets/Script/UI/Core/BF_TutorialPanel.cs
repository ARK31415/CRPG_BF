using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_TutorialPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject _panelRoot;

    [SerializeField]
    private TMP_Text _titleText;

    [SerializeField]
    private TMP_Text _bodyText;

    [SerializeField]
    private ScrollRect _scrollRect;

    [SerializeField]
    private Button _closeButton;

    private readonly HashSet<string> _shownScenes = new();

    public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

    private void Awake()
    {
        Close();
    }

    private void OnEnable()
    {
        _closeButton?.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        _closeButton?.onClick.RemoveListener(Close);
    }

    public void ShowFirst(BF_SceneTutorial tutorial)
    {
        if (tutorial == null || _shownScenes.Contains(tutorial.SceneKey))
        {
            return;
        }

        _shownScenes.Add(tutorial.SceneKey);
        Show(tutorial);
    }

    public void Show(BF_SceneTutorial tutorial)
    {
        if (tutorial == null)
        {
            return;
        }

        if (_titleText != null)
        {
            _titleText.text = tutorial.Title;
        }

        if (_bodyText != null)
        {
            _bodyText.text = tutorial.Text;
        }

        _panelRoot?.SetActive(true);
        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null)
        {
            _scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void Close()
    {
        _panelRoot?.SetActive(false);
    }
}
