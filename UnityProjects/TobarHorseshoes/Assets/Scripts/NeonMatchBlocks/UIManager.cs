using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// UI handler for Neon Match Blocks.
/// Setup:
/// 1) Assign all TMP text fields (scores, turn, level, timer, combo).
/// 2) Assign turn glow Images for P1/P2.
/// 3) Assign Reflex mini-game panel + countdown/result text + left/right tap buttons.
/// 4) Assign floating text prefab (TMP object under Canvas).
/// 5) Assign level result panel texts.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Camera worldCamera;

    [Header("Top HUD")]
    [SerializeField] private TextMeshProUGUI p1ScoreText;
    [SerializeField] private TextMeshProUGUI p2ScoreText;
    [SerializeField] private TextMeshProUGUI p1ComboText;
    [SerializeField] private TextMeshProUGUI p2ComboText;
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image p1TurnGlow;
    [SerializeField] private Image p2TurnGlow;

    [Header("Turn Colors")]
    [SerializeField] private Color activeTurnColor = new Color(0.2f, 0.95f, 1f, 0.95f);
    [SerializeField] private Color inactiveTurnColor = new Color(0.1f, 0.1f, 0.16f, 0.45f);

    [Header("Reflex Mini-Game")]
    [SerializeField] private GameObject reflexPanel;
    [SerializeField] private TextMeshProUGUI reflexCountdownText;
    [SerializeField] private TextMeshProUGUI reflexResultText;
    [SerializeField] private Button reflexLeftButton;
    [SerializeField] private Button reflexRightButton;
    [SerializeField] private Image reflexLeftFlash;
    [SerializeField] private Image reflexRightFlash;

    [Header("Floating Text")]
    [SerializeField] private TextMeshProUGUI floatingTextPrefab;

    [Header("Result Panel")]
    [SerializeField] private GameObject levelResultPanel;
    [SerializeField] private TextMeshProUGUI levelResultTitleText;
    [SerializeField] private TextMeshProUGUI levelResultBodyText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        EnsureCompatibleInputModule();
        ShowReflexPanel(false);
        HideLevelResult();
    }

    private static void EnsureCompatibleInputModule()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
        }

        if (eventSystem == null)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
        {
            legacyModule.enabled = false;
        }

        TouchInputModule touchModule = eventSystem.GetComponent<TouchInputModule>();
        if (touchModule != null)
        {
            touchModule.enabled = false;
        }

        InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemModule == null)
        {
            inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        inputSystemModule.enabled = true;
#elif ENABLE_LEGACY_INPUT_MANAGER
        StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule == null)
        {
            legacyModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        legacyModule.enabled = true;
#endif
    }

    public void UpdateScores(int p1, int p2)
    {
        if (p1ScoreText != null)
        {
            p1ScoreText.text = "P1: " + p1;
        }

        if (p2ScoreText != null)
        {
            p2ScoreText.text = "P2: " + p2;
        }
    }

    public void UpdateCombos(int p1Combo, int p2Combo)
    {
        if (p1ComboText != null)
        {
            p1ComboText.text = "Combo: x" + Mathf.Max(1, p1Combo);
        }

        if (p2ComboText != null)
        {
            p2ComboText.text = "Combo: x" + Mathf.Max(1, p2Combo);
        }
    }

    public void UpdateTurn(int currentPlayer, bool vsAI)
    {
        if (turnText != null)
        {
            turnText.text = currentPlayer == 0 ? "Turn: PLAYER 1" : ("Turn: " + (vsAI ? "AI" : "PLAYER 2"));
        }

        if (p1TurnGlow != null)
        {
            p1TurnGlow.color = currentPlayer == 0 ? activeTurnColor : inactiveTurnColor;
        }

        if (p2TurnGlow != null)
        {
            p2TurnGlow.color = currentPlayer == 1 ? activeTurnColor : inactiveTurnColor;
        }

        Image active = currentPlayer == 0 ? p1TurnGlow : p2TurnGlow;
        if (active != null)
        {
            active.transform.DOKill();
            active.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f, 10, 0.8f);
        }
    }

    public void UpdateLevel(int level, int maxLevel)
    {
        if (levelText != null)
        {
            levelText.text = "Level " + level + " / " + maxLevel;
        }
    }

    public void UpdateTimer(float timeLeft)
    {
        if (timerText == null)
        {
            return;
        }

        timerText.text = "Time: " + Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
    }

    public void ConfigureReflexButtons(Action onLeftTap, Action onRightTap)
    {
        if (reflexLeftButton != null)
        {
            reflexLeftButton.onClick.RemoveAllListeners();
            reflexLeftButton.onClick.AddListener(delegate { onLeftTap?.Invoke(); });
        }

        if (reflexRightButton != null)
        {
            reflexRightButton.onClick.RemoveAllListeners();
            reflexRightButton.onClick.AddListener(delegate { onRightTap?.Invoke(); });
        }
    }

    public void ShowReflexPanel(bool show)
    {
        if (reflexPanel != null)
        {
            reflexPanel.SetActive(show);
        }
    }

    public void SetReflexCountdown(string text)
    {
        if (reflexCountdownText != null)
        {
            reflexCountdownText.text = text;
        }
    }

    public void SetReflexResult(string text)
    {
        if (reflexResultText != null)
        {
            reflexResultText.text = text;
        }
    }

    public void PulseReflexSide(bool leftSide)
    {
        Image img = leftSide ? reflexLeftFlash : reflexRightFlash;
        if (img == null)
        {
            return;
        }

        img.DOKill();
        Color baseColor = img.color;
        baseColor.a = 1f;
        img.color = baseColor;
        img.DOFade(0.2f, 0.2f).SetEase(Ease.OutQuad);
        img.transform.DOPunchScale(Vector3.one * 0.08f, 0.2f, 10, 0.8f);
    }

    public void ShowFloatingText(string message, Vector3 worldPos, Color color, float scale = 1f)
    {
        if (floatingTextPrefab == null || canvas == null)
        {
            return;
        }

        TextMeshProUGUI txt = Instantiate(floatingTextPrefab, canvas.transform);
        txt.text = message;
        txt.color = color;
        txt.rectTransform.localScale = Vector3.one * scale;

        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        Vector3 screenPos = cam != null ? cam.WorldToScreenPoint(worldPos) : worldPos;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect != null)
        {
            Vector2 localPoint;
            Camera eventCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, eventCam, out localPoint);
            txt.rectTransform.anchoredPosition = localPoint;

            Sequence seq = DOTween.Sequence();
            seq.Append(txt.rectTransform.DOAnchorPosY(localPoint.y + 90f, 0.85f).SetEase(Ease.OutCubic));
            seq.Join(txt.DOFade(0f, 0.85f));
            seq.OnComplete(delegate { Destroy(txt.gameObject); });
        }
        else
        {
            Destroy(txt.gameObject, 1f);
        }
    }

    public void ShowLevelResult(
        int level,
        string reason,
        string winner,
        int p1Score,
        int p2Score,
        int p1MaxCombo,
        int p2MaxCombo,
        bool isFinal)
    {
        if (levelResultPanel != null)
        {
            levelResultPanel.SetActive(true);
        }

        if (levelResultTitleText != null)
        {
            levelResultTitleText.text = isFinal ? "NEON MATCH BLOCKS - FINAL" : ("LEVEL " + level + " COMPLETE");
        }

        if (levelResultBodyText != null)
        {
            string body =
                reason + "\n" +
                "Winner: " + winner + "\n" +
                "P1 Score: " + p1Score + "  |  P2 Score: " + p2Score + "\n" +
                "P1 Max Combo: x" + p1MaxCombo + "  |  P2 Max Combo: x" + p2MaxCombo;

            levelResultBodyText.text = body;
        }
    }

    public void HideLevelResult()
    {
        if (levelResultPanel != null)
        {
            levelResultPanel.SetActive(false);
        }
    }
}
