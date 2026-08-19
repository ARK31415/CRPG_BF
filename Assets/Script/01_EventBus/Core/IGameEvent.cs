/// <summary>
/// 事件标记接口：所有事件类型必须实现它（约束 + 类型识别）。
/// 事件类型推荐用 struct（避免堆分配）。
/// </summary>
public interface IGameEvent
{

}

/// <summary>
/// 测试事件：验证总线用，Message 为携带的数据。
/// </summary>
public struct TestEvent : IGameEvent
{
    public string Message;
}
