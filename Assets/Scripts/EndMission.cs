using UnityEngine;

public class EndMission : MonoBehaviour
{private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered the end mission area. Ending mission.");
            GameObject.FindWithTag("Spaceship").GetComponent<SpaceshipController>().EndMission(); // Call the EndMission method on the spaceship controller
        }
    }
}
