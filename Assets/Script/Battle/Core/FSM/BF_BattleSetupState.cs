using System.Collections;
using UnityEngine;

public class BF_BattleSetupState : BF_BattleState
{
    public BF_BattleSetupState(BF_BattleController controller) : base(controller)
    {
    }

    public override IEnumerator Execute()
    {
        controller.SetPhase(BF_BattlePhase.SetupPhase);
        controller.SetUnits(controller.UnitSpawner.SpawnUnits());

        Debug.Log($"[BF] Battle Setup - Units: {controller.Units.Count}");

        // 发布运行时就绪事实，并保持 SetupPhase 等待入场门：
        // 剧情、标题卡或安全回退完成后由 BF_BattleIntroFlow 调用 ReleaseIntroGate。
        controller.NotifyBattleRuntimeReady();
        while (!controller.IsIntroGateReleased && !controller.IsBattleEnded)
        {
            yield return null;
        }

        controller.SetState(new BF_PlayerPhaseState(controller));
        yield return null;
    }
}
