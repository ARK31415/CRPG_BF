using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class BF_SceneLoadManager : Singleton<BF_SceneLoadManager>
{
    private const string MenuAddress = "Menu";
    private const string LevelSelectAddress = "LevelSelect";
    private const string BattlePrepareAddress = "BattlePrepare";

    [SerializeField]
    private BF_FadeController _fadeController;

    private AsyncOperationHandle<SceneInstance> _contentHandle;
    private Scene _contentScene;
    private bool _hasContentScene;

    private const float UnloadProgressEnd = 0.3f;

    public bool IsLoading { get; private set; }

    private async void Start()
    {
        await LoadContent(MenuAddress, BF_GameMode.Menu);
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

    private async Awaitable LoadContent(string address, BF_GameMode targetMode)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        BF_GameModeManager gameModeManager = BF_GameModeManager.Instance;
        if (gameModeManager == null)
        {
            IsLoading = false;
            return;
        }

        gameModeManager.NormalizeTimeScale();
        BF_GameMode previousMode = gameModeManager.CurrentGameMode;
        gameModeManager.SetGameMode(BF_GameMode.Loading);
        await _fadeController.Show();

        try
        {
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
            AsyncOperationHandle<SceneInstance> loadHandle = Addressables.LoadSceneAsync(
                address,
                LoadSceneMode.Additive,
                true);

            await WaitForLoad(loadHandle, loadStart, 1f);
            Debug.Log($"[BF] Content load completed: {address}, status: {loadHandle.Status}");

            if (loadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(loadHandle);
                throw new InvalidOperationException($"Addressable scene load failed: {address}");
            }

            _contentHandle = loadHandle;
            _contentScene = loadHandle.Result.Scene;
            _hasContentScene = true;
            SceneManager.SetActiveScene(loadHandle.Result.Scene);
            _fadeController.SetProgress(1f);
            _fadeController.SetLoadingText("加载完成");
            gameModeManager.SetGameMode(targetMode);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[BF] Load scene failed: {address}");
            Debug.LogException(exception);
            _fadeController.SetLoadingText("加载失败");
            gameModeManager.SetGameMode(previousMode);
        }
        finally
        {
            await _fadeController.Hide();
            IsLoading = false;
        }
    }

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

        _fadeController.SetProgress(end);
        await handle.Task;
    }
}
