using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
    #region Input parameters
    public enum InputModality
    {
        Unimodal,
        MultiModal
    }

    [SerializeField]
    private InputModality inputModality = InputModality.Unimodal;

    public enum InputType
    {
        Button,
        Dial,
        Slider
    }

    [SerializeField]
    private InputType inputType = InputType.Button;

    public enum InputMode
    {
        Sideways,
        Vertical,
        Forward,
        Rotation
    }

    [SerializeField]
    private InputMode inputMode = InputMode.Sideways;

    [SerializeField]
    private SpaceshipController spaceshipController;

    [SerializeField]
    private UnityEngine.Events.UnityEvent buttonEvent;

    [SerializeField]
    private Transform inputVisual;

    private bool isActive = false;
    #endregion

    #region Unimodal input
    public void OnInputHoverEntered(UnityEngine.XR.Interaction.Toolkit.HoverEnterEventArgs args)
    {
        Debug.Log("Hover entered");

        // Check if the input modality is unimodal
        if (inputModality == InputModality.Unimodal)
        {
            // Save the current hand transform
            Transform currentHandTransform = args.interactorObject.transform;

            isActive = true;

            // Handle different input types
            switch (inputType)
            {
                case InputType.Button:
                    Debug.Log("Button input");
                    StartCoroutine(HandleUnimodalButtonInput(currentHandTransform));
                    break;
                case InputType.Dial:
                    Debug.Log("Dial input");
                    StartCoroutine(HandleUnimodalDialInput(currentHandTransform));
                    break;
                case InputType.Slider:
                    Debug.Log("Slider input");
                    StartCoroutine(HandleUnimodalSliderInput(currentHandTransform));
                    break;
            }
        }
    }

    public void OnInputHoverExited(UnityEngine.XR.Interaction.Toolkit.HoverExitEventArgs args)
    {
        Debug.Log("Hover exited");
        isActive = false;
    }

    private System.Collections.IEnumerator HandleUnimodalButtonInput(Transform currentHandTransform)
    {
        // Handle button input

        // Move button visual
        Vector3 originalPosition = inputVisual.localPosition;
        Vector3 pressedPosition = originalPosition - Vector3.up * 0.02f;
        inputVisual.localPosition = pressedPosition;
        yield return new WaitForSeconds(0.5f);
        inputVisual.localPosition = originalPosition;

        buttonEvent.Invoke();

        yield return null;
    }

    private System.Collections.IEnumerator HandleUnimodalDialInput(Transform currentHandTransform)
    {
        // Handle dial input
        Quaternion initialRotation = inputVisual.localRotation;
        Transform dialTransform = inputVisual.transform;
        
        while (isActive)
        {
            // Transform the hand's up direction to the dial's local space
            Vector3 handUpDirectionWorld = currentHandTransform.up;
            Vector3 handUpDirectionLocal = dialTransform.parent.InverseTransformDirection(handUpDirectionWorld);
            
            // Get the dial's local up direction
            Vector3 dialUpLocal = Vector3.up;
            
            // Project hand's up direction onto the plane defined by the dial's up axis in local space
            Vector3 projectedDirectionLocal = Vector3.ProjectOnPlane(handUpDirectionLocal, dialUpLocal).normalized;
            
            if (projectedDirectionLocal.magnitude > 0.1f)
            {
                // Create a local rotation that aligns the dial's forward with the projected hand direction
                Quaternion targetLocalRotation = Quaternion.LookRotation(projectedDirectionLocal, dialUpLocal);
                
                // Convert to euler angles to apply clamping
                Vector3 targetEulerAngles = targetLocalRotation.eulerAngles;
                
                // Normalize the angle to -180 to 180 degrees range
                float normalizedYAngle = targetEulerAngles.y > 180 ? targetEulerAngles.y - 360 : targetEulerAngles.y;
                
                // Clamp the Y rotation between -90 and 90 degrees
                normalizedYAngle = Mathf.Clamp(normalizedYAngle, -90f, 90f);
                
                // Snap the angle to increments of 30 degrees
                normalizedYAngle = Mathf.Round(normalizedYAngle / 30f) * 30f;
                targetEulerAngles.y = normalizedYAngle;
                
                // Apply the clamped rotation to the dial in local space
                inputVisual.localRotation = Quaternion.Euler(targetEulerAngles);
                
                // Send the input value to the spaceship controller
                spaceshipController.setRotationSpeed(normalizedYAngle);
            }
            
            yield return null;
        }
        
        yield return null;
    }

    private System.Collections.IEnumerator HandleUnimodalSliderInput(Transform currentHandTransform)
    {
        // Handle slider input
        // Store initial position and define movement limits
        Vector3 initialPosition = inputVisual.localPosition;
        float minPosition = -0.1f; // Lower limit
        float maxPosition = 0.1f;  // Upper limit
        Vector3 localRightAxis = Vector3.right; // Use the local right axis (1,0,0)

        while (isActive)
        {
            // Get the hand position in local space of the slider's parent
            Vector3 handPosLocal = inputVisual.parent.InverseTransformPoint(currentHandTransform.position);
            
            // Project the hand position onto the local right axis
            float projectedPosition = Vector3.Dot(handPosLocal, localRightAxis);
            
            // Clamp the position within the slider's range
            float clampedPosition = Mathf.Clamp(projectedPosition, minPosition, maxPosition);
            
            // Calculate normalized value (-1 to 1) for the slider by remapping 
            float normalizedValue = (clampedPosition - minPosition) / (maxPosition - minPosition) * 2f - 1f;
            
            // Snap the normalized value to increments of 0.2 (10 distinct positions)
            normalizedValue = Mathf.Round(normalizedValue * 5) / 5;
            
            // Convert normalized value back to position
            clampedPosition = Mathf.Lerp(minPosition, maxPosition, (normalizedValue + 1f) / 2f);
            
            // Apply the snapped position to the slider visual
            Vector3 newPosition = new Vector3(clampedPosition, 0, 0);
            inputVisual.localPosition = newPosition;
            
            // Send the input value to the spaceship controller
            switch (inputMode)
            {
                case InputMode.Sideways:
                    spaceshipController.setSidewaysSpeed(normalizedValue);
                    break;
                case InputMode.Vertical:
                    spaceshipController.setVerticalSpeed(normalizedValue);
                    break;
                case InputMode.Forward:
                    spaceshipController.setForwardSpeed(normalizedValue);
                    break;
            }
            
            yield return null;
        }

        yield return null;
    }
    #endregion

    #region Multimodal input
    public void OnInputSelectEntered(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        Debug.Log("Select entered");

        // Check if the input modality is multimodal
        if (inputModality == InputModality.MultiModal)
        {
            // Save the current hand transform
            Transform currentHandTransform = args.interactorObject.transform;

            isActive = true;

            // Handle different input modes
            switch (inputType)
            {
                case InputType.Button:
                    Debug.Log("Button input");
                    StartCoroutine(HandleMultimodalButtonInput(currentHandTransform));
                    break;
                case InputType.Dial:
                    Debug.Log("Dial input");
                    StartCoroutine(HandleMultimodalDialInput(currentHandTransform));
                    break;
                case InputType.Slider:
                    Debug.Log("Slider input");
                    StartCoroutine(HandleMultimodalSliderInput(currentHandTransform));
                    break;
            }
        }
    }

    public void OnInputSelectExited(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
    {
        Debug.Log("Select exited");
        isActive = false;
    }

    private System.Collections.IEnumerator HandleMultimodalButtonInput(Transform currentHandTransform)
    {
        // Handle button input
        float velocityThreshold = 2.0f;
        bool isExceedingThreshold = false;
        Vector3 previousPosition = currentHandTransform.position;
        Vector3 inputUpAxis = inputVisual.up;

        while (isActive)
        {
            // Calculate the hand's velocity
            Vector3 velocity = (currentHandTransform.position - previousPosition) / Time.deltaTime;
            previousPosition = currentHandTransform.position;
            
            // Project velocity onto input's up axis
            float velocityAlongUpAxis = Vector3.Dot(velocity, -inputUpAxis);
            
            // Check if the projected velocity exceeds the threshold
            if (!isExceedingThreshold && velocityAlongUpAxis > velocityThreshold)
            {
                isExceedingThreshold = true;
                buttonEvent.Invoke();

                // Move button visual
                Vector3 originalPosition = inputVisual.localPosition;
                Vector3 pressedPosition = originalPosition - Vector3.up * 0.02f;
                inputVisual.localPosition = pressedPosition;
                yield return new WaitForSeconds(0.5f);
                inputVisual.localPosition = originalPosition;
                
                Debug.Log("Button pressed");
            }
            else if (isExceedingThreshold && velocityAlongUpAxis < velocityThreshold / 4.0f)
            {
                isExceedingThreshold = false;
            }

            yield return null;
        }
        
        yield return null;
    }

    private System.Collections.IEnumerator HandleMultimodalDialInput(Transform currentHandTransform)
    {
        // Handle dial input
        float rotationVelocityThreshold = 1000.0f; // degrees per second
        bool isRotating = false;
        
        // Track previous rotation
        Quaternion previousRotation = currentHandTransform.rotation;
        float currentAngle = 0f;
        float targetAngle = 0f;
        
        while (isActive)
        {
            // Calculate rotational velocity
            Quaternion deltaRotation = currentHandTransform.rotation * Quaternion.Inverse(previousRotation);
            previousRotation = currentHandTransform.rotation;
            
            // Convert to angle-axis representation
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            
            // Adjust angle (ToAngleAxis returns values in the range [0, 180])
            if (angle > 180f)
            {
                angle -= 360f;
            }
            
            // Calculate angular velocity in degrees per second
            float angularVelocity = angle / Time.deltaTime;
            
            // Project angular velocity onto the dial's rotation axis (usually up for a dial)
            float projectedAngularVelocity = Vector3.Dot(axis * angularVelocity, inputVisual.up);
            
            // Check if the rotational velocity exceeds the threshold
            if (!isRotating && Mathf.Abs(projectedAngularVelocity) > rotationVelocityThreshold)
            {
                isRotating = true;
                
                // Determine direction of rotation
                int direction = projectedAngularVelocity > 0 ? 1 : -1;
                
                // Update target angle by 30 degrees in the appropriate direction
                targetAngle += direction * 30f;
                
                // Clamp the angle between -90 and 90 degrees
                targetAngle = Mathf.Clamp(targetAngle, -90f, 90f);
                
                Debug.Log($"Dial rotating to {targetAngle} degrees");
                
                // Send the input value to the spaceship controller
                spaceshipController.setRotationSpeed(targetAngle);
            }
            else if (isRotating && Mathf.Abs(projectedAngularVelocity) < rotationVelocityThreshold / 4.0f)
            {
                isRotating = false;
            }
            
            // Smoothly interpolate current angle to target angle
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * 10f);
            
            // Apply rotation to the visual
            inputVisual.localRotation = Quaternion.Euler(0, currentAngle, 0);
            
            yield return null;
        }
        
        yield return null;
    }

    private System.Collections.IEnumerator HandleMultimodalSliderInput(Transform currentHandTransform)
    {
        // Handle slider input
        float velocityThreshold = 1.0f; // units per second
        bool isMoving = false;
        Vector3 previousPosition = currentHandTransform.position;
        
        // Define the sliding axis based on input mode
        Vector3 slidingAxis = Vector3.right; // Default to right for sideways movement
        
        // Store initial slider position and define movement limits
        Vector3 initialPosition = inputVisual.localPosition;
        float minPosition = -0.1f; // Lower limit
        float maxPosition = 0.1f;  // Upper limit
        float currentPosition = initialPosition.x;
        float normalizedValue = 0f;
        
        while (isActive)
        {
            // Calculate the hand's velocity
            Vector3 velocity = (currentHandTransform.position - previousPosition) / Time.deltaTime;
            previousPosition = currentHandTransform.position;
            
            // Project velocity onto sliding axis in world space
            Vector3 worldSlidingAxis = inputVisual.parent.TransformDirection(slidingAxis);
            float velocityAlongAxis = Vector3.Dot(velocity, worldSlidingAxis);
            
            // Check if the projected velocity exceeds the threshold
            if (!isMoving && Mathf.Abs(velocityAlongAxis) > velocityThreshold)
            {
                isMoving = true;
                
                // Determine direction of movement
                int direction = velocityAlongAxis > 0 ? 1 : -1;
                
                // Update position by an increment in the appropriate direction
                currentPosition += direction * 0.02f;
                
                // Clamp the position within the slider's range
                currentPosition = Mathf.Clamp(currentPosition, minPosition, maxPosition);
                
                // Calculate normalized value (-1 to 1) for the slider
                normalizedValue = (currentPosition - minPosition) / (maxPosition - minPosition) * 2f - 1f;
                
                // Snap the normalized value to increments of 0.2 (10 distinct positions)
                normalizedValue = Mathf.Round(normalizedValue * 5) / 5;
                
                // Convert normalized value back to position
                currentPosition = Mathf.Lerp(minPosition, maxPosition, (normalizedValue + 1f) / 2f);
                
                Debug.Log($"Slider moving to {normalizedValue}");
                
                // Apply the position to the slider visual
                inputVisual.localPosition = new Vector3(currentPosition, initialPosition.y, initialPosition.z);
                
                // Send the input value to the spaceship controller based on input mode
                switch (inputMode)
                {
                    case InputMode.Sideways:
                        spaceshipController.setSidewaysSpeed(normalizedValue);
                        break;
                    case InputMode.Vertical:
                        spaceshipController.setVerticalSpeed(normalizedValue);
                        break;
                    case InputMode.Forward:
                        spaceshipController.setForwardSpeed(normalizedValue);
                        break;
                }
            }
            else if (isMoving && Mathf.Abs(velocityAlongAxis) < velocityThreshold / 4.0f)
            {
                isMoving = false;
            }
            
            yield return null;
        }
        
        yield return null;
    }
    #endregion
}
