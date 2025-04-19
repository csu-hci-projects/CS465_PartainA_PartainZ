// GitHub Copilot (Claude 3.7 Sonnet, Gemini 2.5 Pro) used to expidite repetetive code writing, provide suggestions, and complete documentation for the following script.

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

/// <summary>
/// Handles user interactions with different input types (Button, Dial, Slider)
/// using either Unimodal (hover-based) or Multimodal (select-based with velocity/rotation checks) input.
/// </summary>
public class InteractionHandler : MonoBehaviour
{
    #region Input parameters
    /// <summary>
    /// Defines the interaction modality: Unimodal (hover) or Multimodal (select).
    /// </summary>
    public enum InputModality
    {
        Unimodal,
        MultiModal
    }

    [Tooltip("Select the interaction modality: Unimodal (hover) or Multimodal (select).")]
    [SerializeField]
    private InputModality inputModality = InputModality.Unimodal;

    /// <summary>
    /// Defines the type of physical input control.
    /// </summary>
    public enum InputType
    {
        Button,
        Dial,
        Slider
    }

    [Tooltip("Select the type of physical input control.")]
    [SerializeField]
    private InputType inputType = InputType.Button;

    /// <summary>
    /// Defines the control axis or action associated with the input (relevant for Sliders and Dials).
    /// </summary>
    public enum InputMode
    {
        Sideways, // Typically X-axis movement/control
        Vertical, // Typically Y-axis movement/control
        Forward,  // Typically Z-axis movement/control
        Rotation  // Typically Y-axis rotation control
    }

    [Tooltip("Select the control axis or action associated with the input.")]
    [SerializeField]
    private InputMode inputMode = InputMode.Sideways;

    [Tooltip("Reference to the SpaceshipController to send input values.")]
    [SerializeField]
    private SpaceshipController spaceshipController;

    [Tooltip("Event triggered when a button input is successfully activated.")]
    [SerializeField]
    private UnityEvent buttonEvent;

    [Tooltip("Reference to the visual representation of the input control.")]
    [SerializeField]
    private Transform inputVisual;

    private Vector3 initialPosition; // Stores the initial local position of the input visual.
    private bool isActive = false; // Tracks if an interaction is currently active.
    #endregion

    private void Start()
    {
        if (inputVisual == null)
        {
            Debug.LogError("Input Visual transform is not assigned.", this);
            enabled = false; // Disable script if visual is missing
            return;
        }
        initialPosition = inputVisual.localPosition;
    }

    #region Unimodal input

    /// <summary>
    /// Called when a hover interaction starts (Unimodal).
    /// </summary>
    public void OnInputHoverEntered(HoverEnterEventArgs args)
    {
        if (inputModality == InputModality.Unimodal)
        {
            Debug.Log("Unimodal Hover entered");
            if (isActive) return; // Prevent multiple activations
            isActive = true;
            StartCoroutine(HandleUnimodalInput(args.interactorObject.transform));
        }
    }

    /// <summary>
    /// Called when a hover interaction ends (Unimodal).
    /// </summary>
    public void OnInputHoverExited(HoverExitEventArgs args)
    {
        if (inputModality == InputModality.Unimodal && isActive)
        {
            Debug.Log("Unimodal Hover exited");
            if (inputType == InputType.Button) return;
            isActive = false;
        }
    }

    private IEnumerator HandleUnimodalInput(Transform currentHandTransform)
    {
        switch (inputType)
        {
            case InputType.Button:
                yield return StartCoroutine(HandleUnimodalButtonInput());
                break;
            case InputType.Dial:
                yield return StartCoroutine(HandleUnimodalDialInput(currentHandTransform));
                break;
            case InputType.Slider:
                yield return StartCoroutine(HandleUnimodalSliderInput(currentHandTransform));
                break;
        }
    }

    private IEnumerator HandleUnimodalButtonInput()
    {
        // Animate button press
        Vector3 originalPosition = inputVisual.localPosition;
        // Use the visual's local up vector to determine the pressed direction
        Vector3 pressedPosition = originalPosition - transform.InverseTransformDirection(inputVisual.up) * 0.02f;
        inputVisual.localPosition = pressedPosition;

        buttonEvent.Invoke(); // Trigger the assigned event

        yield return new WaitForSeconds(0.5f); // Hold visual pressed state

        // Only return to original position if still active (prevents snapping if exited early)
        inputVisual.localPosition = originalPosition;
        isActive = false;
    }

    private IEnumerator HandleUnimodalDialInput(Transform currentHandTransform)
    {
        Transform dialTransform = inputVisual.transform; // Cache transform

        while (isActive)
        {
            // Transform the hand's world up direction to the dial's parent's local space
            Vector3 handUpWorld = currentHandTransform.up;
            Vector3 handUpLocal = dialTransform.parent.InverseTransformDirection(handUpWorld);

            // Project hand's local up direction onto the plane perpendicular to the dial's local up axis (Y-axis)
            Vector3 projectedDirectionLocal = Vector3.ProjectOnPlane(handUpLocal, Vector3.up).normalized;

            if (projectedDirectionLocal.sqrMagnitude > 0.01f) // Use sqrMagnitude for efficiency
            {
                // Create a local rotation that aligns the dial's forward with the projected hand direction
                Quaternion targetLocalRotation = Quaternion.LookRotation(projectedDirectionLocal, Vector3.up);
                Vector3 targetEulerAngles = targetLocalRotation.eulerAngles;

                // Normalize the angle to the [-180, 180] range for easier clamping
                float normalizedYAngle = targetEulerAngles.y > 180 ? targetEulerAngles.y - 360 : targetEulerAngles.y;

                // Clamp the Y rotation and snap to 15-degree increments
                normalizedYAngle = Mathf.Clamp(normalizedYAngle, -90f, 90f);
                normalizedYAngle = Mathf.Round(normalizedYAngle / 15f) * 15f;

                // Apply the snapped rotation
                inputVisual.localRotation = Quaternion.Euler(0, normalizedYAngle, 0); // Only rotate around Y

                // Send the input value to the spaceship controller (scaled)
                spaceshipController.setRotationSpeed(normalizedYAngle / 2f); // Use 2f for float division
            }

            yield return null; // Wait for the next frame
        }
    }

    private IEnumerator HandleUnimodalSliderInput(Transform currentHandTransform)
    {
        float sliderRange = 0.1f; // Half the total range of motion for the slider visual
        // Define limits based on the initial position along the slider's *visual* movement axis (local X)
        float minPositionX = initialPosition.x - sliderRange;
        float maxPositionX = initialPosition.x + sliderRange;

        while (isActive)
        {
            // Get the hand position in the local space of the slider's parent
            Vector3 handPosLocal = transform.InverseTransformPoint(currentHandTransform.position);

            // Project the hand position onto the slider's visual movement axis (local X)
            float projectedPosition = handPosLocal.x;

            // Clamp the projected position within the visual range
            float clampedPositionX = Mathf.Clamp(projectedPosition, minPositionX, maxPositionX);

            // Calculate normalized value [-1, 1] based on the clamped visual position
            float normalizedValue = Mathf.InverseLerp(minPositionX, maxPositionX, clampedPositionX) * 2f - 1f;

            // Snap the normalized value to increments of 0.2 (11 steps: -1, -0.8, ..., 0.8, 1)
            normalizedValue = Mathf.Round(normalizedValue * 5f) / 5f;

            // Convert the snapped normalized value back to a position for the visual
            clampedPositionX = Mathf.Lerp(minPositionX, maxPositionX, (normalizedValue + 1f) / 2f);

            // Apply the snapped position to the slider visual (only affecting X)
            inputVisual.localPosition = new Vector3(clampedPositionX, initialPosition.y, initialPosition.z);

            // Send the snapped input value to the spaceship controller based on the configured *control* mode
            UpdateSpaceshipSpeed(normalizedValue);

            yield return null; // Wait for the next frame
        }
    }
    #endregion

    #region Multimodal input

    /// <summary>
    /// Called when a select interaction starts (Multimodal).
    /// </summary>
    public void OnInputSelectEntered(SelectEnterEventArgs args)
    {
        if (inputModality == InputModality.MultiModal)
        {
            Debug.Log("Multimodal Select entered");
            isActive = true;
            StartCoroutine(HandleMultimodalInput(args.interactorObject.transform));
        }
    }

    /// <summary>
    /// Called when a select interaction ends (Multimodal).
    /// </summary>
    public void OnInputSelectExited(SelectExitEventArgs args)
    {
        if (inputModality == InputModality.MultiModal && isActive)
        {
            Debug.Log("Multimodal Select exited");
            isActive = false;
        }
    }

    private IEnumerator HandleMultimodalInput(Transform currentHandTransform)
    {
        switch (inputType)
        {
            case InputType.Button:
                yield return StartCoroutine(HandleMultimodalButtonInput(currentHandTransform));
                break;
            case InputType.Dial:
                yield return StartCoroutine(HandleMultimodalDialInput(currentHandTransform));
                break;
            case InputType.Slider:
                yield return StartCoroutine(HandleMultimodalSliderInput(currentHandTransform));
                break;
        }
    }

    private IEnumerator HandleMultimodalButtonInput(Transform currentHandTransform)
    {
        float velocityThreshold = 1.0f; // Speed threshold for activation
        float releaseThresholdFactor = 0.25f; // Factor to determine release threshold
        bool isPressed = false; // Tracks if the button is currently considered pressed

        Vector3 previousLocalPosition = transform.InverseTransformPoint(currentHandTransform.position);
        // Button's local activation axis (assuming button moves along local -Y relative to its parent)
        Vector3 inputDownAxis = -transform.InverseTransformDirection(inputVisual.up);

        while (isActive)
        {
            Vector3 currentLocalPosition = transform.InverseTransformPoint(currentHandTransform.position);
            Vector3 localVelocity = (currentLocalPosition - previousLocalPosition) / Time.deltaTime;
            previousLocalPosition = currentLocalPosition;

            // Project velocity onto the button's activation axis
            float velocityAlongDownAxis = Vector3.Dot(localVelocity, inputDownAxis);

            // Check for press activation
            if (!isPressed && velocityAlongDownAxis > velocityThreshold)
            {
                isPressed = true;
                buttonEvent.Invoke();
                Debug.Log("Multimodal Button pressed (velocity trigger)");

                // Animate button press
                Vector3 originalPosition = inputVisual.localPosition;
                // Use the visual's local up vector to determine the pressed direction
                Vector3 pressedPosition = originalPosition + inputDownAxis * 0.02f;
                inputVisual.localPosition = pressedPosition;
                // Start coroutine to handle visual release after delay
                StartCoroutine(ReleaseButtonVisual(originalPosition, 0.5f));
            }
            // Check for release (velocity drops below a fraction of the threshold)
            else if (isPressed && velocityAlongDownAxis < velocityThreshold * releaseThresholdFactor)
            {
                isPressed = false;
            }

            yield return null;
        }
    }

    // Helper coroutine for visual button release animation
    private IEnumerator ReleaseButtonVisual(Vector3 originalPosition, float delay)
    {
        yield return new WaitForSeconds(delay);
        // Only reset visual if the interaction hasn't been stopped externally
        // and the button hasn't been re-pressed immediately.
        // A more robust solution might involve checking the 'isPressed' state here too.
        if (inputVisual.localPosition != originalPosition) // Basic check if it's still pressed
        {
            inputVisual.localPosition = originalPosition;
        }
    }

    private IEnumerator HandleMultimodalDialInput(Transform currentHandTransform)
    {
        float rotationVelocityThreshold = 1000.0f; // Degrees per second threshold
        float releaseThresholdFactor = 0.125f; // Factor for release threshold (1/8)
        bool isRotating = false; // Tracks if rotation threshold was recently met

        // Track previous rotation relative to the dial's parent transform
        Quaternion previousLocalRotation = Quaternion.Inverse(transform.rotation) * currentHandTransform.rotation;
        // Start visual angle from its current state, normalized
        float currentAngle = inputVisual.localEulerAngles.y;
        currentAngle = currentAngle > 180 ? currentAngle - 360 : currentAngle;
        float targetAngle = currentAngle; // Target angle for smooth interpolation

        // Dial's local rotation axis (assuming Y-axis)
        Vector3 dialLocalUpAxis = Vector3.up;

        while (isActive)
        {
            Quaternion currentLocalRotation = Quaternion.Inverse(transform.rotation) * currentHandTransform.rotation;
            Quaternion localDeltaRotation = currentLocalRotation * Quaternion.Inverse(previousLocalRotation);
            previousLocalRotation = currentLocalRotation;

            // Calculate angular velocity around the dial's local up axis
            localDeltaRotation.ToAngleAxis(out float angleDelta, out Vector3 axis);
            angleDelta = angleDelta > 180f ? angleDelta - 360f : angleDelta; // Normalize angle delta
            float angularVelocity = angleDelta / Time.deltaTime;
            float projectedAngularVelocity = Vector3.Dot(axis.normalized * angularVelocity, dialLocalUpAxis); // Ensure axis is normalized

            // Check if rotational velocity exceeds threshold
            if (!isRotating && Mathf.Abs(projectedAngularVelocity) > rotationVelocityThreshold)
            {
                isRotating = true;
                int direction = projectedAngularVelocity > 0 ? 1 : -1;

                // Update target angle by a 15-degree step
                float nextTargetAngle = targetAngle + direction * 15f;

                // Clamp and snap the *next* target angle
                nextTargetAngle = Mathf.Clamp(nextTargetAngle, -90f, 90f);
                nextTargetAngle = Mathf.Round(nextTargetAngle / 15f) * 15f;

                // Only update if the target actually changed
                if (Mathf.Abs(nextTargetAngle - targetAngle) > 0.1f)
                {
                    targetAngle = nextTargetAngle;
                    Debug.Log($"Multimodal Dial rotating to {targetAngle} degrees (velocity trigger)");
                    spaceshipController.setRotationSpeed(targetAngle / 2f); // Update spaceship control
                }
            }
            // Check for rotation stop
            else if (isRotating && Mathf.Abs(projectedAngularVelocity) < rotationVelocityThreshold * releaseThresholdFactor)
            {
                isRotating = false;
            }

            // Smoothly interpolate visual angle towards the target angle
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * 10f); // Adjust smoothing factor as needed
            inputVisual.localRotation = Quaternion.Euler(0, currentAngle, 0); // Apply visual rotation

            yield return null;
        }
    }

    private IEnumerator HandleMultimodalSliderInput(Transform currentHandTransform)
    {
        float velocityThreshold = 2.0f; // Units per second threshold
        float releaseThresholdFactor = 0.125f; // Factor for release threshold (1/8)
        bool isMoving = false; // Tracks if movement threshold was recently met

        Vector3 previousLocalPosition = transform.InverseTransformPoint(currentHandTransform.position);

        // Visual slider *always* moves along local X-axis relative to its parent
        Vector3 localVisualAxis = Vector3.right;
        int visualAxisIndex = 0; // 0 for X

        // Define visual movement limits based on initial X position
        float sliderRange = 0.1f; // Half the total visual range
        float centerPositionX = initialPosition.x;
        float minPositionX = centerPositionX - sliderRange;
        float maxPositionX = centerPositionX + sliderRange;

        // Initialize normalized value based on current visual position, snapped to 0.5 increments
        float currentVisualPositionX = inputVisual.localPosition[visualAxisIndex];
        float initialNormalizedValue = Mathf.InverseLerp(minPositionX, maxPositionX, currentVisualPositionX) * 2f - 1f;
        float normalizedValue = Mathf.Round(initialNormalizedValue * 2f) / 2f; // Snap to -1, -0.5, 0, 0.5, 1

        // Apply snapped initial state to visual
        currentVisualPositionX = Mathf.Lerp(minPositionX, maxPositionX, (normalizedValue + 1f) / 2f);
        Vector3 currentLocalPos = inputVisual.localPosition;
        currentLocalPos[visualAxisIndex] = currentVisualPositionX;
        inputVisual.localPosition = currentLocalPos;

        while (isActive)
        {
            Vector3 currentLocalPosition = transform.InverseTransformPoint(currentHandTransform.position);
            Vector3 localVelocity = (currentLocalPosition - previousLocalPosition) / Time.deltaTime;
            previousLocalPosition = currentLocalPosition;

            // Project velocity onto the *visual* movement axis (local X)
            float velocityAlongVisualAxis = Vector3.Dot(localVelocity, localVisualAxis);

            // Check if velocity exceeds threshold
            if (!isMoving && Mathf.Abs(velocityAlongVisualAxis) > velocityThreshold)
            {
                isMoving = true;
                int direction = velocityAlongVisualAxis > 0 ? 1 : -1;

                // Calculate the next target normalized value (step by 0.5)
                float nextNormalizedValue = normalizedValue + direction * 0.5f;

                // Clamp and snap the *next* target value
                nextNormalizedValue = Mathf.Clamp(nextNormalizedValue, -1f, 1f);
                nextNormalizedValue = Mathf.Round(nextNormalizedValue * 2f) / 2f;

                // Only update if the target actually changed
                if (Mathf.Abs(nextNormalizedValue - normalizedValue) > 0.01f) // Use tolerance for float comparison
                {
                    normalizedValue = nextNormalizedValue;
                    Debug.Log($"Multimodal Slider moving to {normalizedValue} (velocity trigger)");

                    // Update visual position based on the new snapped normalized value
                    currentVisualPositionX = Mathf.Lerp(minPositionX, maxPositionX, (normalizedValue + 1f) / 2f);
                    currentLocalPos = inputVisual.localPosition;
                    currentLocalPos[visualAxisIndex] = currentVisualPositionX;
                    inputVisual.localPosition = currentLocalPos;

                    // Update spaceship control based on the *control* mode
                    UpdateSpaceshipSpeed(normalizedValue);
                }
            }
            // Check for movement stop
            else if (isMoving && Mathf.Abs(velocityAlongVisualAxis) < velocityThreshold * releaseThresholdFactor)
            {
                isMoving = false;
            }

            yield return null;
        }
    }

    /// <summary>
    /// Updates the spaceship controller based on the current normalized value and the selected InputMode.
    /// </summary>
    /// <param name="value">The normalized input value (typically -1 to 1).</param>
    private void UpdateSpaceshipSpeed(float value)
    {
        // Scale value back up if needed (e.g., if range was -2 to 2)
        float scaledValue = value * 2.0f; // Assuming desired control range is -2 to 2

        switch (inputMode)
        {
            case InputMode.Sideways:
                spaceshipController.setSidewaysSpeed(scaledValue);
                break;
            case InputMode.Vertical:
                spaceshipController.setVerticalSpeed(scaledValue);
                break;
            case InputMode.Forward:
                spaceshipController.setForwardSpeed(scaledValue);
                break;
            // Rotation is handled directly in dial methods, but could be added here if needed
            case InputMode.Rotation:
                 // spaceshipController.setRotationSpeed(value * someScalingFactor); // Example
                 break;
        }
    }
    #endregion
}
