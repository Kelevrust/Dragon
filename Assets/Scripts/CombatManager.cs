using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; 

public class CombatManager : MonoBehaviour
{
    public static CombatManager instance;

    [Header("Game Pace")]
    [Tooltip("Time between attacks. Lower is faster.")]
    public float combatPace = 0.5f; 
    [Tooltip("Time to wait before returning to shop.")]
    public float shopReturnDelay = 1.0f;

    [Header("References")]
    public Transform playerBoard;
    public Transform enemyBoard;
    public Transform shopContainer; 
    public GameObject cardPrefab;
    
    [System.Serializable]
    public struct SavedUnit 
    {
        public UnitData template;
        public bool isGolden;
        public int permAttack;
        public int permHealth;
    }

    private List<SavedUnit> savedPlayerRoster = new List<SavedUnit>();
    private Transform graveyard; 

    void Awake() 
    { 
        instance = this; 
        GameObject g = new GameObject("Graveyard");
        g.SetActive(false); 
        graveyard = g.transform;
        DontDestroyOnLoad(g);
    }

    public void StartCombat()
    {
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Recruit) return;

        if (shopContainer != null) foreach (Transform child in shopContainer) Destroy(child.gameObject);
        
        SaveRoster();
        
        GameManager.instance.currentPhase = GameManager.GamePhase.Combat;
        
        SpawnEnemies(); 
        
        StartCoroutine(CombatRoutine());
    }

    void SaveRoster()
    {
        savedPlayerRoster.Clear();
        foreach (Transform child in playerBoard)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if (cd != null) 
            {
                SavedUnit snap = new SavedUnit();
                snap.template = cd.unitData;
                snap.isGolden = cd.isGolden; 
                snap.permAttack = cd.permanentAttack;
                snap.permHealth = cd.permanentHealth;
                savedPlayerRoster.Add(snap);

                if (snap.isGolden) Debug.Log($"Saved GOLDEN status for {snap.template.unitName}");
            }
        }
    }

    void SpawnEnemies()
    {
        foreach (Transform child in enemyBoard) Destroy(child.gameObject);

        AIManager ai = AIManager.instance;
        if (ai != null)
        {
            List<UnitData> enemyRoster = ai.GenerateEnemyBoard(GameManager.instance.turnNumber);
            foreach (UnitData data in enemyRoster)
            {
                GameObject newCard = Instantiate(cardPrefab, enemyBoard);
                newCard.GetComponent<CardDisplay>().LoadUnit(data);
                Destroy(newCard.GetComponent<UnityEngine.UI.Button>()); 
            }
        }
    }

    IEnumerator CombatRoutine()
    {
        yield return new WaitForSeconds(combatPace); 

        List<CardDisplay> players = GetUnits(playerBoard);
        List<CardDisplay> enemies = GetUnits(enemyBoard);

        // Track HP locally to handle death logic without modifying card data directly
        Dictionary<CardDisplay, int> combatHealths = new Dictionary<CardDisplay, int>();
        Dictionary<CardDisplay, int> combatAttacks = new Dictionary<CardDisplay, int>();

        foreach (var p in players) 
        {
            combatHealths[p] = p.currentHealth;
            combatAttacks[p] = p.currentAttack;
        }
        foreach (var e in enemies) 
        {
            combatHealths[e] = e.currentHealth; 
            combatAttacks[e] = e.currentAttack;
        }

        while (players.Count > 0 && enemies.Count > 0)
        {
            // Safety check for empty lists
            if (players.Count == 0 || enemies.Count == 0) break;

            CardDisplay pUnit = players[0];
            CardDisplay eUnit = enemies[0];

            // Register any new tokens spawned mid-combat
            if (!combatHealths.ContainsKey(pUnit)) 
            {
                combatHealths[pUnit] = pUnit.currentHealth;
                combatAttacks[pUnit] = pUnit.currentAttack;
            }
            if (!combatHealths.ContainsKey(eUnit)) 
            {
                combatHealths[eUnit] = eUnit.currentHealth;
                combatAttacks[eUnit] = eUnit.currentAttack;
            }

            yield return StartCoroutine(AnimateAttack(pUnit.transform, eUnit.transform));
            yield return StartCoroutine(AnimateAttack(eUnit.transform, pUnit.transform));

            int pDmg = combatAttacks[eUnit];
            int eDmg = combatAttacks[pUnit];

            combatHealths[pUnit] -= pDmg;
            combatHealths[eUnit] -= eDmg;

            UpdateCombatVisuals(pUnit, combatHealths[pUnit]);
            UpdateCombatVisuals(eUnit, combatHealths[eUnit]);

            bool pDies = combatHealths[pUnit] <= 0;
            bool eDies = combatHealths[eUnit] <= 0;

            if (eDies) KillUnit(eUnit, enemies, true);
            if (pDies) KillUnit(pUnit, players, false);

            // Re-fetch lists to ensure we are working with valid, active objects only
            players = GetUnits(playerBoard);
            enemies = GetUnits(enemyBoard);

            yield return new WaitForSeconds(combatPace);
        }

        // Damage Calculation
        enemies = GetUnits(enemyBoard);
        players = GetUnits(playerBoard);
        int damageTaken = 0;

        if (enemies.Count > 0 && players.Count == 0)
        {
            damageTaken = 1; 
            foreach(var enemy in enemies) damageTaken += enemy.unitData.tier;
        }

        EndCombat(players.Count > 0, damageTaken);
    }

    void UpdateCombatVisuals(CardDisplay unit, int currentHp)
    {
        if (unit.healthText == null) return;
        
        unit.healthText.text = currentHp.ToString();
        
        int maxHp = unit.isGolden ? unit.unitData.baseHealth * 2 : unit.unitData.baseHealth;

        if (currentHp < unit.permanentHealth) unit.healthText.color = Color.red;
        else if (currentHp > maxHp) unit.healthText.color = Color.green;
        else unit.healthText.color = Color.black;
    }

    void KillUnit(CardDisplay unit, List<CardDisplay> list, bool isEnemy)
    {
        // 1. Trigger Abilities
        if (AbilityManager.instance != null)
        {
            try 
            { 
                AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnDeath, unit); 
            }
            catch (System.Exception e) 
            { 
                Debug.LogError($"Deathrattle error: {e.Message}"); 
            }
        }

        // 2. Remove from logic list
        if (list.Contains(unit)) list.Remove(unit);

        // 3. NUCLEAR OPTION: Immediately hide and reparent
        if (graveyard != null) unit.transform.SetParent(graveyard);
        unit.gameObject.SetActive(false); // Hide instantly
        Destroy(unit.gameObject); // Queue destruction
    }

    IEnumerator AnimateAttack(Transform attacker, Transform target)
    {
        if (attacker == null || target == null) yield break;
        Vector3 originalPos = attacker.position;
        Vector3 targetPos = target.position;
        float duration = combatPace * 0.2f; 
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (attacker == null) yield break; 
            attacker.position = Vector3.Lerp(originalPos, targetPos, (elapsed / duration) * 0.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < duration)
        {
            if (attacker == null) yield break;
            attacker.position = Vector3.Lerp(attacker.position, originalPos, (elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        if(attacker != null) attacker.position = originalPos;
    }

    List<CardDisplay> GetUnits(Transform board)
    {
        List<CardDisplay> list = new List<CardDisplay>();
        foreach(Transform child in board)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            // Strict check: Must be active in hierarchy to count
            if(cd != null && cd.gameObject.activeInHierarchy) list.Add(cd);
        }
        return list;
    }

    void EndCombat(bool playerWon, int damageTaken)
    {
        if (damageTaken > 0) GameManager.instance.ModifyHealth(-damageTaken);

        GameManager.instance.LogGameState($"Post-Combat (Damage: {damageTaken})");

        if (GameManager.instance.playerHealth <= 0) DeathSaveManager.instance.StartDeathSequence();
        else StartCoroutine(ReturnToShopRoutine());
    }

    public void ForceReturnToShop() { StartCoroutine(ReturnToShopRoutine()); }

    IEnumerator ReturnToShopRoutine()
    {
        yield return new WaitForSeconds(shopReturnDelay);
        
        // Cleanup with extreme prejudice
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in enemyBoard) toDestroy.Add(child.gameObject);
        foreach (Transform child in playerBoard) toDestroy.Add(child.gameObject);
        
        foreach(GameObject g in toDestroy) {
            if(graveyard != null) g.transform.SetParent(graveyard);
            Destroy(g);
        }
        playerBoard.DetachChildren(); 

        List<CardDisplay> spawnedCards = new List<CardDisplay>();

        // Restore Units
        foreach (SavedUnit snap in savedPlayerRoster)
        {
            GameObject newCard = Instantiate(cardPrefab, playerBoard);
            CardDisplay cd = newCard.GetComponent<CardDisplay>();
            
            cd.LoadUnit(snap.template);
            cd.isPurchased = true; 
            
            spawnedCards.Add(cd);
        }

        // Wait for Start() to finish
        yield return null;

        for(int i=0; i<savedPlayerRoster.Count; i++)
        {
            if (i >= spawnedCards.Count) break;
            
            SavedUnit snap = savedPlayerRoster[i];
            CardDisplay cd = spawnedCards[i];

            if (snap.isGolden) cd.MakeGolden(); 
            
            cd.permanentAttack = snap.permAttack;
            cd.permanentHealth = snap.permHealth;
            
            cd.ResetToPermanent();
            cd.UpdateVisuals(); 
        }

        GameManager.instance.turnNumber++;
        GameManager.instance.StartRecruitPhase();
        
        if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();

        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) shop.RerollShop();
        
        Debug.Log("--- RETURN TO SHOP ---");
    }
}