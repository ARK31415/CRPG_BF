/// <summary>
/// Esc 消费合同：BF_EscapeRouter 按优先级降序询问，返回 true 表示已消费本帧。
/// 实现方在 OnEnable 注册、OnDisable 注销；注册与注销必须成对。
/// </summary>
public interface IBF_EscapeHandler
{
    int EscapePriority { get; }

    bool TryConsumeEscape();
}

/// <summary>
/// Esc 处理器优先级常量。高值先询问；不散落魔法数字。
/// </summary>
public static class BF_EscapePriorities
{
    public const int UI = 200;

    public const int Battle = 100;
}
