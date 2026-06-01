using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinScreenUI : MonoBehaviour
{
    public static WinScreenUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject rootPanel;
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public TMP_Text playtimeText;
    public Button continueButton;

    [Header("Timing")]
    public float showDelayAfterLaunchSeconds = 17f;

    private Coroutine pendingShowRoutine;
    private bool hasBeenShown;

    public bool IsVisible => rootPanel != null && rootPanel.activeSelf;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (rootPanel == gameObject)
        {
            Debug.LogWarning("[WinScreenUI] rootPanel points to the same GameObject as the script. Assign a separate child panel.");
        }

        if (rootPanel != null && rootPanel != gameObject)
        {
            rootPanel.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ScheduleShowAfterRocketLaunch()
    {
        ScheduleShow(showDelayAfterLaunchSeconds);
    }

    public void ScheduleShow(float delaySeconds)
    {
        if (hasBeenShown)
        {
            return;
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("[WinScreenUI] Cannot schedule win screen because the script object is inactive.");
            return;
        }

        if (pendingShowRoutine != null)
        {
            StopCoroutine(pendingShowRoutine);
        }

        pendingShowRoutine = StartCoroutine(ShowAfterDelay(delaySeconds));
    }

    private IEnumerator ShowAfterDelay(float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delaySeconds));
        ShowNow();
        pendingShowRoutine = null;
    }

    private void ShowNow()
    {
        if (hasBeenShown)
        {
            return;
        }

        hasBeenShown = true;

        if (titleText != null)
        {
            titleText.text = "Congratulations!";
        }

        if (subtitleText != null)
        {
            subtitleText.text = "You have successfully launched the rocket and finished the game.";
        }

        if (playtimeText != null)
        {
            playtimeText.text = BuildPlaytimeText();
        }

        if (rootPanel != null)
        {
            rootPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    private string BuildPlaytimeText()
    {
        float totalSeconds = SaveManager.Instance != null ? SaveManager.Instance.TotalPlayTimeSeconds : 0f;
        int clampedSeconds = Mathf.Max(0, Mathf.FloorToInt(totalSeconds));
        int hours = clampedSeconds / 3600;
        int minutes = (clampedSeconds % 3600) / 60;
        int secs = clampedSeconds % 60;
        return $"Total play time: {hours}h {minutes:00}m {secs:00}s";
    }

    public void OnContinueClicked()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }
}