// GitHub Copilot (Claude 3.7 Sonnet) used to expidite repetetive code writing and provide suggestions for the following script.

using UnityEngine;


public class GestureInteraction : MonoBehaviour
{
    [SerializeField]
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor rightHandInteractor;

    [SerializeField]
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor leftHandInteractor;

    [SerializeField]
    private int inputType = 0; // 0 = Button, 1 = Dial, 2 = Slider
    
    [SerializeField]
    private Vector3 activationDirection = Vector3.down; // Relative to inputs's local space
    
    [SerializeField]
    private float velocityThreshold = 2.0f; // Min velocity to trigger button
    
    [SerializeField]
    private float interactionRage = 0.1f; // How far the hand can be from the input to still trigger it

    // [SerializeField]
    // private float inputDelay = 0.5f; // Time between input activations

    private float inputValue = 0.0f;

    private Vector3 currentHandPosition;
    private Vector3 previousHandPosition;
    private Vector3 currentHandRotation;
    private Vector3 previousHandRotation;
    private Vector3 handLateralVelocity;
    private float handRotationalVelocity;
    
    private bool isInputSelected = false;

    [SerializeField]
    private MoveInput moveInput;

    [SerializeField]
    private SpaceshipController spaceshipController;

    public enum InputMode
    {
        Sideways,
        Vertical,
        Forward,
        Rotation
    }

    [SerializeField]
    private InputMode inputMode = InputMode.Rotation;

    // Gesture interaction allows the player to select an input, then accelerate their hand (laterally or rotationally) in the direction of the desired input value.
    // This script will be called by the VR interactable object's 'select' event, and will call events on another object upon successful completion of the gesture.
    
    #region Selection
    public void OnInputSelected(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable)
    {
        // Determine which hand is selecting this object
        Transform handTransform = null;
        
        if (rightHandInteractor != null && rightHandInteractor.IsSelecting(interactable))
        {
            handTransform = rightHandInteractor.transform;
        }
        else if (leftHandInteractor != null && leftHandInteractor.IsSelecting(interactable))
        {
            handTransform = leftHandInteractor.transform;
        }
        
        if (handTransform != null)
        {
            isInputSelected = true;
            if (inputType == 0)
            {
                StartCoroutine(TrackHandLateralVelocity(handTransform));
                StartCoroutine(CheckButtonActivation());
            }
            else if (inputType == 1)
            {
                StartCoroutine(TrackHandRotationalVelocity(handTransform));
                StartCoroutine(CheckDialActivation());
            }
            else if (inputType == 2)
            {
                StartCoroutine(TrackHandLateralVelocity(handTransform));
                StartCoroutine(CheckSliderActivation());
            }
        }
        else
        {
            Debug.LogWarning("Input was selected but couldn't determine which hand selected it");
        }
    }

    public void OnInputDeselected()
    {
        isInputSelected = false;
    }
    #endregion

    #region Button
    [SerializeField] private UnityEngine.Events.UnityEvent onButtonActivated;

    private System.Collections.IEnumerator CheckButtonActivation()
    {
        bool isActivated = false;

        while (isInputSelected)
        {
            // Project velocity onto the specified direction
            Vector3 worldDirection = transform.TransformDirection(activationDirection.normalized);
            float velocityInDirection = Vector3.Dot(handLateralVelocity, worldDirection);
            
            if (!isActivated && velocityInDirection > velocityThreshold)
            {
                isActivated = true;

                // Activate button
                moveInput.pressButton();
                onButtonActivated?.Invoke();
                Debug.Log("Button activated");
                // Wait half a second before allowing another activation
                // yield return new WaitForSeconds(inputDelay);
            }
            else if (velocityInDirection < velocityThreshold / 4)
            {
                isActivated = false;
            }

            yield return null;
        }
    }
    #endregion

    #region Dial
    [SerializeField] private UnityEngine.Events.UnityEvent<float> onDialClockwise;
    [SerializeField] private UnityEngine.Events.UnityEvent<float> onDialCounterclockwise;

    private System.Collections.IEnumerator CheckDialActivation()
    {
        bool isRotating = false;

        while (isInputSelected)
        {
            // Project velocity onto the specified axis
            Vector3 worldAxis = transform.TransformDirection(activationDirection.normalized);
            float rotationSpeed = handRotationalVelocity;
            
            // Check if rotation speed exceeds threshold
            if (!isRotating && Mathf.Abs(rotationSpeed) > (velocityThreshold * 1000))
            {
                isRotating = true;

                if (rotationSpeed > 0)
                {
                    // Clockwise rotation
                    inputValue += 30;
                    Debug.Log(inputValue);
                    moveInput.rotateDial(inputValue);
                    notifyShipController();
                    Debug.Log("Dial rotated clockwise");
                }
                else
                {
                    // Counter-clockwise rotation
                    inputValue -= 30;
                    Debug.Log(inputValue);
                    moveInput.rotateDial(inputValue);
                    notifyShipController();
                    Debug.Log("Dial rotated counter-clockwise");
                }
                // yield return new WaitForSeconds(inputDelay);
            }
            else if (Mathf.Abs(rotationSpeed) < (velocityThreshold * 1000) / 4)
            {
                isRotating = false;
            }
            
            yield return null;
        }
    }
    #endregion

    #region Slider
    [SerializeField] private UnityEngine.Events.UnityEvent<float> onSliderPositive;
    [SerializeField] private UnityEngine.Events.UnityEvent<float> onSliderNegative;

    private System.Collections.IEnumerator CheckSliderActivation()
    {
        bool isSliding = false;

        while (isInputSelected)
        {
            // Project velocity onto the specified direction
            Vector3 worldDirection = transform.TransformDirection(activationDirection.normalized);
            float velocityInDirection = Vector3.Dot(handLateralVelocity, worldDirection);
            
            if (!isSliding && Mathf.Abs(velocityInDirection) > velocityThreshold)
            {
                isSliding = true;

                if (velocityInDirection > 0)
                {
                    // Positive slider movement
                    inputValue += 0.2f;
                    Debug.Log(inputValue);
                    moveInput.moveSlider(inputValue);
                    notifyShipController();
                    Debug.Log("Slider moved in positive direction");
                }
                else
                {
                    // Negative slider movement
                    inputValue -= 0.2f;
                    Debug.Log(inputValue);
                    moveInput.moveSlider(inputValue);
                    notifyShipController();
                    Debug.Log("Slider moved in negative direction");
                }
                // yield return new WaitForSeconds(inputDelay);
            }
            else if (Mathf.Abs(velocityInDirection) < velocityThreshold / 4)
            {
                isSliding = false;
            }
            
            yield return null;
        }
    }
    #endregion

    #region Velocity Tracking
    private System.Collections.IEnumerator TrackHandLateralVelocity(Transform handTransform)
    {
        previousHandPosition = handTransform.position;
        
        while (isInputSelected)
        {
            currentHandPosition = handTransform.position;

            // Check if hand is within interaction range
            float distanceToInput = Vector3.Distance(currentHandPosition, transform.position);
            
            if (distanceToInput <= interactionRage)
            {
                // Calculate velocity
                handLateralVelocity = (currentHandPosition - previousHandPosition) / Time.deltaTime;
            }
            else
            {
                handLateralVelocity = Vector3.zero;
            }
            
            previousHandPosition = currentHandPosition;
            yield return null;
        }

        handLateralVelocity = Vector3.zero;
    }

    private System.Collections.IEnumerator TrackHandRotationalVelocity(Transform handTransform)
    {
        float previousAngle = Vector3.SignedAngle(transform.forward, handTransform.up, activationDirection);
        
        while (isInputSelected)
        {
            // Check if hand is within interaction range
            float distanceToInput = Vector3.Distance(currentHandPosition, transform.position);
            
            if (distanceToInput <= interactionRage)
            {
                // Calculate velocity
                // Convert world direction to local space
                Vector3 localActivationDir = transform.TransformDirection(activationDirection);
                
                // Project the hand's forward direction onto the plane defined by our activation direction
                Vector3 handDirection = handTransform.up;
                Vector3 projectedDirection = Vector3.ProjectOnPlane(handDirection, localActivationDir);
                
                // Calculate the absolute angle around the activation axis
                float angle = Vector3.SignedAngle(transform.forward, projectedDirection, localActivationDir);

                handRotationalVelocity = (angle - previousAngle) / Time.deltaTime;

                previousAngle = angle;
            }
            else
            {
                handRotationalVelocity = 0;
            }

            yield return null;
        }

        handRotationalVelocity = 0;
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
