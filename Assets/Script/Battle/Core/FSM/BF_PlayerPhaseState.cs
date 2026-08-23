using System.Collections;
using UnityEngine;

public class BF_PlayerPhaseState : BF_BattleState
{
    public BF_PlayerPhaseState(BF_BattleController controller) : base(controller)
    {
    }

    public override IEnumerator Enter()
    {
        controller.StartPlayerRound();
        Debug.Log($"[BF] Player Phase Start - Round {controller.Round}");
        controller.SelectFirstPlayerUnit();
        yield return null;
    }

    public override IEnumerator Execute()
    {
        while (!controller.PlayerPhaseEnded)
        {
            if (controller.AreAllUnitsDone(BF_UnitTeam.Player))
            {
                controller.EndPlayerPhase();
            }

            yield return null;
        }

        Debug.Log($"[BF] Player Phase End - Round {controller.Round}");
        controller.SetState(new BF_EnemyPhaseState(controller));
    }
}
