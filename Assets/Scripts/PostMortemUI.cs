using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Post-game analysis UI showing all decisions made during the game
/// Allows players to review their performance and learn from mistakes
/// </summary>
public class PostMortemUI : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject postMortemPanel;
    public TMP_Text headerText;
    public TMP_Text summaryText;
    public Button closeButton;
    public Button exportButton;

    [Header("Phase List")]
    public Transform phaseListContainer;
    public GameObject phaseRowPrefab;

    [Header("Decision Details")]
    public GameObject decisionDetailPanel;
    public TMP_Text decisionDetailText;

    [Header("Filters")]
    public TMP_Dropdown qualityFilterDropdown;
    public TMP_Dropdown typeFilterDropdown;

    private GameLog currentLog;
    private PlayQuality? filterQuality = null;
    private DecisionType? filterType = null;

    void Awake()
    {
        if (postMortemPanel != null) postMortemPanel.SetActive(false);
        if (decisionDetailPanel != null) decisionDetailPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePostMortem);

        if (exportButton != null)
            exportButton.onClick.AddListener(ExportToClipboard);

        SetupFilters();
    }

    private void SetupFilters()
    {
        if (qualityFilterDropdown != null)
        {
            qualityFilterDropdown.ClearOptions();
            var options = new List<string> { "All Qualities" };
            options.AddRange(System.Enum.GetNames(typeof(PlayQuality)));
            qualityFilterDropdown.AddOptions(options);
            qualityFilterDropdown.onValueChanged.AddListener(OnQualityFilterChanged);
        }

        if (typeFilterDropdown != null)
        {
            typeFilterDropdown.ClearOptions();
            var options = new List<string> { "All Types" };
            options.AddRange(System.Enum.GetNames(typeof(DecisionType)));
            typeFilterDropdown.AddOptions(options);
            typeFilterDropdown.onValueChanged.AddListener(OnTypeFilterChanged);
        }
    }

    public void DisplayPostMortem(GameLog log)
    {
        if (log == null) return;

        currentLog = log;

        // Show header
        if (headerText != null)
        {
            headerText.text = $"Game Analysis - {log.heroName}";
        }

        // Show summary
        if (summaryText != null)
        {
            string placement = log.finalPlacement > 0 ? $"#{log.finalPlacement}" : "Incomplete";
            int mmrChange = log.finalMMR - log.startingMMR;
            string mmrText = mmrChange >= 0 ? $"+{mmrChange}" : $"{mmrChange}";

            summaryText.text = $"<b>Placement:</b> {placement}\n" +
                             $"<b>Turns Survived:</b> {log.finalTurn}\n" +
                             $"<b>MMR:</b> {log.startingMMR} → {log.finalMMR} ({mmrText})\n" +
                             $"<b>Average Score:</b> {log.averageScore:F1}/100\n" +
                             $"<b>Optimal Plays:</b> {log.optimalPlays}\n" +
                             $"<b>Critical Mistakes:</b> {log.criticalMistakes}";
        }

        RefreshPhaseList();

        if (postMortemPanel != null)
            postMortemPanel.SetActive(true);
    }

    private void RefreshPhaseList()
    {
        if (phaseListContainer == null || currentLog == null) return;

        // Clear existing
        foreach (Transform child in phaseListContainer)
        {
            Destroy(child.gameObject);
        }

        // Apply filters and create rows
        foreach (var phase in currentLog.phases)
        {
            var filteredDecisions = FilterDecisions(phase.decisions);
            if (filteredDecisions.Count == 0) continue;

            CreatePhaseRow(phase, filteredDecisions);
        }
    }

    private List<DecisionEvaluation> FilterDecisions(List<DecisionEvaluation> decisions)
    {
        var filtered = decisions.AsEnumerable();

        if (filterQuality.HasValue)
            filtered = filtered.Where(d => d.quality == filterQuality.Value);

        if (filterType.HasValue)
            filtered = filtered.Where(d => d.type == filterType.Value);

        return filtered.ToList();
    }

    private void CreatePhaseRow(PhaseEvaluation phase, List<DecisionEvaluation> decisions)
    {
        if (phaseRowPrefab == null) return;

        GameObject row = Instantiate(phaseRowPrefab, phaseListContainer);
        var rowUI = row.GetComponent<PostMortemPhaseRow>();

        if (rowUI != null)
        {
            rowUI.Setup(phase, decisions, this);
        }
        else
        {
            // Fallback: Simple text display
            var text = row.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                string scoreColor = GetScoreColor(phase.overallScore);
                text.text = $"<color={scoreColor}>Turn {phase.turnNumber}: {phase.overallScore:F0}/100</color> - {decisions.Count} decisions";
            }

            var button = row.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => ShowPhaseDetails(phase, decisions));
            }
        }
    }

    public void ShowPhaseDetails(PhaseEvaluation phase, List<DecisionEvaluation> decisions)
    {
        if (decisionDetailPanel == null || decisionDetailText == null) return;

        string details = $"<b><size=24>Turn {phase.turnNumber}</size></b>\n";
        details += $"<b>Overall Score:</b> <color={GetScoreColor(phase.overallScore)}>{phase.overallScore:F1}/100</color>\n\n";

        details += "<b>Decisions:</b>\n\n";

        foreach (var decision in decisions)
        {
            string qualityColor = GetQualityColorHex(decision.quality);
            details += $"<color={qualityColor}>■</color> <b>{decision.actionTaken}</b>\n";
            details += $"   Quality: <color={qualityColor}>{decision.quality}</color>\n";
            details += $"   {decision.reasoning}\n";

            if (decision.alternatives != null && decision.alternatives.Count > 0)
            {
                details += "   <i>Better options:</i>\n";
                foreach (var alt in decision.alternatives)
                {
                    details += $"   • {alt}\n";
                }
            }

            details += "\n";
        }

        decisionDetailText.text = details;
        decisionDetailPanel.SetActive(true);
    }

    private void OnQualityFilterChanged(int index)
    {
        if (index == 0)
        {
            filterQuality = null;
        }
        else
        {
            filterQuality = (PlayQuality)(index - 1);
        }

        RefreshPhaseList();
    }

    private void OnTypeFilterChanged(int index)
    {
        if (index == 0)
        {
            filterType = null;
        }
        else
        {
            filterType = (DecisionType)(index - 1);
        }

        RefreshPhaseList();
    }

    private void ClosePostMortem()
    {
        if (postMortemPanel != null)
            postMortemPanel.SetActive(false);

        if (decisionDetailPanel != null)
            decisionDetailPanel.SetActive(false);
    }

    private void ExportToClipboard()
    {
        if (currentLog == null) return;

        string export = GenerateTextReport(currentLog);
        GUIUtility.systemCopyBuffer = export;

        Debug.Log("Post-mortem report copied to clipboard!");
    }

    private string GenerateTextReport(GameLog log)
    {
        string report = $"=== GAME ANALYSIS: {log.heroName} ===\n\n";
        report += $"Placement: #{log.finalPlacement}\n";
        report += $"Turns: {log.finalTurn}\n";
        report += $"MMR: {log.startingMMR} → {log.finalMMR}\n";
        report += $"Average Score: {log.averageScore:F1}/100\n";
        report += $"Optimal Plays: {log.optimalPlays}\n";
        report += $"Critical Mistakes: {log.criticalMistakes}\n\n";

        foreach (var phase in log.phases)
        {
            report += $"--- Turn {phase.turnNumber} (Score: {phase.overallScore:F1}) ---\n";

            foreach (var decision in phase.decisions)
            {
                report += $"  [{decision.quality}] {decision.actionTaken}\n";
                report += $"    {decision.reasoning}\n";

                if (decision.alternatives != null && decision.alternatives.Count > 0)
                {
                    report += "    Better: " + string.Join(", ", decision.alternatives) + "\n";
                }
            }

            report += "\n";
        }

        return report;
    }

    private string GetScoreColor(float score)
    {
        if (score >= 80) return "green";
        if (score >= 60) return "yellow";
        if (score >= 40) return "orange";
        return "red";
    }

    private string GetQualityColorHex(PlayQuality quality)
    {
        switch (quality)
        {
            case PlayQuality.Optimal: return "#00FF00";
            case PlayQuality.Good: return "#90EE90";
            case PlayQuality.Acceptable: return "#FFFFFF";
            case PlayQuality.Questionable: return "#FFFF00";
            case PlayQuality.Poor: return "#FFA500";
            case PlayQuality.Critical: return "#FF0000";
            default: return "#FFFFFF";
        }
    }
}

/// <summary>
/// Optional: Dedicated component for phase row if you want custom UI
/// Attach to your phase row prefab for more control
/// </summary>
public class PostMortemPhaseRow : MonoBehaviour
{
    public TMP_Text turnText;
    public TMP_Text scoreText;
    public TMP_Text decisionsText;
    public Image scoreBar;
    public Button expandButton;

    private PhaseEvaluation phase;
    private List<DecisionEvaluation> decisions;
    private PostMortemUI parentUI;

    public void Setup(PhaseEvaluation phaseData, List<DecisionEvaluation> decisionList, PostMortemUI parent)
    {
        phase = phaseData;
        decisions = decisionList;
        parentUI = parent;

        if (turnText != null)
            turnText.text = $"Turn {phase.turnNumber}";

        if (scoreText != null)
            scoreText.text = $"{phase.overallScore:F0}/100";

        if (decisionsText != null)
            decisionsText.text = $"{decisions.Count} decisions";

        if (scoreBar != null)
            scoreBar.fillAmount = phase.overallScore / 100f;

        if (expandButton != null)
            expandButton.onClick.AddListener(OnExpand);
    }

    private void OnExpand()
    {
        if (parentUI != null)
            parentUI.ShowPhaseDetails(phase, decisions);
    }
}
