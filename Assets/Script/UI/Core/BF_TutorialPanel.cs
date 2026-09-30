using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教程显示 View：只渲染 ViewData、复位滚动并显隐自身根节点。
/// 不判断场景、不做首次显示策略、不查询教程数据。
/// </summary>
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

    // 只有根节点在整条父链上可见时才算打开；父级被禁用时不得误报显示成功。
    public bool IsOpen => _panelRoot != null && _panelRoot.activeInHierarchy;

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

    public void Show(BF_TutorialViewData data)
    {
        if (data == null)
        {
            return;
        }

        if (_titleText != null)
        {
            _titleText.text = data.Title;
        }

        if (_bodyText != null)
        {
            _bodyText.text = data.Body;
        }

        bool wasOpen = IsOpen;
        _panelRoot?.SetActive(true);
        if (!wasOpen && IsOpen)
        {
            PublishBlockingState(true);
        }
        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null)
        {
            _scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void Close()
    {
        bool wasOpen = IsOpen;
        _panelRoot?.SetActive(false);
        if (wasOpen)
        {
            PublishBlockingState(false);
        }
    }

    private static void PublishBlockingState(bool isOpen)
    {
        GameEventBus.Instance.Publish(
            new BF_BlockingPresentationChangedEvent(BF_BlockingPresentation.Tutorial, isOpen));
    }
}
