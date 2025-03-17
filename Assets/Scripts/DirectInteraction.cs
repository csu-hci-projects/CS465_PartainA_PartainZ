// GitHub Copilot (Claude 3.7 Sonnet) used to expidite repetetive code writing and provide suggestions for the following script.

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;

public class DirectInteraction : MonoBehaviour
{
    [SerializeField]
    private XRBaseInteractor rightHandInteractor;

    [SerializeField]
    private XRBaseInteractor leftHandInteractor;

    [SerializeField]
    private int inputType = 1; // 1 = Dial, 2 = Slider (no button as that uses native selection)
    
    [SerializeField]
    private Vector3 activationDirection = Vector3.up; // The axis to track for rotation or sliding
    
    [SerializeField]
    private float interactionRange = 0.1f; // How far the hand can be from the input to still manipulate it

    // Enum to define what this input controls
    public enum InputMode
    {
        Sideways,
        Vertical,
        Forward,
        Rotation
    }

    [SerializeField]
    private InputMode inputMode = InputMode.Rotation;

    private bool isHovering = false;
    private Transform currentHandTransform;
    private float inputValue = 0.0f;

    #region Events
    [SerializeField] private MoveInput moveInput;
    [SerializeField] private SpaceshipController spaceshipController;
    #endregion

    #region Hover Interaction
    public void OnInputHoverEntered(HoverEnterEventArgs args)
    {

        // Determine which hand is hovering over this object
        if (rightHandInteractor != null && args.interactorObject == rightHandInteractor)
        {
            currentHandTransform = rightHandInteractor.transform;
        }
        else if (leftHandInteractor != null && args.interactorObject == leftHandInteractor)
        {
            currentHandTransform = leftHandInteractor.transform;
        }
        else
        {
            return;
        }

        isHovering = true;

        // Start appropriate tracking based on input type
        if (inputType == 1) // Dial
        {
            StartCoroutine(TrackDialRotation());
        }
        else if (inputType == 2) // Slider
        {
            StartCoroutine(TrackSliderPosition());
        }
    }

    public void OnInputHoverExited(HoverExitEventArgs args)
    {
        if ((rightHandInteractor != null && args.interactorObject == rightHandInteractor) || 
            (leftHandInteractor != null && args.interactorObject == leftHandInteractor))
        {
            isHovering = false;
            currentHandTransform = null;
        }
    }
    #endregion

    #region Dial
    private IEnumerator TrackDialRotation()
    {
        float previousValue = inputValue;

        while (isHovering && currentHandTransform != null)
        {
            // Check if the hand is within the interaction range
            float distance = Vector3.Distance(transform.position, currentHandTransform.position);
            if (distance <= interactionRange)
            {
                // Convert world direction to local space
                Vector3 localActivationDir = transform.TransformDirection(activationDirection);
                
                // Project the hand's forward direction onto the plane defined by our activation direction
                Vector3 handDirection = currentHandTransform.up;
                Vector3 projectedDirection = Vector3.ProjectOnPlane(handDirection, localActivationDir);
                
                // Calculate the absolute angle around the activation axis
                float angle = Vector3.SignedAngle(transform.forward, projectedDirection, localActivationDir);
                
                // Directly set input value based on the hand rotation
                inputValue = Mathf.Clamp(angle, -180.0f, 180.0f);
                
                // Only invoke the event if the value changed
                if (!Mathf.Approximately(inputValue, previousValue))
                {
                    Debug.Log("Dial rotated to " + inputValue);
                    moveInput.rotateDialTo(inputValue);
                    notifyShipController();
                    previousValue = inputValue;
                }
            }
            
            yield return null;
        }
    }
    #endregion

    #region Slider
    private IEnumerator TrackSliderPosition()
    {
        Vector3 initialHandPosition = transform.InverseTransformPoint(currentHandTransform.position);
        float initialProjection = Vector3.Dot(initialHandPosition, activationDirection);
        float previousValue = inputValue;

        while (isHovering && currentHandTransform != null)
        {
            // Check if the hand is within the interaction range
            float distance = Vector3.Distance(transform.position, currentHandTransform.position);
            if (distance <= interactionRange)
            {
                // Get the hand position in local space
                Vector3 localHandPosition = transform.InverseTransformPoint(currentHandTransform.position);
                
                // Project the hand position onto the activation direction
                float currentProjection = Vector3.Dot(localHandPosition, activationDirection);
                
                // Calculate the movement along the activation axis
                float delta = currentProjection - initialProjection;
                
                // Update input value
                inputValue = Mathf.Clamp(inputValue + delta * 10.0f, -1.0f, 1.0f); // Adjust sensitivity as needed
                
                // Only invoke the event if the value changed
                if (inputValue != previousValue)
                {
                    Debug.Log("Slider moved to " + inputValue);
                    moveInput.moverSliderTo(inputValue);
                    notifyShipController();
                    previousValue = inputValue;
                }
                
                // Update the initial projection for the next frame
                initialProjection = currentProjection;
            }
            
            yield return null;
        }
    }
    #endregion

    #region Notify Ship Controller
    public void notifyShipController()
    {
        switch (inputMode)
        {
            case InputMode.Sideways:
                spaceshipController.setSidewaysSpeed(inputValue);
                break;
            case InputMode.Vertical:
                spaceshipController.setVerticalSpeed(inputValue);
                break;
            case InputMode.Forward:
                spaceshipController.setForwardSpeed(inputValue);
                break;
            case InputMode.Rotation:
                spaceshipController.setRotationSpeed(inputValue);
                break;
        }
    }
    #endregion
}
