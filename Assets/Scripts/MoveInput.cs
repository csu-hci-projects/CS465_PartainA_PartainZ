// GitHub Copilot (Claude 3.7 Sonnet) used to expidite repetetive code writing and provide suggestions for the following script.

using UnityEngine;

public class MoveInput : MonoBehaviour
{
    [SerializeField]
    private Vector3 axis = Vector3.up;

    public void pressButton()
    {
        Vector3 originalPosition = transform.localPosition;
        transform.localPosition -= axis * 0.1f;
        StartCoroutine(MoveBack());

        // Coroutine to move back after delay
        System.Collections.IEnumerator MoveBack()
        {
            yield return new WaitForSeconds(0.2f);
            transform.localPosition = originalPosition;
        }
    }

    public void rotateDial(float distance)
    {
        transform.Rotate(axis, distance);
    }

    public void rotateDialTo(float angle)
    {
        transform.localRotation = Quaternion.Euler(axis * angle);
    }

    public void moveSlider(float distance)
    {
        transform.localPosition += axis * distance * 0.1f;
    }

    public void moverSliderTo(float position)
    {
        transform.localPosition = axis * position * 0.1f;
    }
}
