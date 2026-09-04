using UnityEngine;
using UnityEngine.UI;

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
        BF_TutorialPanel panel = FindFirstObjectByType<BF_TutorialPanel>();
        BF_SceneTutorial tutorial = FindFirstObjectByType<BF_SceneTutorial>();
        panel?.Show(tutorial);
    }
}
