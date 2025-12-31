using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement; 
using System.Linq; 

public class DeathSaveManager : MonoBehaviour
{
    public static DeathSaveManager instance;

    [Header("UI References")]
    public GameObject deathScreenPanel; 
    public TMP_Text hpText;
    public TMP_Text targetDCText;
    public TMP_Text rollResultText;
    public TMP_Text diceInfoText;
    
    [Header("Action Buttons")]
    public Button rollButton;     
    public Button succumbButton;  

    [Header("Game Over Buttons")]
    public Button mainMenuButton; 
    public Button playAgainButton; 

    [Header("State")]
    public int exhaustionLevel = 0;
    public int failureCount = 0;
    
    private int[] diceSizes = new int[] { 20, 12, 10, 8, 6 };

    void Awake() { instance = this; }

    void Start()
    {
        if(deathScreenPanel != null) deathScreenPanel.SetActive(false);
        
        if(mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
        if(playAgainButton != null) playAgainButton.gameObject.SetActive(false);
    }

    public void StartDeathSequence()
    {
        deathScreenPanel.SetActive(true);
        
        rollButton.gameObject.SetActive(true);
        succumbButton.gameObject.SetActive(true);
        if(mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
        if(playAgainButton != null) playAgainButton.gameObject.SetActive(false);

        rollButton.interactable = true;
        succumbButton.interactable = true;
        
        rollButton.GetComponentInChildren<TMP_Text>().text = "ROLL TO SAVE";
        rollButton.onClick.RemoveAllListeners();
        rollButton.onClick.AddListener(RollDice);

        succumbButton.onClick.RemoveAllListeners();
        succumbButton.onClick.AddListener(SuccumbToDeath);

        int currentHP = GameManager.instance.playerHealth;
        int targetDC = Mathf.Abs(currentHP) + 1; 

        hpText.text = $"Health: {currentHP}";
        targetDCText.text = $"Save DC: {targetDC}";
        rollResultText.text = "";

        GetDiceConfig(exhaustionLevel, out int count, out int sides);
        diceInfoText.text = $"Rolling {count}d{sides}\n(Exhaustion: {exhaustionLevel})";
    }

    void GetDiceConfig(int level, out int count, out int sides)
    {
        count = 3;
        sides = 20;

        if (level == 1) sides = 12;
        else if (level == 2) sides = 10;
        else if (level == 3) sides = 8;
        else if (level == 4) sides = 6;
        else if (level == 5) sides = 4;
        else if (level == 6) { count = 2; sides = 4; }
        else if (level >= 7) { count = 1; sides = 4; }
    }

    public void RollDice()
    {
        rollButton.interactable = false;
        succumbButton.interactable = false; 
        
        GetDiceConfig(exhaustionLevel, out int count, out int sides);
        
        int total = 0;
        string rollStr = "Rolled: ";
        
        for(int i=0; i<count; i++)
        {
            int r = Random.Range(1, sides + 1);
            total += r;
            rollStr += $"{r} ";
            if(i < count -1) rollStr += "+ ";
        }

        int currentHP = GameManager.instance.playerHealth;
        int targetDC = Mathf.Abs(currentHP) + 1;

        rollStr += $"= <color=yellow><b>{total}</b></color>";
        
        if (total >= targetDC)
        {
            rollStr += "\n<color=green>STABILIZED!</color>";
            rollResultText.text = rollStr;
            StartCoroutine(AutoStabilize());
        }
        else
        {
            rollStr += "\n<color=red>FAILURE...</color>";
            rollResultText.text = rollStr;
            HandleFailure();
        }
    }

    IEnumerator AutoStabilize()
    {
        yield return new WaitForSeconds(1.5f); 
        
        deathScreenPanel.SetActive(false);
        exhaustionLevel++; 
        failureCount = 0;  
        
        GameManager.instance.playerHealth = 1;
        GameManager.instance.isUnconscious = false;
        GameManager.instance.UpdateUI();
        
        FindFirstObjectByType<CombatManager>().ForceReturnToShop();
    }

    void HandleFailure()
    {
        failureCount++;
        
        if (failureCount >= 3)
        {
            GameOver("You failed your death saving throws.\nYou have died.");
        }
        else
        {
            rollButton.interactable = true;
            rollButton.GetComponentInChildren<TMP_Text>().text = "PASS OUT (Unconscious)";
            rollButton.onClick.RemoveAllListeners();
            rollButton.onClick.AddListener(ProceedUnconscious);
        }
    }

    void ProceedUnconscious()
    {
        deathScreenPanel.SetActive(false);
        GameManager.instance.isUnconscious = true;
        FindFirstObjectByType<CombatManager>().ForceReturnToShop();
    }

    public void SuccumbToDeath()
    {
        GameOver("You succumb to the cold embrace of death.");
    }

    // NEW: Victory Logic
    public void TriggerVictory()
    {
        deathScreenPanel.SetActive(true);
        
        // Hide Death Actions
        rollButton.gameObject.SetActive(false);
        succumbButton.gameObject.SetActive(false);

        ShowGameOverButtons();

        // Clear Death Info
        hpText.text = "";
        targetDCText.text = "";
        diceInfoText.text = "";
        
        rollResultText.text = $"<color=green>VICTORY!</color>\nYou are the last one standing!\n\nPlaced: #1";
        
        if (GameManager.instance.heroText != null) GameManager.instance.heroText.text = "CHAMPION";

        // Record Win
        if (PlayerProfile.instance != null)
        {
            PlayerProfile.instance.RecordMatchResult(1);
            rollResultText.text += $"\nRank: {PlayerProfile.instance.mmr}";
        }
    }

    void GameOver(string flavorText)
    {
        deathScreenPanel.SetActive(true);
        
        rollButton.gameObject.SetActive(false);
        succumbButton.gameObject.SetActive(false);

        ShowGameOverButtons();

        // Calculate Placement
        int placement = 8; 
        if (LobbyManager.instance != null)
        {
            int livingOpponents = LobbyManager.instance.opponents.Count(x => !x.isDead);
            placement = 1 + livingOpponents;
            
            string rankText = "";
            if (PlayerProfile.instance != null)
            {
                PlayerProfile.instance.RecordMatchResult(placement);
                rankText = $"\nRank: {PlayerProfile.instance.mmr}";
            }
            
            flavorText += $"\n\nPlaced: #{placement}{rankText}";
        }

        rollResultText.text = $"<color=red>GAME OVER</color>\n{flavorText}";
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
}