using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the game scene (building, elevator, items, player, camera and HUD).
/// Menu: Tools > Build Package Delivery Scene.
/// </summary>
public static class SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string ItemPrefabPath = "Assets/Prefabs/DeliveryItem.prefab";
    private const float BuildingLength = 16f;
    private const float BuildingWidth = 12f;
    private const float SlabThickness = 0.3f;
    private const float WallThickness = 0.3f;
    private const float DoorWidth = 3.2f;
    private const float DoorHeight = 3f;
    private const int IgnoreRaycastLayer = 2;

    [MenuItem("Tools/Build Package Delivery Scene")]
    public static void BuildScene()
    {
        // Create the scene first: opening a new scene unloads unused assets,
        // which would destroy references to assets created before it.
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Material greenMaterial = CreateMaterial("ItemGreen", new Color(0.1f, 0.85f, 0.2f));
        Material redMaterial = CreateMaterial("ItemRed", new Color(0.9f, 0.1f, 0.1f));
        Material groundMaterial = CreateMaterial("Ground", new Color(0.35f, 0.4f, 0.35f));
        Material wallMaterial = CreateMaterial("Wall", new Color(0.85f, 0.82f, 0.75f));
        Material elevatorMaterial = CreateMaterial("Elevator", new Color(0.25f, 0.45f, 0.85f));
        Material playerMaterial = CreateMaterial("Player", new Color(1f, 0.75f, 0.1f));
        Material[] floorMaterials =
        {
            CreateMaterial("FloorGround", new Color(0.55f, 0.55f, 0.6f)),
            CreateMaterial("FloorFirst", new Color(0.6f, 0.5f, 0.4f)),
            CreateMaterial("FloorSecond", new Color(0.45f, 0.55f, 0.65f)),
            CreateMaterial("Roof", new Color(0.4f, 0.3f, 0.3f))
        };

        DeliveryItem itemPrefab = CreateItemPrefab(greenMaterial, redMaterial);

        CreateBox("Ground", null, new Vector3(BuildingLength / 2f, -0.5f, 0f), new Vector3(80f, 1f, 80f), groundMaterial);
        CreateBuilding(wallMaterial, floorMaterials);
        ElevatorController elevator = CreateElevator(elevatorMaterial);
        CreateItems(itemPrefab);

        PlayerController player = CreatePlayer(playerMaterial);
        Camera mainCamera = Camera.main;
        ThirdPersonCamera thirdPersonCamera = mainCamera.gameObject.AddComponent<ThirdPersonCamera>();
        SetReference(thirdPersonCamera, "target", player.transform);
        SetReference(player, "cameraTransform", mainCamera.transform);
        SetReference(elevator, "player", player);

        HudController hud = CreateHud();

        GameObject managerObject = new GameObject("DeliveryManager");
        DeliveryManager deliveryManager = managerObject.AddComponent<DeliveryManager>();
        SetReference(deliveryManager, "itemPrefab", itemPrefab);
        SetReference(deliveryManager, "player", player);
        SetReference(deliveryManager, "elevator", elevator);
        SetReference(deliveryManager, "hud", hud);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Package Delivery scene built: " + ScenePath);
    }

    private static void CreateBuilding(Material wallMaterial, Material[] floorMaterials)
    {
        Transform building = new GameObject("Building").transform;
        float halfWidth = BuildingWidth / 2f;
        float wallHeight = BuildingFloors.FloorHeight - SlabThickness;

        for (int floorIndex = 0; floorIndex < BuildingFloors.FloorCount; floorIndex++)
        {
            float floorY = BuildingFloors.GetFloorSurfaceHeight(floorIndex);
            Transform floor = new GameObject(BuildingFloors.GetFloorName(floorIndex)).transform;
            floor.SetParent(building);

            // Ground floor stands on the ground, upper floors get a slab.
            Vector3 slabSize = new Vector3(BuildingLength, SlabThickness, BuildingWidth);
            if (floorIndex == 0)
            {
                CreateBox("Floor", floor, new Vector3(BuildingLength / 2f, -0.14f, 0f), slabSize, floorMaterials[0]);
            }
            else
            {
                CreateBox("Floor", floor, new Vector3(BuildingLength / 2f, floorY - SlabThickness / 2f, 0f), slabSize, floorMaterials[floorIndex]);
            }

            float wallCenterY = floorY + wallHeight / 2f;
            CreateBox("BackWall", floor, new Vector3(BuildingLength, wallCenterY, 0f),
                new Vector3(WallThickness, wallHeight, BuildingWidth), wallMaterial);
            CreateBox("LeftWall", floor, new Vector3(BuildingLength / 2f, wallCenterY, -halfWidth),
                new Vector3(BuildingLength, wallHeight, WallThickness), wallMaterial);
            CreateBox("RightWall", floor, new Vector3(BuildingLength / 2f, wallCenterY, halfWidth),
                new Vector3(BuildingLength, wallHeight, WallThickness), wallMaterial);

            // Front wall (elevator side) with a door opening.
            float sideLength = halfWidth - DoorWidth / 2f;
            float sideCenterZ = DoorWidth / 2f + sideLength / 2f;
            CreateBox("FrontWallA", floor, new Vector3(0f, wallCenterY, -sideCenterZ),
                new Vector3(WallThickness, wallHeight, sideLength), wallMaterial);
            CreateBox("FrontWallB", floor, new Vector3(0f, wallCenterY, sideCenterZ),
                new Vector3(WallThickness, wallHeight, sideLength), wallMaterial);
            float lintelHeight = wallHeight - DoorHeight;
            CreateBox("DoorTop", floor, new Vector3(0f, floorY + DoorHeight + lintelHeight / 2f, 0f),
                new Vector3(WallThickness, lintelHeight, DoorWidth), wallMaterial);
        }

        float roofY = BuildingFloors.GetFloorSurfaceHeight(BuildingFloors.FloorCount);
        CreateBox("Roof", building, new Vector3(BuildingLength / 2f, roofY - SlabThickness / 2f, 0f),
            new Vector3(BuildingLength, SlabThickness, BuildingWidth), floorMaterials[3]);
    }

    private static ElevatorController CreateElevator(Material elevatorMaterial)
    {
        GameObject elevatorObject = new GameObject("Elevator");
        elevatorObject.transform.position = new Vector3(-1.65f, 0f, 0f);
        Transform cabin = elevatorObject.transform;

        const float cabinSize = 3f;
        const float cabinHeight = 3f;
        const float panelThickness = 0.2f;
        CreateBox("CabinFloor", cabin, cabin.position + new Vector3(0f, -panelThickness / 2f, 0f),
            new Vector3(cabinSize, panelThickness, cabinSize), elevatorMaterial);
        CreateBox("CabinBackWall", cabin, cabin.position + new Vector3(-cabinSize / 2f, cabinHeight / 2f, 0f),
            new Vector3(panelThickness, cabinHeight, cabinSize), elevatorMaterial);
        CreateBox("CabinSideWallA", cabin, cabin.position + new Vector3(0f, cabinHeight / 2f, -cabinSize / 2f),
            new Vector3(cabinSize, cabinHeight, panelThickness), elevatorMaterial);
        CreateBox("CabinSideWallB", cabin, cabin.position + new Vector3(0f, cabinHeight / 2f, cabinSize / 2f),
            new Vector3(cabinSize, cabinHeight, panelThickness), elevatorMaterial);
        CreateBox("CabinRoof", cabin, cabin.position + new Vector3(0f, cabinHeight + panelThickness / 2f, 0f),
            new Vector3(cabinSize, panelThickness, cabinSize), elevatorMaterial);

        return elevatorObject.AddComponent<ElevatorController>();
    }

    private static void CreateItems(DeliveryItem itemPrefab)
    {
        Transform itemsParent = new GameObject("Items").transform;
        Vector3[][] itemPositionsPerFloor =
        {
            new[] { new Vector3(5f, 0f, -3.5f), new Vector3(10f, 0f, 3.5f), new Vector3(13.5f, 0f, -2f) },
            new[] { new Vector3(6f, 0f, 3.5f), new Vector3(12f, 0f, -3.5f) },
            new[] { new Vector3(5f, 0f, -2.5f), new Vector3(9f, 0f, 3.5f), new Vector3(13.5f, 0f, 0f) }
        };

        for (int floorIndex = 0; floorIndex < itemPositionsPerFloor.Length; floorIndex++)
        {
            foreach (Vector3 position in itemPositionsPerFloor[floorIndex])
            {
                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(itemPrefab.gameObject);
                item.transform.SetParent(itemsParent);
                item.transform.position = position + Vector3.up * (BuildingFloors.GetFloorSurfaceHeight(floorIndex) + 0.6f);
            }
        }
    }

    private static PlayerController CreatePlayer(Material playerMaterial)
    {
        GameObject playerObject = new GameObject("Player");
        playerObject.transform.position = new Vector3(8f, 0.05f, 0f);
        playerObject.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        CharacterController characterController = playerObject.AddComponent<CharacterController>();
        characterController.center = new Vector3(0f, 1f, 0f);
        characterController.height = 2f;
        characterController.radius = 0.4f;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(playerObject.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        body.GetComponent<MeshRenderer>().sharedMaterial = playerMaterial;

        // Small visor so the facing direction is visible.
        GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.name = "Visor";
        Object.DestroyImmediate(visor.GetComponent<Collider>());
        visor.transform.SetParent(playerObject.transform, false);
        visor.transform.localPosition = new Vector3(0f, 1.5f, 0.35f);
        visor.transform.localScale = new Vector3(0.5f, 0.15f, 0.15f);

        // The player is on Ignore Raycast so the elevator / camera rays never hit it.
        foreach (Transform child in playerObject.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = IgnoreRaycastLayer;
        }

        return playerObject.AddComponent<PlayerController>();
    }

    private static HudController CreateHud()
    {
        GameObject canvasObject = new GameObject("HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        Vector2 topLeft = new Vector2(0f, 1f);
        Text amountText = CreateText("AmountText", canvasObject.transform, topLeft, new Vector2(30f, -25f), new Vector2(700f, 60f), 44, new Color(1f, 0.9f, 0.2f), TextAnchor.UpperLeft);
        Text floorText = CreateText("FloorText", canvasObject.transform, topLeft, new Vector2(30f, -90f), new Vector2(1100f, 40f), 28, Color.white, TextAnchor.UpperLeft);
        Text readyText = CreateText("ReadyText", canvasObject.transform, topLeft, new Vector2(30f, -135f), new Vector2(1300f, 80f), 28, new Color(1f, 0.5f, 0.5f), TextAnchor.UpperLeft);
        Text deliveryText = CreateText("DeliveryText", canvasObject.transform, topLeft, new Vector2(30f, -225f), new Vector2(1300f, 40f), 30, new Color(0.5f, 1f, 0.6f), TextAnchor.UpperLeft);
        Text messageText = CreateText("MessageText", canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(1600f, 60f), 34, Color.white, TextAnchor.MiddleCenter);
        Text controlsText = CreateText("ControlsText", canvasObject.transform, new Vector2(0f, 0f), new Vector2(30f, 40f), new Vector2(1800f, 40f), 22, new Color(0.85f, 0.85f, 0.85f), TextAnchor.LowerLeft);
        controlsText.text = "Move: WASD / Arrows   Run: Shift   Jump: Space   Look: Mouse   R: ready items   D: deliver   Elevator: G / 1 / 2   Esc: free cursor";

        HudController hud = canvasObject.AddComponent<HudController>();
        SetReference(hud, "amountText", amountText);
        SetReference(hud, "floorText", floorText);
        SetReference(hud, "readyText", readyText);
        SetReference(hud, "deliveryText", deliveryText);
        SetReference(hud, "messageText", messageText);
        return hud;
    }

    private static Text CreateText(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size,
        int fontSize, Color color, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = anchor;
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = "";

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private static DeliveryItem CreateItemPrefab(Material greenMaterial, Material redMaterial)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "DeliveryItem";
        sphere.transform.localScale = Vector3.one * 0.8f;
        sphere.GetComponent<SphereCollider>().isTrigger = true;
        sphere.GetComponent<MeshRenderer>().sharedMaterial = greenMaterial;
        Rigidbody itemRigidbody = sphere.AddComponent<Rigidbody>();
        itemRigidbody.isKinematic = true;
        itemRigidbody.useGravity = false;

        DeliveryItem item = sphere.AddComponent<DeliveryItem>();
        SetReference(item, "greenMaterial", greenMaterial);
        SetReference(item, "redMaterial", redMaterial);

        PrefabUtility.SaveAsPrefabAsset(sphere, ItemPrefabPath);
        Object.DestroyImmediate(sphere);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath).GetComponent<DeliveryItem>();
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, true);
        box.transform.position = position;
        box.transform.localScale = size;
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
        return box;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void SetReference(Object target, string fieldName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(fieldName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
