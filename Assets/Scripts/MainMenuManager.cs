using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio; // Required for Audio Control
using TMPro; // Required for Dropdowns
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Config")]
    [Tooltip("The exact name of your Game Scene (e.g. 'MainScene' or 'Game')")]
    public string gameSceneName = "GameScene";

    [Header("UI References")]
    public GameObject mainMenuPanel; // Parent object for Play/Settings/Quit buttons
    public GameObject settingsPanel;
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Settings UI References")]
    public AudioMixer audioMixer; // Drag your Master Mixer here
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    private Resolution[] resolutions;

    void Start()
    {
        // Ensure settings are closed and menu is open at start
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        // Hook up buttons
        if (playButton != null) playButton.onClick.AddListener(PlayGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        // Initialize Settings Data
        InitializeSettings();
    }

    void InitializeSettings()
    {
        // 1. Setup Resolution Dropdown
        if (resolutionDropdown != null)
        {
            resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                // Format: 1920 x 1080 @ 60Hz
                string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + resolutions[i].refreshRateRatio + "Hz";
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        // 2. Setup Fullscreen Toggle
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
        }

        // 3. Setup Quality Dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            
            string[] names = QualitySettings.names;
            List<string> options = new List<string>(names);
            
            qualityDropdown.AddOptions(options);

            // Auto-detect best settings based on hardware
            int bestQualityIndex = GetHardwareOptimizedQualityIndex();
            
            // Set the quality level (this applies it to the game immediately)
            QualitySettings.SetQualityLevel(bestQualityIndex);
            
            qualityDropdown.value = bestQualityIndex;
            qualityDropdown.RefreshShownValue();
        }
        else
        {
            Debug.LogError("MainMenuManager: Quality Dropdown is not assigned in the Inspector!");
        }
    }

    // --- HARDWARE DETECTION LOGIC ---
    int GetHardwareOptimizedQualityIndex()
    {
        int vram = SystemInfo.graphicsMemorySize;
        int ram = SystemInfo.systemMemorySize;
        int processors = SystemInfo.processorCount;
        
        // Log specs for debugging
        Debug.Log($"Hardware Detected: {vram}MB VRAM, {ram}MB RAM, {processors} Cores");

        int maxIndex = QualitySettings.names.Length - 1;
        
        // Heuristic: If we have lots of VRAM (> 6GB) and RAM (> 16GB), go for Max
        if (vram >= 6000 && ram >= 16000)
        {
            return maxIndex; // Ultra/High
        }
        // If decent specs (> 3GB VRAM), go for Middle/High
        if (vram >= 3000 && ram >= 8000)
        {
            return Mathf.FloorToInt(maxIndex * 0.75f); // 75% Quality
        }
        // If low specs, go Low
        if (vram < 2048)
        {
            return 0; // Lowest
        }

        // Default to Unity's internal suggestion if specs are average
        return QualitySettings.GetQualityLevel();
    }

    // --- GAME FLOW ---

    public void PlayGame()
    {
        Debug.Log("Loading Game Scene...");
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Application...");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false); // Hide main menu
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true); // Show main menu
    }

    // --- SETTINGS FUNCTIONS (Link these in Inspector) ---

    public void SetVolume(float volume)
    {
        // Note: Slider should be set from -80 (Min) to 0 (Max) in Inspector
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", volume);
        }
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutions != null && resolutionIndex < resolutions.Length)
        {
            Resolution resolution = resolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }
    }
}