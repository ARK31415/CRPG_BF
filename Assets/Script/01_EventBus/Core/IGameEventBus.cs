using System;
using System.Collections.Generic;

/// <summary>
/// 事件总线门面接口。
/// 业务只依赖此接口（可替换/可测试），不依赖实现。
/// 约定：请求走接口，通知走事件。
/// </summary>
public interface IGameEventBus
{
    /// <summary>订阅事件，返回令牌。Dispose 令牌即退订（无需记住委托实例）。</summary>
    IDisposable Subscribe<T>(Action<T> handler) where T : IGameEvent;

    /// <summary>手动取消订阅（逃生口）。必须传 Subscribe 时同一个委托实例。</summary>
    void Unsubscribe<T>(Action<T> handler) where T : IGameEvent;

    /// <summary>发布事件，通知所有订阅者（发布方不认识接收方）。</summary>
    void Publish<T>(T gameEvent) where T : IGameEvent;

    /// <summary>清空全部订阅。场景切换/域重载/测试结束时调用。</summary>
    void Clear();
}
