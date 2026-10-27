using UnityEngine;

/// <summary>
/// Orbit camera that follows the player from behind. Mouse rotates the camera.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float targetHeight = 1.6f;
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField] private float collisionRadius = 0.25f;

    private float yaw;
    private float pitch = 15f;

    private void Start()
    {
        yaw = target.eulerAngles.y;
        LockCursor(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(false);
        }
        if (Input.GetMouseButtonDown(0))
        {
            LockCursor(true);
        }
    }

    private void LateUpdate()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = target.position + Vector3.up * targetHeight;
        Vector3 desiredPosition = focusPoint - cameraRotation * Vector3.forward * distance;

        // Keep the camera from going through walls and floors.
        Vector3 toCamera = desiredPosition - focusPoint;
        RaycastHit hit;
        if (Physics.SphereCast(focusPoint, collisionRadius, toCamera.normalized, out hit, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            desiredPosition = focusPoint + toCamera.normalized * hit.distance;
        }

        transform.SetPositionAndRotation(desiredPosition, cameraRotation);
    }

    private void LockCursor(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }
}
