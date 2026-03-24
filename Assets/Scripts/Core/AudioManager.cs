using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private int sfxPoolSize = 8;

    [Header("Volume")]
    [SerializeField] [Range(0f, 1f)] private float masterSFXVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float masterMusicVolume = 0.5f;

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip dungeonMusic;

    AudioSource musicSource;
    AudioSource[] sfxPool;
    int sfxPoolIndex;

    Coroutine musicFadeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupMusicSource();
        SetupSFXPool();
    }

    void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    void HandleGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.MainMenu:
                PlayMusic(menuMusic);
                break;
            case GameState.Playing:
                PlayMusic(dungeonMusic);
                break;
            case GameState.GameOver:
                StopMusic(2f);
                break;
        }
    }

    void SetupMusicSource()
    {
        GameObject musicGO = new GameObject("MusicSource");
        musicGO.transform.SetParent(transform);
        musicSource = musicGO.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = masterMusicVolume;
        musicSource.playOnAwake = false;
    }

    void SetupSFXPool()
    {
        sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject sfxGO = new GameObject($"SFXSource_{i}");
            sfxGO.transform.SetParent(transform);
            sfxPool[i] = sfxGO.AddComponent<AudioSource>();
            sfxPool[i].spatialBlend = 0f;
            sfxPool[i].playOnAwake = false;
        }
    }

    /// <summary>Plays a 2D SFX using the internal pool. Picks the next available (non-playing) source.</summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetPooledSource();
        source.clip = clip;
        source.volume = volume * masterSFXVolume;
        source.Play();
    }

    /// <summary>Plays a 3D spatialised SFX at the given world position. Uses a self-cleaning temporary source.</summary>
    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume * masterSFXVolume);
    }

    /// <summary>
    /// Взрыв: основной звук + второй слой с низким pitch (ощущение «баса»), без движения камеры.
    /// </summary>
    public void PlayExplosionWithBassAtPoint(AudioClip clip, Vector3 position, float mainVolume = 1f)
    {
        if (clip == null) return;

        PlaySFXAtPoint(clip, position, mainVolume);
        PlayClipAtPointWithPitch(clip, position, 0.48f, mainVolume * 0.78f);
    }

    void PlayClipAtPointWithPitch(AudioClip clip, Vector3 position, float pitch, float volume)
    {
        GameObject temp = new GameObject("TempPitchedSFX");
        temp.transform.position = position;

        AudioSource src = temp.AddComponent<AudioSource>();
        src.clip = clip;
        src.pitch = pitch;
        src.volume = volume * masterSFXVolume;
        src.spatialBlend = 1f;
        src.minDistance = 2f;
        src.maxDistance = 48f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.Play();

        float clipLen = clip.length / Mathf.Max(0.05f, Mathf.Abs(pitch));
        Object.Destroy(temp, clipLen + 0.08f);
    }

    /// <summary>Starts playing a music clip with an optional crossfade. Crossfades from any currently playing music.</summary>
    public void PlayMusic(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == null) return;

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        musicFadeCoroutine = StartCoroutine(CrossFade(clip, fadeTime));
    }

    /// <summary>Stops the current music track with a fade-out over fadeTime seconds.</summary>
    public void StopMusic(float fadeTime = 1f)
    {
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        musicFadeCoroutine = StartCoroutine(FadeOut(fadeTime));
    }

    AudioSource GetPooledSource()
    {
        // Prefer a source that is not currently playing.
        for (int i = 0; i < sfxPoolSize; i++)
        {
            int index = (sfxPoolIndex + i) % sfxPoolSize;
            if (!sfxPool[index].isPlaying)
            {
                sfxPoolIndex = (index + 1) % sfxPoolSize;
                return sfxPool[index];
            }
        }

        // All sources busy — steal the next one in round-robin order.
        AudioSource stolen = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPoolSize;
        return stolen;
    }

    IEnumerator CrossFade(AudioClip newClip, float fadeTime)
    {
        float halfFade = fadeTime * 0.5f;

        // Fade out current music.
        if (musicSource.isPlaying && halfFade > 0f)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < halfFade)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfFade);
                yield return null;
            }
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

        // Fade in new music.
        if (halfFade > 0f)
        {
            float elapsed = 0f;
            while (elapsed < halfFade)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, masterMusicVolume, elapsed / halfFade);
                yield return null;
            }
        }

        musicSource.volume = masterMusicVolume;
        musicFadeCoroutine = null;
    }

    IEnumerator FadeOut(float fadeTime)
    {
        if (fadeTime > 0f)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
                yield return null;
            }
        }

        musicSource.Stop();
        musicSource.volume = masterMusicVolume;
        musicFadeCoroutine = null;
    }
}
