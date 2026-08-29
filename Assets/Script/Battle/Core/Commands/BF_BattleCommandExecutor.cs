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
                yield return request.Actor.UseItem(request.Item);
                break;

            case BF_BattleCommandType.EndTurn:
                request.Actor.FinishTurn();
                break;
        }
    }

    private IEnumerator Move(BF_BattleCommandRequest request)
    {
        if (request.Path == null
            || request.Path.Count == 0
            || !request.Actor.CanPay(request.Path.Count))
        {
            yield break;
        }

        Vector2Int target = request.Path[request.Path.Count - 1];
        yield return request.Actor.Move(request.Path);

        if (request.Actor.GridPos == target)
        {
            request.Actor.SpendAP(request.Path.Count);
        }
    }
}
