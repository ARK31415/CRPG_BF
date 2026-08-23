using System.Collections;
using UnityEngine;

public class BF_BattleSetupState : BF_BattleState
{
    public BF_BattleSetupState(BF_BattleController controller) : base(controller)
    {
    }

    public override IEnumerator Execute()
    {
        controller.CacheUnits();
        Debug.Log($"[BF] Battle Setup - Units: {controller.Units.Count}");
        controller.SetState(new BF_PlayerPhaseState(controller));
        yield return null;
    }
}
