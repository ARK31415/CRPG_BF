using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SO_BF_AudioConfig", menuName = "CRPG BF/Game/Audio Config")]
public class BF_AudioConfigSO : ScriptableObject
{
    [Header("Mixer")]
    [SerializeField]
    private AudioMixer _mixer;

    [SerializeField]
    private AudioMixerGroup _bgmGroup;

    [SerializeField]
    private AudioMixerGroup _sfxGroup;

    [SerializeField]
    private string _masterVolumeParameter = "MasterVolume";

    [SerializeField]
    private string _bgmVolumeParameter = "BGMVolume";

    [SerializeField]
    private string _sfxVolumeParameter = "SFXVolume";

    [Tooltip("BGM额外基础增益，单位为dB。")]
    [SerializeField]
    private float _bgmBaseGainDb = 6f;

    [Header("Music")]
    [SerializeField]
    private AudioClip _menuBGM;

    [SerializeField]
    private AudioClip _battleBGM;

    [Header("Stingers")]
    [SerializeField]
    private AudioClip _victory;

    [SerializeField]
    private AudioClip _defeat;

    [SerializeField]
    private AudioClip _complete;

    [Header("SFX")]
    [SerializeField]
    private AudioClip _button;

    [SerializeField]
    private AudioClip _move;

    [SerializeField]
    private AudioClip _basicAttack;

    [SerializeField]
    private AudioClip _skill;

    [SerializeField]
    private AudioClip _item;

    [SerializeField]
    private AudioClip _unitDeath;

    public AudioMixer Mixer => _mixer;
    public AudioMixerGroup BGMGroup => _bgmGroup;
    public AudioMixerGroup SFXGroup => _sfxGroup;
    public string MasterVolumeParameter => _masterVolumeParameter;
    public string BGMVolumeParameter => _bgmVolumeParameter;
    public string SFXVolumeParameter => _sfxVolumeParameter;
    public float BGMBaseGainDb => _bgmBaseGainDb;

    public AudioClip GetBGM(BF_BGM track)
    {
        return track == BF_BGM.Battle ? _battleBGM : _menuBGM;
    }

    public AudioClip GetStinger(BF_Stinger stinger)
    {
        return stinger switch
        {
            BF_Stinger.Defeat => _defeat,
            BF_Stinger.Complete => _complete,
            _ => _victory
        };
    }

    public AudioClip GetSFX(BF_SFX sfx)
    {
        return sfx switch
        {
            BF_SFX.Move => _move,
            BF_SFX.BasicAttack => _basicAttack,
            BF_SFX.Skill => _skill,
            BF_SFX.Item => _item,
            BF_SFX.UnitDeath => _unitDeath,
            _ => _button
        };
    }
}
