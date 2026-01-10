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

        // EVALUATION HOOK: Evaluate end turn decision before combat
        if (DecisionEvaluator.instance != null)
        {
            DecisionEvaluator.instance.EvaluateEndTurnDecision();
        }

        if (shopContainer != null) shopContainer.gameObject.SetActive(false);

        SaveRoster();
        GameManager.instance.currentPhase = GameManager.GamePhase.Combat;
        
        // FIX: Fetch the actual opponent from the Lobby
        if (LobbyManager.instance != null)
        {
            var opponent = LobbyManager.instance.GetNextOpponent();
            if (opponent != null) 
            {
                Debug.Log($"<color=orange>Fighting Opponent: {opponent.name} | {opponent.rank} ({opponent.mmr})</color>");
                SpawnEnemies(opponent); 
            }
            else
            {
                // Fallback / Win condition logic handled by LobbyManager usually
                Debug.LogWarning("No opponent found!");
            }
        }
        else
        {
            // Fallback for testing without Lobby
            SpawnEnemies(null); 
        }

        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(combatStartClip);

        // NEW: Trigger Start of Combat Abilities (e.g. Rabid Bear)
        if (AbilityManager.instance != null)
        {
            AbilityManager.instance.TriggerCombatStartAbilities();
            AbilityManager.instance.RecalculateAuras();
        }

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

    // FIX: Updated to accept an Opponent object
    void SpawnEnemies(LobbyManager.Opponent opponent)
    {
        foreach (Transform child in enemyBoard) Destroy(child.gameObject);

        if (opponent != null)
        {
            // FIX: Use the real simulation roster with buffed stats
            foreach (LobbyManager.SavedAIUnit savedUnit in opponent.roster)
            {
                GameObject newCard = Instantiate(cardPrefab, enemyBoard);
                CardDisplay cd = newCard.GetComponent<CardDisplay>();

                // Set golden flag BEFORE LoadUnit so stats are correctly set
                cd.isGolden = savedUnit.isGolden;
                cd.LoadUnit(savedUnit.template);

                // Override with saved buffed stats
                cd.permanentAttack = savedUnit.permAttack;
                cd.permanentHealth = savedUnit.permHealth;
                cd.currentAttack = savedUnit.permAttack;
                cd.currentHealth = savedUnit.permHealth;
                cd.UpdateVisuals();

                Destroy(newCard.GetComponent<UnityEngine.UI.Button>());
            }
        }
        else if (AIManager.instance != null)
        {
            // Legacy fallback (loads base stats)
            List<UnitData> enemyRoster = AIManager.instance.GenerateEnemyBoard(GameManager.instance.turnNumber);
            foreach (UnitData data in enemyRoster)
            {
                GameObject newCard = Instantiate(cardPrefab, enemyBoard);
                CardDisplay cd = newCard.GetComponent<CardDisplay>();
                cd.LoadUnit(data);
                Destroy(newCard.GetComponent<UnityEngine.UI.Button>());
            }
        }
    }

    IEnumerator CombatRoutine()
    {
        yield return new WaitForSeconds(combatPace); 

        // Initial fetch
        List<CardDisplay> players = GetUnits(playerBoard);
        List<CardDisplay> enemies = GetUnits(enemyBoard);

        int round = 1;

        // Main Loop: Continues as long as both sides have units
        while (players.Count > 0 && enemies.Count > 0)
        {
            // REFRESH LISTS at start of every round to catch spawns/deaths
            players = GetUnits(playerBoard);
            enemies = GetUnits(enemyBoard);
            if (players.Count == 0 || enemies.Count == 0) break;

            // 1. Determine Attackers (Leftmost)
            CardDisplay pAttacker = players[0];
            CardDisplay eAttacker = enemies[0];
            
            // 2. Select Targets
            CardDisplay targetOfPlayer = GetTarget(enemies); 
            CardDisplay targetOfEnemy = GetTarget(players);  

            // --- PLAYER ATTACK ---
            if (pAttacker != null && targetOfPlayer != null)
            {
                yield return StartCoroutine(AnimateAttack(pAttacker.transform, targetOfPlayer.transform));
                
                if (AudioManager.instance != null) AudioManager.instance.PlaySFX(hitClip);
                
                // Logic: Deal actual damage
                int dmg = pAttacker.currentAttack;
                targetOfPlayer.TakeDamage(dmg); 

                // Log
                StringBuilder pLog = new StringBuilder();
                pLog.Append($"<b>Round {round} [P]</b>: {pAttacker.unitData.unitName} -> {targetOfPlayer.unitData.unitName} ({dmg} dmg)");
                
                if (targetOfPlayer.currentHealth <= 0)
                {
                    pLog.Append($" <color=red>DIED</color>");
                    HandleDeath(targetOfPlayer);
                }
                Debug.Log(pLog.ToString());
            }

            // RE-CHECK STATE: Player attack (or a triggered Rush) might have ended combat or killed the enemy attacker
            enemies = GetUnits(enemyBoard); 
            players = GetUnits(playerBoard);
            if (enemies.Count == 0 || players.Count == 0) break;

            // --- ENEMY ATTACK ---
            // Ensure eAttacker is still alive and on board
            if (eAttacker != null && eAttacker.gameObject.activeInHierarchy && eAttacker.currentHealth > 0 && targetOfEnemy != null)
            {
                // Re-validate target (Player's unit might have died from Thorns/Rush in the microseconds between)
                if (targetOfEnemy == null || !targetOfEnemy.gameObject.activeInHierarchy)
                    targetOfEnemy = GetTarget(players);

                if (targetOfEnemy != null)
                {
                    yield return StartCoroutine(AnimateAttack(eAttacker.transform, targetOfEnemy.transform));
                    
                    if (AudioManager.instance != null) AudioManager.instance.PlaySFX(hitClip);

                    int dmg = eAttacker.currentAttack;
                    targetOfEnemy.TakeDamage(dmg);

                    StringBuilder eLog = new StringBuilder();
                    eLog.Append($"<b>Round {round} [E]</b>: {eAttacker.unitData.unitName} -> {targetOfEnemy.unitData.unitName} ({dmg} dmg)");
                    
                    if (targetOfEnemy.currentHealth <= 0)
                    {
                        eLog.Append($" <color=red>DIED</color>");
                        HandleDeath(targetOfEnemy);
                    }
                    Debug.Log(eLog.ToString());
                }
            }

            round++;
            yield return new WaitForSeconds(combatPace);
        }

        // --- END OF COMBAT ---
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
        // Filter out dead/inactive units just in case
        var validDefenders = new List<CardDisplay>();
        foreach(var d in defenders) if(d != null && d.currentHealth > 0 && d.gameObject.activeInHierarchy) validDefenders.Add(d);
        
        if (validDefenders.Count == 0) return null;

        List<CardDisplay> taunts = new List<CardDisplay>();
        foreach(var d in validDefenders)
        {
            if (d.hasTaunt) taunts.Add(d);
        }

        if (taunts.Count > 0) return taunts[Random.Range(0, taunts.Count)];
        return validDefenders[Random.Range(0, validDefenders.Count)];
    }

    public void HandleDeath(CardDisplay unit)
    {
        if (unit == null) return;

        if (!unit.gameObject.activeInHierarchy) return;

        if (unit.hasReborn)
        {
            Debug.Log($"{unit.unitData.unitName} Reborn triggered!");
            unit.hasReborn = false;
            unit.currentHealth = 1;
            unit.UpdateVisuals();
            return;
        }

        if (AudioManager.instance != null) AudioManager.instance.PlaySFX(deathClip);

        Transform oldBoard = unit.transform.parent;

        // FIX: Trigger deathrattle BEFORE moving to graveyard/deactivating
        if (AbilityManager.instance != null)
        {
            try
            {
                Debug.Log($"[DEATHRATTLE] Triggering OnDeath for {unit.unitData.unitName}");
                AbilityManager.instance.TriggerAbilities(AbilityTrigger.OnDeath, unit, oldBoard);
                AbilityManager.instance.TriggerAllyDeathAbilities(unit, oldBoard);
            }
            catch (System.Exception e) { Debug.LogError($"Deathrattle error: {e.Message}"); }
        }

        // Now move to graveyard and deactivate
        if (graveyard != null) unit.transform.SetParent(graveyard);
        unit.gameObject.SetActive(false);

        Destroy(unit.gameObject);
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
            if(cd != null && cd.gameObject.activeInHierarchy && cd.currentHealth > 0) list.Add(cd);
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
                // WIN CONDITION HERE
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
            if (DeathSaveManager.instance != null)
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
            
            // Restore Permanent stats
            cd.permanentAttack = snap.permAttack;
            cd.permanentHealth = snap.permHealth;
            
            // Clear temporary combat buffs
            cd.ClearTempStats();
            cd.ResetToPermanent();
            cd.UpdateVisuals(); 
        }

        GameManager.instance.turnNumber++;
        GameManager.instance.StartRecruitPhase();

        if (AbilityManager.instance != null) AbilityManager.instance.RecalculateAuras();

        // FIX: Removed duplicate shop calls. StartRecruitPhase() already calls HandleTurnStartRefresh()
        // which internally calls ReduceUpgradeCost(). The duplicate calls were causing
        // upgrade cost to reduce by 2-3g per turn instead of 1g.

        Debug.Log("--- RETURN TO SHOP ---");
    }
}