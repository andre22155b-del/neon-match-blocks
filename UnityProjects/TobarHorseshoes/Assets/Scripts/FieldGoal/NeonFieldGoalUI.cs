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
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI modeText;
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
        SetMultiplier(1, 0);
        SetModeLabel(string.Empty);
        SetTimer(60f);
        ShowStatus("Slide to line it up.");
        SetHint("Swipe up on the kick pad.");
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

    public void SetGoals(int goals)
    {
        SetScore(goals);
    }

    public void SetScore(int score)
    {
        if (goalsText != null)
        {
            goalsText.text = "SCORE " + score.ToString("N0");
        }
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

    public void ShowGoalBurst(int pointsAwarded, int multiplier, bool perfectKick, bool clutchActive)
    {
        string title = "IT'S GOOD";
        if (perfectKick && clutchActive)
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
        ShowAnnouncement(
            title,
            "+" + pointsAwarded + " PTS  |  x" + multiplier + " MULTI",
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
        ShowResults(goals, goals, 1, string.Empty);
    }

    public void ShowResults(int score, int goals, int bestMultiplier, string modeLabel)
    {
        HideAnnouncementImmediate();

        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
        }

        if (resultsTitleText != null)
        {
            resultsTitleText.text = GetResultsHeadline(score, bestMultiplier);
        }

        if (resultsBodyText != null)
        {
            string modeLine = string.IsNullOrEmpty(modeLabel) ? string.Empty : modeLabel + "\n";
            resultsBodyText.text = modeLine +
                "Score " + score.ToString("N0") +
                "  |  Goals " + goals.ToString("N0") +
                "  |  Best x" + bestMultiplier +
                "\n3-point makes. Perfect kicks and clutch cash stack hard.";
        }

        if (resultsCtaText != null)
        {
            resultsCtaText.text = GetResultsCta(score, bestMultiplier);
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
            introTime -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(introTime / 0.16f);
            announcementGroup.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1.05f, t);
            yield return null;
        }

        float visibleDuration = 1.1f;
        while (visibleDuration > 0f)
        {
            visibleDuration -= Time.deltaTime;
            float pulse = 1f + Mathf.Sin(Time.time * 11f) * 0.015f;
            announcementGroup.transform.localScale = Vector3.one * pulse;
            yield return null;
        }

        float fadeTime = 0.25f;
        float startAlpha = announcementGroup.alpha;
        while (fadeTime > 0f)
        {
            fadeTime -= Time.deltaTime;
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
                "Pick your mode and start cashing kicks.",
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

        if (arcadeRushButton == null)
        {
            arcadeRushButton = CreateRuntimeButton(
                "ArcadeRushButton",
                titleRoot,
                "ARCADE RUSH",
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
                "60 seconds. Build heat, then clutch up.",
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
            "ARCADE RUSH",
            "BUILD HEAT",
            "60 SEC // STACK MULTIS // CASH OUT IN CLUTCH",
            new Color(0.06f, 0.16f, 0.3f, 0.96f),
            new Color(0.22f, 0.95f, 1f, 1f));
        ConfigureModeCard(
            clutchBlitzButton,
            template,
            "CLUTCH BLITZ",
            "PRESSURE ON",
            "35 SEC // CLUTCH IS LIVE FROM SNAP ONE",
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
            titleScreenBodyText.text = "Arcade-crazy kicks. 3 points per make. Perfect strikes, multipliers, and clutch bonuses stack hard.";
        }

        if (titleScreenTagText != null)
        {
            titleScreenTagText.text = "SOLO RUN // NEON TURF // HYPE ARCADE";
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

    private string GetResultsHeadline(int score, int bestMultiplier)
    {
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

    private string GetResultsCta(int score, int bestMultiplier)
    {
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
                "SOLO RUN // NEON TURF // HYPE ARCADE",
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
        textRef.enableWordWrapping = true;
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
        tmp.enableWordWrapping = true;
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
