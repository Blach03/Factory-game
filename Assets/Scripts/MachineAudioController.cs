using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class MachineAudioController : MonoBehaviour
{
    [Header("Machine Sound")]
    public AudioClip machineLoopClip;
    [Range(0f, 1f)] public float baseVolume = 0.7f;
    [Range(0.5f, 1.5f)] public float basePitch = 1f;

    [Header("Work State")]
    [Tooltip("If enabled, sound plays only while machine works. If disabled, loop plays all the time.")]
    public bool playOnlyWhenWorking = true;

    [Header("Hearing Range (tiles)")]
    [Min(0.1f)] public float baseMinDistance = 2f;
    [Min(0.5f)] public float baseMaxDistance = 10f;
    [Min(1f)] public float maxZoomRangeMultiplier = 2f;

    private AudioSource source;
    private IMachineWorkStateProvider workStateProvider;
    private Camera cachedCamera;
    private CameraController cachedCameraController;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        workStateProvider = GetComponent<IMachineWorkStateProvider>();

        source.playOnAwake = false;
        source.loop = true;
        source.clip = machineLoopClip;
        source.volume = baseVolume;
        source.pitch = basePitch;

        // 3D settings for distance-based attenuation in top-down world.
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.dopplerLevel = 0f;
        source.spread = 0f;
    }

    private void Update()
    {
        if (source == null)
        {
            return;
        }

        if (source.clip != machineLoopClip)
        {
            source.clip = machineLoopClip;
        }

        source.volume = baseVolume;
        source.pitch = basePitch;

        UpdateDistanceForZoom();
        UpdatePlayState();
    }

    private void UpdatePlayState()
    {
        bool shouldPlay = machineLoopClip != null;

        if (shouldPlay && playOnlyWhenWorking)
        {
            shouldPlay = workStateProvider != null && workStateProvider.IsMachineWorking;
        }

        if (shouldPlay)
        {
            if (!source.isPlaying)
            {
                source.Play();
            }
        }
        else
        {
            if (source.isPlaying)
            {
                source.Stop();
            }
        }
    }

    private void UpdateDistanceForZoom()
    {
        float zoomMultiplier = GetZoomRangeMultiplier();

        source.minDistance = baseMinDistance * zoomMultiplier;
        source.maxDistance = Mathf.Max(source.minDistance + 0.1f, baseMaxDistance * zoomMultiplier);
    }

    private float GetZoomRangeMultiplier()
    {
        Camera cam = GetMainCamera();
        if (cam == null || !cam.orthographic)
        {
            return 1f;
        }

        float zoom01;
        if (cachedCameraController != null && cachedCameraController.maxZoom > cachedCameraController.minZoom)
        {
            zoom01 = Mathf.InverseLerp(cachedCameraController.minZoom, cachedCameraController.maxZoom, cam.orthographicSize);
        }
        else
        {
            // Fallback range matching your current project defaults.
            zoom01 = Mathf.InverseLerp(5f, 50f, cam.orthographicSize);
        }

        return Mathf.Lerp(1f, maxZoomRangeMultiplier, zoom01);
    }

    private Camera GetMainCamera()
    {
        if (cachedCamera != null)
        {
            return cachedCamera;
        }

        cachedCamera = Camera.main;
        if (cachedCamera != null)
        {
            cachedCameraController = cachedCamera.GetComponent<CameraController>();
        }

        return cachedCamera;
    }
}
