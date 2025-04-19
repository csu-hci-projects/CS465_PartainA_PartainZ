// GitHub Copilot (Claude 3.7 Sonnet, Gemini 2.5 Pro) used to expidite repetetive code writing, provide suggestions, and complete documentation for the following script.

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro; // Add TextMeshPro namespace

/// <summary>
/// Enum defining the possible states for system indicator lights.
/// </summary>
public enum IndicatorState
{
    Off,
    On,
    Damaged,
    Cooldown // Typically used for weapons after firing
}

/// <summary>
/// Struct to hold the emission colors for different indicator states.
/// Configurable in the Inspector.
/// </summary>
[System.Serializable]
public struct IndicatorColors
{
    [Tooltip("Color when the system is off or inactive.")]
    public Color offColor;
    [Tooltip("Color when the system is on and operational.")]
    public Color onColor;
    [Tooltip("Color when the system is damaged.")]
    public Color damagedColor;
    [Tooltip("Color when the system is in cooldown (e.g., weapons).")]
    public Color cooldownColor;
}

/// <summary>
/// Struct to link a specific ship system to its visual indicator (Renderer and Colors).
/// Configurable in the Inspector.
/// </summary>
[System.Serializable]
public struct SystemIndicator
{
    [Tooltip("The ship system this indicator represents.")]
    public SpaceshipController.ShipSystem system;
    [Tooltip("The Renderer component of the indicator light GameObject.")]
    public Renderer indicatorRenderer;
    [Tooltip("The set of colors for this indicator's states.")]
    public IndicatorColors colors;
}

/// <summary>
/// Controls the spaceship's movement, systems, damage state, repairs, and indicators.
/// </summary>
public class SpaceshipController : MonoBehaviour
{
    #region Variables
    [Header("References")]
    [Tooltip("The main light component representing the ship's status.")]
    [SerializeField]
    private Light shipLight;

    [Tooltip("GameObject for the forward view screen display.")]
    [SerializeField]
    private GameObject forwardViewScreen;
    private Material forwardViewScreenMaterial; // Original material for the forward view screen

    [Tooltip("GameObject for the radar screen display.")]
    [SerializeField]
    private GameObject radarScreen;
    private Material radarScreenMaterial; // Original material for the radar screen

    [Tooltip("Material to apply to screens when their system is damaged.")]
    [SerializeField]
    private Material brokenScreenMaterial;
    [Tooltip("Material to apply to screens when the power system is down.")]
    [SerializeField]
    private Material powerOffScreenMaterial;

    [Tooltip("TextMeshPro Text component to display the current repair code input.")]
    [SerializeField]
    private TMP_Text repairCodeDisplayText; // Add reference for TMP_Text

    [Tooltip("Time in seconds the player has to repair the ship after taking damage before destruction (if mission started).")]
    [SerializeField]
    private float repairTime = 10f;
    private float actualRepairTime; // Stores the configured repair time before the mission starts

    private Color originalColor; // Original color of the ship light

    /// <summary>
    /// Represents the overall state of the ship (intact or damaged).
    /// </summary>
    private enum ShipState
    {
        Intact,
        Damaged
    }
    private ShipState shipState = ShipState.Intact;

    /// <summary>
    /// Represents the state of the ship's shields.
    /// </summary>
    private enum ShieldState
    {
        Active,
        Inactive
    }
    private ShieldState shieldState = ShieldState.Inactive;

    /// <summary>
    /// Enum defining the different repairable systems of the spaceship.
    /// </summary>
    public enum ShipSystem
    {
        Engines,
        ForwardViewScreen,
        RadarScreen,
        Weapons,
        Shields,
        LifeSupport,
        Power
    }

    private List<ShipSystem> damagedSystems = new List<ShipSystem>(); // List of currently damaged systems
    private Dictionary<ShipSystem, string> systemRepairCodes = new Dictionary<ShipSystem, string>(); // Maps systems to their 3-LETTER repair codes
    private string currentInputCode = ""; // Tracks the repair code characters entered by the player
    private const int MAX_CODE_LENGTH = 3; // Max characters for repair codes
    private bool isPowerDown = false; // Flag indicating if the Power system is damaged
    private Coroutine pulseLightCoroutine = null; // Reference to the currently running PulseLight coroutine

    // Static flag to track if the mission has started across scene loads
    private static bool missionHasStarted = false;
    private static float missionStartTime = -1f; // Time the mission started, static to persist across loads if needed conceptually, though Time.time resets

    // Add velocity storage
    private Vector3 currentVelocity = Vector3.zero;
    private float currentAngularVelocity = 0f;

    [Header("Movement Speeds")]
    [SerializeField]
    private float sidewaysSpeed;
    [SerializeField]
    private float verticalSpeed;
    [SerializeField]
    private float forwardSpeed;
    [SerializeField]
    private float rotationSpeed;

    [Header("Weapon Settings")]
    [Tooltip("Transform representing the weapon's firing origin and direction.")]
    [SerializeField]
    private Transform weaponFireOrigin;
    [Tooltip("GameObject representing the weapon visual.")]
    [SerializeField]
    private GameObject weaponVisual;
    [Tooltip("Minimum time in seconds between weapon shots.")]
    [SerializeField]
    private float weaponCooldown = 1.0f;
    private float lastFireTime = -1.0f; // Time.time when the weapon was last fired
    [Tooltip("Maximum distance the weapon raycast travels.")]
    [SerializeField]
    private float shootRange = 100f;
    [Tooltip("Layers the weapon's raycast and spherecast should interact with.")]
    [SerializeField]
    private LayerMask shootLayerMask = ~0; // Default: Everything
    [Tooltip("Name of the function to call on the GameObject hit directly by the weapon's raycast.")]
    [SerializeField]
    private string hitFunctionName = "Hit";
    [Tooltip("Radius for the sphere cast used for indirect hits if the direct raycast misses.")]
    [SerializeField]
    private float indirectHitRadius = 1.0f;
    [Tooltip("Name of the function to call on GameObjects hit indirectly by the weapon's spherecast.")]
    [SerializeField]
    private string indirectHitFunctionName = "OnNearMiss";

    [Header("System Indicators")]
    [Tooltip("Configure the indicator lights for each ship system.")]
    [SerializeField]
    private SystemIndicator[] systemIndicators; // Array to configure indicators in Inspector
    private Dictionary<ShipSystem, SystemIndicator> indicatorMap = new Dictionary<ShipSystem, SystemIndicator>(); // Quick lookup for system indicators
    private Dictionary<ShipSystem, Material> indicatorMaterialInstances = new Dictionary<ShipSystem, Material>(); // Stores material instances for emission control

    [Header("Other Settings")]
    [Tooltip("Mission start location.")]
    [SerializeField]
    private Vector3 startPosition; // Position to start the mission from

    #endregion

    void Start()
    {
        originalColor = shipLight.color;
        if (forwardViewScreen != null) forwardViewScreenMaterial = forwardViewScreen.GetComponent<Renderer>().material;
        if (radarScreen != null) radarScreenMaterial = radarScreen.GetComponent<Renderer>().material;

        // Store the configured repair time
        actualRepairTime = repairTime;

        // Check if the mission had already started before a scene reload
        if (missionHasStarted)
        {
            Debug.Log("Mission already started (detected on scene load). Activating repair timer.");
            repairTime = actualRepairTime; // Activate the timer immediately
            missionStartTime = Time.time; // Set the start time to now
        }
        else
        {
            // Disable the timer initially if mission hasn't started yet
            repairTime = float.PositiveInfinity;
            missionStartTime = -1f; // Ensure start time is reset if mission hasn't started
        }

        // Initialize velocities
        currentVelocity = Vector3.zero;
        currentAngularVelocity = 0f;

        // Assign hardcoded 3-LETTER repair codes for each system
        systemRepairCodes.Add(ShipSystem.Engines, "ENG");
        systemRepairCodes.Add(ShipSystem.ForwardViewScreen, "FVS");
        systemRepairCodes.Add(ShipSystem.RadarScreen, "RAD");
        systemRepairCodes.Add(ShipSystem.Weapons, "WPN");
        systemRepairCodes.Add(ShipSystem.Shields, "SHD");
        systemRepairCodes.Add(ShipSystem.LifeSupport, "LSP");
        systemRepairCodes.Add(ShipSystem.Power, "PWR");

        // Initialize System Indicators based on Inspector configuration
        InitializeIndicators();

        // Initialize repair code display
        ClearInputCode(); // Use this to set initial text to empty

        // If the mission has not started, damage the Power system to simulate a pre-flight check
        if (!missionHasStarted)
        {
            // Start the scenario with the Power system damaged
            // Check if mission has started; if so, the timer is already active from above.
            // If not started, PulseLight will run without the timer initially.
            DamageSystem(ShipSystem.Power);

            Debug.Log("Ship starting damaged after mission start. Ensuring PulseLight coroutine includes mission timer.");
            if (pulseLightCoroutine != null) StopCoroutine(pulseLightCoroutine); // Stop any existing pulse from DamageSystem
            pulseLightCoroutine = StartCoroutine(PulseLight(true)); // Restart pulse with timer active
        }
        else
        {
            // Mission has started, so we can set the ship to the start position
            transform.position = startPosition;
        }
    }

    void Update()
    {
        // Always apply current velocity and rotation regardless of engine state
        transform.position += currentVelocity * Time.deltaTime;
        transform.Rotate(Vector3.up, currentAngularVelocity * Time.deltaTime);

        // Update target velocity based on input/speed settings ONLY if Power is online and Engines are not damaged
        if (!isPowerDown && !IsSystemDamaged(ShipSystem.Engines))
        {
            // Calculate world-space velocity based on local inputs and speed settings
            currentVelocity = (transform.right * sidewaysSpeed) +
                              (transform.up * verticalSpeed) +
                              (transform.forward * forwardSpeed);

            // Calculate desired angular velocity
            currentAngularVelocity = rotationSpeed;
        }
    }

    #region Movement
    // Public methods to potentially allow external control of speeds (e.g., from sliders)
    public void setSidewaysSpeed(float speed) { sidewaysSpeed = speed * 10; }
    public void setVerticalSpeed(float speed) { verticalSpeed = speed * 10; }
    public void setForwardSpeed(float speed) { forwardSpeed = speed * 10; }
    public void setRotationSpeed(float speed) { rotationSpeed = speed; }
    #endregion

    #region Shield
    /// <summary>
    /// Toggles the ship's shields between Active and Inactive states.
    /// Cannot be toggled if Power is down or Shields are damaged.
    /// </summary>
    [ContextMenu("ToggleShield")]
    public void ToggleShield()
    {
        // Check prerequisites
        if (isPowerDown)
        {
            Debug.Log("Cannot toggle shields: Power is down.");
            return;
        }
        if (IsSystemDamaged(ShipSystem.Shields))
        {
            Debug.Log("Cannot toggle shields: Shields are damaged.");
            return;
        }

        // Toggle state
        if (shieldState == ShieldState.Active)
        {
            shieldState = ShieldState.Inactive;
            originalColor = Color.white; // Assuming default light color is white when shields off
            SetIndicatorState(ShipSystem.Shields, IndicatorState.Off);
            if (shipState == ShipState.Intact)
            {
                StartCoroutine(PulseLight(false)); // Brief pulse to indicate change
            }
        }
        else
        {
            shieldState = ShieldState.Active;
            originalColor = Color.cyan; // Assuming cyan light color when shields active
            SetIndicatorState(ShipSystem.Shields, IndicatorState.On);
            if (shipState == ShipState.Intact)
            {
                StartCoroutine(PulseLight(false)); // Brief pulse to indicate change
            }
        }
    }
    #endregion

    #region Ship Damage
    [ContextMenu("Damage (Indirect)")]
    private void DamageIndirect()
    {
        if (shipState == ShipState.Damaged) return; // Prevent damage if already damaged
        Damage(false);
    }

    [ContextMenu("Damage (Direct)")]
    private void DamageDirect()
    {
        if (shipState == ShipState.Damaged) return; // Prevent damage if already damaged
        Damage(true);
    }

    /// <summary>
    /// Applies damage to the ship based on whether it's a direct or indirect hit
    /// and the current shield/ship state. Can result in shield toggle, system damage, or destruction.
    /// </summary>
    /// <param name="isDirect">True for a direct hit, false for an indirect hit.</param>
    public void Damage(bool isDirect = true)
    {
        if (shieldState == ShieldState.Active && !isDirect)
        {
            // Indirect hit on active shields: Toggle shields off
            if (missionHasStarted) // Check mission state before incrementing
            {
                ExperimentTracker.Instance.IncrementTimesShieldBrokenNoDamage(); // Increment shield break (no damage) count
            }
            ToggleShield();
        }
        else if (shipState == ShipState.Intact && shieldState == ShieldState.Active && isDirect)
        {
            // Direct hit on active shields: Toggle shields off, damage systems
            if (missionHasStarted) // Check mission state before incrementing
            {
                ExperimentTracker.Instance.IncrementTimesShieldBrokenWithDamage(); // Increment shield break (with damage) count
            }
            ToggleShield();
            DamageRandomSystems();
        }
        else if (shipState == ShipState.Intact && shieldState == ShieldState.Inactive && !isDirect)
        {
            // Indirect hit on inactive shields: Damage systems
            DamageRandomSystems();
        }
        else if (shipState == ShipState.Intact && shieldState == ShieldState.Inactive && isDirect)
        {
            // Direct hit on inactive shields (intact ship): Destroy ship
            if (missionHasStarted)
            {
                ExperimentTracker.Instance.IncrementTimesDestroyed(); // Increment destruction count
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else if (shipState == ShipState.Damaged)
        {
            // Any hit on an already damaged ship: Destroy ship
            if (missionHasStarted)
            {
                ExperimentTracker.Instance.IncrementTimesDestroyed(); // Increment destruction count
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    /// <summary>
    /// Attempts to set the ship state back to Intact if all systems are repaired.
    /// If called while systems are still damaged, triggers destruction.
    /// </summary>
    [ContextMenu("Repair Ship")]
    public void RepairShip()
    {
        if (shipState == ShipState.Damaged && damagedSystems.Count == 0)
        {
            // All systems fixed, repair the ship
            shipState = ShipState.Intact;
            if (pulseLightCoroutine != null) StopCoroutine(pulseLightCoroutine);
            pulseLightCoroutine = StartCoroutine(PulseLight(false)); // Start repair pulse (fade to green then original)
        }
        else
        {
            // Called RepairShip prematurely or when intact? Treat as fatal error/damage.
            Damage();
        }
    }

    /// <summary>
    /// Coroutine to handle the ship light's pulsing effect when damaged or after repair/shield toggle.
    /// Also manages the repair countdown timer when applicable.
    /// </summary>
    /// <param name="fromDamage">True if the pulse is due to taking damage (red pulse, starts timer), false for other state changes (brief pulse).</param>
    private IEnumerator PulseLight(bool fromDamage = true)
    {
        Color targetColor = (fromDamage ? Color.red : originalColor) * originalColor; // Use red for damage, original for state change pulse
        float originalIntensity = shipLight.intensity;
        float pulseIntensity = originalIntensity * 0.5f;
        float damageElapsedTime = 0f; // Tracks time since damage if mission timer is active

        // Initial fade to target color/intensity
        float time = 0;
        while (time < 0.5f)
        {
            shipLight.color = Color.Lerp(originalColor, targetColor, time / 0.5f);
            shipLight.intensity = Mathf.Lerp(originalIntensity, pulseIntensity, time / 0.5f);
            time += Time.deltaTime;
            yield return null;
        }
        shipLight.color = targetColor; // Ensure final color is set

        // Continuous pulsing loop while damaged
        while (shipState == ShipState.Damaged)
        {
            // Check repair timer only if pulsing due to damage and timer is active
            if (fromDamage && !float.IsPositiveInfinity(repairTime))
            {
                damageElapsedTime += Time.deltaTime;
                if (damageElapsedTime >= repairTime)
                {
                    Debug.Log($"Repair time ({repairTime}s) elapsed. Ship destroyed.");
                    if (missionHasStarted) // Check mission state before incrementing
                    {
                        ExperimentTracker.Instance.IncrementTimesDestroyed(); // Increment destruction count
                    }
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    yield break; // Exit coroutine
                }
            }

            // Pulse intensity down
            time = 0;
            while (time < 0.5f && shipState == ShipState.Damaged) // Check state within loop
            {
                shipLight.intensity = Mathf.Lerp(originalIntensity, pulseIntensity, time / 0.5f);
                time += Time.deltaTime;
                // Re-check timer during pulse
                if (fromDamage && !float.IsPositiveInfinity(repairTime))
                {
                    damageElapsedTime += Time.deltaTime;
                    if (damageElapsedTime >= repairTime)
                    {
                        Debug.Log($"Repair time ({repairTime}s) elapsed during pulse down. Ship destroyed.");
                        if (missionHasStarted) // Check mission state before incrementing
                        {
                            ExperimentTracker.Instance.IncrementTimesDestroyed(); // Increment destruction count
                        }
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                        yield break;
                    }
                }
                yield return null;
            }

             // If ship got repaired during the down pulse, break the outer loop
            if (shipState != ShipState.Damaged) break;

            // Pulse intensity up
            time = 0;
            while (time < 0.5f && shipState == ShipState.Damaged) // Check state within loop
            {
                shipLight.intensity = Mathf.Lerp(pulseIntensity, originalIntensity, time / 0.5f);
                time += Time.deltaTime;
                 // Re-check timer during pulse
                if (fromDamage && !float.IsPositiveInfinity(repairTime))
                {
                    damageElapsedTime += Time.deltaTime;
                    if (damageElapsedTime >= repairTime)
                    {
                        Debug.Log($"Repair time ({repairTime}s) elapsed during pulse up. Ship destroyed.");
                        if (missionHasStarted) // Check mission state before incrementing
                        {
                            ExperimentTracker.Instance.IncrementTimesDestroyed(); // Increment destruction count
                        }
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                        yield break;
                    }
                }
                yield return null;
            }
        }

        // --- Ship is no longer damaged (or pulse was not from damage) ---

        // Fade to green briefly if pulse was from damage (repair complete)
        if (fromDamage)
        {
            time = 0;
            Color startColor = shipLight.color; // Current color (likely red)
            while (time < 0.5f)
            {
                shipLight.color = Color.Lerp(startColor, Color.green * originalColor, time / 0.5f);
                shipLight.intensity = Mathf.Lerp(shipLight.intensity, originalIntensity, time / 0.5f); // Ensure intensity returns to normal
                time += Time.deltaTime;
                yield return null;
            }
        }

        // Fade back to original color and intensity
        time = 0;
        Color finalStartColor = shipLight.color; // Could be red, green, or original
        float finalStartIntensity = shipLight.intensity;
        while (time < 0.5f)
        {
            shipLight.color = Color.Lerp(finalStartColor, originalColor, time / 0.5f);
            shipLight.intensity = Mathf.Lerp(finalStartIntensity, originalIntensity, time / 0.5f);
            time += Time.deltaTime;
            yield return null;
        }

        // Ensure final state is correct
        shipLight.color = originalColor;
        shipLight.intensity = originalIntensity;
        pulseLightCoroutine = null; // Clear the coroutine reference
    }
    #endregion

    #region System Damage and Repair

    public void OnDirectHit() { Debug.Log("AAAAAAAAAAAAAAAAAAAA");Damage(true); } // For direct hit from weapon
    public void OnIndirectHit() { Damage(false); } // For indirect hit from weapon

    /// <summary>
    /// Checks if a specific system is currently in the damaged list.
    /// </summary>
    /// <param name="system">The system to check.</param>
    /// <returns>True if the system is damaged, false otherwise.</returns>
    public bool IsSystemDamaged(ShipSystem system)
    {
        return damagedSystems.Contains(system);
    }

    /// <summary>
    /// Damages up to 3 random, currently operational systems.
    /// </summary>
    public void DamageRandomSystems()
    {
        // Get list of systems that are not already damaged
        var availableSystems = System.Enum.GetValues(typeof(ShipSystem))
            .Cast<ShipSystem>()
            .Where(system => !damagedSystems.Contains(system))
            .ToList();

        // Determine how many systems to damage (max 3, or fewer if not enough are available)
        int systemsToBreak = Mathf.Min(3, availableSystems.Count);

        // Damage the selected number of random systems
        for (int i = 0; i < systemsToBreak; i++)
        {
            if (availableSystems.Count == 0) break; // Safety check

            int randomIndex = Random.Range(0, availableSystems.Count);
            ShipSystem randomSystem = availableSystems[randomIndex];

            DamageSystem(randomSystem);

            // Remove from available list to prevent damaging the same system twice in one event
            availableSystems.RemoveAt(randomIndex);
        }
    }

    /// <summary>
    /// Appends a character (A-Z) from an input string to the current repair code input buffer.
    /// Expects a string containing a single uppercase letter.
    /// Resets the buffer if it's already at the maximum length before appending.
    /// Updates the repair code display text.
    /// </summary>
    /// <param name="inputString">The string containing the character (uppercase A-Z) to append.</param>
    public void AppendInputCharacter(string inputString)
    {
        // Validate the input string
        if (string.IsNullOrEmpty(inputString) || inputString.Length != 1)
        {
            Debug.LogWarning($"Invalid string input: '{inputString}'. Must be a single character string.");
            return;
        }

        char character = inputString[0];

        // Ensure input is an uppercase letter
        if (character < 'A' || character > 'Z')
        {
            Debug.LogWarning($"Invalid character input: {character}. Must be an uppercase letter (A-Z).");
            return;
        }

        // If current code is already max length, start a new code with this character
        if (currentInputCode.Length >= MAX_CODE_LENGTH)
        {
            currentInputCode = character.ToString();
        }
        else // Otherwise, append the character
        {
            currentInputCode += character;
        }
        Debug.Log($"Current Input Code: {currentInputCode}"); // Log for debugging/player feedback
        UpdateRepairCodeDisplay(); // Update the text display
    }

    /// <summary>
    /// Resets the repair code input buffer to an empty string and clears the display text.
    /// </summary>
    public void ClearInputCode()
    {
        currentInputCode = "";
        UpdateRepairCodeDisplay(); // Update the text display to be empty
        Debug.Log("Input code cleared.");
    }

    /// <summary>
    /// Updates the TextMeshPro component to show the current input code string.
    /// </summary>
    private void UpdateRepairCodeDisplay()
    {
        if (repairCodeDisplayText != null)
        {
            repairCodeDisplayText.text = currentInputCode;
        }
    }

    // Context Menu shortcuts for repairing specific systems (primarily for debugging)
    [ContextMenu("Repair Forward View Screen")] public void RepairForwardViewScreen() => RepairSystem(ShipSystem.ForwardViewScreen);
    [ContextMenu("Repair Radar Screen")] public void RepairRadarScreen() => RepairSystem(ShipSystem.RadarScreen);
    [ContextMenu("Repair Engines")] public void RepairEngines() => RepairSystem(ShipSystem.Engines);
    [ContextMenu("Repair Weapons")] public void RepairWeapons() => RepairSystem(ShipSystem.Weapons);
    [ContextMenu("Repair Shields")] public void RepairShields() => RepairSystem(ShipSystem.Shields);
    [ContextMenu("Repair Life Support")] public void RepairLifeSupport() => RepairSystem(ShipSystem.LifeSupport);
    [ContextMenu("Repair Power")] public void RepairPower() => RepairSystem(ShipSystem.Power);

    /// <summary>
    /// Marks a specific system as damaged, updating its state and indicator.
    /// Triggers the damaged ship state if not already damaged.
    /// Handles side effects like disabling screens or shields.
    /// </summary>
    /// <param name="system">The system to damage.</param>
    public void DamageSystem(ShipSystem system)
    {
        if (!damagedSystems.Contains(system))
        {
            damagedSystems.Add(system);
            // Only count system damage if the mission has started
            if (missionHasStarted)
            {
                ExperimentTracker.Instance.IncrementTimesSystemDamaged(); // Increment system damage count
            }
            Debug.Log($"{system} damaged!");
            SetIndicatorState(system, IndicatorState.Damaged);

            // Handle system-specific consequences of damage
            switch (system)
            {
                case ShipSystem.ForwardViewScreen:
                    if (!isPowerDown && forwardViewScreen != null && forwardViewScreen.TryGetComponent<Renderer>(out var renderer))
                        renderer.material = brokenScreenMaterial;
                    break;
                case ShipSystem.RadarScreen:
                    if (!isPowerDown && radarScreen != null && radarScreen.TryGetComponent<Renderer>(out var radarRenderer))
                        radarRenderer.material = brokenScreenMaterial;
                    break;
                case ShipSystem.Engines:
                    // No immediate visual effect, but movement stops in Update()
                    break;
                case ShipSystem.Weapons:
                    // Stop any active cooldown indicator reset coroutine
                    StopCoroutine(nameof(WeaponCooldownIndicatorReset)); // Use nameof for safety
                    break;
                case ShipSystem.Shields:
                    // If shields were active, deactivate them
                    if (shieldState == ShieldState.Active)
                    {
                        shieldState = ShieldState.Inactive;
                        originalColor = Color.white; // Reset light color assumption
                    }
                    break;
                case ShipSystem.LifeSupport:
                    // No immediate visual effect implemented here
                    break;
                case ShipSystem.Power:
                    isPowerDown = true;
                    UpdatePoweredSystemsState(true); // Turn off screens, etc.
                    // If shields were active, deactivate them
                    if (shieldState == ShieldState.Active)
                    {
                        shieldState = ShieldState.Inactive;
                        originalColor = Color.white; // Reset light color assumption
                        SetIndicatorState(ShipSystem.Shields, IndicatorState.Off); // Turn off shield indicator explicitly
                    }
                    break;
            }

            // If the ship was intact, transition to damaged state and start pulsing light
            if (shipState == ShipState.Intact)
            {
                // Only count as "ship damaged" for tracking purposes if the mission has started
                if (missionHasStarted)
                {
                    ExperimentTracker.Instance.IncrementTimesDamaged(); // Increment overall ship damage count ONCE
                }
                shipState = ShipState.Damaged;
                if (pulseLightCoroutine != null) StopCoroutine(pulseLightCoroutine);
                // Start pulsing, potentially with the timer if the mission has started
                pulseLightCoroutine = StartCoroutine(PulseLight(missionHasStarted));
            }
        }
    }

    /// <summary>
    /// Attempts to repair the specified system using the currently entered repair code.
    /// If the system is not damaged, it damages it instead.
    /// If the code is correct, repairs the system and updates its state/indicator.
    /// If the code is incorrect, destroys the ship.
    /// Clears the input code after the attempt.
    /// </summary>
    /// <param name="system">The system to attempt repair on.</param>
    public void RepairSystem(ShipSystem system)
    {
        // Check if the system is actually damaged
        if (!damagedSystems.Contains(system))
        {
            Debug.LogWarning($"Attempted to repair non-damaged system {system}. Damaging it instead!");
            DamageSystem(system); // Penalize by damaging the system
            ClearInputCode();     // Consume the input code attempt
            return;
        }

        // System is damaged, check the entered code (case-sensitive)
        if (systemRepairCodes.TryGetValue(system, out string correctCode) && currentInputCode == correctCode)
        {
            // Correct code entered
            Debug.Log($"Correct code entered for {system}. Repairing...");
            damagedSystems.Remove(system);

            // Handle system-specific consequences of repair
            switch (system)
            {
                case ShipSystem.ForwardViewScreen:
                    if (!isPowerDown && forwardViewScreen != null && forwardViewScreen.TryGetComponent<Renderer>(out var renderer))
                        renderer.material = forwardViewScreenMaterial; // Restore original material if power is on
                    SetIndicatorState(system, IndicatorState.On);
                    break;
                case ShipSystem.RadarScreen:
                    if (!isPowerDown && radarScreen != null && radarScreen.TryGetComponent<Renderer>(out var radarRenderer))
                        radarRenderer.material = radarScreenMaterial; // Restore original material if power is on
                    SetIndicatorState(system, IndicatorState.On);
                    break;
                case ShipSystem.Engines:
                    SetIndicatorState(system, IndicatorState.On); // Movement will resume in Update()
                    break;
                case ShipSystem.Weapons:
                    SetIndicatorState(system, IndicatorState.On); // Ready to fire
                    break;
                case ShipSystem.Shields:
                    SetIndicatorState(system, IndicatorState.Off); // Repaired but inactive by default
                    break;
                case ShipSystem.LifeSupport:
                    SetIndicatorState(system, IndicatorState.On);
                    break;
                case ShipSystem.Power:
                    isPowerDown = false;
                    SetIndicatorState(system, IndicatorState.On);
                    UpdatePoweredSystemsState(false); // Restore power to other systems/screens
                    break;
            }

            // Check if all systems are now repaired
            if (damagedSystems.Count == 0 && shipState == ShipState.Damaged)
            {
                RepairShip(); // Transition ship state back to Intact
            }
        }
        else
        {
            // Incorrect code entered
            string expectedCode = systemRepairCodes.ContainsKey(system) ? systemRepairCodes[system] : "N/A"; // Get expected code for logging
            Debug.LogError($"Incorrect code ('{currentInputCode}') entered for {system} (Expected: '{expectedCode}'). Ship destroyed!");
            if (missionHasStarted) // Check mission state before incrementing
            {
                ExperimentTracker.Instance.IncrementTimesDestroyed(); // Increment destruction count
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Destroy ship
        }

        // Always clear the input code after a repair attempt (success or failure)
        ClearInputCode();
    }

    /// <summary>
    /// Updates the materials of power-dependent systems (screens) based on power status.
    /// </summary>
    /// <param name="powerIsOff">True if power just went off, false if it just came on.</param>
    private void UpdatePoweredSystemsState(bool powerIsOff)
    {
        // Forward View Screen
        if (forwardViewScreen != null && forwardViewScreen.TryGetComponent<Renderer>(out var fvRenderer))
        {
            if (powerIsOff)
            {
                fvRenderer.material = powerOffScreenMaterial;
            }
            else // Power is restored
            {
                // Set material based on whether the screen system itself is still damaged
                fvRenderer.material = IsSystemDamaged(ShipSystem.ForwardViewScreen) ? brokenScreenMaterial : forwardViewScreenMaterial;
            }
        }

        // Radar Screen
        if (radarScreen != null && radarScreen.TryGetComponent<Renderer>(out var radarRenderer))
        {
            if (powerIsOff)
            {
                radarRenderer.material = powerOffScreenMaterial;
            }
            else // Power is restored
            {
                 // Set material based on whether the screen system itself is still damaged
                radarRenderer.material = IsSystemDamaged(ShipSystem.RadarScreen) ? brokenScreenMaterial : radarScreenMaterial;
            }
        }

        // Update indicators for all systems based on power status
        foreach (ShipSystem sys in System.Enum.GetValues(typeof(ShipSystem)))
        {
            if (sys == ShipSystem.Power) continue; // Skip the power indicator itself

            if (powerIsOff)
            {
                // If power goes off, set indicators to Off (unless they are damaged)
                if (!IsSystemDamaged(sys))
                {
                     SetIndicatorState(sys, IndicatorState.Off);
                }
            }
            else // Power is restored
            {
                 // If power comes on, set indicators to On/Off based on their default/current state (unless damaged)
                 if (!IsSystemDamaged(sys))
                 {
                     // Determine the correct 'restored' state
                     IndicatorState restoredState = IndicatorState.Off; // Default to Off
                     switch(sys)
                     {
                         case ShipSystem.Engines:
                         case ShipSystem.ForwardViewScreen:
                         case ShipSystem.RadarScreen:
                         case ShipSystem.Weapons:
                         case ShipSystem.LifeSupport:
                             restoredState = IndicatorState.On;
                             break;
                         case ShipSystem.Shields:
                             // Shields default to Off even when repaired/powered
                             restoredState = (shieldState == ShieldState.Active) ? IndicatorState.On : IndicatorState.Off;
                             break;
                     }
                     SetIndicatorState(sys, restoredState);
                 }
            }
        }
    }
    #endregion

    #region Weapons
    /// <summary>
    /// Fires the weapon if possible (Power On, Weapons not damaged, Cooldown finished).
    /// Performs a raycast for direct hits and a spherecast for indirect hits.
    /// Calls specified functions on hit objects.
    /// Damages the weapon system if fired too quickly.
    /// </summary>
    [ContextMenu("Fire Weapon")]
    public void FireWeapon()
    {
        // Check prerequisites
        if (isPowerDown)
        {
            Debug.LogWarning("Cannot fire: Power system is down!");
            return;
        }
        if (weaponFireOrigin == null)
        {
            Debug.LogError("Weapon Fire Origin not assigned! Cannot fire.");
            return;
        }
        if (weaponVisual == null) // Add check for weapon visual
        {
            Debug.LogError("Weapon Visual not assigned! Cannot show effect.");
            // Allow firing without visual, but log error. Could return here if visual is mandatory.
        }
        if (IsSystemDamaged(ShipSystem.Weapons))
        {
            Debug.LogWarning("Cannot fire: Weapons system is damaged!");
            return;
        }

        // Check cooldown and handle firing too fast
        if (Time.time < lastFireTime + weaponCooldown)
        {
            Debug.LogWarning("Firing too fast! Damaging Weapons system.");
            // Damage weapons only if not already damaged
            if (!IsSystemDamaged(ShipSystem.Weapons))
            {
                DamageSystem(ShipSystem.Weapons);
            }
            return;
        }

        // --- Fire Weapon ---
        lastFireTime = Time.time;
        Debug.Log("Firing weapon!");
        SetIndicatorState(ShipSystem.Weapons, IndicatorState.Cooldown);
        StartCoroutine(nameof(WeaponCooldownIndicatorReset), ShipSystem.Weapons); // Pass system for safety

        // Start the visual effect if assigned
        if (weaponVisual != null)
        {
            StartCoroutine(WeaponVisualEffect());
        }

        Vector3 firePosition = weaponFireOrigin.position;
        Vector3 fireDirection = weaponFireOrigin.forward;

        // 1. Direct Hit Raycast
        RaycastHit directHitInfo;
        Collider directHitCollider = null;
        bool directHit = Physics.Raycast(firePosition, fireDirection, out directHitInfo, shootRange, shootLayerMask);

        if (directHit)
        {
            directHitCollider = directHitInfo.collider;
            Debug.Log($"Direct Hit: {directHitCollider.name} at distance {directHitInfo.distance}");
            // Call the specified function on the hit object
            directHitCollider.SendMessageUpwards(hitFunctionName, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.Log("Direct shot missed.");
        }

        // 2. Indirect Hit SphereCast (along the ray path)
        // SphereCast starts slightly ahead to avoid hitting the ship itself if radius is large
        float sphereCastDistance = directHit ? directHitInfo.distance : shootRange; // Cast up to direct hit point or max range
        RaycastHit[] indirectHits = Physics.SphereCastAll(firePosition + (fireDirection * indirectHitRadius), // Start sphere slightly forward
                                                          indirectHitRadius,
                                                          fireDirection,
                                                          sphereCastDistance - indirectHitRadius, // Adjust distance for starting offset
                                                          shootLayerMask);

        if (indirectHits.Length > 0)
        {
            foreach (RaycastHit indirectHitInfo in indirectHits)
            {
                // Ignore the object that was hit directly (if any)
                if (indirectHitInfo.collider != directHitCollider)
                {
                    Debug.Log($"Indirect Hit: {indirectHitInfo.collider.name}");
                     // Call the specified function on the indirectly hit object
                    indirectHitInfo.collider.SendMessageUpwards(indirectHitFunctionName, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
        else if (!directHit) // Only log complete miss if both ray and sphere missed
        {
            Debug.Log("Shot completely missed (Ray and Sphere).");
        }
    }

    /// <summary>
    /// Coroutine to reset the weapon indicator state from Cooldown back to On after the cooldown period,
    /// but only if the system hasn't been damaged and power is still on.
    /// </summary>
    /// <param name="system">The weapon system (passed for safety, though always Weapons).</param>
    private IEnumerator WeaponCooldownIndicatorReset(ShipSystem system)
    {
        yield return new WaitForSeconds(weaponCooldown);

        // Only reset indicator if weapons are still operational and powered
        if (system == ShipSystem.Weapons && !IsSystemDamaged(system) && !isPowerDown)
        {
            SetIndicatorState(system, IndicatorState.On);
        }
    }

    private IEnumerator WeaponVisualEffect()
    {
        Vector3 originalScale = weaponVisual.transform.localScale;

        // Enable the visual effect
        weaponVisual.SetActive(true);

        // Shrink effect over time
        float time = 0;
        while (time < 1f)
        {
            weaponVisual.transform.localScale = Vector3.Lerp(originalScale, new Vector3(0, 0, originalScale.z), time);
            time += Time.deltaTime;
            yield return null;
        }

        // Disable the visual effect
        weaponVisual.SetActive(false);
        
        // Reset scale for next use
        weaponVisual.transform.localScale = originalScale;
        
        yield return null;
    }
    #endregion

    #region Indicators
    /// <summary>
    /// Initializes the system indicators based on the `systemIndicators` array configured in the Inspector.
    /// Creates material instances for emission control and sets initial states.
    /// </summary>
    private void InitializeIndicators()
    {
        indicatorMap.Clear();
        indicatorMaterialInstances.Clear();

        foreach (var indicatorSetup in systemIndicators)
        {
            if (indicatorSetup.indicatorRenderer != null)
            {
                // Create a unique material instance for this indicator to control emission independently
                Material matInstance = new Material(indicatorSetup.indicatorRenderer.material);
                indicatorSetup.indicatorRenderer.material = matInstance;

                // Store references for quick lookup
                indicatorMap[indicatorSetup.system] = indicatorSetup;
                indicatorMaterialInstances[indicatorSetup.system] = matInstance;

                // Ensure the Emission keyword is enabled on the material instance
                matInstance.EnableKeyword("_EMISSION");

                // Determine the initial state based on the system type (assuming power is initially on, will be corrected by initial Power damage)
                IndicatorState initialState;
                switch (indicatorSetup.system)
                {
                    case ShipSystem.Shields:
                        initialState = IndicatorState.Off; // Shields start off
                        break;
                    case ShipSystem.Engines:
                    case ShipSystem.ForwardViewScreen:
                    case ShipSystem.RadarScreen:
                    case ShipSystem.Weapons:
                    case ShipSystem.LifeSupport:
                    case ShipSystem.Power:
                        initialState = IndicatorState.On; // Most systems start on
                        break;
                    default:
                        initialState = IndicatorState.Off; // Default to off for unhandled systems
                        break;
                }
                // Set the initial color (will be overridden immediately if power starts damaged)
                SetIndicatorState(indicatorSetup.system, initialState);
            }
            else
            {
                Debug.LogWarning($"Indicator Renderer not assigned for system: {indicatorSetup.system} in the Inspector.");
            }
        }
    }

    /// <summary>
    /// Sets the emission color of a system's indicator light based on the desired state.
    /// </summary>
    /// <param name="system">The system whose indicator to update.</param>
    /// <param name="state">The desired state (On, Off, Damaged, Cooldown).</param>
    private void SetIndicatorState(ShipSystem system, IndicatorState state)
    {
        // Check if we have a configured indicator and material instance for this system
        if (indicatorMap.TryGetValue(system, out SystemIndicator indicator) && indicatorMaterialInstances.TryGetValue(system, out Material matInstance))
        {
            Color targetColor;
            // Select the color based on the state from the configured IndicatorColors
            switch (state)
            {
                case IndicatorState.Off:      targetColor = indicator.colors.offColor; break;
                case IndicatorState.On:       targetColor = indicator.colors.onColor; break;
                case IndicatorState.Damaged:  targetColor = indicator.colors.damagedColor; break;
                case IndicatorState.Cooldown: targetColor = indicator.colors.cooldownColor; break;
                default:                      targetColor = Color.black; break; // Default fallback
            }

            // Apply the color to the material instance's emission property
            matInstance.SetColor("_EmissionColor", targetColor);
        }
        else
        {
            // Log a warning only if an indicator was expected (i.e., configured in the array) but not found in the dictionaries
            bool shouldHaveIndicator = systemIndicators.Any(si => si.system == system && si.indicatorRenderer != null);
            if (shouldHaveIndicator)
            {
                Debug.LogWarning($"SetIndicatorState failed: Indicator or material instance not found for system: {system}. Was InitializeIndicators called correctly?");
            }
            // If no indicator was configured for this system, silently ignore.
        }
    }
    #endregion

    #region Debug Repair Input (Context Menu)
    // These methods allow triggering repair attempts directly from the Unity Editor's context menu for testing.

    // --- Engines ---
    [ContextMenu("Input Code: Engines (ENG)")]
    private void InputCode_Engines_ENG()
    {
        ClearInputCode(); // Clear previous input first
        AppendInputCharacter("E"); AppendInputCharacter("N"); AppendInputCharacter("G");
        RepairEngines();
    }

    // --- Forward View Screen ---
    [ContextMenu("Input Code: Forward View (FVS)")]
    private void InputCode_ForwardView_FVS()
    {
        ClearInputCode();
        AppendInputCharacter("F"); AppendInputCharacter("V"); AppendInputCharacter("S");
        RepairForwardViewScreen();
    }

    // --- Radar Screen ---
    [ContextMenu("Input Code: Radar (RAD)")]
    private void InputCode_Radar_RAD()
    {
        ClearInputCode();
        AppendInputCharacter("R"); AppendInputCharacter("A"); AppendInputCharacter("D");
        RepairRadarScreen();
    }

    // --- Weapons ---
    [ContextMenu("Input Code: Weapons (WPN)")]
    private void InputCode_Weapons_WPN()
    {
        ClearInputCode();
        AppendInputCharacter("W"); AppendInputCharacter("P"); AppendInputCharacter("N");
        RepairWeapons();
    }

    // --- Shields ---
    [ContextMenu("Input Code: Shields (SHD)")]
    private void InputCode_Shields_SHD()
    {
        ClearInputCode();
        AppendInputCharacter("S"); AppendInputCharacter("H"); AppendInputCharacter("D");
        RepairShields();
    }

    // --- Life Support ---
    [ContextMenu("Input Code: Life Support (LSP)")]
    private void InputCode_LifeSupport_LSP()
    {
        ClearInputCode();
        AppendInputCharacter("L"); AppendInputCharacter("S"); AppendInputCharacter("P");
        RepairLifeSupport();
    }

    // --- Power ---
    [ContextMenu("Input Code: Power (PWR)")]
    private void InputCode_Power_PWR()
    {
        ClearInputCode();
        AppendInputCharacter("P"); AppendInputCharacter("W"); AppendInputCharacter("R");
        RepairPower();
    }

    // --- Incorrect Code Example ---
    [ContextMenu("Input Code: Incorrect (ABC) for Power")]
    private void InputCode_Incorrect_Power()
    {
        ClearInputCode();
        AppendInputCharacter("A"); AppendInputCharacter("B"); AppendInputCharacter("C");
        RepairPower(); // This will trigger the incorrect code logic (ship destruction)
    }

    // --- Code Without Repair ---
    [ContextMenu("Input Code: No Repair (XYZ)")]
    private void InputCode_NoRepair_XYZ()
    {
        ClearInputCode();
        AppendInputCharacter("X"); AppendInputCharacter("Y"); AppendInputCharacter("Z");
    }
    #endregion

    #region Mission Control
    /// <summary>
    /// Starts the mission timer. Once called, taking damage will start the countdown
    /// defined by `repairTime`. Sets a static flag to persist this state across scene loads.
    /// If the ship is damaged when the mission starts, it will be repaired.
    /// Can only be called once effectively per application run until EndMission is called.
    /// </summary>
    [ContextMenu("Start Mission")]
    public void StartMission()
    {
        // Check if the mission hasn't already been marked as started
        if (!missionHasStarted)
        {
            Debug.Log($"Starting mission timer with duration: {actualRepairTime} seconds.");
            missionHasStarted = true; // Set the static flag
            missionStartTime = Time.time; // Record the start time
            repairTime = actualRepairTime; // Set the actual timer duration for this instance

            // If the ship is damaged when the mission starts, repair it.
            if (shipState == ShipState.Damaged)
            {
                 Debug.Log("Ship is damaged when mission starts. Repairing all systems.");

                 // Stop the damage pulse light immediately
                 if (pulseLightCoroutine != null)
                 {
                    StopCoroutine(pulseLightCoroutine);
                    pulseLightCoroutine = null;
                 }
                 // Reset ship light
                 shipLight.color = originalColor;
                 shipLight.intensity = shipLight.intensity; // Assuming originalIntensity is stored

                 // Store systems that were damaged before clearing the list
                 List<ShipSystem> previouslyDamaged = new List<ShipSystem>(damagedSystems);
                 damagedSystems.Clear(); // Clear the list of damaged systems

                 // Reset ship state
                 shipState = ShipState.Intact;

                 // Check if power was down and restore it
                 if (previouslyDamaged.Contains(ShipSystem.Power))
                 {
                     isPowerDown = false;
                     // UpdatePoweredSystemsState(false) will handle indicators and screens based on their own damage state,
                     // but since we cleared damagedSystems, it should restore them correctly.
                     UpdatePoweredSystemsState(false);
                     // Explicitly set Power indicator to On as UpdatePoweredSystemsState skips it
                     SetIndicatorState(ShipSystem.Power, IndicatorState.On);
                 }
                 else
                 {
                     // If power wasn't down, still need to potentially fix screens and reset indicators
                     // for other systems that might have been damaged.
                     if (previouslyDamaged.Contains(ShipSystem.ForwardViewScreen))
                     {
                         if (forwardViewScreen != null && forwardViewScreen.TryGetComponent<Renderer>(out var renderer))
                             renderer.material = forwardViewScreenMaterial;
                         SetIndicatorState(ShipSystem.ForwardViewScreen, IndicatorState.On);
                     }
                     if (previouslyDamaged.Contains(ShipSystem.RadarScreen))
                     {
                         if (radarScreen != null && radarScreen.TryGetComponent<Renderer>(out var radarRenderer))
                             radarRenderer.material = radarScreenMaterial;
                         SetIndicatorState(ShipSystem.RadarScreen, IndicatorState.On);
                     }
                     // Reset indicators for other potentially damaged systems
                     if (previouslyDamaged.Contains(ShipSystem.Engines)) SetIndicatorState(ShipSystem.Engines, IndicatorState.On);
                     if (previouslyDamaged.Contains(ShipSystem.Weapons)) SetIndicatorState(ShipSystem.Weapons, IndicatorState.On);
                     if (previouslyDamaged.Contains(ShipSystem.Shields)) SetIndicatorState(ShipSystem.Shields, IndicatorState.Off); // Shields default to off
                     if (previouslyDamaged.Contains(ShipSystem.LifeSupport)) SetIndicatorState(ShipSystem.LifeSupport, IndicatorState.On);
                 }

                 // Clear any typed repair code
                 ClearInputCode();
            }
        }
        else
        {
            Debug.LogWarning("Mission timer already started. Cannot start again.");
            // Ensure the timer is active even if called again redundantly
            if (float.IsPositiveInfinity(repairTime))
            {
                repairTime = actualRepairTime;
            }
        }
    }

    /// <summary>
    /// Ends the current mission, calculates the duration, updates the ExperimentTracker,
    /// and resets the mission state flags.
    /// </summary>
    [ContextMenu("End Mission")]
    public void EndMission()
    {
        if (missionHasStarted)
        {
            float missionDuration = Time.time - missionStartTime;
            Debug.Log($"Mission ended. Duration: {missionDuration:F2} seconds.");

            // Update the tracker with the time
            if (ExperimentTracker.Instance != null)
            {
                ExperimentTracker.Instance.SetBestAttemptTime(missionDuration);
            }
            else
            {
                Debug.LogError("ExperimentTracker instance not found. Cannot record mission time.");
            }

            // Reset mission state
            missionHasStarted = false;
            missionStartTime = -1f;
            repairTime = float.PositiveInfinity; // Disable repair countdown timer
            
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        }
        else
        {
            Debug.LogWarning("Cannot end mission: Mission has not started.");
        }
    }
    #endregion
}
