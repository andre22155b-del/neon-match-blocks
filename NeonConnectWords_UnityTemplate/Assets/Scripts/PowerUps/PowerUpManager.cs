using System;
using UnityEngine;

/// <summary>
/// Tracks unlocks and queued power-up state.
/// Wildcard and bomb are queued for the next board action, while swap is immediate.
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [Header("References")]
    public UIManager uiManager;
    public AudioManager audioManager;

    [Header("Unlock Timing")]
    public int wildcardUnlockTurn = 2;
    public int bombUnlockTurn = 4;
    public int swapUnlockTurn = 6;

    [Header("Reward Amounts")]
    public int wildcardReward = 1;
    public int bombReward = 1;
    public int swapReward = 1;

    public bool HasQueuedWildcard { get; private set; }
    public bool HasQueuedBomb { get; private set; }

    public event Action<PowerUpType> PowerUpUsed;

    private int wildcardCount;
    private int bombCount;
    private int swapCount;

    private bool wildcardUnlocked;
    private bool bombUnlocked;
    private bool swapUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ResetForNewGame()
    {
        wildcardCount = 0;
        bombCount = 0;
        swapCount = 0;
        wildcardUnlocked = false;
        bombUnlocked = false;
        swapUnlocked = false;
        ClearQueuedState();
        RefreshUi();
    }

    public void GrantTutorialLoadout()
    {
        wildcardCount = Mathf.Max(wildcardCount, 1);
        bombCount = Mathf.Max(bombCount, 1);
        swapCount = Mathf.Max(swapCount, 1);
        RefreshUi();
    }

    public void EvaluateUnlocks(int turnCount)
    {
        if (!wildcardUnlocked && turnCount >= wildcardUnlockTurn)
        {
            wildcardUnlocked = true;
            wildcardCount += wildcardReward;
            uiManager?.ShowMessage("Wildcard unlocked");
        }

        if (!bombUnlocked && turnCount >= bombUnlockTurn)
        {
            bombUnlocked = true;
            bombCount += bombReward;
            uiManager?.ShowMessage("Bomb unlocked");
        }

        if (!swapUnlocked && turnCount >= swapUnlockTurn)
        {
            swapUnlocked = true;
            swapCount += swapReward;
            uiManager?.ShowMessage("Swap unlocked");
        }

        RefreshUi();
    }

    public void TryQueueWildcard()
    {
        if (wildcardCount <= 0)
        {
            uiManager?.ShowMessage("No wildcard charges");
            return;
        }

        HasQueuedWildcard = !HasQueuedWildcard;
        if (HasQueuedWildcard)
        {
            HasQueuedBomb = false;
            uiManager?.ShowMessage("Wildcard armed for your next drop");
        }
        else
        {
            uiManager?.ShowMessage("Wildcard cancelled");
        }

        audioManager?.PlayPowerUp(PowerUpType.Wildcard);
        RefreshUi();
    }

    public void TryQueueBomb()
    {
        if (bombCount <= 0)
        {
            uiManager?.ShowMessage("No bomb charges");
            return;
        }

        HasQueuedBomb = !HasQueuedBomb;
        if (HasQueuedBomb)
        {
            HasQueuedWildcard = false;
            uiManager?.ShowMessage("Bomb armed - tap a column");
        }
        else
        {
            uiManager?.ShowMessage("Bomb cancelled");
        }

        audioManager?.PlayPowerUp(PowerUpType.Bomb);
        RefreshUi();
    }

    public void TryUseSwap()
    {
        if (swapCount <= 0)
        {
            uiManager?.ShowMessage("No swap charges");
            return;
        }

        swapCount--;
        HasQueuedBomb = false;
        HasQueuedWildcard = false;

        GameManager.Instance?.RerollCurrentLetter(true);
        audioManager?.PlayPowerUp(PowerUpType.Swap);
        PowerUpUsed?.Invoke(PowerUpType.Swap);
        TutorialManager.Instance?.NotifyPowerUpUsed(PowerUpType.Swap);
        RefreshUi();
    }

    public bool ConsumeQueuedWildcard()
    {
        if (!HasQueuedWildcard || wildcardCount <= 0)
        {
            return false;
        }

        wildcardCount--;
        HasQueuedWildcard = false;
        audioManager?.PlayPowerUp(PowerUpType.Wildcard);
        PowerUpUsed?.Invoke(PowerUpType.Wildcard);
        TutorialManager.Instance?.NotifyPowerUpUsed(PowerUpType.Wildcard);
        RefreshUi();
        return true;
    }

    public void ConsumeQueuedBomb()
    {
        if (!HasQueuedBomb || bombCount <= 0)
        {
            return;
        }

        bombCount--;
        HasQueuedBomb = false;
        audioManager?.PlayPowerUp(PowerUpType.Bomb);
        PowerUpUsed?.Invoke(PowerUpType.Bomb);
        TutorialManager.Instance?.NotifyPowerUpUsed(PowerUpType.Bomb);
        RefreshUi();
    }

    public void ClearQueuedState()
    {
        HasQueuedWildcard = false;
        HasQueuedBomb = false;
        RefreshUi();
    }

    private void RefreshUi()
    {
        uiManager?.UpdatePowerUpCounts(wildcardCount, bombCount, swapCount, HasQueuedWildcard, HasQueuedBomb);
    }
}
