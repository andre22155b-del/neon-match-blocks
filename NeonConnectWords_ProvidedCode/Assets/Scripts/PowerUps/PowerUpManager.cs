using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonConnectWords.Simulation;

public enum PowerUpType
{
    Wildcard,
    Bomb,
    Swap
}

/// <summary>
/// Power-up UI now reflects simulator inventory and forwards actions to the
/// simulator-backed board/game flow.
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

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
    public GameObject swapSelectionPanel;
    public TextMeshProUGUI swapInstructionText;

    [Header("References")]
    public BoardManager boardManager;
    public AudioManager audioManager;
    public UIManager uiManager;
    public SimulationService simulationService;

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
        RefreshButtonStates();

        if (wildcardButton) wildcardButton.onClick.AddListener(ActivateWildcard);
        if (bombButton) bombButton.onClick.AddListener(ActivateBomb);
        if (swapButton) swapButton.onClick.AddListener(BeginSwap);
    }

    public void OnPointsEarned(int totalPoints)
    {
        RefreshButtonStates();
    }

    public void RefreshFromSimulation()
    {
        RefreshButtonStates();
    }

    private void ActivateWildcard()
    {
        if (simulationService == null || !simulationService.ArmWildcard())
        {
            return;
        }

        RefreshButtonStates();
        audioManager?.PlayPowerUp(PowerUpType.Wildcard);
        uiManager?.ShowMessage("Wildcard armed. Your next drop can match any letter.");
        GameManager.Instance?.RefreshPresentation();
    }

    private void ActivateBomb()
    {
        if (!HasBombCharge())
        {
            return;
        }

        StartCoroutine(SelectBombTarget());
    }

    private IEnumerator SelectBombTarget()
    {
        uiManager?.ShowMessage("Tap a tile to bomb its row.");
        bool selected = false;

        while (!selected)
        {
            if (PointerInputUtility.TryGetTapOrClick(out Vector2 tapScreenPos, out int pointerId))
            {
                if (PointerInputUtility.IsPointerOverUi(pointerId))
                {
                    yield return null;
                    continue;
                }

                LetterTile tile = RaycastTile(tapScreenPos);
                if (tile != null)
                {
                    Vector2Int pos = FindTilePosition(tile);
                    if (pos.x >= 0)
                    {
                        if (boardManager != null && boardManager.ClearRow(pos.y))
                        {
                            selected = true;
                            RefreshButtonStates();
                            uiManager?.ShowMessage("Bomb detonated.");
                        }
                    }
                }
            }

            yield return null;
        }
    }

    private void BeginSwap()
    {
        if (!HasSwapCharge())
        {
            return;
        }

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
            if (PointerInputUtility.TryGetTapOrClick(out Vector2 tapScreenPos, out int pointerId))
            {
                if (PointerInputUtility.IsPointerOverUi(pointerId))
                {
                    yield return null;
                    continue;
                }

                LetterTile tile = RaycastTile(tapScreenPos);
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
                        else if (pos != first)
                        {
                            second = pos;
                            step++;
                        }
                    }
                }
            }

            yield return null;
        }

        if (boardManager != null && boardManager.SwapTiles(first, second))
        {
            if (swapSelectionPanel) swapSelectionPanel.SetActive(false);
            RefreshButtonStates();
            audioManager?.PlayPowerUp(PowerUpType.Swap);
            uiManager?.ShowMessage("Tiles swapped.");
        }
    }

    private Vector2Int FindTilePosition(LetterTile tile)
    {
        for (int c = 0; c < boardManager.columns; c++)
        {
            for (int r = 0; r < boardManager.rows; r++)
            {
                if (boardManager.GetTile(c, r) == tile)
                {
                    return new Vector2Int(c, r);
                }
            }
        }

        return new Vector2Int(-1, -1);
    }

    private LetterTile RaycastTile(Vector2 screenPosition)
    {
        if (Camera.main == null)
        {
            return null;
        }

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out RaycastHit hit)
            ? hit.collider.GetComponentInParent<LetterTile>()
            : null;
    }

    private void RefreshButtonStates()
    {
        SimulationPowerUpInventory inventory = simulationService != null && simulationService.State != null
            ? simulationService.State.PowerUps
            : null;

        if (inventory == null)
        {
            SetButtonState(wildcardButton, wildcardCountText, false, 0);
            SetButtonState(bombButton, bombCountText, false, 0);
            SetButtonState(swapButton, swapCountText, false, 0);
            return;
        }

        SetButtonState(wildcardButton, wildcardCountText, inventory.WildcardUnlocked, inventory.WildcardCharges);
        SetButtonState(bombButton, bombCountText, inventory.BombUnlocked, inventory.BombCharges);
        SetButtonState(swapButton, swapCountText, inventory.SwapUnlocked, inventory.SwapCharges);
    }

    private void SetButtonState(Button button, TextMeshProUGUI countText, bool unlocked, int charges)
    {
        if (button) button.interactable = unlocked && charges > 0;
        if (countText)
        {
            countText.text = unlocked ? charges.ToString() : "LOCK";
            countText.color = charges > 0 ? Color.white : Color.gray;
        }
    }

    private bool HasBombCharge()
    {
        return simulationService != null && simulationService.State != null && simulationService.State.PowerUps.BombCharges > 0;
    }

    private bool HasSwapCharge()
    {
        return simulationService != null && simulationService.State != null && simulationService.State.PowerUps.SwapCharges > 0;
    }
}
