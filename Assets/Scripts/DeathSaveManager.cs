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
    public Button wagerButton;      // "Bribe Charon (1 More Turn / Double Risk)"
    public Button acceptFateButton; // "Pay the Ferryman (Standard Loss)"

    [Header("Game Over Buttons")]
    public Button mainMenuButton; 
    public Button playAgainButton; 

    [Header("State")]
    public int exhaustionLevel = 0;
    public int failureCount = 0;
    public bool hasCheatedDeath = false; // Track if they already used the buyback
    
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
        if (deathScreenPanel != null) deathScreenPanel.SetActive(true);
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
        if (pveContainer != null) pveContainer.SetActive(false);
        if (pvpContainer != null) pvpContainer.SetActive(true);

        if (titleText != null) titleText.text = "YOU ARE DEAD";
        
        if (!hasCheatedDeath)
        {
            if (infoText != null) infoText.text = "The Ferryman awaits his coin...\n\n<color=red>Bribe Charon?</color>\nGain: 1 Turn (1 HP, Half Gold)\nRisk: Double MMR Penalty if you die again.";
            if (resultText != null) resultText.text = "";

            if (wagerButton != null)
            {
                wagerButton.interactable = true;
                wagerButton.GetComponentInChildren<TMP_Text>().text = "BRIBE CHARON";
                wagerButton.onClick.RemoveAllListeners();
                wagerButton.onClick.AddListener(AttemptCheatDeath);
            }
        }
        else
        {
            // You already cheated death once and died again. No second chances.
            if (infoText != null) infoText.text = "Charon does not accept the same coin twice.\n\n<color=red>DOUBLE PENALTY APPLIED.</color>";
            if (wagerButton != null) wagerButton.gameObject.SetActive(false); // Hide button
        }

        if (acceptFateButton != null)
        {
            acceptFateButton.interactable = true;
            acceptFateButton.onClick.RemoveAllListeners();
            acceptFateButton.onClick.AddListener(() => GameOver("You paid the Ferryman."));
        }
    }

    public void AttemptCheatDeath()
    {
        if (wagerButton != null) wagerButton.interactable = false;
        if (acceptFateButton != null) acceptFateButton.interactable = false;

        hasCheatedDeath = true;

        if (resultText != null) resultText.text = "<color=green>ONE MORE TURN.</color>";
        
        StartCoroutine(PvPRevive());
    }

    IEnumerator PvPRevive()
    {
        yield return new WaitForSeconds(1.5f);
        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);
        
        if (GameManager.instance != null)
        {
            GameManager.instance.playerHealth = 1;
            GameManager.instance.isUnconscious = false;
            
            // Apply Penalty: Halve current gold (or set a flag for next turn income)
            // Ideally, we reduce income for the NEXT recruit phase.
            // For now, let's just slash current gold to represent the bribe cost immediately.
            GameManager.instance.gold = Mathf.FloorToInt(GameManager.instance.gold / 2f);
            
            GameManager.instance.UpdateUI();
            
            // NOTE: To implement "Half Gold next turn", we would need to add a flag to GameManager 
            // like `GameManager.instance.applyCharonPenalty = true;`
        }
        
        // Return to shop immediately to start that desperate turn
        if (CombatManager.instance != null) CombatManager.instance.ForceReturnToShop();
    }

    // --- PVE LOGIC (D&D SAVES) ---

    void SetupPvEDeath()
    {
        if (pvpContainer != null) pvpContainer.SetActive(false);
        if (pveContainer != null) pveContainer.SetActive(true);

        if (titleText != null) titleText.text = "UNCONSCIOUS";
        
        int currentHP = GameManager.instance != null ? GameManager.instance.playerHealth : 0;
        int targetDC = Mathf.Abs(currentHP) + 1; 

        if (infoText != null) infoText.text = $"Constitution Save DC: {targetDC}\n(Failures: {failureCount}/3)";
        if (resultText != null) resultText.text = "";

        if (rollSaveButton != null)
        {
            rollSaveButton.interactable = true;
            TMP_Text btnText = rollSaveButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "ROLL SAVE";
            
            rollSaveButton.onClick.RemoveAllListeners();
            rollSaveButton.onClick.AddListener(RollDice);
        }

        if (pveSuccumbButton != null)
        {
            pveSuccumbButton.interactable = true;
            pveSuccumbButton.onClick.RemoveAllListeners();
            pveSuccumbButton.onClick.AddListener(() => GameOver("You succumbed to your wounds."));
        }
    }

    public void RollDice()
    {
        if (rollSaveButton != null) rollSaveButton.interactable = false;
        if (pveSuccumbButton != null) pveSuccumbButton.interactable = false; 
        
        // Simple D20 logic for now
        int roll = Random.Range(1, 21);
        int total = roll; // Add modifiers later?

        int currentHP = GameManager.instance != null ? GameManager.instance.playerHealth : 0;
        int targetDC = Mathf.Abs(currentHP) + 1;

        string res = $"Rolled: {roll}";
        
        if (total >= targetDC)
        {
            res += "\n<color=green>STABILIZED!</color>";
            if (resultText != null) resultText.text = res;
            StartCoroutine(PvERevive());
        }
        else
        {
            res += "\n<color=red>FAILURE...</color>";
            if (resultText != null) resultText.text = res;
            HandlePvEFailure();
        }
    }

    IEnumerator PvERevive()
    {
        yield return new WaitForSeconds(1.5f);
        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);
        exhaustionLevel++; 
        failureCount = 0;  
        
        if (GameManager.instance != null)
        {
            GameManager.instance.playerHealth = 1;
            GameManager.instance.isUnconscious = false;
            GameManager.instance.UpdateUI();
        }
        
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
        Debug.Log("Triggering Victory Screen...");
        if (deathScreenPanel != null) deathScreenPanel.SetActive(true);
        if (pveContainer != null) pveContainer.SetActive(false);
        if (pvpContainer != null) pvpContainer.SetActive(false);
        ShowGameOverButtons();

        if (titleText != null) titleText.text = "VICTORY!";
        if (infoText != null) infoText.text = "";
        if (resultText != null) resultText.text = $"<color=green>CHAMPION</color>\nLast one standing.";
        
        if (GameManager.instance != null && GameManager.instance.heroText != null) 
            GameManager.instance.heroText.text = "CHAMPION";
            
        // Safe access to PlayerProfile
        if (PlayerProfile.instance != null) 
        {
            PlayerProfile.instance.RecordMatchResult(1);
        }
    }

    public void SuccumbToDeath()
    {
        GameOver("You concede.");
    }

    void GameOver(string flavorText)
    {
        if (deathScreenPanel != null) deathScreenPanel.SetActive(true);
        if (pveContainer != null) pveContainer.SetActive(false);
        if (pvpContainer != null) pvpContainer.SetActive(false);
        ShowGameOverButtons();

        // Calculate Placement
        int placement = 8; 
        if (LobbyManager.instance != null)
        {
            int livingOpponents = LobbyManager.instance.opponents.Count(x => !x.isDead);
            placement = 1 + livingOpponents;
            
            // Safe recording
            if (PlayerProfile.instance != null)
            {
                // Logic hook: If hasCheatedDeath is true, we should double the MMR loss here.
                // Currently RecordMatchResult calculates based on placement. 
                // You might want to overload that function: RecordMatchResult(placement, hasCheatedDeath)
                PlayerProfile.instance.RecordMatchResult(placement);
                flavorText += $"\n\nRank: {PlayerProfile.instance.mmr}";
            }
            flavorText += $"\nPlaced: #{placement}";
        }

        if (titleText != null) titleText.text = "GAME OVER";
        if (resultText != null) resultText.text = flavorText;
        if (GameManager.instance != null && GameManager.instance.heroText != null) 
            GameManager.instance.heroText.text = "DEAD";
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