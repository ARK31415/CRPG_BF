using System;

/// <summary>
/// 订阅令牌：Dispose 即退订。
/// 闭包捕获退订动作 → 调用方无需记住委托实例（解决 lambda 陷阱）。
/// 幂等：重复 Dispose 安全。
/// </summary>
public sealed class EventBusSubscription : IDisposable
{
    private Action _unsubscribeAction;
    private bool _isDisposed;

    public EventBusSubscription(Action unsubscribeAction)
    {
        _unsubscribeAction = unsubscribeAction;
    }

    public void Dispose()
    {
        if (_isDisposed) return;        // 幂等：第二次直接返回
        _isDisposed = true;
        _unsubscribeAction?.Invoke();   // 执行退订
        _unsubscribeAction = null;      // 置空：释放对闭包（handler）的引用
    }
}
