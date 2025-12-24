using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CombatManager : MonoBehaviour
{
    public static CombatManager instance;

    [Header("References")]
    public Transform playerBoard;
    public Transform enemyBoard;
    public Transform shopContainer;
    public GameObject cardPrefab;

    [Header("Data")]
    public UnitData[] possibleEnemies;

    private List<UnitData> savedPlayerRoster = new List<UnitData>();

    void Awake() { instance = this; }

    public void StartCombat()
    {
        // 1. Clear Shop
        if (shopContainer != null) foreach (Transform child in shopContainer) Destroy(child.gameObject);

        // 2. Save Roster
        SaveRoster();

        // 3. Switch Phase
        GameManager.instance.currentPhase = GameManager.GamePhase.Combat;

        // 4. Spawn & Fight
        SpawnEnemies();
        StartCoroutine(CombatRoutine());
    }

    void SaveRoster()
    {
        savedPlayerRoster.Clear();
        foreach (Transform child in playerBoard)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if (cd != null) savedPlayerRoster.Add(cd.unitData);
        }
    }

    void SpawnEnemies()
    {
        foreach (Transform child in enemyBoard) Destroy(child.gameObject);

        for (int i = 0; i < 3; i++)
        {
            UnitData data = possibleEnemies[Random.Range(0, possibleEnemies.Length)];
            GameObject newCard = Instantiate(cardPrefab, enemyBoard);

            newCard.GetComponent<CardDisplay>().LoadUnit(data);
            Destroy(newCard.GetComponent<UnityEngine.UI.Button>());
        }
    }

    IEnumerator CombatRoutine()
    {
        yield return new WaitForSeconds(1f);

        // Initial Lists
        List<CardDisplay> players = GetUnits(playerBoard);
        List<CardDisplay> enemies = GetUnits(enemyBoard);

        // Runtime Health Tracking
        Dictionary<CardDisplay, int> currentHealths = new Dictionary<CardDisplay, int>();
        foreach (var p in players) currentHealths[p] = p.unitData.baseHealth;
        foreach (var e in enemies) currentHealths[e] = e.unitData.baseHealth;

        // --- THE FIGHT LOOP ---
        while (players.Count > 0 && enemies.Count > 0)
        {
            // Re-fetch lists to account for destroyed objects
            players = GetUnits(playerBoard);
            enemies = GetUnits(enemyBoard);
            if (players.Count == 0 || enemies.Count == 0) break;

            CardDisplay pUnit = players[0];
            CardDisplay eUnit = enemies[0];

            // Animation
            yield return StartCoroutine(AnimateAttack(pUnit.transform, eUnit.transform));
            yield return StartCoroutine(AnimateAttack(eUnit.transform, pUnit.transform));

            // Damage Logic
            int pDmg = eUnit.unitData.baseAttack;
            int eDmg = pUnit.unitData.baseAttack;

            currentHealths[pUnit] -= pDmg;
            currentHealths[eUnit] -= eDmg;

            // Update Text
            if (pUnit.healthText != null) pUnit.healthText.text = currentHealths[pUnit].ToString();
            if (eUnit.healthText != null) eUnit.healthText.text = currentHealths[eUnit].ToString();

            // Death Checks
            bool pDies = currentHealths[pUnit] <= 0;
            bool eDies = currentHealths[eUnit] <= 0;

            if (eDies)
            {
                Destroy(eUnit.gameObject);
                enemies.Remove(eUnit);
            }

            if (pDies)
            {
                Destroy(pUnit.gameObject);
                players.Remove(pUnit);
            }

            yield return new WaitForSeconds(1f);
        }

        // --- DAMAGE CALCULATION PHASE ---
        // Need to re-fetch one last time to be sure we have the survivors
        enemies = GetUnits(enemyBoard);
        players = GetUnits(playerBoard);

        int damageTaken = 0;

        // If Enemy has units left AND Player has none = LOSS
        if (enemies.Count > 0 && players.Count == 0)
        {
            damageTaken = 1; // Base Damage
            foreach (var enemy in enemies)
            {
                damageTaken += enemy.unitData.tier;
            }
            Debug.Log($"Calc Damage: Base(1) + {enemies.Count} Enemies = {damageTaken}");
        }

        EndCombat(players.Count > 0, damageTaken);
    }

    IEnumerator AnimateAttack(Transform attacker, Transform target)
    {
        if (attacker == null || target == null) yield break;
        Vector3 originalPos = attacker.position;
        Vector3 targetPos = target.position;

        float duration = 0.15f;
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
        if (attacker != null) attacker.position = originalPos;
    }

    List<CardDisplay> GetUnits(Transform board)
    {
        List<CardDisplay> list = new List<CardDisplay>();
        foreach (Transform child in board)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if (cd != null) list.Add(cd);
        }
        return list;
    }

    void EndCombat(bool playerWon, int damageTaken)
    {
        if (playerWon)
        {
            Debug.Log("VICTORY!");
        }
        else if (damageTaken > 0)
        {
            Debug.Log($"DEFEAT! Applying {damageTaken} damage to Hero.");
            // --- APPLY DAMAGE TO HERO ---
            GameManager.instance.ModifyHealth(-damageTaken);
        }
        else
        {
            Debug.Log("TIE! No damage taken.");
        }

        StartCoroutine(ReturnToShopRoutine());
    }

    IEnumerator ReturnToShopRoutine()
    {
        yield return new WaitForSeconds(2f);

        foreach (Transform child in enemyBoard) Destroy(child.gameObject);
        foreach (Transform child in playerBoard) Destroy(child.gameObject);

        foreach (UnitData data in savedPlayerRoster)
        {
            GameObject newCard = Instantiate(cardPrefab, playerBoard);
            newCard.GetComponent<CardDisplay>().LoadUnit(data);
            newCard.GetComponent<CardDisplay>().isPurchased = true;
        }

        GameManager.instance.turnNumber++;
        GameManager.instance.StartRecruitPhase();

        ShopManager shop = FindObjectOfType<ShopManager>();
        if (shop != null) shop.RerollShop();

        Debug.Log("--- RETURN TO SHOP ---");
    }
}