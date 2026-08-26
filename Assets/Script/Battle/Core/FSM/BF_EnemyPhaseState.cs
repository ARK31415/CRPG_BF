using System.Collections;
using UnityEngine;

public class BF_EnemyPhaseState : BF_BattleState
{
    public BF_EnemyPhaseState(BF_BattleController controller) : base(controller)
    {
    }

    public override IEnumerator Enter()
    {
        controller.StartEnemyRound();
        controller.SetPhase(BF_BattlePhase.EnemyPhase);

        Debug.Log($"[BF] Enemy Phase Start - Round {controller.Round}");
        yield return null;
    }

    public override IEnumerator Execute()
    {
        yield return controller.RunEnemyPhase();

        if (controller.IsBattleEnded)
        {
            yield break;
        }

        Debug.Log($"[BF] Enemy Phase End - Round {controller.Round}");
        controller.SetState(new BF_PlayerPhaseState(controller));
    }
}
