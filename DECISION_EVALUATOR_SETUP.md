# Decision Evaluator System - Setup Guide

## Overview

The Decision Evaluator System tracks and analyzes player decisions in real-time, providing coaching feedback and post-game analytics. It consists of:

1. **Core Engine** - Evaluates decisions using static rules and dynamic meta data
2. **UI Layer** - Real-time hints and post-game analysis screens
3. **Backend** - PlayFab integration for data persistence and aggregation
4. **Meta Intelligence** - Crowd-sourced optimal play patterns

---

## Installation Steps

### 1. Unity Setup

#### A. Create Required GameObjects

Create these GameObjects in your Game scene:

1. **DecisionEvaluator** (persists across scenes)
   - Add `DecisionEvaluator` component
   - Assign UI references (see below)

2. **CoachingHintUI** (in game scene)
   - Create a Canvas → Panel for the hint popup
   - Add `CoachingHintUI` component
   - Assign UI elements:
     - `hintPanel`: The popup panel
     - `hintTitleText`: Title text (e.g., "CRITICAL MISTAKE!")
     - `hintDescriptionText`: Description of what happened
     - `alternativesText`: List of better alternatives
     - `dismissButton`: Close button
     - `continueButton`: Continue from pause button

3. **PostMortemUI** (separate scene or overlay)
   - Create a full-screen panel
   - Add `PostMortemUI` component
   - Assign UI elements:
     - `postMortemPanel`: Main panel
     - `headerText`: Title
     - `summaryText`: Game summary stats
     - `phaseListContainer`: ScrollView content for phase rows
     - `phaseRowPrefab`: Prefab for each turn row
     - `decisionDetailPanel`: Detail view panel
     - `closeButton`, `exportButton`: Action buttons

#### B. Connect References in DecisionEvaluator

In the `DecisionEvaluator` inspector:
- Drag `CoachingHintUI` component to `hintUI`
- Drag `PostMortemUI` component to `postMortemUI`

#### C. Configure Coaching Settings

In `DecisionEvaluator` inspector, expand "Coaching Settings":
- **Goal**: Choose default coaching intensity (JustHaveFun, ImproveGradually, CompetitiveClimb, Tryhard)
- **Max MMR For Hints**: Set MMR threshold (default: 2000)
- **Show Post Mortem**: Enable/disable end-game analysis
- **Show Real Time Hints**: Enable/disable in-game coaching
- **Pause On Teaching Moments**: Enable/disable game pause for critical mistakes

---

### 2. PlayFab Configuration

#### A. Upload CloudScript

1. Go to your PlayFab Dashboard
2. Navigate to **Automation → Cloud Script**
3. Create a new CloudScript file or edit existing
4. Copy contents from `CloudScript_DecisionAggregation.js`
5. Click **Save and Deploy**

#### B. Set Up Scheduled Task

1. Go to **Automation → Scheduled Tasks**
2. Click **New Scheduled Task**
3. Configure:
   - **Name**: "Daily Meta Aggregation"
   - **Function**: `AggregateDecisionData`
   - **Schedule**: Daily at 3:00 AM (cron: `0 3 * * *`)
   - **Active**: Yes
4. Save

#### C. Initialize TitleData

1. Go to **Economy → Title Data**
2. Add new key: `MetaSnapshot`
3. Set value to:
```json
{
  "version": "1.0",
  "lastUpdated": "2024-01-01T00:00:00Z",
  "avgLevelByTurn": {
    "3": 1.2,
    "5": 2.1,
    "7": 3.0,
    "9": 4.2,
    "11": 5.0
  },
  "avgGoldByTurn": {},
  "unitWinRates": {},
  "unitPlayRates": {},
  "synergyWinRates": {}
}
```
4. Save

---

### 3. Testing

#### A. Test Real-Time Evaluation

1. Start a game
2. Make a "bad" decision (e.g., reroll on turn 1)
3. Check console for evaluation logs:
   ```
   [DECISION] Rerolled shop for 1g - Critical - Never reroll on turn 1!
   ```
4. If coaching hints are enabled, you should see a popup

#### B. Test Post-Mortem

1. Complete a full game
2. `PostMortemUI` should appear automatically (if enabled)
3. Review decisions by turn
4. Test filters and export functionality

#### C. Verify PlayFab Upload

1. After a game ends, check console:
   ```
   [PLAYFAB] Game log uploaded successfully!
   ```
2. In PlayFab Dashboard, go to **Data → PlayStream Monitor**
3. Look for `game_decision_log` events

---

## Usage Examples

### Changing Coaching Settings In-Game

```csharp
// In a settings menu script:
public void SetCoachingMode(int modeIndex)
{
    var settings = DecisionEvaluator.instance.coachingSettings;
    settings.ApplyGoal((CoachingGoal)modeIndex);

    // Save to PlayFab
    PlayFabManager.instance.SaveCoachingSettings(settings);
}
```

### Manually Triggering Post-Mortem

```csharp
// Show post-mortem for current game
if (DecisionEvaluator.instance.currentGameLog != null)
{
    PostMortemUI postMortem = FindObjectOfType<PostMortemUI>();
    postMortem.DisplayPostMortem(DecisionEvaluator.instance.currentGameLog);
}
```

### Custom Evaluation Hook

```csharp
// Add evaluation for hero power usage
if (DecisionEvaluator.instance != null)
{
    var evaluation = new DecisionEvaluation
    {
        type = DecisionType.UseHeroPower,
        quality = PlayQuality.Good,
        actionTaken = "Used hero power: Fireball",
        reasoning = "Good timing with board state",
        impactScore = 15f
    };

    DecisionEvaluator.instance.RecordDecision(evaluation);
}
```

---

## Customization

### Adding New Evaluation Rules

Edit `EvaluationRules.cs`:

```csharp
public static PlayQuality EvaluateMyCustomDecision(int param1, string param2)
{
    // Your logic here
    if (param1 < 5)
        return PlayQuality.Poor;

    return PlayQuality.Good;
}
```

### Adjusting Meta Benchmarks

The `MetaSnapshot` is updated automatically via CloudScript aggregation. To manually adjust:

1. Edit TitleData in PlayFab Dashboard
2. Or modify `MetaSnapshot.InitializeDefaults()` for local fallbacks

### Hero-Specific Economy Rules

Edit `HeroEconomyProfile.GetProfile()` in `DecisionTypes.cs`:

```csharp
public static HeroEconomyProfile GetProfile(string heroName)
{
    var profile = new HeroEconomyProfile
    {
        heroName = heroName,
        hasInterestMechanic = false,
        hasBankingMechanic = false,
        economyWeight = 0.5f
    };

    // Add hero-specific logic
    if (heroName == "Banker Dragon")
    {
        profile.hasBankingMechanic = true;
        profile.hasInterestMechanic = true;
        profile.economyWeight = 0.8f;
    }

    return profile;
}
```

---

## Troubleshooting

### Issue: Hints Not Appearing

1. Check `CoachingSettings.showRealTimeHints` is `true`
2. Verify player MMR is below `maxMMRForHints`
3. Ensure decision quality meets `minimumHintQuality` threshold
4. Check `CoachingHintUI` is assigned in `DecisionEvaluator`

### Issue: PlayFab Upload Failing

1. Verify `#define ENABLE_PLAYFAB` is uncommented in `PlayFabManager.cs`
2. Check PlayFab SDK is installed
3. Ensure player is logged in (`isLoggedIn == true`)
4. Check console for specific error messages

### Issue: Post-Mortem Empty

1. Verify game started with `DecisionEvaluator.instance.StartNewGame()`
2. Check decisions are being recorded (see console logs)
3. Ensure `EndGame()` was called with valid placement

### Issue: Compilation Errors for DecisionEvaluator

- Unity needs to recompile after creating the new scripts
- If errors persist, close and reopen Unity
- Check all scripts are in `Assets/Scripts/` folder

---

## Performance Considerations

- **Evaluation overhead**: ~0.1-0.5ms per decision (negligible)
- **Memory**: ~50KB per game log (cleared after upload)
- **PlayFab calls**: 1 upload per game (rate limit safe)
- **Meta updates**: Once daily (no impact on players)

---

## Future Enhancements

- **Machine Learning**: Replace static rules with ML models trained on high-MMR games
- **Comparative Analysis**: "You vs Top Players" graphs
- **Replay System**: Visualize decision timeline with board states
- **Coach Mode**: AI-driven teaching moments with interactive lessons
- **Leaderboards**: Best decision scores per hero/tier

---

## Support

For issues or questions:
1. Check console logs for detailed error messages
2. Review this documentation
3. Test in a fresh scene with minimal setup
4. Contact your development team

**Version**: 1.0
**Last Updated**: 2024
