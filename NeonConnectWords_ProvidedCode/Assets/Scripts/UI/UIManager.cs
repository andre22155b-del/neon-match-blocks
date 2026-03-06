using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UIManager: Manages all HUD elements — scores, turn indicator, combo bar,
/// streak display, floating score pop-ups, settings panel, mode selection,
/// and game-over screen. Uses smooth easing for all transitions.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inspector References — HUD
    // -----------------------------------------------------------------------
    [Header("Score Panels")]
    public TextMeshProUGUI[] playerScoreTexts;    // one per player
    public Image[] playerScorePanels;
    public TextMeshProUGUI comboMultiplierText;
    public Slider streakBar;
    public TextMeshProUGUI streakLabel;

    [Header("Turn Indicator")]
    public TextMeshProUGUI turnIndicatorText;
    public Image turnIndicatorPanel;
    public Color[] playerTurnColors;

    [Header("Letter Display")]
    public TextMeshProUGUI currentLetterText;
    public Image currentLetterPanel;

    [Header("Timer (Timed Mode)")]
    public GameObject timerGroup;
    public TextMeshProUGUI timerText;
    public Image timerFill;

    [Header("Floating Score")]
    public GameObject floatingScorePrefab;
    public Transform floatingScoreCanvas;

    [Header("Message Banner")]
    public TextMeshProUGUI messageBannerText;
    public CanvasGroup messageBannerGroup;

    // -----------------------------------------------------------------------
    // Inspector References — Panels
    // -----------------------------------------------------------------------
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject modeSelectPanel;
    public GameObject gameHUDPanel;
    public GameObject gameOverPanel;
    public GameObject settingsPanel;
    public GameObject tutorialOverlayPanel;

    [Header("Game Over")]
    public TextMeshProUGUI gameOverTitleText;
    public TextMeshProUGUI gameOverScoresText;
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Settings")]
    public Toggle musicToggle;
    public Toggle sfxToggle;
    public Toggle colorBlindToggle;
    public Toggle reducedFXToggle;
    public Slider letterSizeSlider;

    // -----------------------------------------------------------------------
    // Internal
    // -----------------------------------------------------------------------
    private Camera mainCamera;

    // -----------------------------------------------------------------------
    // Unity Lifecycle
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        mainCamera = Camera.main;
    }

    private void Start()
    {
        ShowMainMenu();
        SetupSettingsCallbacks();
    }

    // -----------------------------------------------------------------------
    // Panel Management
    // -----------------------------------------------------------------------
    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(modeSelectPanel, false);
        SetPanelActive(gameHUDPanel, false);
        SetPanelActive(gameOverPanel, false);
        SetPanelActive(settingsPanel, false);
    }

    public void ShowModeSelect()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(modeSelectPanel, true);
    }

    public void ShowHUD()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(modeSelectPanel, false);
        SetPanelActive(gameHUDPanel, true);
    }

    public void ShowGameOver(int[] scores, string message)
    {
        SetPanelActive(gameOverPanel, true);
        gameOverTitleText.text = message;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < scores.Length; i++)
            sb.AppendLine($"Player {i + 1}: {scores[i]} pts");
        gameOverScoresText.text = sb.ToString();

        StartCoroutine(FadeInPanel(gameOverPanel.GetComponent<CanvasGroup>(), 0.5f));
    }

    public void ToggleSettings(bool open)
    {
        SetPanelActive(settingsPanel, open);
        if (open) StartCoroutine(SlideInPanel(settingsPanel, Vector2.right * -400f, Vector2.zero, 0.3f));
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel) panel.SetActive(active);
    }

    // -----------------------------------------------------------------------
    // HUD Updates
    // -----------------------------------------------------------------------
    public void RefreshAll(int[] scores, int currentPlayer, float combo)
    {
        for (int i = 0; i < scores.Length; i++)
            UpdateScore(i, scores[i]);
        ShowTurnIndicator(currentPlayer);
        UpdateComboMultiplier(combo);
    }

    public void UpdateScore(int playerIndex, int score)
    {
        if (playerIndex < playerScoreTexts.Length)
            StartCoroutine(AnimateScoreCount(playerScoreTexts[playerIndex], score));
    }

    public void ShowTurnIndicator(int playerIndex)
    {
        if (turnIndicatorText)
        {
            turnIndicatorText.text = $"Player {playerIndex + 1}'s Turn";
            if (playerIndex < playerTurnColors.Length)
                turnIndicatorPanel.color = playerTurnColors[playerIndex];

            StartCoroutine(PunchScale(turnIndicatorPanel.transform, 0.2f));
        }
    }

    public void SetCurrentLetter(char c, int playerIndex)
    {
        if (currentLetterText) currentLetterText.text = c.ToString();
        if (playerIndex < playerTurnColors.Length && currentLetterPanel)
            currentLetterPanel.color = playerTurnColors[playerIndex];
        StartCoroutine(PunchScale(currentLetterText.transform, 0.25f));
    }

    public void UpdateComboMultiplier(float multiplier)
    {
        if (comboMultiplierText)
        {
            comboMultiplierText.text = $"x{multiplier:F1}";
            comboMultiplierText.color = Color.Lerp(Color.white, Color.yellow, (multiplier - 1f) / 4f);
        }
    }

    public void UpdateStreakBar(int streak, int threshold)
    {
        if (streakBar) streakBar.value = (float)streak / (threshold * 2f);
        if (streakLabel) streakLabel.text = streak > 0 ? $"Streak x{streak}" : "";
    }

    public void UpdateTimerDisplay(float seconds)
    {
        if (seconds < 0) seconds = 0;
        if (timerText) timerText.text = $"{Mathf.FloorToInt(seconds / 60f):00}:{Mathf.FloorToInt(seconds % 60f):00}";
        if (timerFill) timerFill.fillAmount = seconds / GameManager.Instance.timedDuration;
        if (timerFill) timerFill.color = Color.Lerp(Color.red, Color.cyan, seconds / GameManager.Instance.timedDuration);
    }

    // -----------------------------------------------------------------------
    // Floating Score Pop-ups
    // -----------------------------------------------------------------------
    public void ShowFloatingScore(int points, Vector3 worldPos, string prefix = "")
    {
        if (!floatingScorePrefab || !floatingScoreCanvas) return;

        GameObject go = Instantiate(floatingScorePrefab, floatingScoreCanvas);
        TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.text = prefix + (prefix.Length > 0 ? " +" : "+") + points;

        // Convert world to screen to canvas
        Vector2 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        go.GetComponent<RectTransform>().position = screenPos;

        StartCoroutine(AnimateFloatingScore(go));
    }

    private IEnumerator AnimateFloatingScore(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (!cg) cg = go.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        float duration = 1.2f;
        Vector2 startPos = rt.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rt.anchoredPosition = startPos + Vector2.up * (80f * t);
            cg.alpha = 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
            yield return null;
        }

        Destroy(go);
    }

    // -----------------------------------------------------------------------
    // Message Banner
    // -----------------------------------------------------------------------
    public void ShowMessage(string msg, float duration = 2f)
    {
        StartCoroutine(MessageRoutine(msg, duration));
    }

    private IEnumerator MessageRoutine(string msg, float duration)
    {
        if (!messageBannerText || !messageBannerGroup) yield break;
        messageBannerText.text = msg;
        messageBannerGroup.alpha = 1f;
        yield return new WaitForSeconds(duration);
        float elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            messageBannerGroup.alpha = 1f - elapsed / 0.4f;
            yield return null;
        }
        messageBannerGroup.alpha = 0f;
    }

    // -----------------------------------------------------------------------
    // Settings Callbacks
    // -----------------------------------------------------------------------
    private void SetupSettingsCallbacks()
    {
        if (musicToggle) musicToggle.onValueChanged.AddListener(v => AudioManager.Instance.SetMusicEnabled(v));
        if (sfxToggle) sfxToggle.onValueChanged.AddListener(v => AudioManager.Instance.SetSFXEnabled(v));
        if (colorBlindToggle) colorBlindToggle.onValueChanged.AddListener(v => ApplyColorBlindMode(v));
        if (reducedFXToggle) reducedFXToggle.onValueChanged.AddListener(v => ParticleManager.Instance?.SetReducedFX(v));
        if (letterSizeSlider) letterSizeSlider.onValueChanged.AddListener(v => ApplyLetterSize(v));
    }

    private void ApplyColorBlindMode(bool on)
    {
        // Swap player colors to colorblind-safe palette
        if (playerTurnColors.Length >= 2)
        {
            playerTurnColors[0] = on ? new Color(0f, 0.45f, 0.7f) : new Color(0f, 1f, 1f);    // cyan vs blue
            playerTurnColors[1] = on ? new Color(0.9f, 0.6f, 0f) : new Color(1f, 0.2f, 0.8f); // orange vs pink
        }
    }

    private void ApplyLetterSize(float scale)
    {
        // Scale all letter prefab instances (broadcast to BoardManager)
        foreach (LetterTile tile in FindObjectsOfType<LetterTile>())
            tile.transform.localScale = Vector3.one * scale;
    }

    // -----------------------------------------------------------------------
    // Animation Helpers
    // -----------------------------------------------------------------------
    private IEnumerator PunchScale(Transform t, float duration)
    {
        Vector3 orig = t.localScale;
        float half = duration * 0.5f;

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(orig, orig * 1.25f, elapsed / half);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(orig * 1.25f, orig, elapsed / half);
            yield return null;
        }
        t.localScale = orig;
    }

    private IEnumerator AnimateScoreCount(TextMeshProUGUI text, int targetScore)
    {
        int current = 0;
        if (int.TryParse(text.text, out int parsed)) current = parsed;

        float elapsed = 0f;
        float duration = 0.4f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int display = Mathf.RoundToInt(Mathf.Lerp(current, targetScore, elapsed / duration));
            text.text = display.ToString();
            yield return null;
        }
        text.text = targetScore.ToString();
    }

    private IEnumerator FadeInPanel(CanvasGroup cg, float duration)
    {
        if (!cg) yield break;
        cg.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = elapsed / duration;
            yield return null;
        }
        cg.alpha = 1f;
    }

    private IEnumerator SlideInPanel(GameObject panel, Vector2 from, Vector2 to, float duration)
    {
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (!rt) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rt.anchoredPosition = Vector2.Lerp(from, to, EaseOutCubic(t));
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    // -----------------------------------------------------------------------
    // Button Handlers (wire up in Unity Inspector)
    // -----------------------------------------------------------------------
    public void OnPlayClassic() => GameManager.Instance.StartGame(GameMode.Classic);
    public void OnPlayTimed() => GameManager.Instance.StartGame(GameMode.Timed);
    public void OnPlayPuzzle() => GameManager.Instance.StartGame(GameMode.Puzzle);
    public void OnPlayTutorial() => GameManager.Instance.StartGame(GameMode.Tutorial);
    public void OnPlayAgain() => GameManager.Instance.StartGame(GameManager.Instance.ActiveMode);
    public void OnMainMenu() => GameManager.Instance.ReturnToMainMenu();
    public void OnOpenSettings() => ToggleSettings(true);
    public void OnCloseSettings() => ToggleSettings(false);
}
