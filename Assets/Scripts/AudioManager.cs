using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("UI SFX")]
    public AudioClip uiClickClip;
    [Range(0f, 1f)] public float uiClickVolume = 0.8f;

    [Header("Build SFX")]
    public AudioClip placeBuildingClip;
    [Range(0f, 1f)] public float placeBuildingVolume = 0.9f;
    public AudioClip rotateBuildingClip;
    [Range(0f, 1f)] public float rotateBuildingVolume = 0.8f;
    public AudioClip destroyBuildingClip;
    [Range(0f, 1f)] public float destroyBuildingVolume = 0.9f;

    [Header("Background Music")]
    public List<AudioClip> backgroundMusicPlaylist = new List<AudioClip>();
    [Range(0f, 1f)] public float backgroundMusicVolume = 0.5f;

    [Header("Auto Hook")]
    [Tooltip("How often to scan scene for newly spawned UI Buttons.")]
    [SerializeField] private float uiScanInterval = 0.75f;

    private AudioSource uiSource;
    private AudioSource musicSource;
    private readonly HashSet<int> hookedButtonIds = new HashSet<int>();
    private float uiScanTimer;
    private int currentTrackIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupSources();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ApplyBackgroundMusic();
        HookAllButtonsInScene();
    }

    private void Update()
    {
        uiScanTimer += Time.unscaledDeltaTime;
        if (uiScanTimer >= uiScanInterval)
        {
            uiScanTimer = 0f;
            HookAllButtonsInScene();
        }

        UpdateMusicPlayback();
    }

    public void PlayUIClick()
    {
        if (uiSource == null || uiClickClip == null)
        {
            return;
        }

        uiSource.PlayOneShot(uiClickClip, uiClickVolume);
    }

    public void PlayPlaceBuildingSfx()
    {
        if (uiSource == null || placeBuildingClip == null)
        {
            return;
        }

        uiSource.PlayOneShot(placeBuildingClip, placeBuildingVolume);
    }

    public void PlayRotateBuildingSfx()
    {
        if (uiSource == null || rotateBuildingClip == null)
        {
            return;
        }

        uiSource.PlayOneShot(rotateBuildingClip, rotateBuildingVolume);
    }

    public void PlayDestroyBuildingSfx()
    {
        if (uiSource == null || destroyBuildingClip == null)
        {
            return;
        }

        uiSource.PlayOneShot(destroyBuildingClip, destroyBuildingVolume);
    }

    public void RefreshMusic()
    {
        ApplyBackgroundMusic();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookAllButtonsInScene();
        ApplyBackgroundMusic();
    }

    private void SetupSources()
    {
        uiSource = gameObject.GetComponent<AudioSource>();
        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
        }

        uiSource.playOnAwake = false;
        uiSource.loop = false;
        uiSource.spatialBlend = 0f;

        Transform musicChild = transform.Find("MusicSource");
        if (musicChild == null)
        {
            GameObject musicObject = new GameObject("MusicSource");
            musicObject.transform.SetParent(transform, false);
            musicChild = musicObject.transform;
        }

        musicSource = musicChild.GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = musicChild.gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
    }

    private void ApplyBackgroundMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.volume = backgroundMusicVolume;
        musicSource.loop = false;

        if (backgroundMusicPlaylist == null || backgroundMusicPlaylist.Count == 0)
        {
            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }
            musicSource.clip = null;
            currentTrackIndex = -1;
            return;
        }

        // Keep current track if still valid, otherwise start from first playlist entry.
        if (!IsCurrentTrackIndexValid())
        {
            currentTrackIndex = 0;
            PlayTrackAtIndex(currentTrackIndex);
            return;
        }

        AudioClip expected = backgroundMusicPlaylist[currentTrackIndex];
        if (expected == null)
        {
            PlayNextValidTrack();
            return;
        }

        if (musicSource.clip != expected)
        {
            PlayTrackAtIndex(currentTrackIndex);
            return;
        }

        if (!musicSource.isPlaying)
        {
            // If stopped manually, continue from next track in order.
            PlayNextValidTrack();
        }
    }

    private void UpdateMusicPlayback()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.volume = backgroundMusicVolume;

        if (backgroundMusicPlaylist == null || backgroundMusicPlaylist.Count == 0)
        {
            return;
        }

        // Track ended naturally: play next one. Full playlist loops after last item.
        if (!musicSource.isPlaying && musicSource.clip != null)
        {
            PlayNextValidTrack();
        }
    }

    private bool IsCurrentTrackIndexValid()
    {
        return currentTrackIndex >= 0 && currentTrackIndex < backgroundMusicPlaylist.Count;
    }

    private void PlayNextValidTrack()
    {
        if (backgroundMusicPlaylist == null || backgroundMusicPlaylist.Count == 0)
        {
            return;
        }

        int playlistCount = backgroundMusicPlaylist.Count;
        int startIndex = IsCurrentTrackIndexValid() ? currentTrackIndex : -1;

        for (int step = 1; step <= playlistCount; step++)
        {
            int nextIndex = (startIndex + step + playlistCount) % playlistCount;
            AudioClip clip = backgroundMusicPlaylist[nextIndex];
            if (clip == null)
            {
                continue;
            }

            currentTrackIndex = nextIndex;
            PlayTrackAtIndex(currentTrackIndex);
            return;
        }

        // Playlist has only null entries.
        musicSource.Stop();
        musicSource.clip = null;
    }

    private void PlayTrackAtIndex(int index)
    {
        if (backgroundMusicPlaylist == null || index < 0 || index >= backgroundMusicPlaylist.Count)
        {
            return;
        }

        AudioClip clip = backgroundMusicPlaylist[index];
        if (clip == null)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.Play();
    }

    private void HookAllButtonsInScene()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];
            if (btn == null)
            {
                continue;
            }

            int id = btn.GetInstanceID();
            if (!hookedButtonIds.Add(id))
            {
                continue;
            }

            btn.onClick.AddListener(() => PlayButtonClick(btn));
        }
    }

    private void PlayButtonClick(Button button)
    {
        if (button == null)
        {
            return;
        }

        ButtonAudioOverride custom = button.GetComponent<ButtonAudioOverride>();
        if (custom != null && custom.clickClip != null)
        {
            if (uiSource != null)
            {
                uiSource.PlayOneShot(custom.clickClip, custom.volume);
            }

            if (!custom.alsoPlayDefaultClick)
            {
                return;
            }
        }

        PlayUIClick();
    }
}
