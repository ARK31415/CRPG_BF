using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_SettingsPanel : MonoBehaviour
{
    [SerializeField]
    private Slider _masterSlider;

    [SerializeField]
    private Slider _bgmSlider;

    [SerializeField]
    private Slider _sfxSlider;

    [SerializeField]
    private Toggle _fullscreenToggle;

    [SerializeField]
    private TMP_Dropdown _resolutionDropdown;

    [SerializeField]
    private TMP_Dropdown _pathPlanningDropdown;

    [SerializeField]
    private Button _defaultsButton;

    [SerializeField]
    private Button _closeButton;

    private bool _refreshing;
    private IDisposable _settingsSubscription;

    public bool IsOpen => gameObject.activeSelf;

    private void OnEnable()
    {
        _settingsSubscription = GameEventBus.Instance.Subscribe<BF_SettingsChangedEvent>(_ => Refresh());
        _masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        _bgmSlider?.onValueChanged.AddListener(OnBGMChanged);
        _sfxSlider?.onValueChanged.AddListener(OnSFXChanged);
        _fullscreenToggle?.onValueChanged.AddListener(OnFullscreenChanged);
        _resolutionDropdown?.onValueChanged.AddListener(OnResolutionChanged);
        _pathPlanningDropdown?.onValueChanged.AddListener(OnPathPlanningChanged);
        _defaultsButton?.onClick.AddListener(ResetDefaults);
        _closeButton?.onClick.AddListener(Close);
        Refresh();
    }

    private void OnDisable()
    {
        _settingsSubscription?.Dispose();
        _settingsSubscription = null;
        _masterSlider?.onValueChanged.RemoveListener(OnMasterChanged);
        _bgmSlider?.onValueChanged.RemoveListener(OnBGMChanged);
        _sfxSlider?.onValueChanged.RemoveListener(OnSFXChanged);
        _fullscreenToggle?.onValueChanged.RemoveListener(OnFullscreenChanged);
        _resolutionDropdown?.onValueChanged.RemoveListener(OnResolutionChanged);
        _pathPlanningDropdown?.onValueChanged.RemoveListener(OnPathPlanningChanged);
        _defaultsButton?.onClick.RemoveListener(ResetDefaults);
        _closeButton?.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        BF_SettingsService settings = BF_SettingsService.Instance;
        if (settings == null)
        {
            return;
        }

        _refreshing = true;
        if (_masterSlider != null)
        {
            _masterSlider.value = settings.MasterVolume;
        }

        if (_bgmSlider != null)
        {
            _bgmSlider.value = settings.BGMVolume;
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.value = settings.SFXVolume;
        }

        if (_fullscreenToggle != null)
        {
            _fullscreenToggle.isOn = settings.Fullscreen;
        }

        RefreshResolutions(settings);

        if (_pathPlanningDropdown != null)
        {
            // 每次刷新重建确定选项列表，避免依赖“已有两个选项”的状态。
            _pathPlanningDropdown.ClearOptions();
            _pathPlanningDropdown.AddOptions(new List<string> { "自动最低消耗", "手动绘制" });
            _pathPlanningDropdown.SetValueWithoutNotify((int)settings.PathPlanningMode);
            _pathPlanningDropdown.RefreshShownValue();
        }

        _refreshing = false;
    }

    private void RefreshResolutions(BF_SettingsService settings)
    {
        if (_resolutionDropdown == null || settings == null)
        {
            return;
        }

        Resolution[] resolutions = settings.GetResolutions();
        List<string> options = new();
        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add($"{resolutions[i].width} x {resolutions[i].height}");
        }

        _resolutionDropdown.ClearOptions();
        _resolutionDropdown.AddOptions(options);
        if (options.Count > 0)
        {
            _resolutionDropdown.SetValueWithoutNotify(settings.GetResolutionIndex());
            _resolutionDropdown.RefreshShownValue();
        }
    }

    private void OnMasterChanged(float value)
    {
        if (!_refreshing)
        {
            BF_SettingsService.Instance?.SetMasterVolume(value);
        }
    }

    private void OnBGMChanged(float value)
    {
        if (!_refreshing)
        {
            BF_SettingsService.Instance?.SetBGMVolume(value);
        }
    }

    private void OnSFXChanged(float value)
    {
        if (!_refreshing)
        {
            BF_SettingsService.Instance?.SetSFXVolume(value);
        }
    }

    private void OnFullscreenChanged(bool value)
    {
        if (!_refreshing)
        {
            BF_SettingsService.Instance?.SetFullscreen(value);
        }
    }

    private void OnResolutionChanged(int index)
    {
        if (!_refreshing)
        {
            BF_SettingsService.Instance?.SetResolution(index);
        }
    }

    private void OnPathPlanningChanged(int index)
    {
        if (!_refreshing)
        {
            BF_SettingsService.Instance?.SetPathPlanningMode((BF_PathPlanningMode)index);
        }
    }

    private void ResetDefaults()
    {
        BF_SettingsService.Instance?.ResetDefaults();
    }
}
