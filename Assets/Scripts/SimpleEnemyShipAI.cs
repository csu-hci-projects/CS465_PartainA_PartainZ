// GitHub Copilot (Gemini 2.5 Pro) used to clean up and document this script.

using UnityEngine;
using System.Collections; // Required for Coroutines

public class SimpleEnemyShipAI : MonoBehaviour
{
    private Transform targetShip;

    [Header("Movement")]
    public float minMoveTime = 1.0f;
    public float maxMoveTime = 3.0f;
    public float minMoveDistance = 2.0f;
    public float maxMoveDistance = 5.0f;
    public float moveSpeed = 5.0f; // Speed of strafing movement
    public LayerMask obstacleLayers; // Layers to check for collision
    public int maxMoveRetries = 5; // Max attempts to find a clear path

    [Header("Shooting")]
    public float minShootTime = 0.5f;
    public float maxShootTime = 1.5f;
    [SerializeField] // Changed to SerializeField for consistency, can remain public if needed
    private Transform weaponFireOrigin; // Point from where the weapon raycast originates
    public GameObject weaponVisual; // Assign the visual effect object/prefab instance
    public GameObject indicatorObject; // Assign the visual indicator object/prefab instance
    public Vector3 indicatorScaleTarget = new Vector3(2f, 2f, 2f); // Scale indicator reaches
    public float indicatorGrowTime = 0.3f; // Time for indicator to grow
    public float shootRange = 100f; // Max distance of the shot raycast
    public string hitFunctionName = "TakeDamage"; // Function to call on hit object
    [Space] // Add some space in the inspector
    public float indirectHitRadius = 1.0f; // Radius for the sphere cast if direct raycast misses
    public string indirectHitFunctionName = "OnNearMiss"; // Function to call on indirect hit object
    public LayerMask shootLayerMask = ~0; // Layers the raycast/spherecast should hit (default: everything)

    [Header("Health")]
    public float health = 4f;
    public GameObject[] damagableParts;

    private float moveTimer;
    private float shootTimer;
    private bool isActive = false;
    private Vector3 targetPosition;
    private bool isShooting = false; // Flag to indicate shooting animation is active
    private Vector3 initialIndicatorScale; // Store initial scale

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        targetShip = GameObject.FindWithTag("Spaceship").transform;
        // Initialize targetPosition to current position to avoid moving immediately
        targetPosition = transform.position;
        // Ensure AI is inactive at start
        isActive = false;

        if (indicatorObject != null)
        {
            initialIndicatorScale = indicatorObject.transform.localScale;
            indicatorObject.SetActive(false); // Start with indicator hidden
        }
        else
        {
            Debug.LogWarning("Indicator Object not assigned.", this);
        }
    }

    /// <summary>
    /// Activates the AI logic.
    /// </summary>
    public void Activate()
    {
        if (targetShip == null)
        {
            Debug.LogError("Target Ship is not assigned!", this);
            return; // Don't activate if no target
        }
        isActive = true;
        ResetMoveTimer();
        ResetShootTimer();
        targetPosition = transform.position; // Start movement from current spot
        Debug.Log($"{gameObject.name} AI Activated.");
    }

    /// <summary>
    /// Deactivates the AI logic.
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        Debug.Log($"{gameObject.name} AI Deactivated.");
    }

    /// <summary>
    /// Handles the logic when the ship takes a direct hit.
    /// Reduces health significantly and destroys multiple parts.
    /// </summary>
    public void OnDirectHit()
    {
        Debug.Log($"{gameObject.name} took a direct hit!");
        health -= 2f; // Reduce health by 2 on direct hit
        DamageParts(2); // Attempt to destroy up to 4 parts

        if (health <= 0f)
        {
            Debug.Log($"{gameObject.name} destroyed by direct hit.");
            Destroy(gameObject); // Destroy if health is depleted
        }
    }

    /// <summary>
    /// Handles the logic when the ship takes an indirect (near miss) hit.
    /// Reduces health slightly and destroys fewer parts.
    /// </summary>
    public void OnIndirectHit()
    {
        Debug.Log($"{gameObject.name} took an indirect hit!");
        health -= 1f; // Reduce health by 1 on indirect hit
        DamageParts(1); // Attempt to destroy up to 2 parts

        if (health <= 0f)
        {
            Debug.Log($"{gameObject.name} destroyed by indirect hit.");
            Destroy(gameObject); // Destroy if health is depleted
        }
    }

    /// <summary>
    /// Destroys a specified number of random, available damageable parts.
    /// </summary>
    /// <param name="count">The maximum number of parts to destroy.</param>
    private void DamageParts(int count)
    {
        if (damagableParts == null || damagableParts.Length == 0) return;

        // Filter out already destroyed (null) parts
        var availableParts = new System.Collections.Generic.List<GameObject>(damagableParts);
        availableParts.RemoveAll(part => part == null);

        if (availableParts.Count == 0)
        {
            Debug.Log($"{gameObject.name} has no remaining parts to damage.");
            return; // No parts left to destroy
        }

        int partsToDestroyCount = Mathf.Min(count, availableParts.Count); // Destroy up to 'count', or fewer if not enough available

        Debug.Log($"{gameObject.name} attempting to destroy {partsToDestroyCount} parts.");

        for (int i = 0; i < partsToDestroyCount; i++)
        {
            if (availableParts.Count == 0) break; // Stop if we run out of parts unexpectedly

            int randomIndex = Random.Range(0, availableParts.Count);
            GameObject partToDestroy = availableParts[randomIndex];

            if (partToDestroy != null)
            {
                Debug.Log($"{gameObject.name} destroying part: {partToDestroy.name}");
                Destroy(partToDestroy);

                // Find the original index in damagableParts and set it to null
                // This prevents trying to destroy it again later
                for (int j = 0; j < damagableParts.Length; j++)
                {
                    if (damagableParts[j] == partToDestroy)
                    {
                        damagableParts[j] = null;
                        break;
                    }
                }
                // Remove from the temporary list to avoid selecting it again in this loop
                availableParts.RemoveAt(randomIndex);
            }
            else
            {
                // If a null somehow remained in availableParts, remove it and retry the loop iteration
                availableParts.RemoveAt(randomIndex);
                i--; // Decrement i to ensure we still attempt to destroy the correct number of parts
            }
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (!isActive || targetShip == null)
        {
            return; // Do nothing if inactive or no target
        }

        // --- Look At Target ---
        // Only look at target if not currently in the shooting animation
        if (!isShooting)
        {
            transform.LookAt(targetShip);
        }

        // --- Movement Logic ---
        HandleMovement();

        // --- Shooting Logic ---
        HandleShooting();
    }

    private void HandleMovement()
    {
        moveTimer -= Time.deltaTime;

        if (moveTimer <= 0f)
        {
            bool foundClearPath = false;
            for (int i = 0; i < maxMoveRetries; i++)
            {
                // Choose a potential new strafe position
                float distance = Random.Range(minMoveDistance, maxMoveDistance);
                int directionIndex = Random.Range(0, 4); // 0: Up, 1: Down, 2: Left, 3: Right
                Vector3 moveDirection = Vector3.zero;

                switch (directionIndex)
                {
                    case 0: moveDirection = transform.up; break;
                    case 1: moveDirection = -transform.up; break;
                    case 2: moveDirection = -transform.right; break;
                    case 3: moveDirection = transform.right; break;
                }

                Vector3 potentialTargetPosition = transform.position + moveDirection * distance;
                Vector3 currentPosition = transform.position;
                Vector3 directionVector = (potentialTargetPosition - currentPosition).normalized;
                float moveDistance = Vector3.Distance(currentPosition, potentialTargetPosition);

                // Check if the path is clear
                if (!IsPathBlocked(currentPosition, directionVector, moveDistance))
                {
                    targetPosition = potentialTargetPosition;
                    foundClearPath = true;
                    break; // Exit retry loop
                }
            }

            // If no clear path found after retries, maybe just stay put or use the last attempted position
            if (!foundClearPath)
            {
                targetPosition = transform.position;
                Debug.LogWarning($"{gameObject.name} could not find clear path after {maxMoveRetries} retries.");
            }

            ResetMoveTimer();
        }

        // Move towards the target strafe position
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
    }

    private bool IsPathBlocked(Vector3 startPosition, Vector3 direction, float distance)
    {
        Bounds totalBounds = GetTotalBounds();
        if (totalBounds.size == Vector3.zero)
        {
            Debug.LogWarning($"{gameObject.name} has no child renderers to calculate bounds for BoxCast.", this);
            return false; // Assume clear if no bounds
        }

        Vector3 halfExtents = totalBounds.size / 2f;
        float minExtent = 0.05f;
        halfExtents.x = Mathf.Max(halfExtents.x, minExtent);
        halfExtents.y = Mathf.Max(halfExtents.y, minExtent);
        halfExtents.z = Mathf.Max(halfExtents.z, minExtent);

        bool hitDetected = Physics.BoxCast(totalBounds.center, halfExtents, direction, transform.rotation, distance, obstacleLayers);

        #if UNITY_EDITOR
        if (hitDetected) { Debug.DrawRay(totalBounds.center, direction * distance, Color.red, 1.0f); }
        else { Debug.DrawRay(totalBounds.center, direction * distance, Color.green, 1.0f); }
        #endif

        return hitDetected;
    }

    private Bounds GetTotalBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(transform.position, Vector3.zero); // No renderers, return zero bounds
        }

        Bounds totalBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            Collider col = renderers[i].GetComponent<Collider>();
            if (col != null && col.isTrigger)
            {
                continue;
            }
            totalBounds.Encapsulate(renderers[i].bounds);
        }
        return totalBounds;
    }

    private void HandleShooting()
    {
        // Don't countdown shoot timer if already shooting
        if (isShooting) return;

        shootTimer -= Time.deltaTime;

        if (shootTimer <= 0f)
        {
            // Start the shooting coroutine
            StartCoroutine(ShootCoroutine());
            ResetShootTimer(); // Reset timer immediately or after shot? Resetting here allows cooldown to start during charge.
        }
    }

    private IEnumerator ShootCoroutine()
    {
        if (indicatorObject == null)
        {
            Debug.LogError("Cannot shoot: Indicator Object not assigned!", this);
            yield break; // Exit if no indicator
        }
        // Add check for weapon fire origin
        if (weaponFireOrigin == null)
        {
             Debug.LogError("Cannot shoot: Weapon Fire Origin not assigned!", this);
             yield break; // Exit if no fire origin
        }
        if (isShooting) yield break; // Prevent overlapping shots

        isShooting = true;
        indicatorObject.SetActive(true); // Show indicator

        // --- Indicator Grow Phase ---
        float elapsedTime = 0f;
        Vector3 startScale = initialIndicatorScale; // Use stored initial scale
        while (elapsedTime < indicatorGrowTime)
        {
            indicatorObject.transform.localScale = Vector3.Lerp(startScale, indicatorScaleTarget, elapsedTime / indicatorGrowTime);
            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        indicatorObject.transform.localScale = indicatorScaleTarget; // Ensure target scale is reached

        // --- Fire Shot (Instant) ---
        Debug.Log($"{gameObject.name} Firing!");
        RaycastHit directHitInfo;
        Collider directHitCollider = null; // Store the collider hit by the direct raycast

        // Start the visual effect if assigned
        if (weaponVisual != null)
        {
            StartCoroutine(WeaponVisualEffect());
        }

        // Use the weaponFireOrigin's position and forward direction
        Vector3 firePosition = weaponFireOrigin.position;
        Vector3 fireDirection = weaponFireOrigin.forward;

        // 1. Perform Direct Raycast using firePosition and fireDirection
        bool directHit = Physics.Raycast(firePosition, fireDirection, out directHitInfo, shootRange, shootLayerMask);

        if (directHit)
        {
            // Direct Hit Logic
            directHitCollider = directHitInfo.collider; // Store the hit collider
            Debug.Log($"{gameObject.name} direct hit {directHitCollider.gameObject.name}");
            // Use SendMessageUpwards to ensure the function is found even if the script is on a parent object
            directHitCollider.SendMessageUpwards(hitFunctionName, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.Log($"{gameObject.name} direct shot missed.");
        }

        // 2. Always Perform SphereCastAll for Indirect Hits using firePosition and fireDirection
        // Calculate sphere cast distance based on direct hit or range
        float sphereCastDistance = directHit ? directHitInfo.distance : shootRange;
        // Offset the spherecast origin slightly forward to avoid hitting self immediately
        RaycastHit[] indirectHits = Physics.SphereCastAll(firePosition + (fireDirection * indirectHitRadius), indirectHitRadius, fireDirection, sphereCastDistance, shootLayerMask);

        if (indirectHits.Length > 0)
        {
            foreach (RaycastHit indirectHitInfo in indirectHits)
            {
                // Check if this collider was the one hit by the direct raycast
                if (indirectHitInfo.collider != directHitCollider)
                {
                    Debug.Log($"{gameObject.name} indirect hit {indirectHitInfo.collider.gameObject.name}");
                    // Send the indirect hit message (SendMessageUpwards might also be safer here, depending on structure)
                    indirectHitInfo.collider.SendMessageUpwards(indirectHitFunctionName, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
        else if (!directHit) // Only log "completely missed" if both casts missed
        {
            Debug.Log($"{gameObject.name} shot completely missed (Ray and Sphere).");
        }

        // --- Reset Indicator ---
        indicatorObject.transform.localScale = initialIndicatorScale; // Instantly shrink back
        indicatorObject.SetActive(false); // Hide indicator
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

        // --- End Shooting State ---
        isShooting = false;
        
        yield return null;
    }

    private void ResetMoveTimer()
    {
        moveTimer = Random.Range(minMoveTime, maxMoveTime);
    }

    private void ResetShootTimer()
    {
        shootTimer = Random.Range(minShootTime, maxShootTime);
    }

    [ContextMenu("Activate AI")]
    private void ActivateFromContextMenu()
    {
        // Ensure target is assigned if activating from editor
        if (targetShip == null)
        {
            Debug.LogError("Cannot activate AI from context menu: Target Ship is not assigned!", this);
            return;
        }
        Activate();
    }

    [ContextMenu("Deactivate AI")]
    private void DeactivateFromContextMenu()
    {
        Deactivate();
    }
}
