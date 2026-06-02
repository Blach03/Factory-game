using UnityEngine;

[DisallowMultipleComponent]
public class ButtonAudioOverride : MonoBehaviour
{
    [Tooltip("Custom click sound for this specific button.")]
    public AudioClip clickClip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("If false and clip is assigned, this button uses custom clip instead of default click.")]
    public bool alsoPlayDefaultClick = false;
}
