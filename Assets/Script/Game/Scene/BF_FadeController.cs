using System.Threading.Tasks;
using UnityEngine;

public class BF_FadeController : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup _canvasGroup;

    [SerializeField]
    private float _fadeSpeed = 4f;

    private void Awake()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
    }

    public async Task Show()
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        await FadeTo(1f);
    }

    public async Task Hide()
    {
        await FadeTo(0f);
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
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
