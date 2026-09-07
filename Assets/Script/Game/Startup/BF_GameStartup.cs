using System;
using UnityEngine;

/// <summary>
/// Persistent 中唯一的应用启动入口，等待初始菜单加载并记录结果。
/// 不初始化存档或业务服务，不控制遮罩，也不作为全局就绪服务。
/// 场景必须挂载一个启用的组件；不能依赖 SceneLoadManager 自动启动。
/// </summary>
public class BF_GameStartup : MonoBehaviour
{
    private bool _isReady;

    // 在 Start 发起加载，确保 Persistent 内各服务的同步 Awake/OnEnable 已完成。
    private async void Start()
    {
        Debug.Log("[BF] 开始启动", this);
        try
        {
            _isReady = await BF_SceneLoadManager.Instance.LoadMenuAsync();
        }
        catch (Exception exception)
        {
            // Unity 生命周期入口不能把未处理的异步异常留给调用者。
            _isReady = false;
            Debug.LogException(exception, this);
        }

        if (_isReady)
        {
            Debug.Log("[BF] 启动完成", this);
        }
        else
        {
            Debug.LogError("[BF] 启动失败", this);
        }
    }
}
