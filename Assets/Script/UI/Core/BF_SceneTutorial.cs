using UnityEngine;

public class BF_SceneTutorial : MonoBehaviour
{
    [SerializeField]
    private string _title;

    [TextArea(5, 24)]
    [SerializeField]
    private string _text;

    public string Title => _title;
    public string Text => _text;
    public string SceneKey => string.IsNullOrEmpty(gameObject.scene.path)
        ? gameObject.scene.name
        : gameObject.scene.path;

    private void Start()
    {
        BF_TutorialPanel panel = FindFirstObjectByType<BF_TutorialPanel>();
        panel?.ShowFirst(this);
    }
}
