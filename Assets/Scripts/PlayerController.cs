using UnityEngine;

/// <summary>
/// Third person movement for the player capsule using a CharacterController.
/// Movement is relative to the camera direction.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float gravity = 15f;
    [SerializeField] private float turnSpeed = 12f;

    private CharacterController characterController;
    private float verticalVelocity;

    public int CurrentFloor
    {
        get { return BuildingFloors.GetFloorIndex(transform.position.y); }
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 moveDirection = Vector3.ClampMagnitude(cameraForward * verticalInput + cameraRight * horizontalInput, 1f);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float moveSpeed = isRunning ? runSpeed : walkSpeed;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (characterController.isGrounded)
        {
            verticalVelocity = -1f;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = jumpPower;
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 velocity = moveDirection * moveSpeed + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>Moves the player together with a moving platform (the elevator cabin).</summary>
    public void MoveWithPlatform(Vector3 platformDelta)
    {
        characterController.Move(platformDelta);
    }
}
