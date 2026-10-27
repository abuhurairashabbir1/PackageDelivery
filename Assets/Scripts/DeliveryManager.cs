using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs the delivery game:
///  R - randomly turns green items red (ready) for 2 minutes.
///  Touching a red item collects it (the item is destroyed) and gives a destination floor and time limit.
///  D - delivers the package on the destination floor (spawns a green item at the player, -200 Rs).
///  Late delivery returns the item green to its original position (+200 Rs).
///  When the 2 minutes run out, every remaining red item turns green (+200 Rs each).
/// </summary>
public class DeliveryManager : MonoBehaviour
{
    public static DeliveryManager Instance { get; private set; }

    [SerializeField] private DeliveryItem itemPrefab;
    [SerializeField] private PlayerController player;
    [SerializeField] private ElevatorController elevator;
    [SerializeField] private HudController hud;

    [Header("Money")]
    [SerializeField] private int startingAmount = 2000;
    [SerializeField] private int amountPerDelivery = 200;

    [Header("Timers (seconds)")]
    [SerializeField] private float readyDuration = 120f;
    [SerializeField] private float baseDeliveryTime = 20f;
    [SerializeField] private float deliveryTimePerFloor = 10f;

    private readonly List<DeliveryItem> items = new List<DeliveryItem>();

    private int remainingAmount;
    private bool isReadyRoundActive;
    private float readyTimeLeft;
    private bool isGameComplete;

    // The package currently carried by the player.
    private bool isCarryingPackage;
    private Vector3 packageHomePosition;
    private int packageDestinationFloor;
    private float deliveryTimeLeft;

    private void Awake()
    {
        Instance = this;
        remainingAmount = startingAmount;
    }

    // Also set here because recompiling scripts during Play mode clears static fields,
    // and OnEnable (unlike Awake) runs again after that reload.
    private void OnEnable()
    {
        Instance = this;
    }

    private void Start()
    {
        hud.ShowMessage("Press R to make items ready for delivery.", 5f);
    }

    private void Update()
    {
        if (!isGameComplete)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                StartReadyRound();
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                TryDeliverPackage();
            }
            UpdateReadyRoundTimer();
            UpdateDeliveryTimer();
        }
        RefreshHud();
    }

    public void RegisterItem(DeliveryItem item)
    {
        if (!items.Contains(item))
        {
            items.Add(item);
        }
    }

    private void StartReadyRound()
    {
        if (isReadyRoundActive)
        {
            hud.ShowMessage("Items are already ready. Deliver them before the time runs out!", 3f);
            return;
        }

        List<DeliveryItem> greenItems = new List<DeliveryItem>();
        foreach (DeliveryItem item in items)
        {
            if (item != null && !item.IsReady)
            {
                greenItems.Add(item);
            }
        }
        if (greenItems.Count == 0)
        {
            hud.ShowMessage("No items available right now.", 3f);
            return;
        }

        // Each item has a random chance to become ready; at least one always does.
        int readyCount = 0;
        foreach (DeliveryItem item in greenItems)
        {
            if (Random.value < 0.5f)
            {
                item.SetReady(true);
                readyCount++;
            }
        }
        if (readyCount == 0)
        {
            greenItems[Random.Range(0, greenItems.Count)].SetReady(true);
        }

        isReadyRoundActive = true;
        readyTimeLeft = readyDuration;
        hud.ShowMessage("Items are ready! " + GetReadyItemsText(), 4f);
    }

    public void TryCollectItem(DeliveryItem item)
    {
        if (item == null || isGameComplete || !item.IsReady)
        {
            return;
        }
        if (isCarryingPackage)
        {
            hud.ShowMessage("You are already carrying a package. Deliver it first!", 2f);
            return;
        }

        isCarryingPackage = true;
        packageHomePosition = item.HomePosition;
        packageDestinationFloor = GetRandomOtherFloor(item.FloorIndex);
        int floorDistance = Mathf.Abs(packageDestinationFloor - item.FloorIndex);
        deliveryTimeLeft = baseDeliveryTime + deliveryTimePerFloor * floorDistance;

        items.Remove(item);
        Destroy(item.gameObject);

        hud.ShowMessage("Package collected! Deliver it to the " + BuildingFloors.GetFloorName(packageDestinationFloor)
            + " in " + Mathf.CeilToInt(deliveryTimeLeft) + " seconds.", 4f);

        if (CountReadyItems() == 0)
        {
            EndReadyRound();
        }
    }

    private void TryDeliverPackage()
    {
        // D is also the strafe-right key, so pressing it without a package does nothing.
        if (!isCarryingPackage)
        {
            return;
        }
        if (player.CurrentFloor != packageDestinationFloor)
        {
            hud.ShowMessage("Wrong floor! Deliver to the " + BuildingFloors.GetFloorName(packageDestinationFloor) + ".", 2f);
            return;
        }

        isCarryingPackage = false;
        SpawnGreenItem(player.transform.position + Vector3.up * 0.5f);
        remainingAmount -= amountPerDelivery;

        if (remainingAmount <= 0)
        {
            remainingAmount = 0;
            isGameComplete = true;
            hud.ShowMessage("All Rs " + startingAmount + " covered. You win!", 9999f);
        }
        else
        {
            hud.ShowMessage("Delivered! -" + amountPerDelivery + " Rs", 3f);
        }
    }

    private void UpdateDeliveryTimer()
    {
        if (!isCarryingPackage)
        {
            return;
        }

        deliveryTimeLeft -= Time.deltaTime;
        if (deliveryTimeLeft <= 0f)
        {
            // Failed: the item goes back to its original position in green.
            isCarryingPackage = false;
            SpawnGreenItem(packageHomePosition);
            remainingAmount += amountPerDelivery;
            hud.ShowMessage("Delivery failed! The item went back. +" + amountPerDelivery + " Rs", 3f);
        }
    }

    private void UpdateReadyRoundTimer()
    {
        if (!isReadyRoundActive)
        {
            return;
        }

        readyTimeLeft -= Time.deltaTime;
        if (readyTimeLeft <= 0f)
        {
            int expiredCount = 0;
            foreach (DeliveryItem item in items)
            {
                if (item != null && item.IsReady)
                {
                    item.SetReady(false);
                    expiredCount++;
                }
            }
            remainingAmount += expiredCount * amountPerDelivery;
            EndReadyRound();
            hud.ShowMessage("Time up! " + expiredCount + " item(s) turned green. +" + (expiredCount * amountPerDelivery)
                + " Rs. Press R to start again.", 5f);
        }
    }

    private void EndReadyRound()
    {
        isReadyRoundActive = false;
        readyTimeLeft = 0f;
    }

    private void SpawnGreenItem(Vector3 position)
    {
        DeliveryItem newItem = Instantiate(itemPrefab, position, Quaternion.identity);
        newItem.SetReady(false);
        RegisterItem(newItem);
    }

    private int GetRandomOtherFloor(int fromFloor)
    {
        int floorIndex = Random.Range(0, BuildingFloors.FloorCount - 1);
        if (floorIndex >= fromFloor)
        {
            floorIndex++;
        }
        return floorIndex;
    }

    private int CountReadyItems()
    {
        int readyCount = 0;
        foreach (DeliveryItem item in items)
        {
            if (item != null && item.IsReady)
            {
                readyCount++;
            }
        }
        return readyCount;
    }

    private string GetReadyItemsText()
    {
        int[] readyPerFloor = new int[BuildingFloors.FloorCount];
        foreach (DeliveryItem item in items)
        {
            if (item != null && item.IsReady)
            {
                readyPerFloor[item.FloorIndex]++;
            }
        }

        List<string> floorTexts = new List<string>();
        for (int floorIndex = 0; floorIndex < readyPerFloor.Length; floorIndex++)
        {
            if (readyPerFloor[floorIndex] > 0)
            {
                floorTexts.Add(BuildingFloors.GetFloorName(floorIndex) + " (" + readyPerFloor[floorIndex] + ")");
            }
        }
        return "Items to collect on: " + string.Join(", ", floorTexts);
    }

    private void RefreshHud()
    {
        hud.SetAmountText("Remaining: Rs " + remainingAmount);
        hud.SetFloorText("You are on: " + BuildingFloors.GetFloorName(player.CurrentFloor)
            + (elevator.IsPlayerInside ? "   [In elevator: press G / 1 / 2]" : ""));

        if (isReadyRoundActive)
        {
            int secondsLeft = Mathf.CeilToInt(readyTimeLeft);
            hud.SetReadyText(GetReadyItemsText() + "\nReady time left: " + (secondsLeft / 60) + ":" + (secondsLeft % 60).ToString("00"));
        }
        else
        {
            hud.SetReadyText(isGameComplete ? "" : "No items ready. Press R to get new items.");
        }

        if (isCarryingPackage)
        {
            hud.SetDeliveryText("Carrying package -> Deliver to " + BuildingFloors.GetFloorName(packageDestinationFloor)
                + " in " + Mathf.CeilToInt(deliveryTimeLeft) + " s (press D)");
        }
        else
        {
            hud.SetDeliveryText("");
        }
    }
}
