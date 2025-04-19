using UnityEngine;

public class MissionStarter : MonoBehaviour
{
    [SerializeField] private GameObject missionEndTrigger;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger has the "Player" tag
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered the mission area. Starting mission.");
            GameObject.FindWithTag("Spaceship").GetComponent<SpaceshipController>().StartMission();
            missionEndTrigger.SetActive(true);
            Destroy(gameObject);
        }
    }    
}
