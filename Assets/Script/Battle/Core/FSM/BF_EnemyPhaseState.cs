using System.Collections;
using UnityEngine;

public class BF_EnemyPhaseState : BF_BattleState {
    public BF_EnemyPhaseState(BF_BattleController controller) : base(controller) {
    }

    public override IEnumerator Enter() {
        Debug.Log($"[BF] Enemy Phase Start - Round {controller.Round}");
        yield return null;
    }

    public override IEnumerator Execute() {
        while (controller.TryGetNextUnit(BF_UnitTeam.Enemy, out BF_BattleUnit unit)) {
            unit.FinishTurn();
            Debug.Log($"[BF] Enemy Unit Auto Pass: {unit.name}");
            yield return null;
        }

        Debug.Log($"[BF] Enemy Phase End - Round {controller.Round}");
        controller.SetState(new BF_PlayerPhaseState(controller));
    }
}
