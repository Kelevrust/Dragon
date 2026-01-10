using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; 
using TMPro; 
using System.Linq; 

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;

    [Header("Configuration")]
    public GameObject cardPrefab; 
    public Transform shopContainer; 
    public int shopSize = 3;
    public int rerollCost = 1; 

    [Header("Tavern Tech")]
    public int tavernTier = 1;
    public int[] tierCosts = new int[] { 0, 0, 5, 7, 8, 9, 10 }; 
    public int maxTier = 6;
    public int currentDiscount = 0;
    public int[] shopSlotsPerTier = new int[] { 0, 3, 4, 4, 5, 5, 6 }; 

    [Header("Freeze System")]
    public bool isFrozen = false;
    public Button freezeButton;
    public Image freezeButtonImage; 
    public TMP_Text freezeButtonText; 
    
    [Header("Freeze Visuals")]
    public Color frozenColor = Color.cyan;
    public Color normalColor = Color.white;
    public string frozenString = "LOCKED";
    public string normalString = "FREEZE";

    [Header("UI References")]
    public TMP_Text upgradeButtonText; 

    [Header("Database & Pool")]
    public UnitData[] availableUnits; 
    public List<Tribe> activeTribes = new List<Tribe>();
    public int maxTribesInGame = 5;
    
    private int[] poolSizeByTier = new int[] { 0, 16, 15, 13, 11, 9, 7 };

    void Awake() { instance = this; }

    void Start()
    {
        InitializeGamePool();
        UpdateUpgradeUI();
        UpdateFreezeUI(); 
        RerollShop();
    }

    void InitializeGamePool()
    {
        List<Tribe> allTribes = System.Enum.GetValues(typeof(Tribe)).Cast<Tribe>().ToList();
        allTribes.Remove(Tribe.None); 
        
        activeTribes.Clear();
        
        for (int i = 0; i < allTribes.Count; i++) {
             Tribe temp = allTribes[i];
             int randomIndex = Random.Range(i, allTribes.Count);
             allTribes[i] = allTribes[randomIndex];
             allTribes[randomIndex] = temp;
        }
        
        for(int i = 0; i < Mathf.Min(maxTribesInGame, allTribes.Count); i++)
        {
            activeTribes.Add(allTribes[i]);
        }
        
        Debug.Log("Active Tribes: " + string.Join(", ", activeTribes));
    }

    public void SetPoolSizes(int[] newSizes)
    {
        if (newSizes.Length == poolSizeByTier.Length)
        {
            poolSizeByTier = newSizes;
        }
    }

    public void ForceSetTier(int tier)
    {
        tavernTier = Mathf.Clamp(tier, 1, maxTier);
        UpdateUpgradeUI();
    }

    public void ModifyTribes(Tribe tribe, bool add)
    {
        if (add && !activeTribes.Contains(tribe))
        {
            activeTribes.Add(tribe);
        }
        else if (!add && activeTribes.Contains(tribe))
        {
            activeTribes.Remove(tribe);
        }
    }

    public void ToggleFreeze()
    {
        isFrozen = !isFrozen;
        if (isFrozen) ApplyHeroFreezeVisuals();
        else ClearFreezeVisuals();
        UpdateFreezeUI();
    }

    void ApplyHeroFreezeVisuals()
    {
        HeroData hero = GameManager.instance.activeHero;
        if (hero != null && hero.freezeSound != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(hero.freezeSound);
        }

        if (hero != null && hero.freezeVFXPrefab != null)
        {
            foreach(Transform child in shopContainer)
            {
                if (child.GetComponentInChildren<FreezeVFXTag>() != null) continue;

                GameObject vfx = Instantiate(hero.freezeVFXPrefab, child);
                if (vfx.transform is RectTransform rt)
                {
                    rt.anchoredPosition = Vector2.zero;
                    rt.localScale = Vector3.one; 
                }
                else
                {
                    vfx.transform.localPosition = new Vector3(0, 0, -50); 
                }
                vfx.AddComponent<FreezeVFXTag>(); 
            }
        }
    }

    void ClearFreezeVisuals()
    {
        FreezeVFXTag[] effects = shopContainer.GetComponentsInChildren<FreezeVFXTag>(true);
        foreach(var fx in effects) Destroy(fx.gameObject);
    }

    void UpdateFreezeUI()
    {
        if (freezeButtonImage != null) freezeButtonImage.color = isFrozen ? frozenColor : normalColor;
        if (freezeButtonText != null) freezeButtonText.text = isFrozen ? frozenString : normalString;
    }

    public void HandleTurnStartRefresh()
    {
        if (isFrozen)
        {
            isFrozen = false;
            ClearFreezeVisuals();
            UpdateFreezeUI();
            GenerateCards(); 
            return;
        }

        ReduceUpgradeCost(); 
        RerollShop();
    }

    public void RerollShop()
    {
        if (GameManager.instance.isUnconscious) return;
        
        isFrozen = false;
        ClearFreezeVisuals();
        UpdateFreezeUI();

        ClearShop();
        GenerateCards();
    }

    public void ReduceUpgradeCost()
    {
        if (tavernTier < maxTier)
        {
            currentDiscount++;
            UpdateUpgradeUI();
        }
    }

    public void OnRerollClick()
    {
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit) return;
        if (GameManager.instance.isUnconscious) return;

        if (GameManager.instance.TrySpendGold(rerollCost))
        {
            GameManager.instance.LogAction("Rerolled Shop");
            RerollShop();
        }
    }

    public void OnUpgradeClick()
    {
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit) return;
        if (tavernTier >= maxTier) return;

        int cost = GetUpgradeCost();

        if (GameManager.instance.TrySpendGold(cost))
        {
            tavernTier++;
            currentDiscount = 0;
            GameManager.instance.LogAction($"Upgraded Tavern to Tier {tavernTier}");
            GameManager.instance.UpdateUI(); 
            UpdateUpgradeUI(); 
            GenerateCards();
        }
    }

    int GetUpgradeCost()
    {
        int nextTier = tavernTier + 1;
        if (nextTier < tierCosts.Length) return Mathf.Max(0, tierCosts[nextTier] - currentDiscount);
        return 0; 
    }

    void UpdateUpgradeUI()
    {
        if (upgradeButtonText != null)
        {
            if (tavernTier >= maxTier) upgradeButtonText.text = "Max Tier";
            else upgradeButtonText.text = $"Upgrade ({GetUpgradeCost()}g)";
        }
    }

    void ClearShop()
    {
        foreach (Transform child in shopContainer) Destroy(child.gameObject);
        shopContainer.DetachChildren(); 
    }

    void GenerateCards()
    {
        int targetSize = 3;
        if (tavernTier < shopSlotsPerTier.Length) targetSize = shopSlotsPerTier[tavernTier];

        int currentCount = shopContainer.childCount;
        int cardsNeeded = targetSize - currentCount;

        if (cardsNeeded <= 0) return;

        string spawnLog = $"Shop Spawned (Tier {tavernTier}): ";

        List<UnitData> candidatePool = BuildWeightedPool();
        
        if (candidatePool.Count == 0)
        {
            Debug.LogError("Card Pool Empty! Cannot generate shop.");
            return;
        }

        for (int i = 0; i < cardsNeeded; i++)
        {
            if (candidatePool.Count == 0) break;

            int pickIndex = Random.Range(0, candidatePool.Count);
            UnitData pickedUnit = candidatePool[pickIndex];
            
            GameObject newCard = Instantiate(cardPrefab, shopContainer);
            spawnLog += $"[{pickedUnit.unitName} T{pickedUnit.tier}] ";

            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null) display.LoadUnit(pickedUnit);
        }
        Debug.Log(spawnLog);
    }

    List<UnitData> BuildWeightedPool()
    {
        List<UnitData> pool = new List<UnitData>();
        
        Dictionary<string, int> playerHeld = new Dictionary<string, int>();
        CountUnitsInHierarchy(GameManager.instance.playerBoard, playerHeld);
        CountUnitsInHierarchy(GameManager.instance.playerHand, playerHeld);

        foreach (UnitData u in availableUnits)
        {
            if (u.tier > tavernTier) continue;
            if (u.tribe != Tribe.None && !activeTribes.Contains(u.tribe)) continue;

            int maxCopies = 15; 
            if (u.tier < poolSizeByTier.Length) maxCopies = poolSizeByTier[u.tier];

            int heldByPlayer = playerHeld.ContainsKey(u.id) ? playerHeld[u.id] : 0;
            
            float heldByAI = 0;
            if (LobbyManager.instance != null && LobbyManager.instance.aiPoolUsage.ContainsKey(u.id))
            {
                heldByAI = LobbyManager.instance.aiPoolUsage[u.id];
            }

            int remaining = Mathf.FloorToInt(maxCopies - heldByPlayer - heldByAI);
            
            // FIX: Prevent pool starvation by enforcing minimum of 1 for non-Golden logic (soft pool)
            remaining = Mathf.Max(1, remaining);

            for (int k = 0; k < remaining; k++) pool.Add(u);
        }

        return pool;
    }

    void CountUnitsInHierarchy(Transform parent, Dictionary<string, int> counts)
    {
        if (parent == null) return;
        foreach (Transform child in parent)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if (cd != null && cd.unitData != null)
            {
                if (!counts.ContainsKey(cd.unitData.id)) counts[cd.unitData.id] = 0;
                int value = cd.isGolden ? 3 : 1;
                counts[cd.unitData.id] += value;
            }
        }
    }
}