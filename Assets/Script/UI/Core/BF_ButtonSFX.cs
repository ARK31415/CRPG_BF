using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BF_ButtonSFX : MonoBehaviour
{
    [SerializeField]
    private BF_SFX _sfx = BF_SFX.UIButton;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(Play);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(Play);
    }

    private void Play()
    {
        GameEventBus.Instance.Publish(new BF_PlaySFXEvent(_sfx));
    }
}
