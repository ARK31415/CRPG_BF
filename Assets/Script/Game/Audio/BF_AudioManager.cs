using System;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-90)]
public class BF_AudioManager : Singleton<BF_AudioManager>
{
    [SerializeField]
    private BF_AudioConfigSO _config;

    [SerializeField]
    private AudioSource _bgmSource;

    [SerializeField]
    private AudioSource _sfxSource;

    private IDisposable _bgmSubscription;
    private IDisposable _stingerSubscription;
    private IDisposable _sfxSubscription;
    private IDisposable _settingsSubscription;
    private IDisposable _gameModeSubscription;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        SetupSources();
    }

    private void OnEnable()
    {
        if (Instance != this)
        {
            return;
        }

        _bgmSubscription = GameEventBus.Instance.Subscribe<BF_PlayBGMEvent>(OnPlayBGM);
        _stingerSubscription = GameEventBus.Instance.Subscribe<BF_PlayStingerEvent>(OnPlayStinger);
        _sfxSubscription = GameEventBus.Instance.Subscribe<BF_PlaySFXEvent>(OnPlaySFX);
        _settingsSubscription = GameEventBus.Instance.Subscribe<BF_SettingsChangedEvent>(_ => ApplySettings());
        _gameModeSubscription = GameEventBus.Instance.Subscribe<BF_GameModeChangedEvent>(OnGameModeChanged);
    }

    private void Start()
    {
        ApplySettings();

        if (BF_GameModeManager.Instance != null)
        {
            PlayForMode(BF_GameModeManager.Instance.CurrentGameMode);
        }
    }

    private void OnDisable()
    {
        _bgmSubscription?.Dispose();
        _stingerSubscription?.Dispose();
        _sfxSubscription?.Dispose();
        _settingsSubscription?.Dispose();
        _gameModeSubscription?.Dispose();
        _bgmSubscription = null;
        _stingerSubscription = null;
        _sfxSubscription = null;
        _settingsSubscription = null;
        _gameModeSubscription = null;
    }

    public void PlayBGM(BF_BGM track)
    {
        AudioClip clip = _config != null ? _config.GetBGM(track) : null;
        if (_bgmSource == null || clip == null)
        {
            return;
        }

        if (_bgmSource.clip == clip && _bgmSource.isPlaying)
        {
            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.loop = true;
        _bgmSource.Play();
    }

    public void PlayStinger(BF_Stinger stinger)
    {
        AudioClip clip = _config != null ? _config.GetStinger(stinger) : null;
        if (_sfxSource == null || clip == null)
        {
            return;
        }

        _sfxSource.PlayOneShot(clip);
    }

    public void PlaySFX(BF_SFX sfx)
    {
        AudioClip clip = _config != null ? _config.GetSFX(sfx) : null;
        if (_sfxSource == null || clip == null)
        {
            return;
        }

        _sfxSource.PlayOneShot(clip);
    }

    private void SetupSources()
    {
        if (_bgmSource != null)
        {
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            if (_config != null && _config.BGMGroup != null)
            {
                _bgmSource.outputAudioMixerGroup = _config.BGMGroup;
            }
        }

        if (_sfxSource != null)
        {
            _sfxSource.playOnAwake = false;
            if (_config != null && _config.SFXGroup != null)
            {
                _sfxSource.outputAudioMixerGroup = _config.SFXGroup;
            }
        }
    }

    private void ApplySettings()
    {
        BF_SettingsService settings = BF_SettingsService.Instance;
        float master = settings != null ? settings.MasterVolume : 1f;
        float bgm = settings != null ? settings.BGMVolume : 1f;
        float sfx = settings != null ? settings.SFXVolume : 1f;

        bool mixerApplied = ApplyMixerSettings(master, bgm, sfx);
        if (mixerApplied)
        {
            if (_bgmSource != null)
            {
                _bgmSource.volume = 1f;
            }

            if (_sfxSource != null)
            {
                _sfxSource.volume = 1f;
            }

            return;
        }

        if (_bgmSource != null)
        {
            _bgmSource.volume = master * bgm;
        }

        if (_sfxSource != null)
        {
            _sfxSource.volume = master * sfx;
        }
    }

    private bool ApplyMixerSettings(float master, float bgm, float sfx)
    {
        if (_config == null || _config.Mixer == null)
        {
            return false;
        }

        bool masterApplied = SetMixerVolume(_config.MasterVolumeParameter, master);
        bool bgmApplied = SetMixerVolume(_config.BGMVolumeParameter, bgm, _config.BGMBaseGainDb);
        bool sfxApplied = SetMixerVolume(_config.SFXVolumeParameter, sfx);
        return masterApplied && bgmApplied && sfxApplied;
    }

    private bool SetMixerVolume(string parameter, float value, float baseGainDb = 0f)
    {
        if (string.IsNullOrEmpty(parameter))
        {
            return false;
        }

        float decibels = value <= 0f ? -80f : baseGainDb + 20f * Mathf.Log10(value);
        return _config.Mixer.SetFloat(parameter, decibels);
    }

    private void OnPlayBGM(BF_PlayBGMEvent gameEvent)
    {
        PlayBGM(gameEvent.Track);
    }

    private void OnPlayStinger(BF_PlayStingerEvent gameEvent)
    {
        PlayStinger(gameEvent.Stinger);
    }

    private void OnPlaySFX(BF_PlaySFXEvent gameEvent)
    {
        PlaySFX(gameEvent.SFX);
    }

    private void OnGameModeChanged(BF_GameModeChangedEvent gameEvent)
    {
        PlayForMode(gameEvent.CurrentMode);
    }

    private void PlayForMode(BF_GameMode mode)
    {
        if (mode == BF_GameMode.Battle || mode == BF_GameMode.Result)
        {
            PlayBGM(BF_BGM.Battle);
        }
        else if (mode == BF_GameMode.Menu)
        {
            PlayBGM(BF_BGM.Menu);
        }
    }
}
