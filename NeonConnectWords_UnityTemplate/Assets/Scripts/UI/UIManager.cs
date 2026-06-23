using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles HUD updates, menu navigation, feedback banners, and UI button plumbing.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject modeSelectPanel;
    public GameObject hudPanel;
    public GameObject gameOverPanel;
    public GameObject settingsPanel;
    public GameObject tutorialPanel;

    [Header("HUD")]
    public TMP_Text modeText;
    public TMP_Text[] scoreTexts;
    public TMP_Text turnText;
    public TMP_Text currentLetterText;
    public TMP_Text comboText;
    public Slider comboSlider;
    public TMP_Text timerText;
    public Image timerFill;
    public GameObject timerGroup;
    public TMP_Text puzzleObjectiveText;

    [Header("Power Ups")]
    public Button wildcardButton;
    public Button bombButton;
    public Button swapButton;
    public TMP_Text wildcardCountText;
    public TMP_Text bombCountText;
    public TMP_Text swapCountText;

    [Header("Messages")]
    public TMP_Text messageText;
    public CanvasGroup messageCanvasGroup;

    [Header("Floating Score")]
    public GameObject floatingScorePrefab;
    public RectTransform floatingScoreCanvas;

    [Header("Menus")]
    public Button playButton;
    public Button classicModeButton;
    public Button timedModeButton;
    public Button puzzleModeButton;
    public Button tutorialModeButton;
    public Button playAgainButton;
    public Button mainMenuButton;
    public TMP_Text gameOverTitleText;
    public TMP_Text gameOverScoresText;

    [Header("Tutorial")]
    public TMP_Text tutorialTitleText;
    public TMP_Text tutorialBodyText;
    public Button tutorialNextButton;
    public Button tutorialSkipButton;

    [Header("Settings")]
    public Toggle reducedEffectsToggle;

    private Camera mainCamera;
    private Coroutine messageRoutine;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        HookButtons();
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        SetActive(mainMenuPanel, true);
        SetActive(modeSelectPanel, false);
        SetActive(hudPanel, false);
        SetActive(gameOverPanel, false);
        SetActive(settingsPanel, false);
        HideTutorialOverlay();
    }

    public void ShowModeSelect()
    {
        SetActive(mainMenuPanel, false);
        SetActive(modeSelectPanel, true);
        SetActive(gameOverPanel, false);
    }

    public void ShowGameHUD()
    {
        SetActive(mainMenuPanel, false);
        SetActive(modeSelectPanel, false);
        SetActive(hudPanel, true);
        SetActive(gameOverPanel, false);
    }

    public void ShowGameOver(string title, int[] scores)
    {
        SetActive(gameOverPanel, true);
        SetActive(hudPanel, false);
        SetActive(modeSelectPanel, false);
        SetActive(mainMenuPanel, false);

        if (gameOverTitleText != null)
        {
            gameOverTitleText.text = title;
        }

        if (gameOverScoresText != null)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < scores.Length; i++)
            {
                builder.AppendLine($"Player {i + 1}: {scores[i]}");
            }

            gameOverScoresText.text = builder.ToString();
        }
    }

    public void RefreshHUD(int[] scores, int currentPlayer, char currentLetter, float combo, GameMode mode, bool tutorialMode)
    {
        if (scoreTexts != null)
        {
            for (int i = 0; i < scoreTexts.Length; i++)
            {
                if (scoreTexts[i] != null && i < scores.Length)
                {
                    scoreTexts[i].text = scores[i].ToString();
                }
            }
        }

        if (turnText != null)
        {
            turnText.text = $"Player {currentPlayer + 1} Turn";
        }

        if (currentLetterText != null)
        {
            currentLetterText.text = currentLetter.ToString();
        }

        if (comboText != null)
        {
            comboText.text = $"x{combo:0.0}";
        }

        if (comboSlider != null)
        {
            comboSlider.value = Mathf.InverseLerp(1f, 4.5f, combo);
        }

        if (modeText != null)
        {
            modeText.text = tutorialMode ? "Tutorial" : mode.ToString();
        }
    }

    public void UpdateTimerVisibility(bool visible)
    {
        SetActive(timerGroup, visible);
    }

    public void UpdateTimerDisplay(float remaining, float totalDuration)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        if (timerFill != null && totalDuration > 0f)
        {
            float normalized = Mathf.Clamp01(remaining / totalDuration);
            timerFill.fillAmount = normalized;
            timerFill.color = Color.Lerp(new Color(1f, 0.25f, 0.3f), new Color(0.2f, 1f, 1f), normalized);
        }
    }

    public void UpdatePowerUpCounts(int wildcardCount, int bombCount, int swapCount, bool wildcardQueued, bool bombQueued)
    {
        if (wildcardCountText != null)
        {
            wildcardCountText.text = wildcardCount.ToString();
        }

        if (bombCountText != null)
        {
            bombCountText.text = bombCount.ToString();
        }

        if (swapCountText != null)
        {
            swapCountText.text = swapCount.ToString();
        }

        if (wildcardButton != null)
        {
            ColorBlock colors = wildcardButton.colors;
            colors.normalColor = wildcardQueued ? new Color(0.95f, 0.95f, 0.3f) : Color.white;
            wildcardButton.colors = colors;
        }

        if (bombButton != null)
        {
            ColorBlock colors = bombButton.colors;
            colors.normalColor = bombQueued ? new Color(1f, 0.6f, 0.25f) : Color.white;
            bombButton.colors = colors;
        }
    }

    public void ShowPuzzleObjective(string text)
    {
        if (puzzleObjectiveText != null)
        {
            puzzleObjectiveText.text = text;
        }
    }

    public void ShowMessage(string text, float duration = 1.4f)
    {
        if (messageText == null || messageCanvasGroup == null)
        {
            return;
        }

        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }

        messageRoutine = StartCoroutine(ShowMessageRoutine(text, duration));
    }

    public void ShowFloatingScore(int points, Vector3 worldPosition, string label)
    {
        if (floatingScorePrefab == null || floatingScoreCanvas == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        GameObject instance = Instantiate(floatingScorePrefab, floatingScoreCanvas);
        TMP_Text text = instance.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = $"{label} +{points}";
        }

        Vector3 screenPoint = mainCamera != null ? mainCamera.WorldToScreenPoint(worldPosition) : Vector3.zero;
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.position = screenPoint;

        StartCoroutine(FloatingScoreRoutine(rect, text, instance));
    }

    public void ShowTutorialStep(string title, string body, bool showNextButton)
    {
        SetActive(tutorialPanel, true);

        if (tutorialTitleText != null)
        {
            tutorialTitleText.text = title;
        }

        if (tutorialBodyText != null)
        {
            tutorialBodyText.text = body;
        }

        if (tutorialNextButton != null)
        {
            tutorialNextButton.gameObject.SetActive(showNextButton);
        }
    }

    public void HideTutorialOverlay()
    {
        SetActive(tutorialPanel, false);
    }

    private void HookButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(() =>
            {
                PlayUiClick();
                ShowModeSelect();
            });
        }

        if (classicModeButton != null)
        {
            classicModeButton.onClick.AddListener(() => StartSelectedMode(GameMode.Classic));
        }

        if (timedModeButton != null)
        {
            timedModeButton.onClick.AddListener(() => StartSelectedMode(GameMode.Timed));
        }

        if (puzzleModeButton != null)
        {
            puzzleModeButton.onClick.AddListener(() => StartSelectedMode(GameMode.Puzzle));
        }

        if (tutorialModeButton != null)
        {
            tutorialModeButton.onClick.AddListener(() =>
            {
                PlayUiClick();
                GameManager.Instance?.StartTutorialMode();
            });
        }

        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(() =>
            {
                PlayUiClick();
                GameManager.Instance?.RestartCurrentGame();
            });
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(() =>
            {
                PlayUiClick();
                GameManager.Instance?.ReturnToMainMenu();
            });
        }

        if (wildcardButton != null)
        {
            wildcardButton.onClick.AddListener(() => PowerUpManager.Instance?.TryQueueWildcard());
        }

        if (bombButton != null)
        {
            bombButton.onClick.AddListener(() => PowerUpManager.Instance?.TryQueueBomb());
        }

        if (swapButton != null)
        {
            swapButton.onClick.AddListener(() => PowerUpManager.Instance?.TryUseSwap());
        }

        if (reducedEffectsToggle != null)
        {
            reducedEffectsToggle.onValueChanged.AddListener(value =>
            {
                ParticleManager.Instance?.SetReducedFxMode(value);
                BoardManager boardManager = FindObjectOfType<BoardManager>();
                if (boardManager != null)
                {
                    boardManager.reducedAnimationMode = value;
                }
            });
        }
    }

    private void StartSelectedMode(GameMode mode)
    {
        PlayUiClick();
        GameManager.Instance?.StartGame(mode);
    }

    private void PlayUiClick()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.PlayUIClick();
        }
    }

    private IEnumerator ShowMessageRoutine(string text, float duration)
    {
        messageText.text = text;
        messageCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(duration);

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            messageCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.3f);
            yield return null;
        }

        messageCanvasGroup.alpha = 0f;
    }

    private IEnumerator FloatingScoreRoutine(RectTransform rect, TMP_Text text, GameObject instance)
    {
        CanvasGroup group = instance.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = instance.AddComponent<CanvasGroup>();
        }

        Vector3 start = rect.position;
        Vector3 end = start + Vector3.up * 60f;

        float elapsed = 0f;
        while (elapsed < 0.8f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.8f);
            rect.position = Vector3.Lerp(start, end, t);
            group.alpha = 1f - t;
            if (text != null)
            {
                text.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.3f), t);
            }

            yield return null;
        }

        Destroy(instance);
    }

    private void SetActive(GameObject target, bool value)
    {
        if (target != null)
        {
            target.SetActive(value);
        }
    }
}
