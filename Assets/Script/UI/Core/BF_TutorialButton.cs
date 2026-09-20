using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教程重看按钮：只向 BF_TutorialManager 请求打开当前场景教程，
/// 不查找教程面板或场景教程组件。无教程时请求安全失败。
/// </summary>
[RequireComponent(typeof(Button))]
public class BF_TutorialButton : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _button?.onClick.AddListener(Open);
    }

    private void OnDisable()
    {
        _button?.onClick.RemoveListener(Open);
    }

    private void Open()
    {
        BF_TutorialManager.Instance?.OpenCurrentTutorial();
    }
}
