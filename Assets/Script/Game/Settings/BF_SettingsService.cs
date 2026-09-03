using UnityEngine;

[DefaultExecutionOrder(-100)]
public class BF_SettingsService : Singleton<BF_SettingsService>
{
    private const string MasterKey = "BF_Settings_MasterVolume";
    private const string BGMKey = "BF_Settings_BGMVolume";
    private const string SFXKey = "BF_Settings_SFXVolume";
    private const string FullscreenKey = "BF_Settings_Fullscreen";
    private const string WidthKey = "BF_Settings_Width";
    private const string HeightKey = "BF_Settings_Height";

    public float MasterVolume { get; private set; } = 1f;
    public float BGMVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;
    public bool Fullscreen { get; private set; } = true;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        Load();
    }

    public Resolution[] GetResolutions()
    {
        return Screen.resolutions;
    }

    public int GetResolutionIndex()
    {
        Resolution[] resolutions = GetResolutions();
        int width = PlayerPrefs.GetInt(WidthKey, Screen.currentResolution.width);
        int height = PlayerPrefs.GetInt(HeightKey, Screen.currentResolution.height);

        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == width && resolutions[i].height == height)
            {
                return i;
            }
        }

        return resolutions.Length > 0 ? resolutions.Length - 1 : 0;
    }

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

    public void SetFullscreen(bool value)
    {
        Fullscreen = value;
        Screen.fullScreen = value;
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        SaveAndNotify();
    }

    public void SetResolution(int index)
    {
        Resolution[] resolutions = GetResolutions();
        if (index < 0 || index >= resolutions.Length)
        {
            return;
        }

        Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Fullscreen);
        PlayerPrefs.SetInt(WidthKey, resolution.width);
        PlayerPrefs.SetInt(HeightKey, resolution.height);
        SaveAndNotify();
    }

    public void ResetDefaults()
    {
        MasterVolume = 1f;
        BGMVolume = 1f;
        SFXVolume = 1f;
        Fullscreen = true;
        Screen.fullScreen = true;

        Resolution current = Screen.currentResolution;
        Screen.SetResolution(current.width, current.height, true);
        PlayerPrefs.DeleteKey(MasterKey);
        PlayerPrefs.DeleteKey(BGMKey);
        PlayerPrefs.DeleteKey(SFXKey);
        PlayerPrefs.DeleteKey(FullscreenKey);
        PlayerPrefs.DeleteKey(WidthKey);
        PlayerPrefs.DeleteKey(HeightKey);
        SaveAndNotify();
    }

    private void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
        BGMVolume = PlayerPrefs.GetFloat(BGMKey, 1f);
        SFXVolume = PlayerPrefs.GetFloat(SFXKey, 1f);
        Fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;

        int width = PlayerPrefs.GetInt(WidthKey, Screen.currentResolution.width);
        int height = PlayerPrefs.GetInt(HeightKey, Screen.currentResolution.height);
        Screen.SetResolution(width, height, Fullscreen);
    }

    private void SaveAndNotify()
    {
        PlayerPrefs.Save();
        GameEventBus.Instance.Publish(new BF_SettingsChangedEvent());
    }
}
