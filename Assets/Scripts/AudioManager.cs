using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;

    [Header("Music Source")]
    [SerializeField] private AudioSource musicSource;

    private const string KEY_MUSIC = "audio_music_on";
    private const string KEY_SFX = "audio_sfx_on";
    private const string KEY_UI = "audio_ui_on";

    private const string KEY_MUSIC_VOL = "audio_music_vol";
    private const string KEY_SFX_VOL = "audio_sfx_vol";
    private const string KEY_UI_VOL = "audio_ui_vol";

    private const string PARAM_MUSIC = "MusicVolume";
    private const string PARAM_SFX = "SFXVolume";
    private const string PARAM_UI = "UIVolume";

    private const float MUTED_DB = -80f;

    public bool MusicOn { get; private set; }
    public bool SFXOn { get; private set; }
    public bool UIOn { get; private set; }

    public AudioMixerGroup MusicGroup => musicGroup;
    public AudioMixerGroup SFXGroup => sfxGroup;
    public AudioMixerGroup UIGroup => uiGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource != null && musicGroup != null)
            musicSource.outputAudioMixerGroup = musicGroup;

        LoadAndApply();
    }

    public void SetMusic(bool on)
    {
        MusicOn = on;
        ApplyToMixer(PARAM_MUSIC, on, KEY_MUSIC_VOL);
        PlayerPrefs.SetInt(KEY_MUSIC, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSFX(bool on)
    {
        SFXOn = on;
        ApplyToMixer(PARAM_SFX, on, KEY_SFX_VOL);
        PlayerPrefs.SetInt(KEY_SFX, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetUI(bool on)
    {
        UIOn = on;
        ApplyToMixer(PARAM_UI, on, KEY_UI_VOL);
        PlayerPrefs.SetInt(KEY_UI, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;

        musicSource.outputAudioMixerGroup = musicGroup;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
    {
        PlayOneShotAt(clip, position, sfxGroup, volume);
    }

    public void PlayUI(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        GameObject obj = new GameObject("UI Sound");
        AudioSource source = obj.AddComponent<AudioSource>();

        source.clip = clip;
        source.volume = volume;
        source.outputAudioMixerGroup = uiGroup;
        source.Play();

        Destroy(obj, clip.length);
    }

    private void PlayOneShotAt(AudioClip clip, Vector3 position, AudioMixerGroup group, float volume)
    {
        if (clip == null) return;

        GameObject obj = new GameObject("One Shot Sound");
        obj.transform.position = position;

        AudioSource source = obj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = group;
        source.Play();

        Destroy(obj, clip.length);
    }

    private void LoadAndApply()
    {
        MusicOn = PlayerPrefs.GetInt(KEY_MUSIC, 1) == 1;
        SFXOn = PlayerPrefs.GetInt(KEY_SFX, 1) == 1;
        UIOn = PlayerPrefs.GetInt(KEY_UI, 1) == 1;

        ApplyToMixer(PARAM_MUSIC, MusicOn, KEY_MUSIC_VOL);
        ApplyToMixer(PARAM_SFX, SFXOn, KEY_SFX_VOL);
        ApplyToMixer(PARAM_UI, UIOn, KEY_UI_VOL);
    }

    private void ApplyToMixer(string parameter, bool on, string volumeKey)
    {
        if (mainMixer == null)
        {
            Debug.LogWarning("[AudioManager] mainMixer is not assigned!");
            return;
        }

        if (on)
        {
            float volume = PlayerPrefs.GetFloat(volumeKey, 0f);
            mainMixer.SetFloat(parameter, volume);
        }
        else
        {
            if (mainMixer.GetFloat(parameter, out float current) && current > MUTED_DB + 1f)
                PlayerPrefs.SetFloat(volumeKey, current);

            mainMixer.SetFloat(parameter, MUTED_DB);
        }
    }
}