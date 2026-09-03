using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class BF_SceneLoadManager : Singleton<BF_SceneLoadManager>
{
    private const string MenuAddress = "Menu";
    private const string LevelSelectAddress = "LevelSelect";
    private const string BattlePrepareAddress = "BattlePrepare";

    [SerializeField]
    private BF_FadeController _fadeController;

    [SerializeField]
    private BF_GameModeManager _gameModeManager;

    private AsyncOperationHandle<SceneInstance> _contentHandle;
    private Scene _contentScene;
    private bool _hasContentScene;

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
        _gameModeManager.NormalizeTimeScale();
        BF_GameMode previousMode = _gameModeManager.CurrentGameMode;
        _gameModeManager.SetGameMode(BF_GameMode.Loading);
        await _fadeController.Show();

        try
        {
            if (_hasContentScene)
            {
                Debug.Log($"[BF] Unload content: {_contentScene.name}, handle valid: {_contentHandle.IsValid()}");

                if (_contentHandle.IsValid())
                {
                    await Addressables.UnloadSceneAsync(_contentHandle).Task;
                }
                else if (_contentScene.IsValid() && _contentScene.isLoaded)
                {
                    await SceneManager.UnloadSceneAsync(_contentScene);
                }

                _hasContentScene = false;
                Debug.Log("[BF] Content unloaded");
            }

            Debug.Log($"[BF] Load content: {address}");
            AsyncOperationHandle<SceneInstance> loadHandle = Addressables.LoadSceneAsync(
                address,
                LoadSceneMode.Additive,
                true);

            await loadHandle.Task;
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
            _gameModeManager.SetGameMode(targetMode);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[BF] Load scene failed: {address}");
            Debug.LogException(exception);
            _gameModeManager.SetGameMode(previousMode);
        }
        finally
        {
            await _fadeController.Hide();
            IsLoading = false;
        }
    }
}
