using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; 

public class CombatManager : MonoBehaviour
{
    public static CombatManager instance;

    [Header("Game Pace")]
    public float combatPace = 0.5f; 
    public float shopReturnDelay = 1.0f;

    [Header("References")]
    public Transform playerBoard;
    public Transform enemyBoard;
    public Transform shopContainer; 
    public Transform playerHand; 
    public GameObject cardPrefab;
    
    [System.Serializable]
    public struct SavedUnit 
    {
        public UnitData template;
        public bool isGolden;
        public int permAttack;
        public int permHealth;
        public bool wasInHand; 
        // Note: We don't save Reborn/Shield states for next round (they reset), 
        // unless you want persistent broken shields? Standard is reset.
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
        foreach (Transform child in playerBoard) SaveUnitFromTransform(child, false);
        if (GameManager.instance.playerHand != null)
        {
            foreach (Transform child in GameManager.instance.playerHand) SaveUnitFromTransform(child, true);
        }
    }

    void SaveUnitFromTransform(Transform t, bool inHand)
    {
        CardDisplay cd = t.GetComponent<CardDisplay>();
        if (cd != null) 
        {
            SavedUnit snap = new SavedUnit();
            snap.template = cd.unitData;
            snap.isGolden = cd.isGolden; 
            snap.permAttack = cd.permanentAttack;
            snap.permHealth = cd.permanentHealth;
            snap.wasInHand = inHand;
            savedPlayerRoster.Add(snap);
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

        // SYNC STARTING STATS to CardDisplay internal trackers
        // We will rely on CardDisplay.currentHealth/Attack for the source of truth
        // instead of a local Dictionary, because CardDisplay handles Divine Shield logic.

        while (players.Count > 0 && enemies.Count > 0)
        {
            players = GetUnits(playerBoard);
            enemies = GetUnits(enemyBoard);
            if (players.Count == 0 || enemies.Count == 0) break;

            // 1. Pick Attacker (Leftmost)
            CardDisplay pAttacker = players[0];
            CardDisplay eAttacker = enemies[0];

            // 2. Pick Targets (Handle TAUNT)
            CardDisplay pTarget = GetTarget(pAttacker, enemies); // Player unit attacks Enemy unit
            CardDisplay eTarget = GetTarget(eAttacker, players); // Enemy unit attacks Player unit

            // 3. Animation
            yield return StartCoroutine(AnimateAttack(pAttacker.transform, pTarget.transform));
            yield return StartCoroutine(AnimateAttack(eAttacker.transform, eTarget.transform));

            // 4. Deal Damage
            // Note: We use the Attacker's attack vs the Target's TakeDamage function
            int pDmg = eAttacker.currentAttack;
            int eDmg = pAttacker.currentAttack;

            pTarget.TakeDamage(pDmg); // Enemy hits Player Unit
            eTarget.TakeDamage(eDmg); // Player hits Enemy Unit

            // 5. Check Deaths
            bool pDies = pTarget.currentHealth <= 0;
            bool eDies = eTarget.currentHealth <= 0;

            // Handle Reborn / Death
            if (eDies) HandleDeath(eTarget, enemies);
            if (pDies) HandleDeath(pTarget, players);

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

    CardDisplay GetTarget(CardDisplay attacker, List<CardDisplay> defenders)
    {
        // Check for Taunt
        List<CardDisplay> taunts = new List<CardDisplay>();
        foreach(var d in defenders)
        {
            if (d.unitData.hasTaunt) taunts.Add(d);
        }

        if (taunts.Count > 0)
        {
            return taunts[Random.Range(0, taunts.Count)];
        }
        
        // No taunt, random target
        return defenders[Random.Range(0, defenders.Count)];
    }

    void HandleDeath(CardDisplay unit, List<CardDisplay> list)
    {
        // 1. REBORN CHECK
        if (unit.hasReborn)
        {
            Debug.Log($"{unit.unitData.unitName} Reborn!");
            unit.hasReborn = false; // Consume Reborn
            unit.currentHealth = 1; // Set HP to 1
            unit.damageTaken = unit.permanentHealth - 1; // Sync damage tracking
            unit.UpdateVisuals();
            return; // SURVIVED!
        }

        // 2. DEATHRATTLES
        if (AbilityManager.instance != null)
        {
            try { AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnDeath, unit); }
            catch (System.Exception e) { Debug.LogError($"Deathrattle error: {e.Message}"); }
        }

        // 3. CLEANUP
        list.Remove(unit);
        if (graveyard != null) unit.transform.SetParent(graveyard);
        unit.gameObject.SetActive(false);
        Destroy(unit.gameObject);
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
            if(cd != null && cd.gameObject.activeSelf) list.Add(cd);
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
        
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in enemyBoard) toDestroy.Add(child.gameObject);
        foreach (Transform child in playerBoard) toDestroy.Add(child.gameObject);
        if (GameManager.instance.playerHand != null)
            foreach (Transform child in GameManager.instance.playerHand) toDestroy.Add(child.gameObject);
        
        foreach(GameObject g in toDestroy) {
            if(graveyard != null) g.transform.SetParent(graveyard);
            Destroy(g);
        }
        playerBoard.DetachChildren(); 

        List<CardDisplay> spawnedCards = new List<CardDisplay>();

        foreach (SavedUnit snap in savedPlayerRoster)
        {
            Transform targetParent = snap.wasInHand ? GameManager.instance.playerHand : playerBoard;
            GameObject newCard = Instantiate(cardPrefab, targetParent);
            CardDisplay cd = newCard.GetComponent<CardDisplay>();
            cd.LoadUnit(snap.template);
            cd.isPurchased = true; 
            spawnedCards.Add(cd);
        }

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
        if (shop != null) 
        {
            shop.ReduceUpgradeCost();
            shop.RerollShop();
        }
        
        Debug.Log("--- RETURN TO SHOP ---");
    }
}