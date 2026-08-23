using UnityEngine;

public class BF_GameModeManager : Singleton<BF_GameModeManager>
{
    public BF_GameMode CurrentGameMode { get; private set; }

    [SerializeField]
    private BF_GameMode _defaultGameMode = BF_GameMode.Battle;
}
