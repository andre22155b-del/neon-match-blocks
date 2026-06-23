using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NeonFieldGoalUI : MonoBehaviour
{
    public event Action RestartPressed;
    public event Action ModeSelectPressed;
    public event Action<FieldGoalMode> ModeSelected;

    [Header("HUD")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI goalsText;
    public TextMeshProUGUI runStatsText;
    public TextMeshProUGUI windText;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI modeText;
    public TextMeshProUGUI kickStateText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI hintText;
    public Image kickPowerFill;
    public RectTransform kickReticle;
    public float kickReticleMaxX = 105f;
    public float kickReticleMaxY = 150f;
    public Color timerNormalColor = new Color(0.22f, 0.95f, 1f, 1f);
    public Color timerWarningColor = new Color(1f, 0.62f, 0.22f, 1f);
    public float timerWarningScale = 1.12f;
    public float goalsPulseScale = 1.14f;
    public Color kickLowColor = new Color(0.18f, 0.96f, 1f, 0.26f);
    public Color kickHighColor = new Color(1f, 0.48f, 0.22f, 0.9f);
    public Color kickPerfectColor = new Color(1f, 0.94f, 0.34f, 1f);
    public float previewPulseScale = 1.08f;
    public float statusPulseScale = 1.08f;

    [Header("Controls")]
    public CanvasGroup controlsCanvasGroup;
    public FieldGoalHoldButton moveLeftButton;
    public FieldGoalHoldButton moveRightButton;
    public KickInputController kickInputController;

    [Header("Announcements")]
    public CanvasGroup announcementGroup;
    public Image announcementBackdrop;
    public TextMeshProUGUI announcementTitleText;
    public TextMeshProUGUI announcementSubtitleText;

    [Header("Results")]
    public GameObject resultsPanel;
    public TextMeshProUGUI resultsTitleText;
    public TextMeshProUGUI resultsBodyText;
    public Button restartButton;
    public Button modeSelectButton;

    [Header("Title Screen")]
    public GameObject titleScreenPanel;
    public TextMeshProUGUI titleScreenBodyText;
    public TextMeshProUGUI titleScreenRecordsText;
    public Button arcadeRushButton;
    public Button clutchBlitzButton;

    private KickerLaneMover boundLaneMover;
    private KickInputController boundKickInput;
    private Action<bool> leftButtonHandler;
    private Action<bool> rightButtonHandler;
    private Action<float, float, bool> previewHandler;
    private Coroutine announcementRoutine;
    private float timerSecondsRemaining;
    private float goalsPulseTimer;
    private float statusPulseTimer;
    private RectTransform resultsPanelRect;
    private RectTransform titleScreenTitleRect;
    private RectTransform arcadeRushCardRect;
    private RectTransform clutchBlitzCardRect;
    private RectTransform titleGridRoot;
    private Image gameplayTopRibbon;
    private Image gameplayKickStatePlate;
    private Image gameplayBottomPlate;
    private Image gameplayBottomGlow;
    private Image titleTopGlow;
    private Image titleBottomGlow;
    private TextMeshProUGUI titleScreenTagText;
    private TextMeshProUGUI resultsCtaText;

    private void Start()
    {
        EnsureDynamicUi();

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (modeSelectButton != null)
        {
            modeSelectButton.onClick.AddListener(OnModeSelectClicked);
        }

        if (arcadeRushButton != null)
        {
            arcadeRushButton.onClick.AddListener(OnArcadeRushClicked);
        }

        if (clutchBlitzButton != null)
        {
            clutchBlitzButton.onClick.AddListener(OnClutchBlitzClicked);
        }

        if (announcementGroup != null)
        {
            announcementGroup.alpha = 0f;
            announcementGroup.gameObject.SetActive(false);
        }

        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }

        if (titleScreenPanel != null)
        {
            titleScreenPanel.SetActive(false);
        }

        SetScore(0);
        SetRunStats(0, 0, 0);
        SetMultiplier(1, 0);
        SetModeLabel(string.Empty);
        SetKickState(20, 3, false);
        SetTimer(60f);
        SetWind(0f, 0f);
        ShowStatus("Slide to line it up.");
        SetHint("Swipe up on the kick pad.");
        SetBestRecords(0, 0, 0, "STREET ROOKIE", "LOCAL BOARD EMPTY");
        SetKickPreview(0f, 0f, false);
    }

    private void Update()
    {
        UpdateTimerPresentation();
        UpdateGoalsPulse();
        UpdateStatusPulse();
        UpdateMenuPresentation();
    }

    private void OnDestroy()
    {
        UnbindControls();

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        if (modeSelectButton != null)
        {
            modeSelectButton.onClick.RemoveListener(OnModeSelectClicked);
        }

        if (arcadeRushButton != null)
        {
            arcadeRushButton.onClick.RemoveListener(OnArcadeRushClicked);
        }

        if (clutchBlitzButton != null)
        {
            clutchBlitzButton.onClick.RemoveListener(OnClutchBlitzClicked);
        }
    }

    public void BindControls(KickerLaneMover laneMover, KickInputController kickController)
    {
        UnbindControls();
        boundLaneMover = laneMover;
        boundKickInput = kickController != null ? kickController : kickInputController;
        kickInputController = boundKickInput;

        if (moveLeftButton != null && boundLaneMover != null)
        {
            leftButtonHandler = delegate(bool pressed) { boundLaneMover.SetLeftPressed(pressed); };
            moveLeftButton.HoldChanged += leftButtonHandler;
        }

        if (moveRightButton != null && boundLaneMover != null)
        {
            rightButtonHandler = delegate(bool pressed) { boundLaneMover.SetRightPressed(pressed); };
            moveRightButton.HoldChanged += rightButtonHandler;
        }

        if (boundKickInput != null)
        {
            previewHandler = SetKickPreview;
            boundKickInput.PreviewChanged += previewHandler;
        }
    }

    public void SetControlsEnabled(bool enabled)
    {
        if (controlsCanvasGroup != null)
        {
            controlsCanvasGroup.alpha = enabled ? 1f : 0.45f;
            controlsCanvasGroup.interactable = enabled;
            controlsCanvasGroup.blocksRaycasts = enabled;
        }

        if (moveLeftButton != null)
        {
            moveLeftButton.SetInteractable(enabled);
        }

        if (moveRightButton != null)
        {
            moveRightButton.SetInteractable(enabled);
        }

        if (kickInputController != null)
        {
            kickInputController.SetInputEnabled(enabled);
        }
    }

    public void SetTimer(float secondsRemaining)
    {
        timerSecondsRemaining = Mathf.Max(0f, secondsRemaining);
        if (timerText == null)
        {
            return;
        }

        int wholeSeconds = Mathf.CeilToInt(timerSecondsRemaining);
        int minutes = wholeSeconds / 60;
        int seconds = wholeSeconds % 60;
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void SetWind(float direction01, float strength01)
    {
        if (windText == null)
        {
            return;
        }

        windText.text = FieldGoalWindMath.GetHudLabel(direction01, strength01);
        windText.color = strength01 >= 0.66f
            ? new Color(1f, 0.84f, 0.42f, 0.96f)
            : strength01 > 0.02f
                ? new Color(0.74f, 0.96f, 1f, 0.94f)
                : new Color(0.66f, 0.88f, 0.92f, 0.8f);
    }

    public void SetGoals(int goals)
    {
        SetRunStats(goals, 0, 0);
    }

    public void SetScore(int score)
    {
        if (goalsText != null)
        {
            goalsText.text = "SCORE " + score.ToString("N0");
        }
    }

    public void SetRunStats(int goals, int longestMadeKick)
    {
        SetRunStats(goals, longestMadeKick, 0);
    }

    public void SetRunStats(int goals, int longestMadeKick, int bestStreak)
    {
        if (runStatsText == null)
        {
            return;
        }

        string longestLabel = longestMadeKick > 0 ? longestMadeKick + " YD" : "--";
        runStatsText.text = "GOALS " + goals.ToString("N0") + "  |  LONGEST " + longestLabel + "  |  BEST HEAT " + Mathf.Max(0, bestStreak);
    }

    public void SetMultiplier(int multiplier, int streak)
    {
        if (multiplierText == null)
        {
            return;
        }

        string label;
        if (multiplier <= 1)
        {
            label = "x1 READY";
        }
        else if (streak <= 0)
        {
            label = "x" + multiplier + " LIVE";
        }
        else
        {
            label = "x" + multiplier + " HEAT";
        }

        if (streak > 0)
        {
            label += "  |  STREAK " + streak;
        }

        multiplierText.text = label;
    }

    public void SetModeLabel(string modeLabel)
    {
        if (modeText != null)
        {
            modeText.text = modeLabel;
        }
    }

    public void SetKickState(int yardLine, int basePoints, bool movingGoalActive)
    {
        if (kickStateText == null)
        {
            return;
        }

        string pointLabel = basePoints >= 5 ? "5 PT BANGER" : basePoints + " PTS";
        string movementLabel = movingGoalActive ? "SWAYING GOAL" : "SET UPRIGHTS";
        kickStateText.text = yardLine + " YD  |  " + pointLabel + "  |  " + movementLabel;
    }

    public void ShowStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public void SetHint(string message)
    {
        if (hintText != null)
        {
            hintText.text = message;
        }
    }

    public void SetBestRecords(int bestScore, int bestLongestKick)
    {
        SetBestRecords(bestScore, bestLongestKick, 0, "STREET ROOKIE", "LOCAL BOARD EMPTY");
    }

    public void SetBestRecords(int bestScore, int bestLongestKick, int bestStreak, string rewardLabel, string boardLabel)
    {
        if (titleScreenRecordsText == null)
        {
            return;
        }

        string longestLabel = bestLongestKick > 0 ? bestLongestKick + " YD" : "--";
        titleScreenRecordsText.text =
            "BEST SCORE " + bestScore.ToString("N0") + "  |  BEST BANGER " + longestLabel + "  |  BEST HEAT " + Mathf.Max(0, bestStreak) +
            "\n" +
            rewardLabel + "  |  " + boardLabel;
    }

    public void ShowSpotlight(string title, string subtitle, Color color)
    {
        ShowAnnouncement(title, subtitle, color);
    }

    public void ShowItsGood()
    {
        ShowAnnouncement(
            "IT'S GOOD",
            "Goal lights up. Crowd goes nuclear.",
            new Color(0.2f, 0.95f, 1f, 0.95f));
    }

    public void ShowModeIntro(string title, string subtitle)
    {
        ShowAnnouncement(title, subtitle, new Color(1f, 0.94f, 0.34f, 0.95f));
    }

    public void ShowFinalDrive()
    {
        ShowAnnouncement(
            "FINAL DRIVE",
            "Ten seconds. Run the score up.",
            new Color(1f, 0.62f, 0.22f, 0.95f));
    }

    public void ShowGoalBurst(int pointsAwarded, int multiplier, bool perfectKick, bool clutchActive, int yardLine, bool longBomb)
    {
        string title = "IT'S GOOD";
        if (longBomb)
        {
            title = yardLine + " YD BANGER";
        }
        else if (perfectKick && clutchActive)
        {
            title = "CLUTCH LASER";
        }
        else if (perfectKick)
        {
            title = "PURE STRIKE";
        }
        else if (multiplier >= 3)
        {
            title = "HEAT CHECK";
        }

        Color color = clutchActive || perfectKick ? kickPerfectColor : kickHighColor;
        string subtitle = "+" + pointsAwarded + " PTS  |  x" + multiplier + " MULTI";
        if (longBomb)
        {
            subtitle += "  |  STREET CASH";
        }
        else if (perfectKick)
        {
            subtitle += "  |  CLEAN";
        }

        ShowAnnouncement(
            title,
            subtitle,
            color);
    }

    public void PulseGoals()
    {
        goalsPulseTimer = 0.32f;
    }

    public void PulseStatus()
    {
        statusPulseTimer = 0.26f;
    }

    public void ShowPerfectStrike()
    {
        ShowStatus("PURE STRIKE.");
        PulseStatus();
    }

    public void ShowResults(int goals)
    {
        ShowResults(goals, goals, 0, 1, 0, string.Empty, string.Empty);
    }

    public void ShowResults(int score, int goals, int longestMadeKick, int bestMultiplier, string modeLabel)
    {
        ShowResults(score, goals, longestMadeKick, bestMultiplier, 0, modeLabel, string.Empty);
    }

    public void ShowResults(int score, int goals, int longestMadeKick, int bestMultiplier, int bestStreak, string modeLabel, string progressionSummary)
    {
        HideAnnouncementImmediate();

        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
        }

        if (resultsTitleText != null)
        {
            resultsTitleText.text = GetResultsHeadline(score, bestMultiplier, longestMadeKick);
        }

        if (resultsBodyText != null)
        {
            string modeLine = string.IsNullOrEmpty(modeLabel) ? string.Empty : modeLabel + "\n";
            string longestKickLabel = longestMadeKick > 0 ? longestMadeKick + " YD" : "--";
            resultsBodyText.text = modeLine +
                "Score " + score.ToString("N0") +
                "  |  Goals " + goals.ToString("N0") +
                "  |  Longest " + longestKickLabel +
                "  |  Heat " + Mathf.Max(0, bestStreak) +
                "\n50+ YD bangers are worth 5. Misses stay fast. The moving goal stays live while the clock cooks." +
                (string.IsNullOrEmpty(progressionSummary) ? string.Empty : "\n" + progressionSummary);
        }

        if (resultsCtaText != null)
        {
            resultsCtaText.text = GetResultsCta(score, bestMultiplier, longestMadeKick);
        }
    }

    public void HideResults()
    {
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }
    }

    public void ShowTitleScreen()
    {
        HideAnnouncementImmediate();
        UpdateTitleScreenCopy();

        if (titleScreenPanel != null)
        {
            titleScreenPanel.SetActive(true);
        }

        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }
    }

    public void HideTitleScreen()
    {
        if (titleScreenPanel != null)
        {
            titleScreenPanel.SetActive(false);
        }
    }

    public void SetKickPreview(float power, float aim, bool active)
    {
        float clampedPower = Mathf.Clamp01(power);

        if (kickPowerFill != null)
        {
            kickPowerFill.fillAmount = active ? clampedPower : 0f;
            if (!active)
            {
                kickPowerFill.color = kickLowColor;
            }
            else
            {
                Color fillColor = Color.Lerp(kickLowColor, kickHighColor, clampedPower);
                if (clampedPower >= 0.8f)
                {
                    fillColor = Color.Lerp(fillColor, kickPerfectColor, Mathf.InverseLerp(0.8f, 1f, clampedPower));
                }

                kickPowerFill.color = fillColor;
            }
        }

        if (kickReticle != null)
        {
            kickReticle.anchoredPosition = active
                ? new Vector2(Mathf.Clamp(aim, -1f, 1f) * kickReticleMaxX, clampedPower * kickReticleMaxY)
                : Vector2.zero;
            float pulseScale = active ? Mathf.Lerp(1f, previewPulseScale, clampedPower) : 1f;
            if (active && clampedPower >= 0.78f)
            {
                pulseScale *= 1f + Mathf.Sin(Time.unscaledTime * 12f) * 0.05f;
            }

            kickReticle.localScale = Vector3.one * pulseScale;

            Image reticleImage = kickReticle.GetComponent<Image>();
            if (reticleImage != null)
            {
                reticleImage.color = active
                    ? Color.Lerp(Color.white, kickPerfectColor, Mathf.InverseLerp(0.72f, 1f, clampedPower))
                    : Color.white;
            }
        }
    }

    private void UnbindControls()
    {
        if (moveLeftButton != null && leftButtonHandler != null)
        {
            moveLeftButton.HoldChanged -= leftButtonHandler;
        }

        if (moveRightButton != null && rightButtonHandler != null)
        {
            moveRightButton.HoldChanged -= rightButtonHandler;
        }

        if (boundKickInput != null && previewHandler != null)
        {
            boundKickInput.PreviewChanged -= previewHandler;
        }

        leftButtonHandler = null;
        rightButtonHandler = null;
        previewHandler = null;
    }

    private void UpdateTimerPresentation()
    {
        if (timerText == null)
        {
            return;
        }

        if (timerSecondsRemaining <= 10f)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
            timerText.color = Color.Lerp(timerNormalColor, timerWarningColor, pulse);
            timerText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, timerWarningScale, pulse);
            return;
        }

        timerText.color = timerNormalColor;
        timerText.rectTransform.localScale = Vector3.one;
    }

    private void UpdateGoalsPulse()
    {
        if (goalsText == null)
        {
            return;
        }

        if (goalsPulseTimer <= 0f)
        {
            goalsText.rectTransform.localScale = Vector3.one;
            return;
        }

        goalsPulseTimer -= Time.unscaledDeltaTime;
        float t = 1f - Mathf.Clamp01(goalsPulseTimer / 0.32f);
        float curve = Mathf.Sin(t * Mathf.PI);
        goalsText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, goalsPulseScale, curve);
    }

    private void UpdateStatusPulse()
    {
        if (statusText == null)
        {
            return;
        }

        if (statusPulseTimer <= 0f)
        {
            statusText.rectTransform.localScale = Vector3.one;
            return;
        }

        statusPulseTimer -= Time.unscaledDeltaTime;
        float t = 1f - Mathf.Clamp01(statusPulseTimer / 0.26f);
        float curve = Mathf.Sin(t * Mathf.PI);
        statusText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, statusPulseScale, curve);
    }

    private void UpdateMenuPresentation()
    {
        float time = Time.unscaledTime;

        if (titleScreenPanel != null && titleScreenPanel.activeSelf)
        {
            AnimateMenuCard(titleScreenTitleRect, time, 0f, 1f, 1.028f, 0.55f);
            AnimateMenuCard(arcadeRushCardRect, time, 0.12f, 1f, 1.03f, 1.15f);
            AnimateMenuCard(clutchBlitzCardRect, time, 0.54f, 1f, 1.035f, 1.35f);

            if (titleTopGlow != null)
            {
                Color topColor = titleTopGlow.color;
                topColor.a = 0.17f + Mathf.Abs(Mathf.Sin(time * 1.05f)) * 0.08f;
                titleTopGlow.color = topColor;
            }

            if (titleBottomGlow != null)
            {
                Color bottomColor = titleBottomGlow.color;
                bottomColor.a = 0.12f + Mathf.Abs(Mathf.Sin(time * 1.28f + 0.4f)) * 0.08f;
                titleBottomGlow.color = bottomColor;
            }

            if (titleGridRoot != null)
            {
                titleGridRoot.localScale = Vector3.one * (1f + Mathf.Sin(time * 0.45f) * 0.01f);
            }

            return;
        }

        ResetMenuRect(titleScreenTitleRect);
        ResetMenuRect(arcadeRushCardRect);
        ResetMenuRect(clutchBlitzCardRect);

        if (resultsPanel != null && resultsPanel.activeSelf)
        {
            AnimateMenuCard(resultsPanelRect, time, 0.18f, 1f, 1.018f, 0.85f);
            return;
        }

        ResetMenuRect(resultsPanelRect);
    }

    private void ShowAnnouncement(string title, string subtitle, Color color)
    {
        if (announcementRoutine != null)
        {
            StopCoroutine(announcementRoutine);
        }

        announcementRoutine = StartCoroutine(AnnouncementRoutine(title, subtitle, color));
    }

    private IEnumerator AnnouncementRoutine(string title, string subtitle, Color color)
    {
        if (announcementGroup == null)
        {
            yield break;
        }

        announcementGroup.gameObject.SetActive(true);
        announcementGroup.alpha = 1f;
        announcementGroup.transform.localScale = Vector3.one * 0.9f;

        if (announcementBackdrop != null)
        {
            Color bg = color;
            bg.a = 0.24f;
            announcementBackdrop.color = bg;
        }

        if (announcementTitleText != null)
        {
            announcementTitleText.text = title;
            announcementTitleText.color = color;
        }

        if (announcementSubtitleText != null)
        {
            announcementSubtitleText.text = subtitle;
        }

        float introTime = 0.16f;
        while (introTime > 0f)
        {
            introTime -= Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(introTime / 0.16f);
            announcementGroup.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1.05f, t);
            yield return null;
        }

        float visibleDuration = 1.1f;
        while (visibleDuration > 0f)
        {
            visibleDuration -= Time.unscaledDeltaTime;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 11f) * 0.015f;
            announcementGroup.transform.localScale = Vector3.one * pulse;
            yield return null;
        }

        float fadeTime = 0.25f;
        float startAlpha = announcementGroup.alpha;
        while (fadeTime > 0f)
        {
            fadeTime -= Time.unscaledDeltaTime;
            announcementGroup.alpha = Mathf.Lerp(0f, startAlpha, fadeTime / 0.25f);
            announcementGroup.transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, fadeTime / 0.25f);
            yield return null;
        }

        announcementGroup.alpha = 0f;
        announcementGroup.transform.localScale = Vector3.one;
        announcementGroup.gameObject.SetActive(false);
        announcementRoutine = null;
    }

    private void OnRestartClicked()
    {
        RestartPressed?.Invoke();
    }

    private void OnModeSelectClicked()
    {
        ModeSelectPressed?.Invoke();
    }

    private void OnArcadeRushClicked()
    {
        ModeSelected?.Invoke(FieldGoalMode.ArcadeRush);
    }

    private void OnClutchBlitzClicked()
    {
        ModeSelected?.Invoke(FieldGoalMode.ClutchBlitz);
    }

    private void EnsureDynamicUi()
    {
        TextMeshProUGUI template = timerText != null ? timerText : goalsText;
        if (template == null)
        {
            return;
        }

        EnsureGameplayHudBackdrop();

        if (goalsText != null)
        {
            RectTransform scoreRect = goalsText.rectTransform;
            scoreRect.anchorMin = new Vector2(0.5f, 0.68f);
            scoreRect.anchorMax = new Vector2(0.5f, 0.68f);
            scoreRect.sizeDelta = new Vector2(280f, 48f);
            scoreRect.anchoredPosition = new Vector2(0f, 10f);
            scoreRect.localScale = Vector3.one;
        }

        if (runStatsText == null && goalsText != null)
        {
            runStatsText = CreateRuntimeText(
                "RunStatsText",
                goalsText.transform.parent,
                "GOALS 0  |  LONGEST --",
                20,
                new Vector2(0.5f, 0.26f),
                new Vector2(0.5f, 0.26f),
                new Vector2(300f, 28f),
                Vector2.zero,
                template);
            runStatsText.color = new Color(0.78f, 0.95f, 1f, 0.92f);
        }

        if (timerText != null)
        {
            RectTransform timerPanelRect = timerText.transform.parent as RectTransform;
            if (timerPanelRect != null && timerPanelRect.name == "TimerPanel")
            {
                timerPanelRect.anchorMin = new Vector2(1f, 1f);
                timerPanelRect.anchorMax = new Vector2(1f, 1f);
                timerPanelRect.sizeDelta = new Vector2(300f, 138f);
                timerPanelRect.anchoredPosition = new Vector2(-216f, -102f);
                timerPanelRect.localScale = Vector3.one;
            }

            RectTransform timerRect = timerText.rectTransform;
            timerRect.anchorMin = new Vector2(0.5f, 0.66f);
            timerRect.anchorMax = new Vector2(0.5f, 0.66f);
            timerRect.sizeDelta = new Vector2(240f, 48f);
            timerRect.anchoredPosition = new Vector2(0f, 6f);
            timerRect.localScale = Vector3.one;
        }

        if (windText == null && timerText != null)
        {
            windText = CreateRuntimeText(
                "WindText",
                timerText.transform.parent,
                "WIND CALM",
                18,
                new Vector2(0.5f, 0.22f),
                new Vector2(0.5f, 0.22f),
                new Vector2(240f, 26f),
                Vector2.zero,
                template);
            windText.color = new Color(0.66f, 0.88f, 0.92f, 0.8f);
        }
        else if (windText != null)
        {
            windText.fontSize = 18;
            windText.alignment = TextAlignmentOptions.Center;
            RectTransform windRect = windText.rectTransform;
            windRect.anchorMin = new Vector2(0.5f, 0.22f);
            windRect.anchorMax = new Vector2(0.5f, 0.22f);
            windRect.sizeDelta = new Vector2(240f, 26f);
            windRect.anchoredPosition = Vector2.zero;
            windRect.localScale = Vector3.one;
        }

        if (multiplierText == null && goalsText != null)
        {
            multiplierText = CreateRuntimeText(
                "MultiplierText",
                goalsText.transform.parent,
                "x1 READY",
                22,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(260f, 34f),
                new Vector2(0f, -18f),
                template);
            multiplierText.color = new Color(1f, 0.94f, 0.34f, 0.96f);
        }

        if (modeText == null)
        {
            modeText = CreateRuntimeText(
                "ModeText",
                transform,
                string.Empty,
                28,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(520f, 42f),
                new Vector2(0f, -166f),
                template);
            modeText.color = new Color(1f, 0.94f, 0.34f, 0.92f);
        }
        if (modeText != null)
        {
            modeText.fontSize = 20;
            modeText.alignment = TextAlignmentOptions.Center;
            RectTransform modeRect = modeText.rectTransform;
            modeRect.anchorMin = new Vector2(0.5f, 1f);
            modeRect.anchorMax = new Vector2(0.5f, 1f);
            modeRect.sizeDelta = new Vector2(560f, 32f);
            modeRect.anchoredPosition = new Vector2(0f, -102f);
            modeRect.localScale = Vector3.one;
        }

        if (kickStateText == null)
        {
            kickStateText = CreateRuntimeText(
                "KickStateText",
                transform,
                "20 YD  |  3 PTS  |  SET UPRIGHTS",
                20,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(760f, 30f),
                new Vector2(0f, -198f),
                template);
            kickStateText.color = new Color(0.80f, 0.98f, 0.72f, 0.95f);
        }
        if (kickStateText != null)
        {
            kickStateText.fontSize = 18;
            kickStateText.alignment = TextAlignmentOptions.Center;
            RectTransform kickStateRect = kickStateText.rectTransform;
            kickStateRect.anchorMin = new Vector2(0.5f, 1f);
            kickStateRect.anchorMax = new Vector2(0.5f, 1f);
            kickStateRect.sizeDelta = new Vector2(860f, 28f);
            kickStateRect.anchoredPosition = new Vector2(0f, -136f);
            kickStateRect.localScale = Vector3.one;
        }

        if (statusText != null)
        {
            statusText.fontSize = 34;
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0.5f, 0f);
            statusRect.anchorMax = new Vector2(0.5f, 0f);
            statusRect.sizeDelta = new Vector2(1180f, 48f);
            statusRect.anchoredPosition = new Vector2(0f, 316f);
            statusRect.localScale = Vector3.one;
        }

        if (hintText != null)
        {
            hintText.fontSize = 24;
            RectTransform hintRect = hintText.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(1260f, 34f);
            hintRect.anchoredPosition = new Vector2(0f, 274f);
            hintRect.localScale = Vector3.one;
        }

        if (resultsPanel != null && modeSelectButton == null)
        {
            if (restartButton != null)
            {
                restartButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-170f, -160f);
            }

            modeSelectButton = CreateRuntimeButton(
                "ModeSelectButton",
                resultsPanel.transform,
                "MODES",
                new Vector2(280f, 96f),
                new Vector2(170f, -160f),
                template);
        }

        if (resultsPanel != null)
        {
            resultsPanelRect = resultsPanel.GetComponent<RectTransform>();
            if (resultsBodyText != null)
            {
                resultsBodyText.fontSize = 28;
                RectTransform bodyRect = resultsBodyText.rectTransform;
                bodyRect.anchorMin = new Vector2(0.5f, 0.48f);
                bodyRect.anchorMax = new Vector2(0.5f, 0.48f);
                bodyRect.sizeDelta = new Vector2(800f, 220f);
                bodyRect.anchoredPosition = Vector2.zero;
            }

            if (resultsCtaText == null)
            {
                resultsCtaText = CreateRuntimeText(
                    "ResultsCtaText",
                    resultsPanel.transform,
                    "Lock in and run it back.",
                    22,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(760f, 44f),
                    new Vector2(0f, 58f),
                    template);
                resultsCtaText.color = new Color(1f, 0.92f, 0.34f, 0.95f);
            }
        }

        if (titleScreenPanel == null)
        {
            titleScreenPanel = CreateRuntimePanel("TitleScreenPanel", transform, new Color(0.02f, 0.04f, 0.08f, 0.96f));
            StretchRect(titleScreenPanel.GetComponent<RectTransform>());
        }

        Transform titleRoot = titleScreenPanel != null ? titleScreenPanel.transform : null;
        if (titleRoot == null)
        {
            return;
        }

        EnsureTitleBackdrop(titleRoot, template);

        if (titleRoot.Find("TitleScreenTitle") == null)
        {
            TextMeshProUGUI createdTitle = CreateRuntimeText(
                "TitleScreenTitle",
                titleRoot,
                "NEON FIELDGOAL",
                82,
                new Vector2(0.5f, 0.74f),
                new Vector2(0.5f, 0.74f),
                new Vector2(900f, 100f),
                Vector2.zero,
                template);
            createdTitle.color = kickPerfectColor;
            titleScreenTitleRect = createdTitle.rectTransform;
        }
        else if (titleScreenTitleRect == null)
        {
            Transform existingTitle = titleRoot.Find("TitleScreenTitle");
            if (existingTitle != null)
            {
                titleScreenTitleRect = existingTitle.GetComponent<RectTransform>();
            }
        }

        if (titleScreenBodyText == null)
        {
            titleScreenBodyText = CreateRuntimeText(
                "TitleScreenBodyText",
                titleRoot,
                "Pick your mode and start stacking local legend runs.",
                30,
                new Vector2(0.5f, 0.6f),
                new Vector2(0.5f, 0.6f),
                new Vector2(980f, 120f),
                Vector2.zero,
                template);
            titleScreenBodyText.color = new Color(0.88f, 0.96f, 1f, 0.94f);
        }

        if (titleScreenBodyText != null)
        {
            titleScreenBodyText.fontSize = 30;
            titleScreenBodyText.alignment = TextAlignmentOptions.Center;
        }

        if (titleScreenRecordsText == null)
        {
            titleScreenRecordsText = CreateRuntimeText(
                "TitleScreenRecordsText",
                titleRoot,
                "BEST SCORE 0  |  BEST BANGER --  |  BEST HEAT 0\nSTREET ROOKIE  |  LOCAL BOARD EMPTY",
                24,
                new Vector2(0.5f, 0.52f),
                new Vector2(0.5f, 0.52f),
                new Vector2(980f, 70f),
                Vector2.zero,
                template);
            titleScreenRecordsText.color = new Color(1f, 0.92f, 0.34f, 0.92f);
        }
        if (titleScreenRecordsText != null)
        {
            titleScreenRecordsText.fontSize = 22;
            titleScreenRecordsText.alignment = TextAlignmentOptions.Center;
            RectTransform recordsRect = titleScreenRecordsText.rectTransform;
            recordsRect.anchorMin = new Vector2(0.5f, 0.52f);
            recordsRect.anchorMax = new Vector2(0.5f, 0.52f);
            recordsRect.sizeDelta = new Vector2(1080f, 72f);
            recordsRect.anchoredPosition = Vector2.zero;
        }

        if (arcadeRushButton == null)
        {
            arcadeRushButton = CreateRuntimeButton(
                "ArcadeRushButton",
                titleRoot,
                "SCORE ATTACK",
                new Vector2(360f, 110f),
                new Vector2(0f, -40f),
                template,
                new Vector2(0.5f, 0.5f));
        }
        arcadeRushCardRect = arcadeRushButton != null ? arcadeRushButton.GetComponent<RectTransform>() : arcadeRushCardRect;

        if (arcadeRushButton.transform.Find("ArcadeRushHint") == null)
        {
            CreateRuntimeText(
                "ArcadeRushHint",
                arcadeRushButton.transform,
                "60 seconds. Pure score chase with unlocks.",
                20,
                new Vector2(0.5f, 0.18f),
                new Vector2(0.5f, 0.18f),
                new Vector2(310f, 26f),
                Vector2.zero,
                template).color = new Color(0.86f, 0.94f, 1f, 0.9f);
        }
        PromoteLegacyHintToFooter(arcadeRushButton.transform, "ArcadeRushHint");

        if (clutchBlitzButton == null)
        {
            clutchBlitzButton = CreateRuntimeButton(
                "ClutchBlitzButton",
                titleRoot,
                "CLUTCH BLITZ",
                new Vector2(360f, 110f),
                new Vector2(0f, -180f),
                template,
                new Vector2(0.5f, 0.5f));
        }
        clutchBlitzCardRect = clutchBlitzButton != null ? clutchBlitzButton.GetComponent<RectTransform>() : clutchBlitzCardRect;

        if (clutchBlitzButton.transform.Find("ClutchBlitzHint") == null)
        {
            CreateRuntimeText(
                "ClutchBlitzHint",
                clutchBlitzButton.transform,
                "35 seconds. Every kick is pressure.",
                20,
                new Vector2(0.5f, 0.18f),
                new Vector2(0.5f, 0.18f),
                new Vector2(310f, 26f),
                Vector2.zero,
                template).color = new Color(0.86f, 0.94f, 1f, 0.9f);
        }
        PromoteLegacyHintToFooter(clutchBlitzButton.transform, "ClutchBlitzHint");

        ConfigureModeCard(
            arcadeRushButton,
            template,
            "SCORE ATTACK",
            "CHASE THE BOARD",
            "60 SEC // STACK HEAT // UNLOCK REWARDS",
            new Color(0.06f, 0.16f, 0.3f, 0.96f),
            new Color(0.22f, 0.95f, 1f, 1f));
        ConfigureModeCard(
            clutchBlitzButton,
            template,
            "CLUTCH BLITZ",
            "PRESSURE ON",
            "35 SEC // SWAYING GOAL // STREET CASH",
            new Color(0.18f, 0.08f, 0.24f, 0.96f),
            new Color(1f, 0.48f, 0.84f, 1f));
        ConfigureActionButton(
            restartButton,
            template,
            "RUN IT BACK",
            "Jump straight into another heater.",
            new Color(0.11f, 0.2f, 0.33f, 0.96f),
            kickPerfectColor);
        ConfigureActionButton(
            modeSelectButton,
            template,
            "SWITCH MODES",
            "Flip the vibe and chase a new run.",
            new Color(0.14f, 0.12f, 0.24f, 0.96f),
            new Color(0.22f, 0.95f, 1f, 1f));
    }

    private void UpdateTitleScreenCopy()
    {
        if (titleScreenBodyText != null)
        {
            titleScreenBodyText.text = "Arcade-crazy score attack. Build heat, unlock stronger kick rewards, and stack local legend runs while the bars pulse and the uprights sway.";
        }

        if (titleScreenTagText != null)
        {
            titleScreenTagText.text = "STREET HYPE // FUTURE TURF // LOCAL LEGENDS";
        }
    }

    private void HideAnnouncementImmediate()
    {
        if (announcementRoutine != null)
        {
            StopCoroutine(announcementRoutine);
            announcementRoutine = null;
        }

        if (announcementGroup != null)
        {
            announcementGroup.alpha = 0f;
            announcementGroup.transform.localScale = Vector3.one;
            announcementGroup.gameObject.SetActive(false);
        }
    }

    private string GetResultsHeadline(int score, int bestMultiplier, int longestMadeKick)
    {
        if (longestMadeKick >= 55)
        {
            return "LONG BOMB";
        }

        if (score >= 60 || bestMultiplier >= 5)
        {
            return "CITY LIGHTS";
        }

        if (score >= 36 || bestMultiplier >= 4)
        {
            return "HEAT CHECK";
        }

        if (score >= 18 || bestMultiplier >= 3)
        {
            return "CLEAN RUN";
        }

        return "TIME'S UP";
    }

    private string GetResultsCta(int score, int bestMultiplier, int longestMadeKick)
    {
        if (longestMadeKick >= 50)
        {
            return "That banger had juice. Run it back and chase an even deeper bomb.";
        }

        if (score >= 60 || bestMultiplier >= 5)
        {
            return "That run was glowing. Smash RUN IT BACK and break the skyline.";
        }

        if (bestMultiplier >= 3)
        {
            return "You found heat. One cleaner streak and the score pops off.";
        }

        return "One hot streak flips the whole board. Smash RUN IT BACK.";
    }

    private void AnimateMenuCard(RectTransform rect, float time, float phase, float minScale, float maxScale, float rotationAmount)
    {
        if (rect == null)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(time * 1.8f + phase * Mathf.PI * 2f);
        float scale = Mathf.Lerp(minScale, maxScale, pulse);
        rect.localScale = Vector3.one * scale;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.2f + phase * 5f) * rotationAmount);
    }

    private void ResetMenuRect(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void EnsureTitleBackdrop(Transform titleRoot, TextMeshProUGUI template)
    {
        if (titleRoot == null)
        {
            return;
        }

        if (titleTopGlow == null)
        {
            titleTopGlow = EnsureChildImage(
                titleRoot,
                "TitleTopGlow",
                new Color(0.1f, 0.42f, 0.7f, 0.22f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(1240f, 440f),
                new Vector2(0f, -90f));
            titleTopGlow.transform.SetAsFirstSibling();
        }

        if (titleBottomGlow == null)
        {
            titleBottomGlow = EnsureChildImage(
                titleRoot,
                "TitleBottomGlow",
                new Color(0.92f, 0.2f, 0.68f, 0.16f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(1320f, 360f),
                new Vector2(0f, 110f));
            titleBottomGlow.transform.SetAsFirstSibling();
        }

        if (titleGridRoot == null)
        {
            GameObject gridRoot = new GameObject("TitleGridRoot", typeof(RectTransform));
            gridRoot.transform.SetParent(titleRoot, false);
            titleGridRoot = gridRoot.GetComponent<RectTransform>();
            titleGridRoot.anchorMin = new Vector2(0.5f, 0f);
            titleGridRoot.anchorMax = new Vector2(0.5f, 0f);
            titleGridRoot.sizeDelta = new Vector2(1180f, 260f);
            titleGridRoot.anchoredPosition = new Vector2(0f, 82f);
            titleGridRoot.localScale = Vector3.one;
            titleGridRoot.SetAsFirstSibling();

            for (int i = 0; i < 7; i++)
            {
                float y = -112f + i * 34f;
                EnsureChildImage(
                    titleGridRoot,
                    "GridH" + i,
                    new Color(0.22f, 0.95f, 1f, i == 0 ? 0.34f : 0.14f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(1060f - i * 100f, 2f),
                    new Vector2(0f, y));
            }

            for (int i = 0; i < 6; i++)
            {
                float x = -360f + i * 144f;
                EnsureChildImage(
                    titleGridRoot,
                    "GridV" + i,
                    new Color(0.22f, 0.95f, 1f, 0.08f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(2f, 184f - Mathf.Abs(i - 2) * 14f),
                    new Vector2(x, -26f));
            }
        }

        if (titleScreenTagText == null)
        {
            titleScreenTagText = CreateRuntimeText(
                "TitleScreenTagText",
                titleRoot,
                "STREET HYPE // FUTURE TURF // SOLO RUN",
                22,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(760f, 34f),
                new Vector2(0f, -42f),
                template);
            titleScreenTagText.color = new Color(0.22f, 0.95f, 1f, 0.88f);
            titleScreenTagText.characterSpacing = 2f;
        }
    }

    private void EnsureGameplayHudBackdrop()
    {
        gameplayTopRibbon = EnsureChildImage(
            transform,
            "GameplayTopRibbon",
            new Color(0.03f, 0.06f, 0.12f, 0.72f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(1560f, 110f),
            new Vector2(0f, -112f));
        if (gameplayTopRibbon != null)
        {
            gameplayTopRibbon.transform.SetAsFirstSibling();
        }

        gameplayKickStatePlate = EnsureChildImage(
            transform,
            "GameplayKickStatePlate",
            new Color(0.02f, 0.09f, 0.08f, 0.72f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(940f, 66f),
            new Vector2(0f, -122f));
        if (gameplayKickStatePlate != null)
        {
            gameplayKickStatePlate.transform.SetAsFirstSibling();
        }

        gameplayBottomPlate = EnsureChildImage(
            transform,
            "GameplayBottomPlate",
            new Color(0.03f, 0.06f, 0.12f, 0.76f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(1320f, 118f),
            new Vector2(0f, 294f));
        if (gameplayBottomPlate != null)
        {
            gameplayBottomPlate.transform.SetAsFirstSibling();
        }

        gameplayBottomGlow = EnsureChildImage(
            transform,
            "GameplayBottomGlow",
            new Color(0.22f, 0.95f, 1f, 0.12f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(980f, 28f),
            new Vector2(0f, 334f));
        if (gameplayBottomGlow != null)
        {
            gameplayBottomGlow.transform.SetAsFirstSibling();
        }

        Transform existingTitlePanel = transform.Find("TitlePanel");
        if (existingTitlePanel != null)
        {
            RectTransform titlePanelRect = existingTitlePanel.GetComponent<RectTransform>();
            if (titlePanelRect != null)
            {
                titlePanelRect.anchorMin = new Vector2(0.5f, 1f);
                titlePanelRect.anchorMax = new Vector2(0.5f, 1f);
                titlePanelRect.sizeDelta = new Vector2(440f, 68f);
                titlePanelRect.anchoredPosition = new Vector2(0f, -58f);
                titlePanelRect.localScale = Vector3.one;
            }

            Image titlePanelImage = existingTitlePanel.GetComponent<Image>();
            if (titlePanelImage != null)
            {
                titlePanelImage.color = new Color(0.04f, 0.08f, 0.14f, 0.82f);
            }

            Transform existingTitleText = existingTitlePanel.Find("TitleText");
            if (existingTitleText != null)
            {
                TextMeshProUGUI titleText = existingTitleText.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    titleText.fontSize = 28;
                    titleText.characterSpacing = 3f;
                    titleText.color = kickPerfectColor;
                }
            }
        }
    }

    private void ConfigureModeCard(
        Button button,
        TextMeshProUGUI template,
        string label,
        string eyebrow,
        string footer,
        Color baseColor,
        Color accentColor)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(420f, 132f);
        }

        Image background = button.GetComponent<Image>();
        if (background != null)
        {
            background.color = baseColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, accentColor, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.18f);
        button.colors = colors;

        EnsureChildImage(
            button.transform,
            "AccentBar",
            accentColor,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(356f, 5f),
            new Vector2(0f, -12f));
        EnsureChildImage(
            button.transform,
            "InnerGlow",
            new Color(accentColor.r, accentColor.g, accentColor.b, 0.13f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(382f, 98f),
            Vector2.zero);

        TextMeshProUGUI labelText = EnsureButtonText(
            button.transform,
            "Label",
            label,
            30,
            new Vector2(0.5f, 0.62f),
            new Vector2(0.5f, 0.62f),
            new Vector2(320f, 42f),
            Vector2.zero,
            template);
        labelText.text = label;
        labelText.color = Color.white;
        labelText.fontStyle = FontStyles.Bold;
        labelText.characterSpacing = 2.5f;

        TextMeshProUGUI eyebrowText = EnsureButtonText(
            button.transform,
            "Eyebrow",
            eyebrow,
            16,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(320f, 24f),
            new Vector2(0f, -28f),
            template);
        eyebrowText.text = eyebrow;
        eyebrowText.color = accentColor;
        eyebrowText.characterSpacing = 2.6f;

        TextMeshProUGUI footerText = EnsureButtonText(
            button.transform,
            "Footer",
            footer,
            18,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(348f, 36f),
            new Vector2(0f, 18f),
            template);
        footerText.text = footer;
        footerText.color = new Color(0.86f, 0.95f, 1f, 0.88f);
        footerText.characterSpacing = 0.8f;
    }

    private void PromoteLegacyHintToFooter(Transform buttonRoot, string legacyName)
    {
        if (buttonRoot == null)
        {
            return;
        }

        Transform footer = buttonRoot.Find("Footer");
        if (footer != null)
        {
            Transform legacy = buttonRoot.Find(legacyName);
            if (legacy != null && legacy != footer)
            {
                legacy.gameObject.SetActive(false);
            }

            return;
        }

        Transform legacyHint = buttonRoot.Find(legacyName);
        if (legacyHint != null)
        {
            legacyHint.name = "Footer";
        }
    }

    private void ConfigureActionButton(
        Button button,
        TextMeshProUGUI template,
        string label,
        string sublabel,
        Color baseColor,
        Color accentColor)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(Mathf.Max(300f, rect.sizeDelta.x), Mathf.Max(104f, rect.sizeDelta.y));
        }

        Image background = button.GetComponent<Image>();
        if (background != null)
        {
            background.color = baseColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, accentColor, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.22f);
        button.colors = colors;

        EnsureChildImage(
            button.transform,
            "AccentBar",
            accentColor,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(Mathf.Max(220f, rect != null ? rect.sizeDelta.x - 30f : 240f), 4f),
            new Vector2(0f, -10f));

        TextMeshProUGUI labelText = EnsureButtonText(
            button.transform,
            "Label",
            label,
            28,
            new Vector2(0.5f, 0.63f),
            new Vector2(0.5f, 0.63f),
            new Vector2(Mathf.Max(220f, rect != null ? rect.sizeDelta.x - 20f : 260f), 34f),
            Vector2.zero,
            template);
        labelText.text = label;
        labelText.color = accentColor;
        labelText.fontStyle = FontStyles.Bold;
        labelText.characterSpacing = 2f;

        TextMeshProUGUI subLabelText = EnsureButtonText(
            button.transform,
            "SubLabel",
            sublabel,
            17,
            new Vector2(0.5f, 0.18f),
            new Vector2(0.5f, 0.18f),
            new Vector2(Mathf.Max(220f, rect != null ? rect.sizeDelta.x - 36f : 240f), 24f),
            Vector2.zero,
            template);
        subLabelText.text = sublabel;
        subLabelText.color = new Color(0.88f, 0.96f, 1f, 0.84f);
    }

    private Image EnsureChildImage(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 size,
        Vector2 anchoredPosition)
    {
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(name);
        Image image;
        if (existing != null)
        {
            image = existing.GetComponent<Image>();
            if (image == null)
            {
                image = existing.gameObject.AddComponent<Image>();
            }
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            image = go.GetComponent<Image>();
        }

        image.color = color;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        return image;
    }

    private TextMeshProUGUI EnsureButtonText(
        Transform parent,
        string name,
        string text,
        int fontSize,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 size,
        Vector2 anchoredPosition,
        TextMeshProUGUI template)
    {
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(name);
        TextMeshProUGUI textRef = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
        if (textRef == null && name == "Label")
        {
            TextMeshProUGUI[] texts = parent.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                string childName = texts[i].name;
                if (childName == "SubLabel" || childName == "Eyebrow" || childName == "Footer")
                {
                    continue;
                }

                if (texts[i].transform.parent == parent)
                {
                    textRef = texts[i];
                    textRef.name = name;
                    break;
                }
            }
        }

        if (textRef == null)
        {
            textRef = CreateRuntimeText(name, parent, text, fontSize, anchorMin, anchorMax, size, anchoredPosition, template);
        }

        if (template != null)
        {
            textRef.font = template.font;
            textRef.fontSharedMaterial = template.fontSharedMaterial;
        }

        textRef.text = text;
        textRef.fontSize = fontSize;
        textRef.alignment = TextAlignmentOptions.Center;
        textRef.textWrappingMode = TextWrappingModes.Normal;
        RectTransform rect = textRef.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        return textRef;
    }

    private static GameObject CreateRuntimePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Image image = panel.GetComponent<Image>();
        image.color = color;
        return panel;
    }

    private static TextMeshProUGUI CreateRuntimeText(
        string name,
        Transform parent,
        string text,
        int fontSize,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 size,
        Vector2 anchoredPosition,
        TextMeshProUGUI template)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (template != null)
        {
            tmp.font = template.font;
            tmp.fontSharedMaterial = template.fontSharedMaterial;
        }

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        RectTransform rect = tmp.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        return tmp;
    }

    private static Button CreateRuntimeButton(
        string name,
        Transform parent,
        string label,
        Vector2 size,
        Vector2 anchoredPosition,
        TextMeshProUGUI template,
        Vector2? anchors = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        Vector2 anchor = anchors ?? new Vector2(0.5f, 0.5f);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.08f, 0.16f, 0.28f, 0.96f);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.14f, 0.24f, 0.4f, 1f);
        colors.pressedColor = new Color(0.06f, 0.12f, 0.2f, 1f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;

        TextMeshProUGUI labelText = CreateRuntimeText(
            "Label",
            go.transform,
            label,
            28,
            new Vector2(0.5f, 0.62f),
            new Vector2(0.5f, 0.62f),
            new Vector2(size.x - 30f, 38f),
            Vector2.zero,
            template);
        labelText.fontStyle = FontStyles.Bold;
        labelText.characterSpacing = 2f;

        return button;
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
