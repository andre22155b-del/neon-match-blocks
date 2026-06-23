using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TobarUI : MonoBehaviour
{
    public event Action<AimAssistMode> AimAssistChanged;
    public event Action<PitchDistanceMode> DistanceModeChanged;

    [Header("Widgets")]
    public Slider powerMeter;
    public RectTransform aimReticle;
    public float aimReticleMaxOffset = 120f;
    public Text scoreText;
    public Text turnText;
    public Text roundLogText;
    public Text gameOverText;
    public Dropdown aimAssistDropdown;
    public Dropdown distanceDropdown;
    public bool useFallbackOnGui = true;

    private readonly Queue<string> roundLines = new Queue<string>();
    private float powerValue;
    private int scoreP1;
    private int scoreP2;
    private string turnLabel;
    private string winnerLabel;
    private AimAssistMode currentAimAssist = AimAssistMode.Low;
    private PitchDistanceMode currentDistance = PitchDistanceMode.Feet25;

    private void Start()
    {
        if (aimAssistDropdown != null)
        {
            aimAssistDropdown.onValueChanged.AddListener(OnAimAssistValueChanged);
        }

        if (distanceDropdown != null)
        {
            distanceDropdown.onValueChanged.AddListener(OnDistanceValueChanged);
        }

        SetPower(0f);
        SetAim(0f);
        SetScore(0, 0);
        SetTurn(0, 1);
        ShowWinner(null);
        AimAssistChanged?.Invoke(currentAimAssist);
        DistanceModeChanged?.Invoke(currentDistance);
    }

    public void SetPower(float value)
    {
        powerValue = Mathf.Clamp01(value);
        if (powerMeter != null)
        {
            powerMeter.value = powerValue;
        }
    }

    public void SetAim(float aim)
    {
        if (aimReticle == null)
        {
            return;
        }

        Vector2 pos = aimReticle.anchoredPosition;
        pos.x = Mathf.Clamp(aim, -1f, 1f) * aimReticleMaxOffset;
        aimReticle.anchoredPosition = pos;
    }

    public void SetScore(int playerOne, int playerTwo)
    {
        scoreP1 = playerOne;
        scoreP2 = playerTwo;
        if (scoreText != null)
        {
            scoreText.text = "P1 " + playerOne + " : " + playerTwo + " P2";
        }
    }

    public void SetTurn(int playerIndex, int shoeNumber)
    {
        turnLabel = "Player " + (playerIndex + 1) + " - Shoe " + shoeNumber;
        if (turnText == null)
        {
            return;
        }

        turnText.text = turnLabel;
    }

    public void AddRoundLog(string line)
    {
        roundLines.Enqueue(line);
        while (roundLines.Count > 6)
        {
            roundLines.Dequeue();
        }

        if (roundLogText != null)
        {
            roundLogText.text = string.Join("\n", roundLines.ToArray());
        }
    }

    public void ShowWinner(string winnerText)
    {
        winnerLabel = winnerText == null ? string.Empty : winnerText;
        if (gameOverText == null)
        {
            return;
        }

        gameOverText.text = winnerLabel;
    }

    private void OnAimAssistValueChanged(int value)
    {
        AimAssistMode mode = (AimAssistMode)Mathf.Clamp(value, 0, 2);
        currentAimAssist = mode;
        AimAssistChanged?.Invoke(mode);
    }

    private void OnDistanceValueChanged(int value)
    {
        PitchDistanceMode mode = value == 1 ? PitchDistanceMode.Feet40 : PitchDistanceMode.Feet25;
        currentDistance = mode;
        DistanceModeChanged?.Invoke(mode);
    }

    private void OnGUI()
    {
        if (!useFallbackOnGui || HasCanvasWidgets())
        {
            return;
        }

        GUILayout.BeginArea(new Rect(12f, 12f, 420f, 280f), GUI.skin.box);
        GUILayout.Label("Tobar Horseshoes");
        GUILayout.Label("Score: P1 " + scoreP1 + " : " + scoreP2 + " P2");
        GUILayout.Label("Turn: " + (string.IsNullOrEmpty(turnLabel) ? "Waiting..." : turnLabel));
        GUILayout.Label("Power: " + Mathf.RoundToInt(powerValue * 100f) + "%");

        if (!string.IsNullOrEmpty(winnerLabel))
        {
            GUILayout.Label("Winner: " + winnerLabel);
        }

        GUILayout.Space(8f);
        GUILayout.Label("Aim Assist");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Off")) SetAimAssistFromFallback(AimAssistMode.Off);
        if (GUILayout.Button("Low")) SetAimAssistFromFallback(AimAssistMode.Low);
        if (GUILayout.Button("High")) SetAimAssistFromFallback(AimAssistMode.High);
        GUILayout.EndHorizontal();
        GUILayout.Label("Current: " + currentAimAssist);

        GUILayout.Space(6f);
        GUILayout.Label("Distance");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("25 ft")) SetDistanceFromFallback(PitchDistanceMode.Feet25);
        if (GUILayout.Button("40 ft")) SetDistanceFromFallback(PitchDistanceMode.Feet40);
        GUILayout.EndHorizontal();
        GUILayout.Label("Current: " + (currentDistance == PitchDistanceMode.Feet25 ? "25 ft" : "40 ft"));

        GUILayout.Space(8f);
        GUILayout.Label("Round Log");
        foreach (string line in roundLines)
        {
            GUILayout.Label(line);
        }

        GUILayout.EndArea();
    }

    private bool HasCanvasWidgets()
    {
        return powerMeter != null ||
               scoreText != null ||
               turnText != null ||
               roundLogText != null ||
               gameOverText != null ||
               aimAssistDropdown != null ||
               distanceDropdown != null;
    }

    private void SetAimAssistFromFallback(AimAssistMode mode)
    {
        if (currentAimAssist == mode)
        {
            return;
        }

        currentAimAssist = mode;
        AimAssistChanged?.Invoke(mode);
    }

    private void SetDistanceFromFallback(PitchDistanceMode mode)
    {
        if (currentDistance == mode)
        {
            return;
        }

        currentDistance = mode;
        DistanceModeChanged?.Invoke(mode);
    }
}
