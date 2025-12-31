using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Text; 

public class CombatManager : MonoBehaviour
{
    public static CombatManager instance;

    [Header("Game Pace")]
    [Tooltip("Time between attacks. Lower is faster.")]
    public float combatPace = 0.5f; 
    [Tooltip("Time to wait before returning to shop.")]
    public float shopReturnDelay = 1.0f;

    [Header("Audio FX")]
    public AudioClip combatStartClip;
    public AudioClip attackClip;
    public AudioClip hitClip;
    public AudioClip deathClip;
    public AudioClip victoryClip;
    public AudioClip defeatClip;

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

        // --- FREEZE FIX: HIDE SHOP ONLY ---
        if (shopContainer != null) 
        {
            // Do NOT Destroy children here. Just hide the container.
            shopContainer.gameObject.SetActive(false);
        }
        
        SaveRoster();
        GameManager.instance.currentPhase = GameManager.GamePhase.Combat;
        
        if (LobbyManager.instance != null)
        {
            var opponent = LobbyManager.instance.GetNextOpponent();
            if (opponent != null) Debug.Log($"<color=orange>Fighting Opponent: {opponent.name} | {opponent.rank} ({opponent.mmr})</color>");
        }

        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(combatStartClip);

        SpawnEnemies(); 
        StartCoroutine(CombatRoutine());
    }

    void SaveRoster()
    {
        savedPlayerRoster.Clear();
        int goldenCount = 0;

        foreach (Transform child in playerBoard) 
        {
            if (SaveUnitFromTransform(child, false)) goldenCount++;
        }
        if (GameManager.instance.playerHand != null)
        {
            foreach (Transform child in GameManager.instance.playerHand)
            {
                if (SaveUnitFromTransform(child, true)) goldenCount++;
            }
        }
        Debug.Log($"Saved Roster: {savedPlayerRoster.Count} Units ({goldenCount} Golden).");
    }

    bool SaveUnitFromTransform(Transform t, bool inHand)
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
            return snap.isGolden;
        }
        return false;
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

        Dictionary<CardDisplay, int> combatHealths = new Dictionary<CardDisplay, int>();
        Dictionary<CardDisplay, int> combatAttacks = new Dictionary<CardDisplay, int>();
        Dictionary<CardDisplay, bool> currentShields = new Dictionary<CardDisplay, bool>();

        foreach (var p in players) { combatHealths[p] = p.currentHealth; combatAttacks[p] = p.currentAttack; currentShields[p] = p.hasDivineShield; }
        foreach (var e in enemies) { combatHealths[e] = e.currentHealth; combatAttacks[e] = e.currentAttack; currentShields[e] = e.hasDivineShield; }

        int round = 1;

        while (players.Count > 0 && enemies.Count > 0)
        {
            players = GetUnits(playerBoard);
            enemies = GetUnits(enemyBoard);
            if (players.Count == 0 || enemies.Count == 0) break;

            CardDisplay pAttacker = players[0];
            CardDisplay eAttacker = enemies[0];
            
            CardDisplay targetOfPlayer = GetTarget(enemies); 
            CardDisplay targetOfEnemy = GetTarget(players);  

            // Sync dynamic tokens
            if (!combatHealths.ContainsKey(pAttacker)) { combatHealths[pAttacker] = pAttacker.currentHealth; combatAttacks[pAttacker] = pAttacker.currentAttack; currentShields[pAttacker] = pAttacker.hasDivineShield; }
            if (!combatHealths.ContainsKey(eAttacker)) { combatHealths[eAttacker] = eAttacker.currentHealth; combatAttacks[eAttacker] = eAttacker.currentAttack; currentShields[eAttacker] = eAttacker.hasDivineShield; }
            if (!combatHealths.ContainsKey(targetOfPlayer)) { combatHealths[targetOfPlayer] = targetOfPlayer.currentHealth; combatAttacks[targetOfPlayer] = targetOfPlayer.currentAttack; currentShields[targetOfPlayer] = targetOfPlayer.hasDivineShield; }
            if (!combatHealths.ContainsKey(targetOfEnemy)) { combatHealths[targetOfEnemy] = targetOfEnemy.currentHealth; combatAttacks[targetOfEnemy] = targetOfEnemy.currentAttack; currentShields[targetOfEnemy] = targetOfEnemy.hasDivineShield; }

            yield return StartCoroutine(AnimateAttack(pAttacker.transform, targetOfPlayer.transform));
            yield return StartCoroutine(AnimateAttack(eAttacker.transform, targetOfEnemy.transform));

            if (AudioManager.instance != null) AudioManager.instance.PlaySFX(hitClip);

            int damageFromPlayer = combatAttacks[pAttacker];
            int damageFromEnemy = combatAttacks[eAttacker];

            StringBuilder roundLog = new StringBuilder();
            roundLog.Append($"<b>Round {round}</b>: ");

            string pLog = $"[P] {pAttacker.unitData.unitName} -> {targetOfPlayer.unitData.unitName}";
            if (currentShields[targetOfPlayer] && damageFromPlayer > 0)
            {
                currentShields[targetOfPlayer] = false;
                targetOfPlayer.BreakShield(); 
                pLog += " (Blocked)";
                damageFromPlayer = 0; 
            }
            combatHealths[targetOfPlayer] -= damageFromPlayer;
            pLog += $" ({damageFromPlayer} dmg)";

            string eLog = $"[E] {eAttacker.unitData.unitName} -> {targetOfEnemy.unitData.unitName}";
            if (currentShields[targetOfEnemy] && damageFromEnemy > 0)
            {
                currentShields[targetOfEnemy] = false;
                targetOfEnemy.BreakShield();
                eLog += " (Blocked)";
                damageFromEnemy = 0;
            }
            combatHealths[targetOfEnemy] -= damageFromEnemy;
            eLog += $" ({damageFromEnemy} dmg)";

            roundLog.Append($"{pLog} | {eLog}. ");

            UpdateCombatVisuals(targetOfPlayer, combatHealths[targetOfPlayer]);
            UpdateCombatVisuals(targetOfEnemy, combatHealths[targetOfEnemy]);

            bool enemyDies = combatHealths[targetOfPlayer] <= 0;
            bool playerDies = combatHealths[targetOfEnemy] <= 0;

            if (enemyDies) 
            {
                roundLog.Append($"<color=red>{targetOfPlayer.unitData.unitName} DIED</color>. ");
                HandleDeath(targetOfPlayer, enemies);
            }
            if (playerDies) 
            {
                roundLog.Append($"<color=red>{targetOfEnemy.unitData.unitName} DIED</color>. ");
                HandleDeath(targetOfEnemy, players);
            }

            Debug.Log(roundLog.ToString());
            round++;
            yield return new WaitForSeconds(combatPace);
        }

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

    CardDisplay GetTarget(List<CardDisplay> defenders)
    {
        List<CardDisplay> taunts = new List<CardDisplay>();
        foreach(var d in defenders)
        {
            if (d.unitData.hasTaunt) taunts.Add(d);
        }

        if (taunts.Count > 0) return taunts[Random.Range(0, taunts.Count)];
        return defenders[Random.Range(0, defenders.Count)];
    }

    void HandleDeath(CardDisplay unit, List<CardDisplay> list)
    {
        if (unit.hasReborn)
        {
            Debug.Log($"{unit.unitData.unitName} Reborn triggered!");
            unit.hasReborn = false;
            return;
        }

        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(deathClip);

        Transform oldBoard = unit.transform.parent;

        if (graveyard != null) unit.transform.SetParent(graveyard);
        unit.gameObject.SetActive(false); 

        if (AbilityManager.instance != null)
        {
            try 
            { 
                AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnDeath, unit, oldBoard); 
                AbilityManager.instance.TriggerAllyDeathAbilities(unit, oldBoard);
            }
            catch (System.Exception e) { Debug.LogError($"Deathrattle error: {e.Message}"); }
        }

        list.Remove(unit);
        Destroy(unit.gameObject);
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

    IEnumerator AnimateAttack(Transform attacker, Transform target)
    {
        if (attacker == null || target == null) yield break;

        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(attackClip);

        CardDisplay cd = attacker.GetComponent<CardDisplay>();
        bool usedProjectile = false;

        if (cd != null && cd.unitData != null && cd.unitData.attackProjectilePrefab != null)
        {
            GameObject projObj = Instantiate(cd.unitData.attackProjectilePrefab);
            Projectile proj = projObj.GetComponent<Projectile>();
            
            if (proj != null)
            {
                Vector3 startPos = attacker.position;
                startPos.z -= 10f; 
                
                Vector3 targetPos = target.position;
                targetPos.z -= 10f; 

                float travelTime = combatPace * 0.4f; 
                proj.Initialize(startPos, targetPos, travelTime);
                
                usedProjectile = true;
                
                yield return new WaitForSeconds(travelTime);
            }
            else
            {
                Destroy(projObj);
            }
        }

        if (!usedProjectile)
        {
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
        if (damageTaken > 0) 
        {
            GameManager.instance.ModifyHealth(-damageTaken);
            if (AudioManager.instance != null) AudioManager.instance.PlaySFX(defeatClip);
        }
        else if (playerWon)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlaySFX(victoryClip);
        }
        
        if (AnalyticsManager.instance != null)
            AnalyticsManager.instance.TrackRoundResult(GameManager.instance.turnNumber, playerWon, damageTaken, GameManager.instance.playerHealth);
        GameManager.instance.LogGameState($"Post-Combat (Damage: {damageTaken})");

        if (LobbyManager.instance != null)
        {
            int damageDealt = 0;
            if (playerWon)
            {
                damageDealt = 1; 
                List<CardDisplay> survivors = GetUnits(playerBoard);
                foreach(var s in survivors) damageDealt += s.unitData.tier;
            }
            LobbyManager.instance.ReportPlayerVsBotResult(playerWon, damageDealt);
            LobbyManager.instance.SimulateRoundForBots(GameManager.instance.turnNumber);
            
            if (LobbyManager.instance.GetNextOpponent() == null)
            {
                Debug.Log("Victory! No opponents remaining.");
                if (DeathSaveManager.instance != null) 
                {
                     GameManager.instance.currentPhase = GameManager.GamePhase.Death;
                     DeathSaveManager.instance.TriggerVictory();
                }
                return; 
            }
        }

        if (GameManager.instance.playerHealth <= 0) 
        {
            GameManager.instance.currentPhase = GameManager.GamePhase.Death;
            DeathSaveManager.instance.StartDeathSequence();
        }
        else 
        {
            StartCoroutine(ReturnToShopRoutine());
        }
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

        // --- FREEZE FIX: SHOW SHOP AGAIN ---
        if (shopContainer != null) 
        {
            shopContainer.gameObject.SetActive(true);
        }

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
            shop.HandleTurnStartRefresh(); 
        }
        
        Debug.Log("--- RETURN TO SHOP ---");
    }
}