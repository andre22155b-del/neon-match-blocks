using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum PowerUpType { Wildcard, Bomb, Swap }

/// <summary>
/// PowerUpManager: Tracks power-up inventory, handles activation logic,
/// and manages progressive unlock thresholds.
/// Each power-up type has its own activation flow.
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inspector Config
    // -----------------------------------------------------------------------
    [Header("Unlock Thresholds (points)")]
    public int wildcardUnlockAt = 20;
    public int bombUnlockAt = 50;
    public int swapUnlockAt = 80;

    [Header("Charge Costs (uses earned every N points)")]
    public int wildcardEvery = 30;
    public int bombEvery = 60;
    public int swapEvery = 90;

    [Header("UI References")]
    public Button wildcardButton;
    public Button bombButton;
    public Button swapButton;
    public TextMeshProUGUI wildcardCountText;
    public TextMeshProUGUI bombCountText;
    public TextMeshProUGUI swapCountText;

    [Header("Swap UI")]
    public GameObject swapSelectionPanel;   // shown during swap targeting
    public TextMeshProUGUI swapInstructionText;

    [Header("References")]
    public BoardManager boardManager;
    public AudioManager audioManager;
    public UIManager uiManager;

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------
    private int wildcardCharges = 0;
    private int bombCharges = 0;
    private int swapCharges = 0;

    private bool wildcardUnlocked = false;
    private bool bombUnlocked = false;
    private bool swapUnlocked = false;

    private int pointsAtLastWildcard = 0;
    private int pointsAtLastBomb = 0;
    private int pointsAtLastSwap = 0;

    // Swap selection state
    private bool selectingSwap = false;
    private Vector2Int swapFirst = new Vector2Int(-1, -1);
    private bool swapFirstSelected = false;

    // -----------------------------------------------------------------------
    // Unity Lifecycle
    // -----------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        RefreshButtonStates();

        if (wildcardButton) wildcardButton.onClick.AddListener(ActivateWildcard);
        if (bombButton) bombButton.onClick.AddListener(ActivateBomb);
        if (swapButton) swapButton.onClick.AddListener(BeginSwap);
    }

    // -----------------------------------------------------------------------
    // Unlock & Charge Logic
    // -----------------------------------------------------------------------
    public void OnPointsEarned(int totalPoints)
    {
        // Unlock checks
        if (!wildcardUnlocked && totalPoints >= wildcardUnlockAt)
        {
            wildcardUnlocked = true;
            wildcardCharges = 1;
            uiManager.ShowMessage("Wildcard power-up unlocked! ★");
            pointsAtLastWildcard = totalPoints;
        }
        if (!bombUnlocked && totalPoints >= bombUnlockAt)
        {
            bombUnlocked = true;
            bombCharges = 1;
            uiManager.ShowMessage("Bomb power-up unlocked! 💣");
            pointsAtLastBomb = totalPoints;
        }
        if (!swapUnlocked && totalPoints >= swapUnlockAt)
        {
            swapUnlocked = true;
            swapCharges = 1;
            uiManager.ShowMessage("Swap power-up unlocked! ⇄");
            pointsAtLastSwap = totalPoints;
        }

        // Recharge after unlock
        if (wildcardUnlocked && totalPoints - pointsAtLastWildcard >= wildcardEvery)
        {
            wildcardCharges++;
            pointsAtLastWildcard = totalPoints;
        }
        if (bombUnlocked && totalPoints - pointsAtLastBomb >= bombEvery)
        {
            bombCharges++;
            pointsAtLastBomb = totalPoints;
        }
        if (swapUnlocked && totalPoints - pointsAtLastSwap >= swapEvery)
        {
            swapCharges++;
            pointsAtLastSwap = totalPoints;
        }

        RefreshButtonStates();
    }

    // -----------------------------------------------------------------------
    // Wildcard Activation
    // -----------------------------------------------------------------------
    private void ActivateWildcard()
    {
        if (wildcardCharges <= 0) return;
        wildcardCharges--;
        RefreshButtonStates();
        audioManager.PlayPowerUp(PowerUpType.Wildcard);
        uiManager.ShowMessage("Wildcard! Drop as any letter.");
        GameManager.Instance.SetCurrentLetter('*');
    }

    // -----------------------------------------------------------------------
    // Bomb Activation
    // -----------------------------------------------------------------------
    private void ActivateBomb()
    {
        if (bombCharges <= 0) return;
        StartCoroutine(SelectBombTarget());
    }

    private IEnumerator SelectBombTarget()
    {
        uiManager.ShowMessage("Click a tile to bomb its row or column!");
        bool selected = false;

        while (!selected)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    LetterTile tile = hit.collider.GetComponentInParent<LetterTile>();
                    if (tile != null)
                    {
                        selected = true;
                        // Find position on board
                        Vector2Int pos = FindTilePosition(tile);
                        if (pos.x >= 0)
                        {
                            bombCharges--;
                            RefreshButtonStates();
                            audioManager.PlayPowerUp(PowerUpType.Bomb);
                            // Bomb clears the row
                            boardManager.ClearRow(pos.y);
                            uiManager.ShowMessage($"BOOM! Row {pos.y + 1} cleared!");
                        }
                    }
                }
            }
            yield return null;
        }
    }

    // -----------------------------------------------------------------------
    // Swap Activation
    // -----------------------------------------------------------------------
    private void BeginSwap()
    {
        if (swapCharges <= 0) return;
        selectingSwap = true;
        swapFirstSelected = false;
        if (swapSelectionPanel) swapSelectionPanel.SetActive(true);
        if (swapInstructionText) swapInstructionText.text = "Select FIRST tile to swap";
        StartCoroutine(SwapSelectionRoutine());
    }

    private IEnumerator SwapSelectionRoutine()
    {
        Vector2Int first = new Vector2Int(-1, -1);
        Vector2Int second = new Vector2Int(-1, -1);
        int step = 0;

        while (step < 2)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    LetterTile tile = hit.collider.GetComponentInParent<LetterTile>();
                    if (tile != null)
                    {
                        Vector2Int pos = FindTilePosition(tile);
                        if (pos.x >= 0)
                        {
                            if (step == 0)
                            {
                                first = pos;
                                tile.PlayWordHighlight();
                                if (swapInstructionText) swapInstructionText.text = "Select SECOND tile to swap";
                                step++;
                            }
                            else if (step == 1 && pos != first)
                            {
                                second = pos;
                                step++;
                            }
                        }
                    }
                }
            }
            yield return null;
        }

        if (swapSelectionPanel) swapSelectionPanel.SetActive(false);
        selectingSwap = false;

        if (boardManager.SwapTiles(first, second))
        {
            swapCharges--;
            RefreshButtonStates();
            audioManager.PlayPowerUp(PowerUpType.Swap);
            uiManager.ShowMessage("Tiles swapped!");
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
    private Vector2Int FindTilePosition(LetterTile tile)
    {
        for (int c = 0; c < boardManager.columns; c++)
            for (int r = 0; r < boardManager.rows; r++)
                if (boardManager.GetTile(c, r) == tile)
                    return new Vector2Int(c, r);
        return new Vector2Int(-1, -1);
    }

    private void RefreshButtonStates()
    {
        SetButtonState(wildcardButton, wildcardCountText, wildcardUnlocked, wildcardCharges);
        SetButtonState(bombButton, bombCountText, bombUnlocked, bombCharges);
        SetButtonState(swapButton, swapCountText, swapUnlocked, swapCharges);
    }

    private void SetButtonState(Button btn, TextMeshProUGUI countTxt, bool unlocked, int charges)
    {
        if (btn) btn.interactable = unlocked && charges > 0;
        if (countTxt)
        {
            countTxt.text = unlocked ? charges.ToString() : "🔒";
            countTxt.color = charges > 0 ? Color.white : Color.gray;
        }
    }
}
