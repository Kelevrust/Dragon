using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement; 
using System.Linq; 

public class DeathSaveManager : MonoBehaviour
{
    public static DeathSaveManager instance;

    [Header("Game Mode")]
    public bool isPvPMode = true; // Toggle this based on scene/mode

    [Header("UI References")]
    public GameObject deathScreenPanel; 
    public TMP_Text titleText;
    public TMP_Text infoText;
    public TMP_Text resultText;
    
    [Header("PvE Actions (D&D Style)")]
    public GameObject pveContainer;
    public Button rollSaveButton;     
    public Button pveSuccumbButton;  

    [Header("PvP Actions (The Wager)")]
    public GameObject pvpContainer;
    public Button wagerButton;      // "Cheat Death (50% Chance / Double Risk)"
    public Button acceptFateButton; // "Pay the Ferryman (Standard Loss)"

    [Header("Game Over Buttons")]
    public Button mainMenuButton; 
    public Button playAgainButton; 

    [Header("State")]
    public int exhaustionLevel = 0;
    public int failureCount = 0;
    
    void Awake() { instance = this; }

    void Start()
    {
        if(deathScreenPanel != null) deathScreenPanel.SetActive(false);
        if(pveContainer != null) pveContainer.SetActive(false);
        if(pvpContainer != null) pvpContainer.SetActive(false);
        HideGameOverButtons();
    }

    public void StartDeathSequence()
    {
        deathScreenPanel.SetActive(true);
        HideGameOverButtons();

        if (isPvPMode)
        {
            SetupPvPDeath();
        }
        else
        {
            SetupPvEDeath();
        }
    }

    // --- PVP LOGIC (THE WAGER) ---

    void SetupPvPDeath()
    {
        pveContainer.SetActive(false);
        pvpContainer.SetActive(true);

        titleText.text = "YOU ARE DEAD";
        infoText.text = "The Ferryman awaits his coin...\n\n<color=red>Risk it all?</color>\nWin: Revive with 1 HP\nLose: Double MMR Penalty";
        resultText.text = "";

        wagerButton.interactable = true;
        wagerButton.onClick.RemoveAllListeners();
        wagerButton.onClick.AddListener(FlipCoin);

        acceptFateButton.interactable = true;
        acceptFateButton.onClick.RemoveAllListeners();
        acceptFateButton.onClick.AddListener(() => GameOver("You paid the Ferryman."));
    }

    public void FlipCoin()
    {
        wagerButton.interactable = false;
        acceptFateButton.interactable = false;

        float outcome = Random.value; // 0.0 to 1.0

        if (outcome > 0.5f)
        {
            // WIN
            resultText.text = "<color=green>DEATH REJECTED YOU.</color>";
            StartCoroutine(PvPRevive());
        }
        else
        {
            // LOSS
            resultText.text = "<color=red>THE ABYSS TAKES YOU.</color>";
            // In a real backend, we would send a signal here to double the MMR deduction
            GameOver("You gambled with your soul... and lost.");
        }
    }

    IEnumerator PvPRevive()
    {
        yield return new WaitForSeconds(1.5f);
        deathScreenPanel.SetActive(false);
        
        GameManager.instance.playerHealth = 1;
        GameManager.instance.isUnconscious = false;
        GameManager.instance.UpdateUI();
        
        // Return to shop immediately
        if (CombatManager.instance != null) CombatManager.instance.ForceReturnToShop();
    }

    // --- PVE LOGIC (D&D SAVES) ---

    void SetupPvEDeath()
    {
        pvpContainer.SetActive(false);
        pveContainer.SetActive(true);

        titleText.text = "UNCONSCIOUS";
        int currentHP = GameManager.instance.playerHealth;
        int targetDC = Mathf.Abs(currentHP) + 1; 

        infoText.text = $"Constitution Save DC: {targetDC}\n(Failures: {failureCount}/3)";
        resultText.text = "";

        rollSaveButton.interactable = true;
        rollSaveButton.GetComponentInChildren<TMP_Text>().text = "ROLL SAVE";
        rollSaveButton.onClick.RemoveAllListeners();
        rollSaveButton.onClick.AddListener(RollDice);

        pveSuccumbButton.interactable = true;
        pveSuccumbButton.onClick.RemoveAllListeners();
        pveSuccumbButton.onClick.AddListener(() => GameOver("You succumbed to your wounds."));
    }

    public void RollDice()
    {
        rollSaveButton.interactable = false;
        pveSuccumbButton.interactable = false; 
        
        // Simple D20 logic for now
        int roll = Random.Range(1, 21);
        int total = roll; // Add modifiers later?

        int currentHP = GameManager.instance.playerHealth;
        int targetDC = Mathf.Abs(currentHP) + 1;

        string res = $"Rolled: {roll}";
        
        if (total >= targetDC)
        {
            res += "\n<color=green>STABILIZED!</color>";
            resultText.text = res;
            StartCoroutine(PvERevive());
        }
        else
        {
            res += "\n<color=red>FAILURE...</color>";
            resultText.text = res;
            HandlePvEFailure();
        }
    }

    IEnumerator PvERevive()
    {
        yield return new WaitForSeconds(1.5f);
        deathScreenPanel.SetActive(false);
        exhaustionLevel++; 
        failureCount = 0;  
        
        GameManager.instance.playerHealth = 1;
        GameManager.instance.isUnconscious = false;
        GameManager.instance.UpdateUI();
        
        if (CombatManager.instance != null) CombatManager.instance.ForceReturnToShop();
    }

    void HandlePvEFailure()
    {
        failureCount++;
        if (failureCount >= 3)
        {
            GameOver("You failed your death saving throws.");
        }
        else
        {
            // Refresh UI for next roll
            SetupPvEDeath();
        }
    }

    // --- SHARED LOGIC ---

    public void TriggerVictory()
    {
        deathScreenPanel.SetActive(true);
        pveContainer.SetActive(false);
        pvpContainer.SetActive(false);
        ShowGameOverButtons();

        titleText.text = "VICTORY!";
        infoText.text = "";
        resultText.text = $"<color=green>CHAMPION</color>\nLast one standing.";
        
        if (GameManager.instance.heroText != null) GameManager.instance.heroText.text = "CHAMPION";
        if (PlayerProfile.instance != null) PlayerProfile.instance.RecordMatchResult(1);
    }

    public void SuccumbToDeath()
    {
        GameOver("You concede.");
    }

    void GameOver(string flavorText)
    {
        deathScreenPanel.SetActive(true);
        pveContainer.SetActive(false);
        pvpContainer.SetActive(false);
        ShowGameOverButtons();

        // Calculate Placement
        int placement = 8; 
        if (LobbyManager.instance != null)
        {
            int livingOpponents = LobbyManager.instance.opponents.Count(x => !x.isDead);
            placement = 1 + livingOpponents;
            
            if (PlayerProfile.instance != null)
            {
                PlayerProfile.instance.RecordMatchResult(placement);
                flavorText += $"\n\nRank: {PlayerProfile.instance.mmr}";
            }
            flavorText += $"\nPlaced: #{placement}";
        }

        titleText.text = "GAME OVER";
        resultText.text = flavorText;
        if (GameManager.instance.heroText != null) GameManager.instance.heroText.text = "DEAD";
    }

    void ShowGameOverButtons()
    {
        if(mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true);
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() => {
                Time.timeScale = 1f;
                SceneManager.LoadScene(0);
            });
        }
        
        if(playAgainButton != null)
        {
            playAgainButton.gameObject.SetActive(true);
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(() => {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            });
        }
    }
    
    void HideGameOverButtons()
    {
        if(mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
        if(playAgainButton != null) playAgainButton.gameObject.SetActive(false);
    }
}