using UnityEngine;

/// <summary>
/// Unity 侧适配层：给纯 C# 的事件总线注入 Unity 日志实现
/// 事件总线本身保持零 Unity 依赖
/// </summary>
public static class EventBusUnityAdapter
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InjectLog()
    {
        // 语法糖等价写法：GameEventBus.LogError = msg => Debug.LogError($"[GameEventBus] {msg}");
        GameEventBus.LogError = LogToUnity;   // 方法组转换 → new Action<string>(LogToUnity)
    }

    /// <summary>Unity 的真实现：把总线抛出的错误字符串打进 Unity 控制台。</summary>
    private static void LogToUnity(string msg)
    {
        Debug.LogError($"[GameEventBus] {msg}");
    }
}
