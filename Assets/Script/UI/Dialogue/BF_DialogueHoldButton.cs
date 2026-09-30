using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>把指针按住语义转发给对话快进；键盘与手柄由 BF_InputManager 统一处理。</summary>
public class BF_DialogueHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private BF_DialoguePanel _dialoguePanel;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _dialoguePanel?.BeginFastForward();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _dialoguePanel?.EndFastForward();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _dialoguePanel?.EndFastForward();
    }

    private void OnDisable()
    {
        _dialoguePanel?.EndFastForward();
    }
}
