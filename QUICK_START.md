# Decision Evaluator - Quick Start Guide

## ⚡ 5-Minute Setup

### Step 1: Add Bootstrap to Scene (30 seconds)
```
1. In your Game scene hierarchy
2. Create Empty GameObject
3. Name it "DecisionEvaluatorBootstrap"
4. Add Component → DecisionEvaluatorBootstrap
5. Check "Auto Create Evaluator"
6. Done! The system will auto-initialize on play
```

### Step 2: Test It Works (1 minute)
```
1. Press Play
2. Start a game
3. Buy a unit
4. Check Console - you should see:
   [DECISION] Bought X - Good/Acceptable/etc.
5. If you see this, evaluation is working!
```

### Step 3: (Optional) Add Real-Time Hints (2 minutes)
```
1. Create UI → Canvas → Panel
2. Add Component → CoachingHintUI
3. Add child TextMeshPro texts:
   - Title Text
   - Description Text
   - Alternatives Text
4. Add Button → "Dismiss"
5. Assign references in CoachingHintUI inspector
6. Play → Make bad decision → See hint!
```

### Step 4: (Optional) Add Post-Game Analysis (2 minutes)
```
1. Create UI → Canvas → Panel (full screen)
2. Add Component → PostMortemUI
3. Add TextMeshPro for summary
4. Add ScrollView for phase list
5. Create simple prefab for phase rows
6. Assign references
7. Play full game → See analysis!
```

---

## 🎮 Usage

### In Code - Trigger Evaluation
```csharp
// Already done! Hooks are in GameManager/ShopManager
// Just play normally, system tracks automatically
```

### In Inspector - Change Coaching Mode
```csharp
// Find DecisionEvaluator in scene
// Expand "Coaching Settings"
// Change "Goal" dropdown:
//   - JustHaveFun (no hints)
//   - ImproveGradually (post-game only)
//   - CompetitiveClimb (occasional hints)
//   - Tryhard (aggressive coaching)
```

### Test Without Playing
```csharp
// Right-click DecisionEvaluatorBootstrap in Inspector
// Select "Test Mock Decision"
// A critical mistake hint will appear
```

---

## 🐛 Troubleshooting

### "DecisionEvaluator does not exist" errors
- **Solution**: Close and reopen Unity to recompile

### No hints showing up
- **Solution**: Check CoachingSettings.showRealTimeHints = true
- **Solution**: Make sure decision quality is Poor or Critical
- **Solution**: Verify CoachingHintUI is connected

### No post-mortem at game end
- **Solution**: Check CoachingSettings.showPostMortem = true
- **Solution**: Verify PostMortemUI is connected
- **Solution**: Make sure EndGame() was called

---

## 📊 What Gets Evaluated

- ✅ **Buying units** - Synergy, tier, timing
- ✅ **Selling units** - Buffed units, synergy loss
- ✅ **Rerolling shop** - Turn timing, gold efficiency
- ✅ **Leveling tavern** - Meta pace comparison
- ✅ **Ending turn** - Unspent gold, weak board

---

## 🎯 Decision Quality Scale

| Quality | Meaning | Hint Shown? |
|---------|---------|-------------|
| **Optimal** | Best possible play | No |
| **Good** | Solid decision | No |
| **Acceptable** | Okay, minor issues | No |
| **Questionable** | Risky choice | Sometimes |
| **Poor** | Clear mistake | Yes |
| **Critical** | Game-losing error | Always |

---

## 📁 Files You Got

### Must-Have (Core)
- `DecisionTypes.cs` - Data structures
- `DecisionEvaluator.cs` - Main engine
- `EvaluationRules.cs` - Evaluation logic
- `MetaSnapshot.cs` - Meta data
- `DecisionEvaluatorBootstrap.cs` - Easy setup

### Optional (UI)
- `CoachingHintUI.cs` - Real-time hints
- `PostMortemUI.cs` - Post-game analysis

### Backend
- `PlayFabManager.cs` (extended) - Upload/download
- `CloudScript_DecisionAggregation.js` - Server logic

### Modified
- `GameManager.cs` - Added hooks
- `ShopManager.cs` - Added hooks
- `CombatManager.cs` - Added hooks

### Documentation
- `DECISION_EVALUATOR_SETUP.md` - Full guide
- `DECISION_EVALUATOR_SUMMARY.md` - Overview
- `QUICK_START.md` - This file

---

## 🚀 Next Steps

### Minimal (Just Tracking)
1. Add Bootstrap to scene ✓
2. Play game ✓
3. Check console logs ✓
4. Done!

### With Hints
1. Create CoachingHintUI panel
2. Assign references
3. Set coaching goal to "CompetitiveClimb"
4. Make bad decisions → see hints

### Full Featured
1. Create both UIs
2. Configure PlayFab
3. Upload CloudScript
4. Test full flow
5. Iterate on rules

---

## 💡 Pro Tips

- **Start Simple**: Just Bootstrap + console logs
- **Test Deliberately**: Make obvious mistakes to trigger hints
- **Use Context Menu**: Right-click Bootstrap for test tools
- **Read Console**: All evaluations logged with colors
- **Iterate Rules**: Adjust thresholds in EvaluationRules.cs

---

## 📞 Need Help?

1. Read `DECISION_EVALUATOR_SETUP.md` for details
2. Check console for error messages
3. Use Bootstrap's "Print Current Settings"
4. Try "Test Mock Decision" to verify UI

---

## ✨ That's It!

You now have a working decision evaluation system. Start playing and watch your decisions get analyzed in real-time!

**Minimum Viable Setup**: Just add Bootstrap → Play → Check console
**Full Experience**: Add UIs → Configure PlayFab → Enjoy coaching

Happy evaluating! 🎮
