using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent 场景中的设置唯一入口：音量、画面与玩法设置经 PlayerPrefs 持久化，变更后广播设置事件。
/// 画面规则（窗口候选、默认尺寸解析、显示模式、帧率白名单）只在本服务实现，UI 不接触 Unity 画面 API。
/// </summary>
[DefaultExecutionOrder(-100)]
public class BF_SettingsService : Singleton<BF_SettingsService>
{
    #region 常量与静态缓存

    // PlayerPrefs 键
    private const string MasterKey = "BF_Settings_MasterVolume";
    private const string BGMKey = "BF_Settings_BGMVolume";
    private const string SFXKey = "BF_Settings_SFXVolume";
    private const string FullscreenKey = "BF_Settings_Fullscreen";
    private const string WidthKey = "BF_Settings_Width";
    private const string HeightKey = "BF_Settings_Height";
    private const string PathPlanningModeKey = "BF_Settings_PathPlanningMode";
    private const string TargetFrameRateKey = "BF_Settings_TargetFrameRate";

    // 窗口候选固定三档；帧率上限固定三档。
    private static readonly Vector2Int[] WindowResolutionCandidates =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
    };

    private static readonly int[] TargetFrameRateCandidates = { 60, 90, 120 };

    private const int DefaultWindowWidth = 1280;
    private const int DefaultWindowHeight = 720;
    private const int DefaultTargetFrameRate = 60;

    // 对外只读视图的缓存，避免每次调用重新分配。
    private readonly List<Vector2Int> _windowResolutionOptions = new();
    private readonly List<int> _targetFrameRateOptions = new();

    // 当前窗口尺寸（保存值；全屏时不使用，也不被全屏切换覆盖）
    private int _windowWidth = DefaultWindowWidth;
    private int _windowHeight = DefaultWindowHeight;

    #endregion

    #region 对外接口

    // 音量
    public float MasterVolume { get; private set; } = 1f;
    public float BGMVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;

    // 画面与玩法
    public bool Fullscreen { get; private set; }
    public int TargetFrameRate { get; private set; } = DefaultTargetFrameRate;
    public BF_PathPlanningMode PathPlanningMode { get; private set; } = BF_PathPlanningMode.Automatic;

    #endregion

    #region 生命周期

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        Load();
    }

    #endregion

    #region 查询

    public Vector2Int GetDesktopResolution()
    {
        return new Vector2Int(Display.main.systemWidth, Display.main.systemHeight);
    }

    public IReadOnlyList<Vector2Int> GetWindowResolutionOptions()
    {
        Vector2Int desktop = GetDesktopResolution();
        _windowResolutionOptions.Clear();

        for (int i = 0; i < WindowResolutionCandidates.Length; i++)
        {
            Vector2Int candidate = WindowResolutionCandidates[i];
            if (candidate.x <= desktop.x && candidate.y <= desktop.y)
            {
                _windowResolutionOptions.Add(candidate);
            }
        }

        if (_windowResolutionOptions.Count == 0)
        {
            _windowResolutionOptions.Add(ResolveDefaultWindowResolution());
        }

        return _windowResolutionOptions;
    }

    public int GetWindowResolutionIndex()
    {
        IReadOnlyList<Vector2Int> options = GetWindowResolutionOptions();
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].x == _windowWidth && options[i].y == _windowHeight)
            {
                return i;
            }
        }

        return 0;
    }

    public IReadOnlyList<int> GetTargetFrameRateOptions()
    {
        _targetFrameRateOptions.Clear();
        _targetFrameRateOptions.AddRange(TargetFrameRateCandidates);
        return _targetFrameRateOptions;
    }

    public int GetTargetFrameRateIndex()
    {
        for (int i = 0; i < TargetFrameRateCandidates.Length; i++)
        {
            if (TargetFrameRateCandidates[i] == TargetFrameRate)
            {
                return i;
            }
        }

        return 0;
    }

    #endregion

    #region 音量设置

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterKey, MasterVolume);
        SaveAndNotify();
    }

    public void SetBGMVolume(float value)
    {
        BGMVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(BGMKey, BGMVolume);
        SaveAndNotify();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SFXKey, SFXVolume);
        SaveAndNotify();
    }

    #endregion

    #region 画面设置

    public void SetFullscreen(bool value)
    {
        if (Fullscreen == value)
        {
            return;
        }

        Fullscreen = value;
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);

        if (value)
        {
            // 切全屏：固定桌面原生分辨率，不覆盖窗口宽高。
            Vector2Int desktop = GetDesktopResolution();
            Screen.SetResolution(desktop.x, desktop.y, FullScreenMode.FullScreenWindow);
        }
        else
        {
            // 切窗口：恢复最后保存的窗口宽高。
            Screen.SetResolution(_windowWidth, _windowHeight, FullScreenMode.Windowed);
        }

        SaveAndNotify();
    }

    public bool SetWindowResolution(int index)
    {
        if (Fullscreen)
        {
            // 全屏时拒绝修改，不能只依赖 UI 禁用。
            return false;
        }

        IReadOnlyList<Vector2Int> options = GetWindowResolutionOptions();
        if (index < 0 || index >= options.Count)
        {
            return false;
        }

        Vector2Int resolution = options[index];
        Screen.SetResolution(resolution.x, resolution.y, FullScreenMode.Windowed);
        _windowWidth = resolution.x;
        _windowHeight = resolution.y;
        PlayerPrefs.SetInt(WidthKey, _windowWidth);
        PlayerPrefs.SetInt(HeightKey, _windowHeight);
        SaveAndNotify();
        return true;
    }

    public bool SetTargetFrameRate(int frameRate)
    {
        if (!IsValidTargetFrameRate(frameRate))
        {
            // 表外值拒绝：不修改当前值、不保存、不广播。
            return false;
        }

        if (TargetFrameRate == frameRate)
        {
            return true;
        }

        TargetFrameRate = frameRate;
        ApplyTargetFrameRate();
        PlayerPrefs.SetInt(TargetFrameRateKey, TargetFrameRate);
        SaveAndNotify();
        return true;
    }

    #endregion

    #region 玩法设置

    public void SetPathPlanningMode(BF_PathPlanningMode mode)
    {
        if (PathPlanningMode == mode)
        {
            return;
        }

        PathPlanningMode = mode;
        PlayerPrefs.SetInt(PathPlanningModeKey, (int)mode);
        SaveAndNotify();
    }

    #endregion

    #region 默认值

    public void ResetDefaults()
    {
        MasterVolume = 1f;
        BGMVolume = 1f;
        SFXVolume = 1f;
        Fullscreen = false;
        PathPlanningMode = BF_PathPlanningMode.Automatic;
        TargetFrameRate = DefaultTargetFrameRate;

        // 统一走默认解析：正常桌面 1280×720，低分辨率设备使用桌面安全回退尺寸。
        Vector2Int window = ResolveDefaultWindowResolution();
        _windowWidth = window.x;
        _windowHeight = window.y;

        PlayerPrefs.DeleteKey(MasterKey);
        PlayerPrefs.DeleteKey(BGMKey);
        PlayerPrefs.DeleteKey(SFXKey);
        PlayerPrefs.DeleteKey(FullscreenKey);
        PlayerPrefs.DeleteKey(WidthKey);
        PlayerPrefs.DeleteKey(HeightKey);
        PlayerPrefs.DeleteKey(PathPlanningModeKey);
        PlayerPrefs.DeleteKey(TargetFrameRateKey);

        ApplyTargetFrameRate();
        Screen.SetResolution(_windowWidth, _windowHeight, FullScreenMode.Windowed);

        SaveAndNotify();
    }

    #endregion

    #region 持久化

    private void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
        BGMVolume = PlayerPrefs.GetFloat(BGMKey, 1f);
        SFXVolume = PlayerPrefs.GetFloat(SFXKey, 1f);
        Fullscreen = PlayerPrefs.GetInt(FullscreenKey, 0) == 1;
        PathPlanningMode = (BF_PathPlanningMode)PlayerPrefs.GetInt(
            PathPlanningModeKey,
            (int)BF_PathPlanningMode.Automatic);

        // 窗口尺寸：读取旧值并规范化，随后覆盖写回同一组键，避免每次启动重复迁移。
        int width = PlayerPrefs.GetInt(WidthKey, 0);
        int height = PlayerPrefs.GetInt(HeightKey, 0);
        Vector2Int window = ResolveWindowResolution(width, height);
        _windowWidth = window.x;
        _windowHeight = window.y;
        PlayerPrefs.SetInt(WidthKey, _windowWidth);
        PlayerPrefs.SetInt(HeightKey, _windowHeight);

        // 帧率上限：缺失或非法值归一为 60 并覆盖写回。
        int frameRate = PlayerPrefs.GetInt(TargetFrameRateKey, DefaultTargetFrameRate);
        TargetFrameRate = IsValidTargetFrameRate(frameRate) ? frameRate : DefaultTargetFrameRate;
        PlayerPrefs.SetInt(TargetFrameRateKey, TargetFrameRate);

        PlayerPrefs.Save();

        ApplyTargetFrameRate();

        if (Fullscreen)
        {
            Vector2Int desktop = GetDesktopResolution();
            Screen.SetResolution(desktop.x, desktop.y, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(_windowWidth, _windowHeight, FullScreenMode.Windowed);
        }
    }

    private Vector2Int ResolveWindowResolution(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return ResolveDefaultWindowResolution();
        }

        IReadOnlyList<Vector2Int> options = GetWindowResolutionOptions();
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].x == width && options[i].y == height)
            {
                return options[i];
            }
        }

        return ResolveDefaultWindowResolution();
    }

    private Vector2Int ResolveDefaultWindowResolution()
    {
        Vector2Int desktop = GetDesktopResolution();
        if (desktop.x >= DefaultWindowWidth && desktop.y >= DefaultWindowHeight)
        {
            return new Vector2Int(DefaultWindowWidth, DefaultWindowHeight);
        }

        Debug.LogWarning(
            $"[BF_SettingsService] 桌面尺寸 {desktop.x}×{desktop.y} 无法容纳 {DefaultWindowWidth}×{DefaultWindowHeight}，使用桌面尺寸作为安全回退候选。");
        return desktop;
    }

    private static bool IsValidTargetFrameRate(int frameRate)
    {
        for (int i = 0; i < TargetFrameRateCandidates.Length; i++)
        {
            if (TargetFrameRateCandidates[i] == frameRate)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyTargetFrameRate()
    {
        // vSyncCount 非 0 时 Unity 会忽略 targetFrameRate，因此固定关闭 VSync。
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
    }

    private void SaveAndNotify()
    {
        PlayerPrefs.Save();
        GameEventBus.Instance.Publish(new BF_SettingsChangedEvent());
    }

    #endregion
}
