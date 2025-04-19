// GitHub Copilot (Gemini 2.5 Pro) used to clean up and document this script.

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq; // Required for OrderBy
using UnityEngine.SceneManagement; // Required for SceneManager

public class ExperimentTracker : MonoBehaviour
{
    public static ExperimentTracker Instance { get; private set; }

    [System.Serializable]
    public class ModalityStats
    {
        public int TimesSystemDamaged = 0;
        public int TimesDamaged = 0;
        public int TimesDestroyed = 0;
        public int TimesShieldBrokenNoDamage = 0;
        public int TimesShieldBrokenWithDamage = 0;
        public float BestAttemptTime = float.MaxValue; // Initialize high so any time is better
    }

    [System.Serializable]
    public class ParticipantStats
    {
        public string ID;
        public Dictionary<string, ModalityStats> Modalities = new Dictionary<string, ModalityStats>();

        public ParticipantStats(string id)
        {
            ID = id;
        }
    }

    // Reference to experiment level to load
    [SerializeField]
    private string experimentLevelName = "SpaceScene"; // Set this to your actual level name

    // References to the Spaceship prefabs
    [SerializeField]
    private GameObject unimodalSpaceshipPrefab; // Assign in Inspector
    [SerializeField]
    private GameObject multimodalSpaceshipPrefab; // Assign in Inspector

    private Dictionary<string, ParticipantStats> participantsData = new Dictionary<string, ParticipantStats>();
    private string csvPath;

    // Store the currently active participant and modality
    private string currentParticipantId;
    private string currentModality;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            csvPath = Path.Combine(Application.streamingAssetsPath, "ExperimentStats.csv");
            EnsureStreamingAssetsFolder();
            LoadDataFromSpreadsheet();
            SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe to scene loaded event
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void EnsureStreamingAssetsFolder()
    {
        string streamingAssetsPath = Application.streamingAssetsPath;
        if (!Directory.Exists(streamingAssetsPath))
        {
            Directory.CreateDirectory(streamingAssetsPath);
            Debug.Log($"Created StreamingAssets folder at: {streamingAssetsPath}");
        }
    }

    private ParticipantStats GetOrCreateParticipant(string participantId)
    {
        if (!participantsData.TryGetValue(participantId, out ParticipantStats participant))
        {
            participant = new ParticipantStats(participantId);
            participantsData.Add(participantId, participant);
        }
        return participant;
    }

    private ModalityStats GetOrCreateModality(string participantId, string modality)
    {
        ParticipantStats participant = GetOrCreateParticipant(participantId);
        if (!participant.Modalities.TryGetValue(modality, out ModalityStats modalityStats))
        {
            modalityStats = new ModalityStats();
            participant.Modalities.Add(modality, modalityStats);
        }
        return modalityStats;
    }

    // Public Methods

    /// <summary>
    /// Ensures participant and modality records exist. Creates them if not found.
    /// Sets the current participant and modality for subsequent tracking calls.
    /// Loads the experiment level. The spaceship instantiation happens in OnSceneLoaded.
    /// </summary>
    /// <param name="participantId">The participant ID (e.g., "P1").</param>
    /// <param name="modality">The modality name (e.g., "unimodal", "multimodal").</param>
    public void StartOrResumeExperiment(string participantId, string modality)
    {
        // Store the current context
        this.currentParticipantId = participantId;
        this.currentModality = modality;

        GetOrCreateModality(participantId, modality); // This handles creating both if needed
        UpdateSpreadsheet(); // Ensure spreadsheet reflects the potentially new entry
        Debug.Log($"Starting experiment for Participant '{participantId}', Modality '{modality}'. Loading level '{experimentLevelName}'.");

        // Load the experiment level - Instantiation will happen in OnSceneLoaded
        SceneManager.LoadScene(experimentLevelName);
    }

    /// <summary>
    /// Increments the 'Times System Damaged' stat for the current participant and modality.
    /// </summary>
    public void IncrementTimesSystemDamaged()
    {
        if (string.IsNullOrEmpty(currentParticipantId) || string.IsNullOrEmpty(currentModality))
        {
            Debug.LogError("Cannot increment Times System Damaged: Current participant/modality not set. Call StartOrResumeExperiment first.");
            return;
        }
        ModalityStats stats = GetOrCreateModality(currentParticipantId, currentModality);
        stats.TimesSystemDamaged++;
        UpdateSpreadsheet();
        Debug.Log($"Times System Damaged incremented for {currentParticipantId} - {currentModality}. New count: {stats.TimesSystemDamaged}");
    }

    /// <summary>
    /// Increments the 'Times Damaged' stat for the current participant and modality.
    /// </summary>
    public void IncrementTimesDamaged()
    {
        if (string.IsNullOrEmpty(currentParticipantId) || string.IsNullOrEmpty(currentModality))
        {
            Debug.LogError("Cannot increment Times Damaged: Current participant/modality not set. Call StartOrResumeExperiment first.");
            return;
        }
        ModalityStats stats = GetOrCreateModality(currentParticipantId, currentModality);
        stats.TimesDamaged++;
        UpdateSpreadsheet();
        Debug.Log($"Times Damaged incremented for {currentParticipantId} - {currentModality}. New count: {stats.TimesDamaged}");
    }

    /// <summary>
    /// Increments the 'Times Destroyed' stat for the current participant and modality.
    /// </summary>
    public void IncrementTimesDestroyed()
    {
        if (string.IsNullOrEmpty(currentParticipantId) || string.IsNullOrEmpty(currentModality))
        {
            Debug.LogError("Cannot increment Times Destroyed: Current participant/modality not set. Call StartOrResumeExperiment first.");
            return;
        }
        ModalityStats stats = GetOrCreateModality(currentParticipantId, currentModality);
        stats.TimesDestroyed++;
        UpdateSpreadsheet();
        Debug.Log($"Times Destroyed incremented for {currentParticipantId} - {currentModality}. New count: {stats.TimesDestroyed}");
    }

    /// <summary>
    /// Increments the 'Times Shield Broken (No Damage)' stat for the current participant and modality.
    /// </summary>
    public void IncrementTimesShieldBrokenNoDamage()
    {
        if (string.IsNullOrEmpty(currentParticipantId) || string.IsNullOrEmpty(currentModality))
        {
            Debug.LogError("Cannot increment Times Shield Broken (No Damage): Current participant/modality not set. Call StartOrResumeExperiment first.");
            return;
        }
        ModalityStats stats = GetOrCreateModality(currentParticipantId, currentModality);
        stats.TimesShieldBrokenNoDamage++;
        UpdateSpreadsheet();
        Debug.Log($"Times Shield Broken (No Damage) incremented for {currentParticipantId} - {currentModality}. New count: {stats.TimesShieldBrokenNoDamage}");
    }

    /// <summary>
    /// Increments the 'Times Shield Broken (With Damage)' stat for the current participant and modality.
    /// </summary>
    public void IncrementTimesShieldBrokenWithDamage()
    {
        if (string.IsNullOrEmpty(currentParticipantId) || string.IsNullOrEmpty(currentModality))
        {
            Debug.LogError("Cannot increment Times Shield Broken (With Damage): Current participant/modality not set. Call StartOrResumeExperiment first.");
            return;
        }
        ModalityStats stats = GetOrCreateModality(currentParticipantId, currentModality);
        stats.TimesShieldBrokenWithDamage++;
        UpdateSpreadsheet();
        Debug.Log($"Times Shield Broken (With Damage) incremented for {currentParticipantId} - {currentModality}. New count: {stats.TimesShieldBrokenWithDamage}");
    }

    /// <summary>
    /// Sets the best attempt time for the current participant and modality if the new time is better.
    /// </summary>
    /// <param name="time">The attempt time.</param>
    public void SetBestAttemptTime(float time)
    {
        if (string.IsNullOrEmpty(currentParticipantId) || string.IsNullOrEmpty(currentModality))
        {
            Debug.LogError("Cannot set Best Attempt Time: Current participant/modality not set. Call StartOrResumeExperiment first.");
            return;
        }
        ModalityStats stats = GetOrCreateModality(currentParticipantId, currentModality);
        if (time < stats.BestAttemptTime)
        {
            stats.BestAttemptTime = time;
            UpdateSpreadsheet();
            Debug.Log($"New best time set for {currentParticipantId} - {currentModality}: {stats.BestAttemptTime:F2}s");
        }
        else
        {
             Debug.Log($"Attempt time {time:F2}s is not better than current best {stats.BestAttemptTime:F2}s for {currentParticipantId} - {currentModality}.");
        }
    }

    // Spreadsheet Handling

    private void UpdateSpreadsheet()
    {
        EnsureStreamingAssetsFolder(); // Ensure folder exists before writing

        StringBuilder sb = new StringBuilder();
        // Header
        sb.AppendLine("ParticipantID,Modality,TimesSystemDamaged,TimesDamaged,TimesDestroyed,TimesShieldBrokenNoDamage,TimesShieldBrokenWithDamage,BestAttemptTime");

        // Data rows - Sort by Participant ID then Modality for consistent output
        foreach (var participantPair in participantsData.OrderBy(p => p.Key))
        {
            ParticipantStats participant = participantPair.Value;
            foreach (var modalityPair in participant.Modalities.OrderBy(m => m.Key))
            {
                string modality = modalityPair.Key;
                ModalityStats stats = modalityPair.Value;
                string bestTimeStr = (stats.BestAttemptTime == float.MaxValue) ? "N/A" : stats.BestAttemptTime.ToString("F2"); // Format time nicely
                sb.AppendLine($"{participant.ID},{modality},{stats.TimesSystemDamaged},{stats.TimesDamaged},{stats.TimesDestroyed},{stats.TimesShieldBrokenNoDamage},{stats.TimesShieldBrokenWithDamage},{bestTimeStr}");
            }
        }

        try
        {
            File.WriteAllText(csvPath, sb.ToString());
            // Debug.Log($"Spreadsheet updated at: {csvPath}"); // Can be noisy
        }
        catch (IOException ex)
        {
            Debug.LogError($"Error writing spreadsheet to {csvPath}: {ex.Message}");
        }
    }

    private void LoadDataFromSpreadsheet()
    {
        if (!File.Exists(csvPath))
        {
            Debug.Log($"Spreadsheet file not found at {csvPath}. Starting fresh.");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length <= 1) return; // Empty or only header

            participantsData.Clear(); // Clear current data before loading

            // Check header length to determine if new columns exist
            string[] header = lines[0].Split(',');
            bool hasNewColumns = header.Length >= 8; // Check if header has at least 8 columns

            // Start from index 1 to skip header
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(',');
                // Expect at least 6 columns (old format) or 8 (new format)
                if (values.Length < 6) continue; // Skip malformed lines

                string participantId = values[0].Trim();
                string modality = values[1].Trim();
                int.TryParse(values[2].Trim(), out int timesSystemDamaged);
                int.TryParse(values[3].Trim(), out int timesDamaged);
                int.TryParse(values[4].Trim(), out int timesDestroyed);

                // Initialize new stats to 0
                int timesShieldBrokenNoDamage = 0;
                int timesShieldBrokenWithDamage = 0;
                float bestTime = float.MaxValue;

                // Parse new stats and best time based on column count
                if (hasNewColumns && values.Length >= 8)
                {
                    int.TryParse(values[5].Trim(), out timesShieldBrokenNoDamage);
                    int.TryParse(values[6].Trim(), out timesShieldBrokenWithDamage);
                    // Best time is now at index 7
                    if (values[7].Trim().ToLower() != "n/a" && float.TryParse(values[7].Trim(), out float parsedTime))
                    {
                        bestTime = parsedTime;
                    }
                }
                else if (values.Length >= 6) // Handle old format (best time at index 5)
                {
                    // Best time is at index 5 in the old format
                    if (values[5].Trim().ToLower() != "n/a" && float.TryParse(values[5].Trim(), out float parsedTime))
                    {
                        bestTime = parsedTime;
                    }
                }


                ModalityStats stats = GetOrCreateModality(participantId, modality); // This also creates the participant if needed
                stats.TimesSystemDamaged = timesSystemDamaged;
                stats.TimesDamaged = timesDamaged;
                stats.TimesDestroyed = timesDestroyed;
                stats.TimesShieldBrokenNoDamage = timesShieldBrokenNoDamage; // Assign parsed or default value
                stats.TimesShieldBrokenWithDamage = timesShieldBrokenWithDamage; // Assign parsed or default value
                stats.BestAttemptTime = bestTime;
            }
            Debug.Log($"Loaded data from {csvPath}");
        }
        catch (IOException ex)
        {
            Debug.LogError($"Error reading spreadsheet from {csvPath}: {ex.Message}");
        }
        catch (System.Exception ex)
        {
             Debug.LogError($"Error parsing spreadsheet data: {ex.Message}");
        }
    }

    /// <summary>
    /// Called automatically when a scene finishes loading.
    /// Instantiates the correct spaceship if the loaded scene is the experiment level.
    /// </summary>
    /// <param name="scene">The scene that was loaded.</param>
    /// <param name="mode">The mode the scene was loaded in.</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if the loaded scene is the experiment scene and we have a modality selected
        if (scene.name == experimentLevelName && !string.IsNullOrEmpty(currentModality))
        {
            Debug.Log($"Experiment level '{scene.name}' loaded. Attempting to instantiate spaceship for modality '{currentModality}'.");

            GameObject prefabToInstantiate = null;
            if (currentModality.ToLower() == "unimodal")
            {
                prefabToInstantiate = unimodalSpaceshipPrefab;
            }
            else if (currentModality.ToLower() == "multimodal")
            {
                prefabToInstantiate = multimodalSpaceshipPrefab;
            }

            if (prefabToInstantiate != null)
            {
                Vector3 spawnPosition = new Vector3(512, 0, 0);
                Instantiate(prefabToInstantiate, spawnPosition, Quaternion.Euler(0, 270, 0));
                Debug.Log($"Instantiated '{prefabToInstantiate.name}' at {spawnPosition}.");
            }
            else
            {
                Debug.LogError($"Spaceship prefab for modality '{currentModality}' is not assigned or the modality name is incorrect.");
            }
        }
        else if (scene.name == experimentLevelName)
        {
             Debug.LogWarning($"Loaded experiment level '{scene.name}' but no current modality is set. No spaceship instantiated.");
        }
    }
}
