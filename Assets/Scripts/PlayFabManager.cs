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
}