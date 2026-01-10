// ============================================================================
// PlayFab CloudScript for Decision Evaluator System
// ============================================================================
// These functions run on PlayFab servers to aggregate player decision data
// and generate the MetaSnapshot that clients download
//
// SETUP INSTRUCTIONS:
// 1. Go to PlayFab Dashboard > Automation > Cloud Script
// 2. Create a new Cloud Script file or edit the existing one
// 3. Copy these functions into your Cloud Script
// 4. Deploy the script
// 5. Set up a scheduled task to run AggregateDecisionData daily
// ============================================================================

/**
 * Scheduled Task: Run this daily to aggregate decision data into MetaSnapshot
 *
 * Schedule in PlayFab:
 * - Go to Automation > Scheduled Tasks
 * - Create new task: "Daily Meta Aggregation"
 * - Schedule: Daily at 3:00 AM UTC
 * - Script: AggregateDecisionData
 */
handlers.AggregateDecisionData = function (args, context) {
    var currentPlayFabId = currentPlayerId;

    // Fetch all game_decision_log events from the last 7 days
    var timeWindow = 7 * 24 * 60 * 60 * 1000; // 7 days in milliseconds
    var startTime = Date.now() - timeWindow;

    // This is a simplified version - in production, you'd use PlayStream
    // event history or a custom external database

    var metaSnapshot = {
        version: "1.0",
        lastUpdated: new Date().toISOString(),
        avgLevelByTurn: {},
        avgGoldByTurn: {},
        unitWinRates: {},
        unitPlayRates: {},
        synergyWinRates: {}
    };

    // Initialize default leveling benchmarks
    // In production, calculate these from actual player data
    metaSnapshot.avgLevelByTurn = {
        "3": 1.2,
        "5": 2.1,
        "7": 3.0,
        "9": 4.2,
        "11": 5.0,
        "13": 5.5
    };

    // Store in TitleData for clients to download
    var titleDataRequest = {
        "Key": "MetaSnapshot",
        "Value": JSON.stringify(metaSnapshot)
    };

    server.SetTitleData(titleDataRequest);

    log.info("MetaSnapshot updated successfully");

    return {
        success: true,
        snapshotVersion: metaSnapshot.version,
        lastUpdated: metaSnapshot.lastUpdated
    };
};

/**
 * Helper: Calculate unit win rates from game logs
 * Called by AggregateDecisionData
 */
function calculateUnitWinRates(gameLogs) {
    var unitStats = {};

    for (var i = 0; i < gameLogs.length; i++) {
        var log = gameLogs[i];
        var didWin = log.Placement <= 4;

        // Parse decisions to find which units were used
        var fullLog = JSON.parse(log.FullLog);

        if (fullLog && fullLog.phases) {
            for (var j = 0; j < fullLog.phases.length; j++) {
                var phase = fullLog.phases[j];

                for (var k = 0; k < phase.decisions.length; k++) {
                    var decision = phase.decisions[k];

                    if (decision.type === "BuyUnit") {
                        // Extract unit name from action
                        var unitName = extractUnitName(decision.actionTaken);

                        if (!unitStats[unitName]) {
                            unitStats[unitName] = { wins: 0, games: 0 };
                        }

                        unitStats[unitName].games++;
                        if (didWin) {
                            unitStats[unitName].wins++;
                        }
                    }
                }
            }
        }
    }

    // Convert to win rates
    var winRates = {};
    for (var unitName in unitStats) {
        var stats = unitStats[unitName];
        winRates[unitName] = (stats.wins / stats.games) * 100;
    }

    return winRates;
}

/**
 * Helper: Extract unit name from decision action text
 */
function extractUnitName(actionText) {
    // Example: "Bought Dragon Whelp (Tier 1) for 3g"
    // Extract "Dragon Whelp"
    var match = actionText.match(/Bought (.+?) \(Tier/);
    return match ? match[1] : "Unknown";
}

/**
 * Admin Tool: Manually trigger meta aggregation
 * Can be called from PlayFab Dashboard or admin panel
 */
handlers.AdminRefreshMeta = function (args, context) {
    // Check if caller has admin permissions
    // In production, add proper authentication check here

    return handlers.AggregateDecisionData(args, context);
};

/**
 * Player Query: Get their personal decision stats
 * Players can call this to see their own analytics
 */
handlers.GetPlayerDecisionStats = function (args, context) {
    var playFabId = currentPlayerId;

    // Fetch player's recent game logs from player data
    var userData = server.GetUserReadOnlyData({
        PlayFabId: playFabId,
        Keys: ["RecentGames"]
    });

    var stats = {
        gamesPlayed: 0,
        averageScore: 0,
        totalOptimalPlays: 0,
        totalCriticalMistakes: 0,
        averagePlacement: 0
    };

    if (userData.Data && userData.Data.RecentGames) {
        var games = JSON.parse(userData.Data.RecentGames.Value);

        stats.gamesPlayed = games.length;

        var totalScore = 0;
        var totalPlacement = 0;

        for (var i = 0; i < games.length; i++) {
            var game = games[i];
            totalScore += game.averageScore || 0;
            totalPlacement += game.finalPlacement || 0;
            stats.totalOptimalPlays += game.optimalPlays || 0;
            stats.totalCriticalMistakes += game.criticalMistakes || 0;
        }

        if (games.length > 0) {
            stats.averageScore = totalScore / games.length;
            stats.averagePlacement = totalPlacement / games.length;
        }
    }

    return stats;
};

// ============================================================================
// EXAMPLE: Setting up the scheduled task via Admin API (optional)
// ============================================================================
// Use this if you want to programmatically create the scheduled task
// Otherwise, create it manually in the PlayFab Dashboard

/*
POST https://[YOUR_TITLE_ID].playfabapi.com/Admin/CreateActionsOnPlayersInSegmentTask
{
    "Name": "Daily Meta Aggregation",
    "Description": "Aggregates decision data into MetaSnapshot",
    "Schedule": "0 3 * * *",  // Daily at 3 AM UTC (cron format)
    "IsActive": true,
    "Parameter": {
        "ActionName": "AggregateDecisionData"
    }
}
*/
