// GitHub Copilot (Claude 3.7 Sonnet) used to expidite repetetive code writing and provide suggestions for the following script.

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class SpaceshipController : MonoBehaviour
{
    #region Variables
    [Header("References")]
    [SerializeField]
    private Light shipLight;

    [SerializeField]
    private GameObject forwardViewScreen;
    private Material forwardViewScreenMaterial;
    
    [SerializeField]
    private GameObject radarScreen;
    private Material radarScreenMaterial;

    [SerializeField]
    private Material brokenScreenMaterial;

    [SerializeField]
    private float repairTime = 10f;

    private Color originalColor;

    private enum ShipState
    {
        Intact,
        Damaged
    }
    private ShipState shipState = ShipState.Intact;

    private enum ShieldState
    {
        Active,
        Inactive
    }
    private ShieldState shieldState = ShieldState.Inactive;

    public enum ShipSystem
    {
        Engines,
        ForwardViewScreen,
        RadarScreen,
        // Add more systems here as needed
    }

    private List<ShipSystem> damagedSystems = new List<ShipSystem>();
    
    [SerializeField]
    private float sidewaysSpeed;

    [SerializeField]
    private float verticalSpeed;

    [SerializeField]
    private float forwardSpeed;

    [SerializeField]
    private float rotationSpeed;
    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalColor = shipLight.color;
        forwardViewScreenMaterial = forwardViewScreen.GetComponent<Renderer>().material;
        radarScreenMaterial = radarScreen.GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsSystemDamaged(ShipSystem.Engines))
        {
            Rotate(rotationSpeed);
            Move(sidewaysSpeed, verticalSpeed, forwardSpeed);
        }
    }

    #region Movement
    public void setSidewaysSpeed(float speed)
    {
        sidewaysSpeed = speed * 10;
    }

    public void setVerticalSpeed(float speed)
    {
        verticalSpeed = speed * 10;
    }

    public void setForwardSpeed(float speed)
    {
        forwardSpeed = speed * 10;
    }

    public void setRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    private void Move(float sidewaysSpeed, float verticalSpeed, float forwardSpeed)
    {
        transform.position += transform.right * sidewaysSpeed * Time.deltaTime;
        transform.position += transform.up * verticalSpeed * Time.deltaTime;
        transform.position += transform.forward * forwardSpeed * Time.deltaTime;
    }

    private void Rotate(float speed)
    {
        transform.Rotate(Vector3.up, speed * Time.deltaTime);
    }
    #endregion

    #region Shield
    [ContextMenu("ToggleShield")]
    public void ToggleShield()
    {
        if (shieldState == ShieldState.Active)
        {
            shieldState = ShieldState.Inactive;
            originalColor = Color.white;
            if (shipState == ShipState.Intact)
            {
                StartCoroutine(PulseLight(false));
            }
        }
        else
        {
            shieldState = ShieldState.Active;
            originalColor = Color.cyan;
            if (shipState == ShipState.Intact)
            {
                StartCoroutine(PulseLight(false));
            }
        }
    }
    #endregion

    #region Ship Damage
    [ContextMenu("Damage (Indirect)")]
    public void DamageIndirect()
    {
        Damage(false);
    }

    [ContextMenu("Damage (Direct)")]
    public void DamageDirect()
    {
        Damage();
    }

    public void Damage(bool isDirect = true)
    {
        if (shieldState == ShieldState.Active && !isDirect)
        {
            ToggleShield();
        }
        else if (shipState == ShipState.Intact && shieldState == ShieldState.Active && isDirect)
        {
            ToggleShield();
            DamageRandomSystems();
            shipState = ShipState.Damaged;
            StartCoroutine(PulseLight());
        }
        else if (shipState == ShipState.Intact && shieldState == ShieldState.Inactive && !isDirect)
        {
            DamageRandomSystems();
            shipState = ShipState.Damaged;
            StartCoroutine(PulseLight());
        }
        else if (shipState == ShipState.Intact && shieldState == ShieldState.Inactive && isDirect)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else if (shipState == ShipState.Damaged)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    [ContextMenu("Repair Ship")]
    public void RepairShip()
    {
        if (shipState == ShipState.Damaged && damagedSystems.Count == 0)
        {
            shipState = ShipState.Intact;
        }
        else
        {
            Damage();
        }
    }

    private System.Collections.IEnumerator PulseLight(bool fromDamage = true)
    {
        Color damagedColor = (fromDamage ? Color.red : originalColor) * originalColor;

        float originalIntensity = shipLight.intensity;
        float newIntensity = originalIntensity * 0.5f;
        
        // Track elapsed time for auto-destruct
        float damageElapsedTime = 0f;

        // Fade to damaged color
        float time = 0;
        while (time < 0.5f)
        {
            shipLight.color = Color.Lerp(originalColor, damagedColor, time / 0.5f);
            shipLight.intensity = Mathf.Lerp(originalIntensity, newIntensity, time / 0.5f);
            time += Time.deltaTime;
            yield return null;
        }

        while (shipState == ShipState.Damaged)
        {
            // Increment damage timer if this is from damage
            if (fromDamage)
            {
                damageElapsedTime += Time.deltaTime;
                
                // Destroy ship if repair time has elapsed
                if (damageElapsedTime >= repairTime)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    yield break;
                }
            }
            
            // Pulse down
            time = 0;
            while (time < 0.5f && shipState == ShipState.Damaged)
            {
                shipLight.intensity = Mathf.Lerp(originalIntensity, newIntensity, time / 0.5f);
                time += Time.deltaTime;
                
                if (fromDamage)
                    damageElapsedTime += Time.deltaTime;
                    
                if (fromDamage && damageElapsedTime >= repairTime)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    yield break;
                }
                
                yield return null;
            }
            
            // Pulse up
            time = 0;
            while (time < 0.5f && shipState == ShipState.Damaged)
            {
                shipLight.intensity = Mathf.Lerp(newIntensity, originalIntensity, time / 0.5f);
                time += Time.deltaTime;
                
                if (fromDamage)
                    damageElapsedTime += Time.deltaTime;
                    
                if (fromDamage && damageElapsedTime >= repairTime)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    yield break;
                }
                
                yield return null;
            }
        }

        // Fade to green
        time = 0;
        while (time < 0.5f)
        {
            shipLight.color = Color.Lerp((fromDamage ? Color.green : originalColor) * originalColor, originalColor, time / 0.5f);
            shipLight.intensity = Mathf.Lerp(newIntensity, originalIntensity, time / 0.5f);
            time += Time.deltaTime;
            yield return null;
        }

        // Fade to original color
        time = 0;
        while (time < 0.5f)
        {
            shipLight.color = Color.Lerp(originalColor, originalColor, time / 0.5f);
            time += Time.deltaTime;
            yield return null;
        }
    }
    #endregion

    #region System Damage
    public bool IsSystemDamaged(ShipSystem system)
    {
        return damagedSystems.Contains(system);
    }

    public void DamageRandomSystems()
    {
        // Get list of all available systems that aren't already damaged
        var availableSystems = System.Enum.GetValues(typeof(ShipSystem))
            .Cast<ShipSystem>()
            .Where(system => !damagedSystems.Contains(system))
            .ToList();

        // Determine how many systems to damage (up to 3, limited by available systems)
        int systemsToBreak = Mathf.Min(3, availableSystems.Count);
        
        // Damage the selected number of random systems
        for (int i = 0; i < systemsToBreak; i++)
        {
            // Select a random available system
            int randomIndex = Random.Range(0, availableSystems.Count);
            ShipSystem randomSystem = availableSystems[randomIndex];
            
            // Damage the system
            DamageSystem(randomSystem);
            
            // Remove from available systems to prevent duplicate selection
            availableSystems.RemoveAt(randomIndex);
        }
    }

    [ContextMenu("Repair Forward View Screen")]
    public void RepairForwardViewScreen()
    {
        RepairSystem(ShipSystem.ForwardViewScreen);
    }

    [ContextMenu("Repair Radar Screen")]
    public void RepairRadarScreen()
    {
        RepairSystem(ShipSystem.RadarScreen);
    }

    [ContextMenu("Repair Engines")]
    public void RepairEngines()
    {
        RepairSystem(ShipSystem.Engines);
    }

    public void DamageSystem(ShipSystem system)
    {
        if (!damagedSystems.Contains(system))
        {
            damagedSystems.Add(system);
            // Handle specific system damage effects
            switch (system)
            {
                case ShipSystem.ForwardViewScreen:
                    if (forwardViewScreen != null && forwardViewScreen.TryGetComponent<Renderer>(out var renderer))
                        renderer.material = brokenScreenMaterial;
                    break;
                case ShipSystem.RadarScreen:
                    if (radarScreen != null && radarScreen.TryGetComponent<Renderer>(out var radarRenderer))
                        radarRenderer.material = brokenScreenMaterial;
                    break;
                case ShipSystem.Engines:
                    // Implement engine damage effects
                    break;
            }
        }
    }

    public void RepairSystem(ShipSystem system)
    {
        if (damagedSystems.Contains(system))
        {
            damagedSystems.Remove(system);
            // Handle specific system repair effects
            switch (system)
            {
                case ShipSystem.ForwardViewScreen:
                    if (forwardViewScreen != null && forwardViewScreen.TryGetComponent<Renderer>(out var renderer))
                        renderer.material = forwardViewScreenMaterial;
                    break;
                case ShipSystem.RadarScreen:
                    if (radarScreen != null && radarScreen.TryGetComponent<Renderer>(out var radarRenderer))
                        radarRenderer.material = radarScreenMaterial;
                    break;
                case ShipSystem.Engines:
                    // Implement engine repair effects
                    break;
            }
        }
        else
        {
            DamageSystem(system);
            shipState = ShipState.Damaged;
            StartCoroutine(PulseLight());
        }
    }
    #endregion
}
