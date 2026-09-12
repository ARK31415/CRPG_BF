using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent 场景中的 Esc 唯一消费者与 BF_EscapeRequestEvent 唯一订阅者。
/// 收到请求后：Loading 第一优先级；按优先级询问处理器；
/// 无人消费时按 GameMode 执行暂停 / 恢复 / Menu 返回。
/// 处理器自行判断当前上下文是否参与消费（Battle 只在 Battle 模式）。
/// </summary>
[DefaultExecutionOrder(5)]
public class BF_EscapeRouter : Singleton<BF_EscapeRouter>
{
    #region 运行时数据

    private readonly List<IBF_EscapeHandler> _handlers = new();
    private IDisposable _escapeRequestSubscription;

    #endregion

    #region 生命周期

    private void OnEnable()
    {
        _escapeRequestSubscription = GameEventBus.Instance.Subscribe<BF_EscapeRequestEvent>(OnEscapeRequested);
    }

    private void OnDisable()
    {
        _escapeRequestSubscription?.Dispose();
        _escapeRequestSubscription = null;
    }

    #endregion

    #region 对外接口

    public void Register(IBF_EscapeHandler handler)
    {
        if (handler == null)
        {
            return;
        }

        for (int i = 0; i < _handlers.Count; i++)
        {
            if (ReferenceEquals(_handlers[i], handler))
            {
                return;
            }
        }

        _handlers.Add(handler);
    }

    public void Unregister(IBF_EscapeHandler handler)
    {
        if (handler == null)
        {
            return;
        }

        for (int i = 0; i < _handlers.Count; i++)
        {
            if (ReferenceEquals(_handlers[i], handler))
            {
                _handlers.RemoveAt(i);
                return;
            }
        }
    }

    #endregion

    #region 路由

    // Esc 请求入口（唯一订阅者）：Loading 第一优先级 → 按优先级询问 → GameMode 默认行为。
    private void OnEscapeRequested(BF_EscapeRequestEvent requestEvent)
    {
        if (IsLoading())
        {
            return;
        }

        if (TryConsumeByHandlers())
        {
            return;
        }

        HandleDefault();
    }

    // Loading 第一优先级：加载任务或 Loading 模式任一成立即屏蔽 Esc。
    private bool IsLoading()
    {
        BF_SceneLoadManager sceneLoad = BF_SceneLoadManager.Instance;
        if (sceneLoad != null && sceneLoad.IsLoading)
        {
            return true;
        }

        BF_GameModeManager gameModeManager = BF_GameModeManager.Instance;
        return gameModeManager != null
            && gameModeManager.CurrentGameMode == BF_GameMode.Loading;
    }

    // 按优先级降序询问快照；单个处理器异常时记录并停止本帧传播。
    private bool TryConsumeByHandlers()
    {
        List<IBF_EscapeHandler> snapshot = BuildHandlerSnapshot();

        for (int i = 0; i < snapshot.Count; i++)
        {
            IBF_EscapeHandler handler = snapshot[i];
            try
            {
                if (handler.TryConsumeEscape())
                {
                    return true;
                }
            }
            catch (System.Exception exception)
            {
                // 处理器可能已部分修改状态：不在同一帧继续询问后续处理器。
                Debug.LogError($"[BF] Escape handler failed: {handler.GetType().Name}", this);
                Debug.LogException(exception, this);
                return true;
            }
        }

        return false;
    }

    // 清理 fake-null 后排序快照；注册与注销不维护顺序。
    private List<IBF_EscapeHandler> BuildHandlerSnapshot()
    {
        for (int i = _handlers.Count - 1; i >= 0; i--)
        {
            if (_handlers[i] is UnityEngine.Object unityObject && unityObject == null)
            {
                _handlers.RemoveAt(i);
            }
        }

        List<IBF_EscapeHandler> snapshot = new(_handlers);
        snapshot.Sort((a, b) => b.EscapePriority.CompareTo(a.EscapePriority));
        return snapshot;
    }

    // 无人消费时的默认行为：暂停 / 恢复 / Menu 返回；Result 与 None 不处理。
    private void HandleDefault()
    {
        BF_GameModeManager gameModeManager = BF_GameModeManager.Instance;
        if (gameModeManager == null)
        {
            return;
        }

        switch (gameModeManager.CurrentGameMode)
        {
            case BF_GameMode.Battle:
                gameModeManager.PauseBattle();
                break;

            case BF_GameMode.Paused:
                gameModeManager.ResumeBattle();
                break;

            case BF_GameMode.Menu:
                HandleMenuNavigation();
                break;
        }
    }

    private void HandleMenuNavigation()
    {
        BF_SceneLoadManager sceneLoad = BF_SceneLoadManager.Instance;
        if (sceneLoad == null)
        {
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "BattlePrepare")
        {
            sceneLoad.LoadLevelSelect();
        }
        else if (sceneName == "LevelSelect")
        {
            sceneLoad.LoadMenu();
        }
    }

    #endregion
}
