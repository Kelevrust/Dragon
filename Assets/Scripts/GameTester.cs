using UnityEngine;
using UnityEngine.UI;

public class GameTester : MonoBehaviour
{
    [Header("Automation Settings")]
    public bool isAutoPlaying = false;
    [Range(1f, 50f)] public float simulationSpeed = 1f; // Increased max speed

    [Header("Stop Conditions")]
    public int stopOnTurn = 20;
    public bool stopOnDefeat = false; // CHANGED: Default to false so it keeps going

    [Header("AI Difficulty Control")]
    [Range(0.5f, 5.0f)] public float aiDifficulty = 1.0f;

    [Header("Logic Delays")]
    public float actionInterval = 0.2f; // Faster clicking by default
    private float timer;
    private int lastHealth;

    void Start()
    {
        if (GameManager.instance != null)
        {
            lastHealth = GameManager.instance.playerHealth;
        }
    }

    void Update()
    {
        // 1. Sync AI Difficulty
        if (AIManager.instance != null)
        {
            AIManager.instance.difficultyMultiplier = aiDifficulty;
        }

        // 2. Set Game Speed
        Time.timeScale = simulationSpeed;

        if (!isAutoPlaying) return;

        // 3. Safety Check
        if (GameManager.instance == null) return;

        // 4. Check Stop Conditions
        if (GameManager.instance.turnNumber >= stopOnTurn)
        {
            Debug.Log($"<color=orange>Tester: Reached Turn {stopOnTurn}. Stopping.</color>");
            isAutoPlaying = false;
            return;
        }

        if (stopOnDefeat && GameManager.instance.playerHealth < lastHealth)
        {
            Debug.Log("<color=red>Tester: Player took damage. Stopping.</color>");
            isAutoPlaying = false;
            return;
        }
        lastHealth = GameManager.instance.playerHealth;

        // 5. Bot Action Timer
        timer += Time.unscaledDeltaTime;
        if (timer >= actionInterval)
        {
            timer = 0;
            RunBotDecision();
        }
    }

    void RunBotDecision()
    {
        // Death Logic
        if (DeathSaveManager.instance != null && DeathSaveManager.instance.deathScreenPanel.activeSelf)
        {
            HandleDeathScreen();
            return;
        }

        // Recruit Logic
        if (GameManager.instance.currentPhase == GameManager.GamePhase.Recruit)
        {
            // If unconscious, just skip turn
            if (GameManager.instance.isUnconscious)
            {
                Debug.Log("Tester: Unconscious. Skipping turn.");
                CombatManager.instance.StartCombat();
                return;
            }

            // Try to Buy, otherwise Fight
            if (!TryBuyAnything())
            {
                // Only fight if we are sure we can't buy anything
                Debug.Log("Tester: Shop done. Starting Combat.");
                CombatManager.instance.StartCombat();
            }
        }
    }

    bool TryBuyAnything()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop == null || shop.shopContainer == null) return false;

        foreach (Transform child in shop.shopContainer)
        {
            CardDisplay card = child.GetComponent<CardDisplay>();
            if (card != null && !card.isPurchased)
            {
                // Check Affordability
                if (GameManager.instance.gold >= card.unitData.cost)
                {
                    // Check Board Space
                    GameObject playerBoard = GameObject.Find("PlayerBoard");
                    if (playerBoard.transform.childCount >= 7)
                    {
                        // Board full, maybe sell logic later? For now, stop buying.
                        return false;
                    }

                    // BUY!
                    if (GameManager.instance.TrySpendGold(card.unitData.cost))
                    {
                        Debug.Log($"Tester: Buying {card.unitData.unitName}");
                        card.transform.SetParent(playerBoard.transform);
                        card.isPurchased = true;
                        return true; // Action taken, wait for next tick
                    }
                }
            }
        }
        return false; // Found nothing affordable/valid
    }

    void HandleDeathScreen()
    {
        DeathSaveManager dsm = DeathSaveManager.instance;
        if (dsm != null && dsm.rollButton != null && dsm.rollButton.interactable)
        {
            Debug.Log("Tester: Rolling Death Save...");
            dsm.rollButton.onClick.Invoke();
        }
    }
}