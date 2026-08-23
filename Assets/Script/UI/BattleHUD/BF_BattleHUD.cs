using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BF_BattleHUD : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _phaseText;
    [SerializeField]
    private TMP_Text _roundText;
    [SerializeField]
    private Button _endTurnButton;


    private void Awake()
    {
        GameEventBus.Instance?.Subscribe<BF_BattlePhaseChangeEvent>(OnPhaseChanged).UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnEnable()
    {
        _endTurnButton.onClick.AddListener(OnEndTurnClicked);
    }
    private void OnDisable()
    {
        _endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
    }

    private void OnPhaseChanged(BF_BattlePhaseChangeEvent gameEvent)
    {
        _roundText.text = gameEvent.Round > 0 ? $"第{gameEvent.Round}回合" : string.Empty;

        _phaseText.text = gameEvent.Phase switch
        {
            BF_BattlePhase.SetupPhase => "战斗准备",
            BF_BattlePhase.PlayerPhase => "玩家回合",
            BF_BattlePhase.EnemyPhase => "敌方回合",
            _ => string.Empty
        };
        Debug.Log($"[BF] HUD Phase: {gameEvent.Phase}");
    }

    private void OnEndTurnClicked()
    {
        GameEventBus.Instance.Publish(new BF_EndPlayerPhaseRequestEvent());
    }
}
