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

        // --- FIX: Use CURRENT stats (with Buffs) not Base stats ---
        Dictionary<CardDisplay, int> combatHealths = new Dictionary<CardDisplay, int>();
        Dictionary<CardDisplay, int> combatAttacks = new Dictionary<CardDisplay, int>();

        foreach (var p in players) 
        {
            combatHealths[p] = p.currentHealth;
            combatAttacks[p] = p.currentAttack;
        }
        foreach (var e in enemies) 
        {
            combatHealths[e] = e.currentHealth; // AI usually has base, but prepared for future buffs
            combatAttacks[e] = e.currentAttack;
        }

        while (players.Count > 0 && enemies.Count > 0)
        {
            players = GetUnits(playerBoard);
            enemies = GetUnits(enemyBoard);
            
            if (players.Count == 0 || enemies.Count == 0) break;

            CardDisplay pUnit = players[0];
            CardDisplay eUnit = enemies[0];

            // Register tokens spawned mid-combat
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

            // Use the Snapshotted Attack values
            int pDmg = combatAttacks[eUnit];
            int eDmg = combatAttacks[pUnit];

            combatHealths[pUnit] -= pDmg;
            combatHealths[eUnit] -= eDmg;

            // Visual Updates with Color Logic
            UpdateCombatVisuals(pUnit, combatHealths[pUnit]);
            UpdateCombatVisuals(eUnit, combatHealths[eUnit]);

            bool pDies = combatHealths[pUnit] <= 0;
            bool eDies = combatHealths[eUnit] <= 0;

            if (eDies) KillUnit(eUnit, enemies, true);
            if (pDies) KillUnit(pUnit, players, false);

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
        
        // Simple visual check for Red text during combat
        if (currentHp < unit.permanentHealth) unit.healthText.color = Color.red;
        // Note: We don't revert to Green/Black here easily without tracking max, 
        // leaving it Red is good for "Damaged" feedback.
    }

    void KillUnit(CardDisplay unit, List<CardDisplay> list, bool isEnemy)
    {
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
        
        foreach(GameObject g in toDestroy) {
            if(graveyard != null) g.transform.SetParent(graveyard);
            Destroy(g);
        }
        playerBoard.DetachChildren(); 

        foreach (SavedUnit snap in savedPlayerRoster)
        {
            GameObject newCard = Instantiate(cardPrefab, playerBoard);
            CardDisplay cd = newCard.GetComponent<CardDisplay>();
            
            cd.LoadUnit(snap.template);
            cd.isPurchased = true; 
            
            if (snap.isGolden) cd.MakeGolden(); 
            
            // Restore Persistent Stats
            cd.permanentAttack = snap.permAttack;
            cd.permanentHealth = snap.permHealth;
            
            cd.ResetToPermanent();
            cd.UpdateVisuals(); 
        }

        GameManager.instance.turnNumber++;
        GameManager.instance.StartRecruitPhase();
        
        // Re-Apply Auras so Wolf buffs come back
        if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();

        ShopManager shop = FindFirstObjectByType<ShopManager>();
        if (shop != null) shop.RerollShop();
    }
}