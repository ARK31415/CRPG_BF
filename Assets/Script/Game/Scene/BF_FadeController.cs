using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_FadeController : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private float _fadeSpeed = 4f;

    [SerializeField]
    private GameObject _loadingPanel;

    [SerializeField]
    private Slider _progressBar;

    [SerializeField]
    private TMP_Text _loadingText;

    private void Awake()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        SetProgress(0f);
        SetLoading(false);
    }

    public async Task Show()
    {
        SetLoading(true);
        SetProgress(0f);
        SetLoadingText("正在加载...");
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        await FadeTo(1f);
    }

    public async Task Hide()
    {
        await FadeTo(0f);
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        SetLoading(false);
    }

    public void SetProgress(float value)
    {
        if (_progressBar != null)
        {
            _progressBar.value = Mathf.Clamp01(value);
        }
    }

    public void SetLoadingText(string text)
    {
        if (_loadingText != null)
        {
            _loadingText.text = text;
        }
    }

    private void SetLoading(bool isLoading)
    {
        _loadingPanel?.SetActive(isLoading);
    }

    private async Task FadeTo(float targetAlpha)
    {
        while (!Mathf.Approximately(_canvasGroup.alpha, targetAlpha))
        {
            _canvasGroup.alpha = Mathf.MoveTowards(
                _canvasGroup.alpha,
                targetAlpha,
                _fadeSpeed * Time.unscaledDeltaTime);

            await Task.Yield();
        }
    }
}
