

public class BF_BattlePhaseChangeEvent : IGameEvent
{
    public BF_BattlePhase Phase;
    public int Round;

    public BF_BattlePhaseChangeEvent(BF_BattlePhase phase, int round)
    {
        Phase = phase;
        Round = round;
    }
}
