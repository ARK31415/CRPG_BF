using System.Collections;
using UnityEngine;

/// <summary>
/// 玩家和敌方共用的战斗命令执行入口。
/// </summary>
public class BF_BattleCommandExecutor
{
    public IEnumerator Execute(BF_BattleCommandRequest request)
    {
        if (request == null || request.Actor == null)
        {
            yield break;
        }

        switch (request.Type)
        {
            case BF_BattleCommandType.Move:
                yield return Move(request);
                break;

            case BF_BattleCommandType.Skill:
                yield return request.Actor.UseSkill(request.Skill, request.TargetPos);
                break;

            case BF_BattleCommandType.Item:
                yield return request.Actor.UseBattleItem(request.ItemSlot);
                break;

            case BF_BattleCommandType.EndTurn:
                request.Actor.FinishTurn();
                break;
        }
    }

    private IEnumerator Move(BF_BattleCommandRequest request)
    {
        BF_BattleUnit actor = request.Actor;
        BF_BoardManager board = actor?.Board;

        if (actor == null
            || board == null
            || !board.IsInitialized
            || request.Path == null
            || request.Path.Count == 0)
        {
            Debug.LogWarning("[BF] Move rejected: actor, board or path is unavailable.");
            yield break;
        }

        // 执行前根据当前 Board 重新验证整条路径并计算真实地形成本，不信任预览成本。
        if (!board.TryValidateMovePath(actor.GridPos, request.Path, out int totalCost, out string failReason))
        {
            Debug.LogWarning($"[BF] Move rejected for {actor.DisplayName}: {failReason}");
            yield break;
        }

        if (!actor.CanPay(totalCost))
        {
            Debug.LogWarning($"[BF] Move rejected for {actor.DisplayName}: cannot pay {totalCost} AP.");
            yield break;
        }

        Vector2Int target = request.Path[request.Path.Count - 1];
        yield return actor.Move(request.Path);

        // 到达目标后按执行端验证的实际成本扣 AP；未到达不扣费。
        if (actor.GridPos == target)
        {
            actor.SpendAP(totalCost);
        }
    }
}
