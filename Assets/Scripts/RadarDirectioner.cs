using UnityEngine;

public class RadarDirectioner : MonoBehaviour
{
    [SerializeField] private Transform target;

    void Update()
    {
        if (target == null) {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
            transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
        }
    }
}
