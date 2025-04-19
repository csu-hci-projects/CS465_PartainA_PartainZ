// GitHub Copilot (Gemini 2.5 Pro) used to clean up and document this script.

using UnityEngine;
using System.Collections.Generic; // Required for using List

public class ScatterAsteroids : MonoBehaviour
{
    public GameObject asteroidPrefab; // Reference to the asteroid prefab
    public int numberOfAsteroids = 50; // How many asteroids to spawn
    public Bounds spawnBounds; // The area within which asteroids can spawn
    public float minDistance = 48f; // Minimum distance between asteroids
    public float asteroidRadius = 16f; // Estimated radius for overlap checks
    public int maxSpawnAttemptsPerAsteroid = 20; // Max attempts to find a valid spot per asteroid
    public LayerMask spawnOverlapLayerMask = -1; // Layers to check for overlap (-1 means everything)

    private List<Vector3> spawnedPositions = new List<Vector3>(); // Keep track of where asteroids are

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (asteroidPrefab == null)
        {
            Debug.LogError("Asteroid Prefab not assigned in ScatterAsteroids script.");
            return;
        }

        SpawnAsteroids();
    }

    void SpawnAsteroids()
    {
        if (asteroidPrefab == null) // Ensure prefab is valid before proceeding
        {
            Debug.LogError("Asteroid Prefab is not assigned.");
            return;
        }

        for (int i = 0; i < numberOfAsteroids; i++)
        {
            int attempts = 0;
            bool positionFound = false;
            while (attempts < maxSpawnAttemptsPerAsteroid && !positionFound)
            {
                // Generate a random position within the bounds
                float randomX = Random.Range(spawnBounds.min.x, spawnBounds.max.x);
                float randomY = Random.Range(spawnBounds.min.y, spawnBounds.max.y);
                float randomZ = Random.Range(spawnBounds.min.z, spawnBounds.max.z);
                Vector3 potentialPosition = new Vector3(randomX, randomY, randomZ);

                // 1. Check for overlap with existing objects using Physics.CheckSphere
                bool overlaps = Physics.CheckSphere(potentialPosition, asteroidRadius, spawnOverlapLayerMask, QueryTriggerInteraction.Ignore);

                bool validPosition = !overlaps; // Position is valid if it doesn't overlap

                // 2. If no overlap, check minimum distance from other *spawned* asteroids
                if (validPosition)
                {
                    foreach (Vector3 existingPosition in spawnedPositions)
                    {
                        if (Vector3.Distance(potentialPosition, existingPosition) < minDistance)
                        {
                            validPosition = false;
                            break; // Too close to another spawned asteroid
                        }
                    }
                }

                // 3. If still valid, spawn the asteroid
                if (validPosition)
                {
                    // Instantiate the asteroid at the valid position
                    Instantiate(asteroidPrefab, potentialPosition, Random.rotation, transform); // Parent to this object for organization
                    spawnedPositions.Add(potentialPosition); // Add to our list
                    positionFound = true;
                }

                attempts++;
            }

            if (!positionFound)
            {
                Debug.LogWarning($"Could not find a valid position for asteroid {i + 1} after {maxSpawnAttemptsPerAsteroid} attempts. Check overlap layers, bounds, number/minDistance, radius, or max attempts.");
            }
        }
    }

    // Optional: Visualize the spawn bounds in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnBounds.center, spawnBounds.size);
    }
}
