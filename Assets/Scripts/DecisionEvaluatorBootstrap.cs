using UnityEngine;

/// <summary>
/// Helper script to easily set up the Decision Evaluator System in your scene
/// Attach this to a GameObject to auto-create the evaluator if it doesn't exist
/// </summary>
public class DecisionEvaluatorBootstrap : MonoBehaviour
{
    [Header("Auto-Setup")]
    [Tooltip("If true, creates DecisionEvaluator automatically on Awake")]
    public bool autoCreateEvaluator = true;

    [Header("Default Settings")]
    public CoachingGoal defaultCoachingGoal = CoachingGoal.ImproveGradually;
    public int maxMMRForHints = 2000;

    [Header("Debug")]
    public bool verboseLogging = false;

    void Awake()
    {
        if (autoCreateEvaluator && DecisionEvaluator.instance == null)
        {
            CreateDecisionEvaluator();
        }
    }

    private void CreateDecisionEvaluator()
    {
        // Create persistent GameObject
        GameObject evaluatorObj = new GameObject("DecisionEvaluator");
        DontDestroyOnLoad(evaluatorObj);

        // Add component
        var evaluator = evaluatorObj.AddComponent<DecisionEvaluator>();

        // Configure default settings
        evaluator.coachingSettings.ApplyGoal(defaultCoachingGoal);
        evaluator.coachingSettings.maxMMRForHints = maxMMRForHints;

        if (verboseLogging)
        {
            Debug.Log("<color=cyan>[BOOTSTRAP]</color> DecisionEvaluator created and configured!");
            Debug.Log($"  - Coaching Goal: {defaultCoachingGoal}");
            Debug.Log($"  - Max MMR for Hints: {maxMMRForHints}");
        }

        // Try to find UI components in scene
        AutoConnectUIComponents(evaluator);
    }

    private void AutoConnectUIComponents(DecisionEvaluator evaluator)
    {
        // Try to find CoachingHintUI
        var hintUI = FindFirstObjectByType<CoachingHintUI>();
        if (hintUI != null)
        {
            evaluator.hintUI = hintUI;
            if (verboseLogging) Debug.Log("  - Found and connected CoachingHintUI");
        }
        else if (verboseLogging)
        {
            Debug.LogWarning("  - CoachingHintUI not found in scene. Real-time hints will not display.");
        }

        // Try to find PostMortemUI
        var postMortemUI = FindFirstObjectByType<PostMortemUI>();
        if (postMortemUI != null)
        {
            evaluator.postMortemUI = postMortemUI;
            if (verboseLogging) Debug.Log("  - Found and connected PostMortemUI");
        }
        else if (verboseLogging)
        {
            Debug.LogWarning("  - PostMortemUI not found in scene. Post-game analysis will not display.");
        }
    }

    // ============================================================================
    // EDITOR HELPERS
    // ============================================================================

#if UNITY_EDITOR
    [ContextMenu("Force Create Evaluator")]
    public void ForceCreateEvaluator()
    {
        if (DecisionEvaluator.instance != null)
        {
            Debug.LogWarning("DecisionEvaluator already exists!");
            return;
        }

        CreateDecisionEvaluator();
        Debug.Log("DecisionEvaluator created!");
    }

    [ContextMenu("Print Current Settings")]
    public void PrintCurrentSettings()
    {
        if (DecisionEvaluator.instance == null)
        {
            Debug.Log("No DecisionEvaluator found.");
            return;
        }

        var settings = DecisionEvaluator.instance.coachingSettings;
        Debug.Log("=== DECISION EVALUATOR SETTINGS ===");
        Debug.Log($"Coaching Goal: {settings.goal}");
        Debug.Log($"Show Post Mortem: {settings.showPostMortem}");
        Debug.Log($"Show Real-Time Hints: {settings.showRealTimeHints}");
        Debug.Log($"Pause on Teaching Moments: {settings.pauseOnTeachingMoments}");
        Debug.Log($"Minimum Hint Quality: {settings.minimumHintQuality}");
        Debug.Log($"Max MMR for Hints: {settings.maxMMRForHints}");
    }

    [ContextMenu("Test Mock Decision")]
    public void TestMockDecision()
    {
        if (DecisionEvaluator.instance == null)
        {
            Debug.LogError("DecisionEvaluator not found! Cannot test.");
            return;
        }

        // Create a mock poor decision
        var evaluation = new DecisionEvaluation
        {
            type = DecisionType.Reroll,
            quality = PlayQuality.Critical,
            turnNumber = 1,
            goldBefore = 3,
            goldAfter = 2,
            tavernTier = 1,
            playerHealth = 30,
            heroName = "Test Hero",
            actionTaken = "Rerolled shop on turn 1 for 1g",
            reasoning = "Never reroll on turn 1! You need that gold for units.",
            impactScore = -20f
        };

        evaluation.alternatives.Add("Save gold and buy units");
        evaluation.alternatives.Add("Level up tavern tier");

        Debug.Log("<color=yellow>[TEST]</color> Simulating critical mistake...");

        // This should trigger a hint if settings allow
        if (DecisionEvaluator.instance.hintUI != null)
        {
            DecisionEvaluator.instance.hintUI.ShowHint(evaluation, false);
        }
        else
        {
            Debug.LogWarning("CoachingHintUI not connected. Hint would not display.");
        }
    }
#endif
}
