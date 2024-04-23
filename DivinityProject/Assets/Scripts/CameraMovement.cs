using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Steven Bertolucci
/// 04/16/2024
/// Master controller for camera movement related actions.
/// </summary>
public class CameraMovement : MonoBehaviour
{

    /// <summary>
    /// The camera "head" component for handling zooming.
    /// </summary>
    public GameObject CameraHead;

    /// <summary>
    /// The radius of the circle the camera rotates around during a drag rotation.
    /// </summary>
    public float RotationRadius = 10.0f;

    /// <summary>
    /// The number of degrees the scrollwheel must be rotated to achieve maximum zoom.
    /// </summary>
    public float MaxZoomDegrees = 360.0f;

    /// <summary>
    /// The speed to pan the camera at.
    /// </summary>
    public float PanSpeed = 5.0f;

    /// <summary>
    /// The speed to zoom the camera at.
    /// </summary>
    public float ZoomSpeed = 5.0f;

    /// <summary>
    /// The speed to rotate the camera at.
    /// </summary>
    public float RotateSpeed = 100.0f;

    /// <summary>
    /// Curve representing the horizontal (forward/backward) zoom offset of the camera.
    /// </summary>
    public AnimationCurve CameraPathHorizontal;

    /// <summary>
    /// Curve representing the vertical zoom offset of the camera.
    /// </summary>
    public AnimationCurve CameraPathVertical;

    /// <summary>
    /// Curve representing the zoom angle of the camera.
    /// </summary>
    public AnimationCurve CameraPathAngle;

    /// <summary>
    /// At least on Windows, Unity returns scroll values in eighths of a degree. This can
    /// apparently vary by OS - since we want this to be at least somewhat portable, we'll
    /// normalize the scrolling by this factor to get the degrees, and drive the rest of the
    /// code from that.
    /// </summary>
    public int StepsToDegree = 8;

    // Pan control parameters
    private Vector3 panDirection;

    // Rotation control parameters
    private Vector3 cameraDragStart;
    private bool cameraIsDragging = false;
    private Vector3 rotationAxis;

    // Zoom control parameters
    private float targetZoomLevel = 0.0f;
    private float currentZoomLevel = 0.0f;

    /// <summary>
    /// Set up initial values, calculate starting rotation axis.
    /// </summary>
    void Start()
    {

        panDirection = Vector3.zero;
        cameraDragStart = Vector3.zero;

        // Get the forward vector of the camera and flatten it to the xz plane so we can consistently apply
        // the scaling radius without worrying about y-axis rotation.
        Vector3 cameraForward = this.transform.forward;
        cameraForward.y = 0;
        rotationAxis = Vector3.Normalize(cameraForward);

    }

    /// <summary>
    /// Apply all camera movements.
    /// </summary>
    void Update()
    {

        // Apply panning relative to the camera's current y rotation
        Vector3 rotatedPan = Quaternion.Euler(0, transform.eulerAngles.y, 0) * panDirection;
        Vector3 scaledPan = Time.deltaTime * PanSpeed * rotatedPan;

        // Apply panning (also moving rotation axis so it doesn't get off)
        transform.position += scaledPan;
        rotationAxis += scaledPan;

        // Apply rotation
        if (cameraIsDragging)
        {

            // Compute the total distance the mouse has moved since dragging started. We normalize this
            // to the screen width to ensure consistent results across different resolutions.
            Vector2 newCameraDragStart = Input.mousePosition;
            float dragDistance = (newCameraDragStart.x - cameraDragStart.x) * RotateSpeed / Screen.width;
            cameraDragStart = newCameraDragStart;

            Vector3 rotationPoint = RotationRadius * rotationAxis;
            transform.RotateAround(rotationPoint, Vector3.up, dragDistance);

        }

        // Apply zoom. We cut things off after a certain threshold, otherwise certain driving curves
        // take forever to complete
        if (Math.Abs(targetZoomLevel - currentZoomLevel) < 0.001f)
        {
            currentZoomLevel = targetZoomLevel;
        }

        // Determine how far along the zoom path we should move this frame
        float zoomStep = Time.deltaTime * ZoomSpeed * (targetZoomLevel - currentZoomLevel);
        currentZoomLevel = Mathf.Clamp(currentZoomLevel + zoomStep, 0, 1);

        // Evaluate appropriate points on driving curves and apply to position and rotation
        float headX = CameraHead.transform.localPosition.x;
        float headY = -1 * CameraPathVertical.Evaluate(currentZoomLevel);
        float headZ = CameraPathHorizontal.Evaluate(currentZoomLevel);
        float headRotation = CameraPathAngle.Evaluate(currentZoomLevel);

        Vector3 cameraHeadAngle = CameraHead.transform.localEulerAngles;
        cameraHeadAngle.x = headRotation;

        CameraHead.transform.localPosition = new Vector3(headX, headY, headZ);
        CameraHead.transform.localEulerAngles = cameraHeadAngle;

    }

    /// <summary>
    /// Callback triggered when a camera pan occurs.
    /// </summary>
    /// <param name="context">The action context</param>
    public void OnPan(InputAction.CallbackContext context)
    {

        if (context.canceled)
        {
            panDirection = Vector3.zero;
        }
        else
        {

            // Read input into a 3 vector so we can easily scale/rotate it appropriately later.
            Vector2 axis = context.ReadValue<Vector2>();
            panDirection = new Vector3(axis.x, 0, axis.y);

        }

    }

    /// <summary>
    /// Callback triggered when a drag rotation occurs.
    /// </summary>
    /// <param name="context">The action context</param>
    public void OnRotate(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            cameraIsDragging = true;
            cameraDragStart = Input.mousePosition;
        }
        else if (context.canceled)
        {
            cameraIsDragging = false;
        }
    }

    /// <summary>
    /// Callback triggered when a camera zoom occurs.
    /// </summary>
    /// <param name="context">The action context</param>
    public void OnZoom(InputAction.CallbackContext context)
    {

        // Get number of scrolled degrees normalized
        float degrees = context.ReadValue<float>() / StepsToDegree / MaxZoomDegrees;
        targetZoomLevel = Mathf.Clamp(targetZoomLevel + degrees, 0, 1);

    }

}
