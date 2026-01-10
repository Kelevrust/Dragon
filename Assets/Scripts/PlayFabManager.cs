// UNCOMMENT THIS LINE ONLY AFTER YOU HAVE INSTALLED THE PLAYFAB SDK AND RESTARTED UNITY
#define ENABLE_PLAYFAB 

using UnityEngine;
using System.Collections.Generic;

#if ENABLE_PLAYFAB
using PlayFab;
using PlayFab.ClientModels;
#endif

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager instance;

    [Header("Status")]
    public bool isLoggedIn = false;
    public string playFabId;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Login();
    }

    public void Login()
    {
#if ENABLE_PLAYFAB
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
#else
        Debug.LogWarning("PlayFab SDK not found. Using Mock Login.");
        isLoggedIn = true;
        playFabId = "MockID_12345";
        // Mock Load
        if (PlayerProfile.instance != null)
        {
            Debug.Log("Mock: Loaded Player Data.");
        }
#endif
    }

#if ENABLE_PLAYFAB
    void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("PlayFab Login Successful!");
        isLoggedIn = true;
        playFabId = result.PlayFabId;
        
        // Load Player Profile Data after login
        LoadPlayerData();
    }

    void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("PlayFab Login Failed: " + error.GenerateErrorReport());
    }
#endif

    // --- DATA SAVING ---

    public void SavePlayerProfile(PlayerProfile profile)
    {
        if (!isLoggedIn) return;

#if ENABLE_PLAYFAB
        var data = new Dictionary<string, string>
        {
            { "MMR", profile.mmr.ToString() },
            { "LossStreak", profile.lossStreak.ToString() }
            // Add other fields as needed (e.g. Rank, History)
        };

        var request = new UpdateUserDataRequest
        {
            Data = data
        };
        PlayFabClientAPI.UpdateUserData(request, OnDataSendSuccess, OnError);
#else
        Debug.Log($"Mock: Saved Profile (MMR: {profile.mmr})");
#endif
    }

#if ENABLE_PLAYFAB
    void OnDataSendSuccess(UpdateUserDataResult result)
    {
        Debug.Log("Player Profile Saved to PlayFab.");
    }
#endif

    // --- DATA LOADING ---

    public void LoadPlayerData()
    {
        if (!isLoggedIn) return;

#if ENABLE_PLAYFAB
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnDataReceived, OnError);
#endif
    }

#if ENABLE_PLAYFAB
    void OnDataReceived(GetUserDataResult result)
    {
        if (result.Data != null && PlayerProfile.instance != null)
        {
            if (result.Data.ContainsKey("MMR"))
            {
                if (int.TryParse(result.Data["MMR"].Value, out int loadedMMR))
                {
                    PlayerProfile.instance.mmr = loadedMMR;
                }
            }
            
            if (result.Data.ContainsKey("LossStreak"))
            {
                if (int.TryParse(result.Data["LossStreak"].Value, out int loadedStreak))
                {
                    PlayerProfile.instance.lossStreak = loadedStreak;
                }
            }
            
            Debug.Log("Player Data Loaded from PlayFab.");
        }
    }

    void OnError(PlayFabError error)
    {
        Debug.LogError("PlayFab Error: " + error.GenerateErrorReport());
    }
#endif

    // ============================================================================
    // DECISION EVALUATOR INTEGRATION
    // ============================================================================

    public void UploadGameLog(GameLog log)
    {
        if (!isLoggedIn || log == null) return;

#if ENABLE_PLAYFAB
        // Convert GameLog to JSON
        string logJson = JsonUtility.ToJson(log);

        // Create event data
        var eventData = new Dictionary<string, object>
        {
            { "SessionID", log.sessionId },
            { "Hero", log.heroName },
            { "Placement", log.finalPlacement },
            { "FinalTurn", log.finalTurn },
            { "StartMMR", log.startingMMR },
            { "FinalMMR", log.finalMMR },
            { "AvgScore", log.averageScore },
            { "OptimalPlays", log.optimalPlays },
            { "CriticalMistakes", log.criticalMistakes },
            { "FullLog", logJson }
        };

        var request = new PlayFab.ClientModels.WriteClientPlayerEventRequest
        {
            EventName = "game_decision_log",
            Body = eventData
        };

        PlayFabClientAPI.WritePlayerEvent(request, OnGameLogUploaded, OnGameLogUploadFailed);
#else
        Debug.Log($"<color=cyan>[MOCK UPLOAD]</color> Game log for {log.heroName} - Score: {log.averageScore:F1}");
#endif
    }

#if ENABLE_PLAYFAB
    void OnGameLogUploaded(PlayFab.ClientModels.WriteEventResponse result)
    {
        Debug.Log("<color=cyan>[PLAYFAB]</color> Game log uploaded successfully!");
    }

    void OnGameLogUploadFailed(PlayFabError error)
    {
        Debug.LogWarning($"<color=yellow>[PLAYFAB]</color> Failed to upload game log: {error.GenerateErrorReport()}");
        // TODO: Cache locally and retry later
    }
#endif

    // ============================================================================
    // META SNAPSHOT LOADING
    // ============================================================================

    public void LoadMetaSnapshot(System.Action<MetaSnapshot> onLoaded)
    {
#if ENABLE_PLAYFAB
        var request = new PlayFab.ClientModels.GetTitleDataRequest
        {
            Keys = new List<string> { "MetaSnapshot" }
        };

        PlayFabClientAPI.GetTitleData(request, result =>
        {
            if (result.Data != null && result.Data.ContainsKey("MetaSnapshot"))
            {
                string json = result.Data["MetaSnapshot"];
                MetaSnapshot meta = MetaSnapshot.FromJson(json);
                onLoaded?.Invoke(meta);
            }
            else
            {
                Debug.LogWarning("MetaSnapshot not found in TitleData. Using defaults.");
                onLoaded?.Invoke(new MetaSnapshot());
            }
        }, error =>
        {
            Debug.LogWarning($"Failed to load MetaSnapshot: {error.GenerateErrorReport()}");
            onLoaded?.Invoke(new MetaSnapshot());
        });
#else
        Debug.Log("<color=cyan>[MOCK]</color> Loading default MetaSnapshot.");
        onLoaded?.Invoke(new MetaSnapshot());
#endif
    }

    // ============================================================================
    // COACHING SETTINGS PERSISTENCE
    // ============================================================================

    public void SaveCoachingSettings(CoachingSettings settings)
    {
        if (!isLoggedIn) return;

#if ENABLE_PLAYFAB
        var data = new Dictionary<string, string>
        {
            { "CoachingGoal", settings.goal.ToString() },
            { "ShowPostMortem", settings.showPostMortem.ToString() },
            { "ShowRealTimeHints", settings.showRealTimeHints.ToString() }
        };

        var request = new UpdateUserDataRequest
        {
            Data = data
        };

        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("Coaching settings saved."),
            OnError);
#else
        Debug.Log($"<color=cyan>[MOCK]</color> Saved coaching goal: {settings.goal}");
#endif
    }

    public void LoadCoachingSettings(System.Action<CoachingSettings> onLoaded)
    {
        if (!isLoggedIn)
        {
            onLoaded?.Invoke(new CoachingSettings());
            return;
        }

#if ENABLE_PLAYFAB
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            var settings = new CoachingSettings();

            if (result.Data != null)
            {
                if (result.Data.ContainsKey("CoachingGoal"))
                {
                    if (System.Enum.TryParse(result.Data["CoachingGoal"].Value, out CoachingGoal goal))
                    {
                        settings.ApplyGoal(goal);
                    }
                }

                if (result.Data.ContainsKey("ShowPostMortem"))
                {
                    if (bool.TryParse(result.Data["ShowPostMortem"].Value, out bool showPostMortem))
                    {
                        settings.showPostMortem = showPostMortem;
                    }
                }

                if (result.Data.ContainsKey("ShowRealTimeHints"))
                {
                    if (bool.TryParse(result.Data["ShowRealTimeHints"].Value, out bool showHints))
                    {
                        settings.showRealTimeHints = showHints;
                    }
                }
            }

            onLoaded?.Invoke(settings);
        }, error =>
        {
            Debug.LogWarning($"Failed to load coaching settings: {error.GenerateErrorReport()}");
            onLoaded?.Invoke(new CoachingSettings());
        });
#else
        Debug.Log("<color=cyan>[MOCK]</color> Loading default coaching settings.");
        onLoaded?.Invoke(new CoachingSettings());
#endif
    }
}