using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_ResultPanel : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _titleText;

    [SerializeField]
    private TMP_Text _messageText;

    [SerializeField]
    private Button _confirmButton;

    [SerializeField]
    private BF_BattleService _battleService;

    private void OnEnable()
    {
        _confirmButton.interactable = true;
        _confirmButton.onClick.AddListener(OnConfirmClicked);
        Refresh();
    }

    private void OnDisable()
    {
        _confirmButton.onClick.RemoveListener(OnConfirmClicked);
    }

    private void Refresh()
    {
        bool isVictory = _battleService.LastResult == BF_BattleResult.Victory;
        _titleText.text = isVictory ? "VICTORY" : "DEFEAT";

        if (!isVictory)
        {
            _messageText.text = "整备队伍后再次挑战";
        }
        else if (_battleService.CurrentLevel == 3)
        {
            _messageText.text = "Demo 全关卡完成";
        }
        else
        {
            _messageText.text = $"第{_battleService.CurrentLevel + 1}关已解锁";
        }
    }

    private void OnConfirmClicked()
    {
        _confirmButton.interactable = false;
        GameEventBus.Instance.Publish(new BF_ConfirmBattleResultRequestEvent());
    }
}
