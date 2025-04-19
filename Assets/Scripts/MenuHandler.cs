// GitHub Copilot (Gemini 2.5 Pro) used to clean up and document this script.

using UnityEngine;
using UnityEngine.UI; // Required for Button
using TMPro; // Required for TextMeshPro InputFields

public class MenuHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField participantIdInput;
    [SerializeField] private TMP_InputField modalityInput;
    [SerializeField] private Button startExperimentButton;
    [SerializeField] private Button quitButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Add listeners to the buttons
        if (startExperimentButton != null)
        {
            startExperimentButton.onClick.AddListener(StartExperiment);
        }
        else
        {
            Debug.LogError("Start Experiment Button is not assigned in the Inspector.");
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitApplication);
        }
        else
        {
            Debug.LogError("Quit Button is not assigned in the Inspector.");
        }

        // Ensure ExperimentTracker exists
        if (ExperimentTracker.Instance == null)
        {
            Debug.LogError("ExperimentTracker instance not found. Make sure it's in the scene and initialized before the MenuHandler.");
        }
    }

    /// <summary>
    /// Called when the Start Experiment button is clicked.
    /// Reads input fields and starts the experiment via ExperimentTracker.
    /// </summary>
    public void StartExperiment()
    {
        if (participantIdInput == null || modalityInput == null)
        {
            Debug.LogError("One or both Input Fields are not assigned in the Inspector.");
            return;
        }

        string participantId = participantIdInput.text.Trim();
        string modality = modalityInput.text.Trim();

        if (string.IsNullOrEmpty(participantId) || string.IsNullOrEmpty(modality))
        {
            Debug.LogWarning("Participant ID and Modality cannot be empty.");
            return;
        }

        if (ExperimentTracker.Instance != null)
        {
            Debug.Log($"Starting experiment for Participant: '{participantId}', Modality: '{modality}'");
            ExperimentTracker.Instance.StartOrResumeExperiment(participantId, modality);
        }
        else
        {
            Debug.LogError("ExperimentTracker instance is null. Cannot start experiment.");
        }
    }

    /// <summary>
    /// Called when the Quit button is clicked.
    /// Closes the application.
    /// </summary>
    public void QuitApplication()
    {
        Debug.Log("Quitting application...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in the editor
#else
        Application.Quit(); // Quits the built application
#endif
    }

    // Optional: Clean up listeners when the object is destroyed
    void OnDestroy()
    {
        if (startExperimentButton != null)
        {
            startExperimentButton.onClick.RemoveListener(StartExperiment);
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitApplication);
        }
    }
}
