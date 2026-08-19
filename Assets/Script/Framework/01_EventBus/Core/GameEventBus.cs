using System;
using System.Collections.Generic;

/// <summary>
/// 事件总线实现（纯 C#，零 Unity 依赖）。
/// 路线：类型即键（typeof(T)）+ 饿汉单例 + 接口门面 + 快照遍历 + 异常隔离。
/// 单线程模型（主线程），无锁；应用级生命周期。
/// </summary>
public class GameEventBus : IGameEventBus
{
    // ============ 单例：饿汉 vs 懒汉（DCL）对比 ============
    // 最终采用：饿汉 —— CLR 保证线程安全，1 行替代 15 行 DCL，零运行时开销
    // 备选：懒汉（双重检查锁）—— 支持精确创建时机/传参/可重建，事件总线无此需求故弃用
    /*
    private static volatile GameEventBus _instance;
    private static readonly object _lock = new object();

    public static IGameEventBus Instance
    {
        get
        {
            if (_instance == null)                 // ① 快速检查（无锁）
            {
                lock (_lock)                       // ② 拿锁
                {
                    if (_instance == null)         // ③ 二次检查（防重复创建）
                    {
                        _instance = new GameEventBus();
                    }
                }
            }
            return _instance;
        }
    }
    */

    // 饿汉单例：CLR 保证线程安全（1 行替代 DCL）
    private static readonly GameEventBus _instance = new GameEventBus();

    /// <summary>全局入口。返回接口而非实现——换实现不影响调用方。</summary>
    public static IGameEventBus Instance => _instance;

    /// <summary>日志注入点（依赖倒置）：总线不认识 UnityEngine，由 Unity 侧注入 Debug.Log。</summary>
    public static Action<string> LogError = IgnoreLog;

    /// <summary>零实现模板默认值：什么都不做。Unity 启动时被适配层替换为真实现。</summary>
    private static void IgnoreLog(string _) { }

    // 私有构造：调用方无权创建/销毁，生命周期归总线自己管
    private GameEventBus() { }

    // 键 = 事件类型，值 = 回调列表（List 而非单委托：原地增删零分配、元素独立便于隔离异常）
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    /// <summary>
    /// 订阅事件，返回令牌。
    /// 令牌 = 闭包捕获退订动作，Dispose 即退订。
    /// </summary>
    public IDisposable Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var eventType = typeof(T);

        // 懒创建：首次订阅才建列表
        if (!_handlers.TryGetValue(eventType, out var list))
        {
            list = new List<Delegate>();
            _handlers[eventType] = list;
        }

        list.Add(handler);

        // 返回令牌：Dispose 时执行 Unsubscribe
        return new EventBusSubscription(() => Unsubscribe(handler));
    }

    /// <summary>
    /// 手动取消订阅（逃生口）。传参必须是 Subscribe 时【同一个委托实例】。
    /// 槽位清空后自动从字典移除 —— 防"空列表堆积"内存泄漏。
    /// </summary>
    public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var eventType = typeof(T);

        if (_handlers.TryGetValue(eventType, out var list))
        {
            list.Remove(handler);
            if (list.Count == 0)
            {
                _handlers.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// 发布事件，依次调用所有订阅回调。
    /// ToArray() 快照：防"回调里增删同事件"导致遍历崩溃（本次新增收不到、移除仍收到）
    /// try-catch：单个回调异常不影响其他监听者
    /// </summary>
    public void Publish<T>(T gameEvent) where T : IGameEvent
    {
        var eventType = typeof(T);

        if (_handlers.TryGetValue(eventType, out var list))
        {
            foreach (var handler in list.ToArray())
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(gameEvent);
                }
                catch (Exception ex)
                {
                    LogError?.Invoke($"事件 {eventType.Name} 回调异常: {ex.Message}");
                }
            }
        }
    }

        // 每个事件类型一个槽：三列表 + 状态位
    // private readonly Dictionary<Type, EventSlot> _slots = new();

    // private sealed class EventSlot
    // {
    //     public List<Delegate> ListExist = new();
    //     public List<Delegate> AddList = new();
    //     public List<Delegate> DeleteList = new();
    //     public bool IsExecute;
    // }

    /// <summary>清空全部订阅。场景切换/域重载/测试结束时调用。</summary>
    public void Clear()
    {
        _handlers.Clear();
    }
}
