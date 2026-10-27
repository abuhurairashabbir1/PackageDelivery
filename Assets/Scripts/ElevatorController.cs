using UnityEngine;

/// <summary>
/// Moves the elevator cabin between the ground, 1st and 2nd floor.
/// Keys: G = ground, 1 = 1st floor, 2 = 2nd floor (only while the player is inside the cabin).
/// The player is detected inside the cabin using a RayCast only.
/// </summary>
[DefaultExecutionOrder(-50)]
public class ElevatorController : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float detectionRayLength = 2.5f;

    private const float CabinFloorOffset = 0.05f;

    private int targetFloor;
    private string lastRayHitName = "nothing";
    private int currentFloor;

    public bool IsPlayerInside { get; private set; }
    public bool IsMoving { get; private set; }
    public int CurrentFloor { get { return currentFloor; } }

    private void Start()
    {
        currentFloor = 0;
        targetFloor = 0;
        transform.position = GetFloorPosition(0);
    }

    private void Update()
    {
        IsPlayerInside = CheckPlayerInsideWithRaycast();

        if (IsPlayerInside && !IsMoving)
        {
            HandleFloorInput();
        }
        else if (!IsPlayerInside && IsFloorKeyPressed())
        {
            Debug.Log("Elevator: floor key ignored, player is not inside the cabin. Ray hit: " + lastRayHitName);
        }

        if (IsMoving)
        {
            MoveCabin();
        }
    }

    private void HandleFloorInput()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            RequestFloor(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            RequestFloor(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            RequestFloor(2);
        }
    }

    private bool IsFloorKeyPressed()
    {
        return Input.GetKeyDown(KeyCode.G)
            || Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)
            || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
    }

    private void RequestFloor(int floorIndex)
    {
        if (floorIndex == currentFloor)
        {
            return;
        }
        targetFloor = floorIndex;
        IsMoving = true;
    }

    private void MoveCabin()
    {
        Vector3 targetPosition = GetFloorPosition(targetFloor);
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        Vector3 cabinDelta = nextPosition - transform.position;
        bool isGoingUp = cabinDelta.y > 0f;

        // Going up: move the player first so the cabin floor does not push into them.
        // Going down: move the cabin first so the player has room to follow it.
        if (IsPlayerInside && isGoingUp)
        {
            player.MoveWithPlatform(cabinDelta);
        }

        transform.position = nextPosition;
        // Update the cabin colliders now, so the player and the ray see the new position this frame.
        Physics.SyncTransforms();

        if (IsPlayerInside && !isGoingUp)
        {
            player.MoveWithPlatform(cabinDelta);
        }

        if (nextPosition == targetPosition)
        {
            currentFloor = targetFloor;
            IsMoving = false;
        }
    }

    /// <summary>
    /// Casts a ray straight down from the player. If it hits the cabin floor,
    /// the player is standing inside the elevator cabin.
    /// </summary>
    private bool CheckPlayerInsideWithRaycast()
    {
        Vector3 rayOrigin = player.transform.position + Vector3.up;
        Debug.DrawRay(rayOrigin, Vector3.down * detectionRayLength, Color.yellow);
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, detectionRayLength,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            lastRayHitName = hit.transform.name;
            return hit.transform.IsChildOf(transform);
        }
        lastRayHitName = "nothing";
        return false;
    }

    private Vector3 GetFloorPosition(int floorIndex)
    {
        // The cabin floor sits slightly above the floor surface so its top is never level
        // with the ground; otherwise the detection ray could hit the ground instead of the cabin.
        Vector3 position = transform.position;
        position.y = BuildingFloors.GetFloorSurfaceHeight(floorIndex) + CabinFloorOffset;
        return position;
    }
}
