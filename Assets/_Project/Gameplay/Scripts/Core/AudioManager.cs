using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public enum MusicType { MainMenu, Combat, Exploration }

    [System.Serializable]
    public struct MusicMapping
    {
        public MusicType type;
        public AudioClip clip;
    }

    [Header("Settings")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Music Library")]
    [SerializeField] private List<MusicMapping> musicLibrary;
    [SerializeField] private float musicVolume = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DetermineAndPlayMusic(scene.name);
    }

    private void DetermineAndPlayMusic(string sceneName)
    {
        switch (sceneName)
        {
            case "MainMenuScene":
                PlayMusicByType(MusicType.MainMenu);
                break;
            case "CombatScene":
                PlayMusicByType(MusicType.Combat);
                break;
            default:
                PlayMusicByType(MusicType.Exploration);
                break;
        }
    }

    public void PlayMusicByType(MusicType type)
    {
        AudioClip targetClip = musicLibrary.Find(m => m.type == type).clip;

        if (targetClip != null && musicSource.clip != targetClip)
        {
            musicSource.clip = targetClip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null) sfxSource.PlayOneShot(clip, volume);
    }
}