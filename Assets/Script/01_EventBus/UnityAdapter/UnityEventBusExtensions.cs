using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity 适配层：令牌的自动解绑扩展。
/// 解决"忘了解绑泄漏"：把令牌绑定到 GameObject 生命周期，物体销毁时自动退订。
/// 纯 C# 核心（GameEventBus）保持零 Unity 依赖，本文件放 Unity 侧。
/// </summary>
public static class UnityEventBusExtensions
{
    /// <summary>
    /// 绑定 GameObject 生命周期：物体销毁时自动 Dispose 令牌（自动退订）。
    /// 用法：bus.Subscribe<TestEvent>(OnTest).UnRegisterWhenGameObjectDestroyed(gameObject);
    /// 即使忘了手动 Dispose 也安全 —— 销毁那一刻自动清理。
    /// </summary>
    public static IDisposable UnRegisterWhenGameObjectDestroyed(this IDisposable token, GameObject go)
    {
        if (token == null || go == null) return token;

        var trigger = GetOrAddTrigger(go);
        trigger.Add(token);
        return token;   // 返回原令牌：仍可手动 Dispose（幂等，双保险）
    }

    private static UnRegisterOnDestroyTrigger GetOrAddTrigger(GameObject go)
    {
        var trigger = go.GetComponent<UnRegisterOnDestroyTrigger>();
        if (trigger == null)
            trigger = go.AddComponent<UnRegisterOnDestroyTrigger>();
        return trigger;
    }

    /// <summary>
    /// 隐藏触发器：挂在目标 GameObject 上，OnDestroy 时批量 Dispose 所有令牌。
    /// hideFlags 隐藏组件：不干扰场景层级/不显示在 Inspector。
    /// </summary>
    private sealed class UnRegisterOnDestroyTrigger : MonoBehaviour
    {
        private readonly List<IDisposable> _tokens = new();

        public void Add(IDisposable token)
        {
            if (!_tokens.Contains(token))   // 防重复挂载
                _tokens.Add(token);
        }

        private void Awake()
        {
            hideFlags = HideFlags.HideInHierarchy;   // 隐藏组件
        }

        private void OnDestroy()
        {
            foreach (var token in _tokens)
                token?.Dispose();
            _tokens.Clear();
        }
    }
}
