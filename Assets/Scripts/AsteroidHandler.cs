// GitHub Copilot (Gemini 2.5 Pro) used to clean up and document this script.

using UnityEngine;

public class AsteroidHandler : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 4f;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(transform.up, rotationSpeed * Time.deltaTime);
    }

    public void OnDirectHit()
    {
        Destroy(gameObject);
    }

    public void OnIndirectHit()
    {
        // Define split parameters (Consider making these SerializedFields for tuning)
        int numSplits = 3;
        float splitScaleFactor = 0.4f;
        float splitSpawnRange = 16f;
        float minScaleMagnitude = 0.2f; // Minimum scale before destroying instead of splitting

        Vector3 currentPosition = transform.position;
        Vector3 currentScale = transform.localScale;
        Vector3 smallerScale = currentScale * splitScaleFactor;

        // If the resulting scale is too small, just destroy the asteroid
        if (smallerScale.magnitude < minScaleMagnitude)
        {
            Destroy(gameObject);
            return; // Exit the method early
        }

        // Spawn the specified number of smaller asteroids
        for (int i = 0; i < numSplits; i++)
        {
            // Calculate a random position offset within the defined range
            Vector3 spawnOffset = Random.insideUnitSphere * splitSpawnRange;
            Vector3 spawnPosition = currentPosition + spawnOffset;

            // Instantiate a new asteroid (a copy of this one) at the calculated position
            // Use Random.rotation for varied initial orientation
            GameObject splitAsteroid = Instantiate(gameObject, spawnPosition, Random.rotation);

            // Set the scale of the newly created asteroid
            splitAsteroid.transform.localScale = smallerScale;  
        }

        // Destroy the original asteroid gameObject after splitting
        Destroy(gameObject);
    }

    private void OnColliderEnter(Collision collision)
    {
        // Check if the collision is with a projectile or another object
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.SendMessage("OnIndirectHit", SendMessageOptions.DontRequireReceiver);
            OnDirectHit();
        }
    }
}
