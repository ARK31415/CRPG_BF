/// <summary>
/// 内容场景成功变更事实：新内容场景已加载、已设为 Active，且目标 GameMode 已生效。
/// 加载失败不发布本事件；无内容时 CurrentContentAddress 为空字符串。
/// </summary>
public class BF_ContentSceneChangedEvent : IGameEvent
{
    public string Address { get; }
    public string SceneName { get; }
    public BF_GameMode GameMode { get; }

    public BF_ContentSceneChangedEvent(string address, string sceneName, BF_GameMode gameMode)
    {
        Address = address;
        SceneName = sceneName;
        GameMode = gameMode;
    }
}
