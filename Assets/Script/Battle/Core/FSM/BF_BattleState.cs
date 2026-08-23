using System.Collections;

/// <summary>
/// 战斗 FSM 的最小状态基类，参考 CodePath-Traveler 的协程状态生命周期。
/// </summary>
public abstract class BF_BattleState
{
    protected readonly BF_BattleController controller;

    protected BF_BattleState(BF_BattleController controller)
    {
        this.controller = controller;
    }

    public virtual IEnumerator Enter()
    {
        yield break;
    }

    public abstract IEnumerator Execute();

    public virtual IEnumerator Exit()
    {
        yield break;
    }
}
