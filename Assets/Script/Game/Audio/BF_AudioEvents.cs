public class BF_PlayBGMEvent : IGameEvent
{
    public BF_BGM Track { get; }

    public BF_PlayBGMEvent(BF_BGM track)
    {
        Track = track;
    }
}

public class BF_PlayStingerEvent : IGameEvent
{
    public BF_Stinger Stinger { get; }

    public BF_PlayStingerEvent(BF_Stinger stinger)
    {
        Stinger = stinger;
    }
}

public class BF_PlaySFXEvent : IGameEvent
{
    public BF_SFX SFX { get; }

    public BF_PlaySFXEvent(BF_SFX sfx)
    {
        SFX = sfx;
    }
}

public class BF_SettingsChangedEvent : IGameEvent
{
}
