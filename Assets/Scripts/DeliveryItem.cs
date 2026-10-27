using UnityEngine;

/// <summary>
/// A deliverable item (sphere). Green = idle, Red = ready to be collected.
/// </summary>
public class DeliveryItem : MonoBehaviour
{
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material redMaterial;

    private MeshRenderer meshRenderer;

    public bool IsReady { get; private set; }
    public Vector3 HomePosition { get; private set; }
    public int FloorIndex { get; private set; }

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        HomePosition = transform.position;
        FloorIndex = BuildingFloors.GetFloorIndex(HomePosition.y);
        SetReady(false);
    }

    private void Start()
    {
        DeliveryManager.Instance.RegisterItem(this);
    }

    public void SetReady(bool isReady)
    {
        IsReady = isReady;
        meshRenderer.sharedMaterial = isReady ? redMaterial : greenMaterial;
    }

    // Stay (not Enter) so an item that turns red while the player is touching it can still be collected.
    private void OnTriggerStay(Collider other)
    {
        if (IsReady && other.GetComponent<PlayerController>() != null)
        {
            DeliveryManager.Instance.TryCollectItem(this);
        }
    }
}
