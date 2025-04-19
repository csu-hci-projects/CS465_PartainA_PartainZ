using UnityEngine;

public class RadarViewAttacher : MonoBehaviour
{
    void Start()
    {
        var spaceship = GameObject.FindWithTag("Spaceship");
        var constraint = GetComponent<UnityEngine.Animations.PositionConstraint>();

        constraint.AddSource(new UnityEngine.Animations.ConstraintSource
        {
            sourceTransform = spaceship.transform,
            weight = 1f
        });
    }
}
