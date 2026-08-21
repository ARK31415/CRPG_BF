using System.Collections;
using UnityEngine;

public class BF_PlayerPhaseState : BF_BattleState {
    public BF_PlayerPhaseState(BF_BattleController controller) : base(controller) {
    }

    public override IEnumerator Enter() {
        controller.StartPlayerRound();
        Debug.Log($"[BF] Player Phase Start - Round {controller.Round}");
        yield return null;
    }

    public override IEnumerator Execute() {
        while (controller.TryGetNextUnit(BF_UnitTeam.Player, out BF_BattleUnit unit)) {
            controller.CurrentUnit = unit;
            controller.MoveController.SetUnit(unit);
            Debug.Log($"[BF] Player Unit Selected: {unit.name}");

            yield return new WaitUntil(() => controller.MoveController.ActionDone);

            unit.FinishTurn();
            Debug.Log($"[BF] Player Unit Acted: {unit.name}");
        }

        controller.MoveController.ClearUnit();
        Debug.Log($"[BF] Player Phase End - Round {controller.Round}");
        controller.SetState(new BF_EnemyPhaseState(controller));
    }
}
