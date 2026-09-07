using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

/// <summary>
/// 管理唯一内容场景的 Addressables 加载、卸载和 Loading 遮罩。
/// 首次加载由 Persistent 的 GameStartup 发起；本类不决定新游戏或存档初始化。
/// Inspector 必须绑定完整的 FadeController。
/// </summary>
public class BF_SceneLoadManager : Singleton<BF_SceneLoadManager>
{
    #region 常量与静态缓存

    // 场景地址
    private const string MenuAddress = "Menu";
    private const string LevelSelectAddress = "LevelSelect";
    private const string BattlePrepareAddress = "BattlePrepare";

    // 卸载阶段在 Loading 进度条中的终点占比，其余区间留给加载阶段。
    private const float UnloadProgressEnd = 0.3f;

    #endregion

    #region 序列化配置与引用

    [Header("过渡遮罩")]
    [SerializeField]
    private BF_FadeController _fadeController;

    #endregion

    #region 运行时数据

    // 内容场景状态
    private AsyncOperationHandle<SceneInstance> _contentHandle;
    private Scene _contentScene;
    private bool _hasContentScene;

    #endregion

    #region 对外接口

    // 加载状态
    public bool IsLoading { get; private set; }

    #endregion

    #region 加载接口

    /// <summary>
    /// 供 GameStartup 等待首次菜单加载；失败时保留启动遮罩。
    /// 页面返回菜单仍使用 LoadMenu，不进入首次失败策略。
    /// </summary>
    /// <returns>加载和遮罩收尾完成为 true；忙碌拒绝或失败为 false。</returns>
    public async Awaitable<bool> LoadMenuAsync()
    {
        return await LoadContent(MenuAddress, BF_GameMode.Menu, isStartup: true);
    }

    public async void LoadMenu()
    {
        await LoadContent(MenuAddress, BF_GameMode.Menu);
    }

    public async void LoadLevelSelect()
    {
        await LoadContent(LevelSelectAddress, BF_GameMode.Menu);
    }

    public async void LoadBattle(string address)
    {
        await LoadContent(address, BF_GameMode.Battle);
    }

    public async void LoadBattlePrepare()
    {
        await LoadContent(BattlePrepareAddress, BF_GameMode.Menu);
    }

    #endregion

    #region 执行链

    private async Awaitable<bool> LoadContent(
        string address, BF_GameMode targetMode, bool isStartup = false)
    {
        if (IsLoading)
        {
            // 尚未取得加载所有权，不得改变正在执行的请求状态。
            return false;
        }

        IsLoading = true;
        BF_GameModeManager gameModeManager = BF_GameModeManager.Instance;
        if (gameModeManager == null)
        {
            IsLoading = false;
            Debug.LogError("[BF] Load scene failed: GameModeManager is missing.", this);
            return false;
        }

        BF_GameMode previousMode = gameModeManager.CurrentGameMode;
        AsyncOperationHandle<SceneInstance> loadHandle = default;

        try
        {
            gameModeManager.NormalizeTimeScale();
            gameModeManager.SetGameMode(BF_GameMode.Loading);
            await _fadeController.Show();

            bool hasContentScene = _hasContentScene;
            float loadStart = hasContentScene ? UnloadProgressEnd : 0f;

            if (hasContentScene)
            {
                Debug.Log($"[BF] Unload content: {_contentScene.name}, handle valid: {_contentHandle.IsValid()}");

                if (_contentHandle.IsValid())
                {
                    AsyncOperationHandle unloadHandle = Addressables.UnloadSceneAsync(_contentHandle);
                    await WaitForUnload(unloadHandle, 0f, UnloadProgressEnd);
                }
                else if (_contentScene.IsValid() && _contentScene.isLoaded)
                {
                    await WaitForUnload(SceneManager.UnloadSceneAsync(_contentScene), 0f, UnloadProgressEnd);
                }

                _hasContentScene = false;
                _contentHandle = default;
                _contentScene = default;
                Debug.Log("[BF] Content unloaded");
            }

            Debug.Log($"[BF] Load content: {address}");
            loadHandle = Addressables.LoadSceneAsync(
                address,
                LoadSceneMode.Additive,
                true);

            await WaitForLoad(loadHandle, loadStart, 1f);
            Debug.Log($"[BF] Content load completed: {address}, status: {loadHandle.Status}");

            if (loadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException($"Addressable scene load failed: {address}");
            }

            _contentHandle = loadHandle;
            _contentScene = loadHandle.Result.Scene;
            _hasContentScene = true;
            if (!SceneManager.SetActiveScene(_contentScene))
            {
                throw new InvalidOperationException($"Cannot activate scene: {address}");
            }
            _fadeController.SetProgress(1f);
            _fadeController.SetLoadingText("加载完成");
            gameModeManager.SetGameMode(targetMode);
            await _fadeController.Hide();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[BF] Load scene failed: {address}");
            Debug.LogException(exception);
            // 等待任务本身也可能抛异常；失败句柄在此统一释放，不依赖后面的 Status 分支。
            if (loadHandle.IsValid() && loadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(loadHandle);
            }

            if (isStartup)
            {
                gameModeManager.SetGameMode(BF_GameMode.Loading);
                await _fadeController.Show();
                _fadeController.SetLoadingText("启动失败，请重新启动游戏");
            }
            else
            {
                // 保留原有非首次失败行为；旧内容已卸载时，恢复模式不等于恢复旧页面。
                _fadeController.SetLoadingText("加载失败");
                gameModeManager.SetGameMode(previousMode);
                await _fadeController.Hide();
            }

            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region 进度等待

    private async Task WaitForUnload(AsyncOperationHandle handle, float start, float end)
    {
        while (!handle.IsDone)
        {
            _fadeController.SetProgress(Mathf.Lerp(start, end, handle.PercentComplete));
            await Task.Yield();
        }

        _fadeController.SetProgress(end); // Unload handle may auto-release on completion.
    }

    private async Task WaitForUnload(AsyncOperation operation, float start, float end)
    {
        if (operation == null)
        {
            _fadeController.SetProgress(end);
            return;
        }

        while (!operation.isDone)
        {
            _fadeController.SetProgress(Mathf.Lerp(start, end, operation.progress));
            await Task.Yield();
        }

        _fadeController.SetProgress(end);
    }

    private async Task WaitForLoad(
        AsyncOperationHandle<SceneInstance> handle,
        float start,
        float end)
    {
        while (!handle.IsDone)
        {
            _fadeController.SetProgress(Mathf.Lerp(start, end, handle.PercentComplete));
            await Task.Yield();
        }

        await handle.Task;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _fadeController.SetProgress(end);
        }
    }

    #endregion
}
