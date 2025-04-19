using UnityEngine;

public class ActivateEnemy : MonoBehaviour
{
    [SerializeField] private SimpleEnemyShipAI enemyAI;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger has the "Player" tag
        // and if the enemyAI reference is set.
        if (enemyAI != null && other.CompareTag("Player"))
        {
            Debug.Log("Player entered the trigger zone. Activating enemy AI.");
            enemyAI.Activate(); // Call the Activate method on the enemy AI script
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the object exiting the trigger has the "Player" tag
        // and if the enemyAI reference is set.
        if (enemyAI != null && other.CompareTag("Player"))
        {
            enemyAI.Deactivate(); // Call the Deactivate method on the enemy AI script
        }
    }
}
