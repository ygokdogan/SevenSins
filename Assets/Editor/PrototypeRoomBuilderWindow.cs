#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class PrototypeRoomBuilderWindow : EditorWindow
{
    private const string KitPath = "Assets/Thirdparty/Kenney/Prototype Kit/FBX format/";
    private const string RootName = "Prototype 3D Tilemap";

    private enum BrushMode { Floor, Wall, Prop, Erase }

    [SerializeField] private BrushMode brushMode;
    [SerializeField] private GameObject floorPiece;
    [SerializeField] private GameObject wallPiece;
    [SerializeField] private GameObject cornerPiece;
    [SerializeField] private GameObject junctionPiece;
    [SerializeField] private GameObject propPiece;
    [SerializeField] private Transform parent;
    [SerializeField] private float gridSize = 2.5f;
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private int level;
    [SerializeField] private bool fitToGrid = true;
    [SerializeField] private bool addColliders = true;
    [SerializeField] private bool markStatic = true;
    [SerializeField] private bool paintingEnabled = true;
    [SerializeField] private bool fitPropToCell = true;
    [SerializeField] private float placementRotation;
    [SerializeField] private float rotationStep = 90f;
    [SerializeField] private float cornerRotationOffset;
    [SerializeField] private Vector2 scroll;

    private Vector2Int hoveredCell;
    private Vector2Int lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);
    private bool hasHoveredCell;
    private int strokeUndoGroup = -1;

    [MenuItem("Tools/Western Samurai/Prototype 3D Tilemap")]
    private static void Open()
    {
        PrototypeRoomBuilderWindow window = GetWindow<PrototypeRoomBuilderWindow>();
        window.titleContent = new GUIContent("3D Tilemap");
        window.minSize = new Vector2(360f, 500f);
        window.Show();
    }

    // Keep the old menu path working for convenience.
    [MenuItem("Tools/Western Samurai/Prototype Room Builder")]
    private static void OpenLegacy() => Open();

    private void OnEnable()
    {
        LoadDefaults();
        SceneView.duringSceneGui += DuringSceneGui;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGui;
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Prototype 3D Tilemap", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Scene View'da sol tıkla boya, basılı tutup sürükle. Shift + sol tık siler. Q/E seçili parçanın yerleştirme yönünü değiştirir.",
            MessageType.Info);

        paintingEnabled = EditorGUILayout.ToggleLeft("Scene Painting Enabled", paintingEnabled, EditorStyles.boldLabel);
        brushMode = (BrushMode)GUILayout.Toolbar((int)brushMode, new[] { "Floor", "Wall", "Prop", "Erase" });

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
        parent = (Transform)EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true);
        gridSize = Mathf.Max(0.1f, EditorGUILayout.FloatField("Cell Size", gridSize));
        wallHeight = Mathf.Max(0.1f, EditorGUILayout.FloatField("Wall Height", wallHeight));
        level = EditorGUILayout.IntField("Height Level", level);
        EditorGUILayout.LabelField("Paint Height", $"{level * gridSize:0.##}");

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Kenney FBX Brushes", EditorStyles.boldLabel);
        floorPiece = (GameObject)EditorGUILayout.ObjectField("Floor", floorPiece, typeof(GameObject), false);
        wallPiece = (GameObject)EditorGUILayout.ObjectField("Straight Wall", wallPiece, typeof(GameObject), false);
        cornerPiece = (GameObject)EditorGUILayout.ObjectField("Corner", cornerPiece, typeof(GameObject), false);
        junctionPiece = (GameObject)EditorGUILayout.ObjectField("Junction", junctionPiece, typeof(GameObject), false);
        propPiece = (GameObject)EditorGUILayout.ObjectField("Prop / Stairs", propPiece, typeof(GameObject), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Load Kenney Defaults")) LoadDefaults(true);
            if (GUILayout.Button("Detect Cell Size")) DetectCellSize();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
        fitToGrid = EditorGUILayout.Toggle("Fit Models To Grid", fitToGrid);
        fitPropToCell = EditorGUILayout.Toggle("Fit Prop To Cell", fitPropToCell);
        addColliders = EditorGUILayout.Toggle("Add Box Colliders", addColliders);
        markStatic = EditorGUILayout.Toggle("Mark Static", markStatic);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
        placementRotation = EditorGUILayout.FloatField("Placement Y Rotation", placementRotation);
        rotationStep = Mathf.Max(1f, EditorGUILayout.FloatField("Q / E Step", rotationStep));
        cornerRotationOffset = EditorGUILayout.FloatField("Corner Rotation Offset", cornerRotationOffset);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button($"Q  -{rotationStep:0}°")) RotatePlacement(-rotationStep);
            if (GUILayout.Button($"E  +{rotationStep:0}°")) RotatePlacement(rotationStep);
        }

        if (GUILayout.Button("Rebuild Wall Connections")) RebuildAllWalls();

        EditorGUILayout.Space(10f);
        if (GUILayout.Button("Select Tilemap Root"))
        {
            Transform root = FindRoot(false);
            if (root != null) Selection.activeGameObject = root.gameObject;
        }

        GUI.backgroundColor = new Color(1f, 0.55f, 0.45f);
        if (GUILayout.Button("Clear Entire 3D Tilemap"))
        {
            Transform root = FindRoot(false);
            if (root != null && EditorUtility.DisplayDialog("Clear 3D Tilemap", "Bütün çizilmiş tile'lar silinsin mi?", "Clear", "Cancel"))
                Undo.DestroyObjectImmediate(root.gameObject);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "Wall brush komşu hücreleri otomatik okur: düz devamda wall, dönüşte corner, 3-4 bağlantıda junction kullanır.",
            MessageType.None);
        EditorGUILayout.EndScrollView();

        if (GUI.changed) SceneView.RepaintAll();
    }

    private void DuringSceneGui(SceneView sceneView)
    {
        if (!paintingEnabled || EditorApplication.isPlayingOrWillChangePlaymode) return;

        Event current = Event.current;
        float planeHeight = level * gridSize;
        Plane plane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));
        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
        hasHoveredCell = plane.Raycast(ray, out float distance);
        if (!hasHoveredCell) return;

        Vector3 worldPoint = ray.GetPoint(distance);
        hoveredCell = WorldToCell(worldPoint);
        Vector3 cellCenter = CellToWorld(hoveredCell);

        DrawGrid(cellCenter, planeHeight);
        DrawBrushPreview(cellCenter);

        if (current.type == EventType.KeyDown && (current.keyCode == KeyCode.Q || current.keyCode == KeyCode.E))
        {
            RotatePlacement(current.keyCode == KeyCode.E ? rotationStep : -rotationStep);
            current.Use();
            Repaint();
        }

        bool wantsErase = brushMode == BrushMode.Erase || current.shift;
        if ((current.type == EventType.MouseDown || current.type == EventType.MouseDrag) &&
            current.button == 0 && !current.alt && !current.control && !current.command)
        {
            if (current.type == EventType.MouseDown)
            {
                strokeUndoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(wantsErase ? "Erase 3D Tiles" : "Paint 3D Tiles");
                lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);
            }

            if (hoveredCell != lastPaintedCell)
            {
                if (wantsErase) EraseCell(hoveredCell);
                else if (brushMode == BrushMode.Floor) PaintFloor(hoveredCell);
                else if (brushMode == BrushMode.Wall) PaintWall(hoveredCell);
                else PaintProp(hoveredCell);
                lastPaintedCell = hoveredCell;
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            current.Use();
        }

        if (current.type == EventType.MouseUp && current.button == 0)
        {
            if (strokeUndoGroup >= 0) Undo.CollapseUndoOperations(strokeUndoGroup);
            strokeUndoGroup = -1;
            lastPaintedCell = new Vector2Int(int.MinValue, int.MinValue);
        }

        if (current.type == EventType.Layout)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }

    private void DrawGrid(Vector3 center, float height)
    {
        const int radius = 8;
        Vector2Int origin = WorldToCell(center);
        Handles.color = new Color(0.35f, 0.75f, 1f, 0.18f);
        for (int i = -radius; i <= radius + 1; i++)
        {
            float x = (origin.x + i - 0.5f) * gridSize;
            float zMin = (origin.y - radius - 0.5f) * gridSize;
            float zMax = (origin.y + radius + 0.5f) * gridSize;
            Handles.DrawLine(new Vector3(x, height, zMin), new Vector3(x, height, zMax));

            float z = (origin.y + i - 0.5f) * gridSize;
            float xMin = (origin.x - radius - 0.5f) * gridSize;
            float xMax = (origin.x + radius + 0.5f) * gridSize;
            Handles.DrawLine(new Vector3(xMin, height, z), new Vector3(xMax, height, z));
        }
    }

    private void DrawBrushPreview(Vector3 center)
    {
        Color color = brushMode == BrushMode.Erase || Event.current.shift
            ? new Color(1f, 0.2f, 0.15f, 0.35f)
            : new Color(0.15f, 1f, 0.45f, 0.32f);
        Handles.color = color;
        if (brushMode == BrushMode.Wall && !Event.current.shift)
            Handles.DrawSolidRectangleWithOutline(new[]
            {
                center + new Vector3(-gridSize * 0.5f, 0.02f, -0.06f),
                center + new Vector3(-gridSize * 0.5f, 0.02f, 0.06f),
                center + new Vector3(gridSize * 0.5f, 0.02f, 0.06f),
                center + new Vector3(gridSize * 0.5f, 0.02f, -0.06f)
            }, color, Color.white);
        else
            Handles.DrawSolidRectangleWithOutline(new[]
            {
                center + new Vector3(-gridSize * 0.5f, 0.02f, -gridSize * 0.5f),
                center + new Vector3(-gridSize * 0.5f, 0.02f, gridSize * 0.5f),
                center + new Vector3(gridSize * 0.5f, 0.02f, gridSize * 0.5f),
                center + new Vector3(gridSize * 0.5f, 0.02f, -gridSize * 0.5f)
            }, color, Color.white);
    }

    private void PaintFloor(Vector2Int cell)
    {
        if (floorPiece == null) return;
        Transform floors = GetLayer("Floors", true);
        string tileName = Key("Floor", cell);
        if (floors.Find(tileName) != null) return;

        GameObject container = CreateContainer(tileName, CellToWorld(cell), floors);
        SpawnVisual(floorPiece, container.transform, PieceKind.Floor, Quaternion.Euler(0f, placementRotation, 0f));
    }

    private void PaintProp(Vector2Int cell)
    {
        if (propPiece == null) return;
        Transform props = GetLayer("Props", true);
        string tileName = Key("Prop", cell);
        Transform existing = props.Find(tileName);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject container = CreateContainer(tileName, CellToWorld(cell), props);
        SpawnVisual(propPiece, container.transform, PieceKind.Prop, Quaternion.Euler(0f, placementRotation, 0f));
    }

    private void PaintWall(Vector2Int cell)
    {
        if (wallPiece == null) return;
        Transform walls = GetLayer("Walls", true);
        string tileName = Key("Wall", cell);
        if (walls.Find(tileName) == null) CreateContainer(tileName, CellToWorld(cell), walls);
        RefreshWallAndNeighbours(cell);
    }

    private void EraseCell(Vector2Int cell)
    {
        Transform floors = GetLayer("Floors", false);
        Transform walls = GetLayer("Walls", false);
        Transform props = GetLayer("Props", false);
        Transform floor = floors != null ? floors.Find(Key("Floor", cell)) : null;
        Transform wall = walls != null ? walls.Find(Key("Wall", cell)) : null;
        Transform prop = props != null ? props.Find(Key("Prop", cell)) : null;
        if (floor != null) Undo.DestroyObjectImmediate(floor.gameObject);
        if (prop != null) Undo.DestroyObjectImmediate(prop.gameObject);
        if (wall != null)
        {
            Undo.DestroyObjectImmediate(wall.gameObject);
            RefreshWallAndNeighbours(cell);
        }
    }

    private void RefreshWallAndNeighbours(Vector2Int cell)
    {
        RefreshWall(cell);
        RefreshWall(cell + Vector2Int.right);
        RefreshWall(cell + Vector2Int.left);
        RefreshWall(cell + Vector2Int.up);
        RefreshWall(cell + Vector2Int.down);
    }

    private void RefreshWall(Vector2Int cell)
    {
        Transform walls = GetLayer("Walls", false);
        if (walls == null) return;
        Transform container = walls.Find(Key("Wall", cell));
        if (container == null) return;

        while (container.childCount > 0) Undo.DestroyObjectImmediate(container.GetChild(0).gameObject);

        bool east = HasWall(walls, cell + Vector2Int.right);
        bool west = HasWall(walls, cell + Vector2Int.left);
        bool north = HasWall(walls, cell + Vector2Int.up);
        bool south = HasWall(walls, cell + Vector2Int.down);
        int connections = Convert.ToInt32(east) + Convert.ToInt32(west) + Convert.ToInt32(north) + Convert.ToInt32(south);

        GameObject asset = wallPiece;
        PieceKind kind = PieceKind.Wall;
        Quaternion rotation = Quaternion.identity;

        if (connections == 0)
            rotation = Quaternion.Euler(0f, placementRotation, 0f);

        if (connections >= 3 && junctionPiece != null)
        {
            asset = junctionPiece;
            kind = PieceKind.Junction;
        }
        else if (connections == 2 && east != west && north != south && cornerPiece != null)
        {
            asset = cornerPiece;
            kind = PieceKind.Corner;
            float cornerAngle;
            if (north && east) cornerAngle = 0f;
            else if (east && south) cornerAngle = 90f;
            else if (south && west) cornerAngle = 180f;
            else cornerAngle = 270f;
            rotation = Quaternion.Euler(0f, cornerAngle + cornerRotationOffset, 0f);
        }
        else if (north || south)
        {
            rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        SpawnVisual(asset, container, kind, rotation);
    }

    private bool HasWall(Transform walls, Vector2Int cell) => walls.Find(Key("Wall", cell)) != null;

    private void RotatePlacement(float delta)
    {
        placementRotation = Mathf.Repeat(placementRotation + delta, 360f);
        Repaint();
        SceneView.RepaintAll();
    }

    private void RebuildAllWalls()
    {
        Transform walls = GetLayer("Walls", false);
        if (walls == null) return;

        int previousLevel = level;
        var wallCells = new List<(int level, Vector2Int cell)>();
        foreach (Transform wall in walls)
        {
            if (TryParseWallKey(wall.name, out int wallLevel, out Vector2Int cell))
                wallCells.Add((wallLevel, cell));
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Rebuild Wall Connections");
        foreach ((int wallLevel, Vector2Int cell) in wallCells)
        {
            level = wallLevel;
            RefreshWall(cell);
        }
        level = previousLevel;
        Undo.CollapseUndoOperations(undoGroup);
        SceneView.RepaintAll();
    }

    private static bool TryParseWallKey(string objectName, out int wallLevel, out Vector2Int cell)
    {
        wallLevel = 0;
        cell = default;
        string[] parts = objectName.Split('_');
        if (parts.Length != 4 || parts[0] != "Wall" ||
            !int.TryParse(parts[1], out wallLevel) ||
            !int.TryParse(parts[2], out int x) ||
            !int.TryParse(parts[3], out int y))
            return false;

        cell = new Vector2Int(x, y);
        return true;
    }

    private enum PieceKind { Floor, Wall, Corner, Junction, Prop }

    private void SpawnVisual(GameObject asset, Transform container, PieceKind kind, Quaternion rotation)
    {
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        Undo.RegisterCreatedObjectUndo(visual, "Paint 3D Tile");
        visual.name = asset.name;
        visual.transform.SetParent(container, false);
        visual.transform.SetLocalPositionAndRotation(Vector3.zero, rotation);

        if (fitToGrid)
        {
            FitVisual(visual, kind);
            CenterVisualOnCell(visual, container);
        }
        if (addColliders && visual.GetComponentInChildren<Collider>() == null) AddBoundsCollider(visual);
        if (markStatic)
            GameObjectUtility.SetStaticEditorFlags(visual,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
    }

    private void FitVisual(GameObject visual, PieceKind kind)
    {
        Bounds bounds = CalculateLocalBounds(visual);
        if (bounds.size.sqrMagnitude < 0.0001f) return;
        Vector3 scale = visual.transform.localScale;
        if (kind == PieceKind.Floor)
        {
            scale.x *= gridSize / Mathf.Max(0.001f, bounds.size.x);
            scale.z *= gridSize / Mathf.Max(0.001f, bounds.size.z);
        }
        else if (kind == PieceKind.Prop)
        {
            if (fitPropToCell)
            {
                float uniform = Mathf.Min(gridSize / Mathf.Max(0.001f, bounds.size.x),
                    gridSize / Mathf.Max(0.001f, bounds.size.z));
                scale *= uniform;
            }
        }
        else
        {
            scale.y *= wallHeight / Mathf.Max(0.001f, bounds.size.y);
            if (kind == PieceKind.Wall)
                scale.x *= gridSize / Mathf.Max(0.001f, bounds.size.x);
            else
            {
                scale.x *= gridSize / Mathf.Max(0.001f, bounds.size.x);
                scale.z *= gridSize / Mathf.Max(0.001f, bounds.size.z);
            }
        }
        visual.transform.localScale = scale;
    }

    private static void CenterVisualOnCell(GameObject visual, Transform container)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) worldBounds.Encapsulate(renderers[i].bounds);
        Vector3 offset = container.position - worldBounds.center;
        offset.y = 0f;
        visual.transform.position += offset;
    }

    private GameObject CreateContainer(string name, Vector3 position, Transform layerRoot)
    {
        GameObject container = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(container, "Paint 3D Tile");
        container.transform.SetParent(layerRoot);
        container.transform.position = position;
        return container;
    }

    private Transform GetLayer(string name, bool create)
    {
        Transform root = FindRoot(create);
        if (root == null) return null;
        Transform layerRoot = root.Find(name);
        if (layerRoot == null && create)
        {
            GameObject layer = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(layer, "Create 3D Tilemap Layer");
            layer.transform.SetParent(root, false);
            layerRoot = layer.transform;
        }
        return layerRoot;
    }

    private Transform FindRoot(bool create)
    {
        Transform root = parent != null ? parent.Find(RootName) : GameObject.Find(RootName)?.transform;
        if (root == null && create)
        {
            GameObject rootObject = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(rootObject, "Create 3D Tilemap");
            if (parent != null) rootObject.transform.SetParent(parent, false);
            root = rootObject.transform;
        }
        return root;
    }

    private Vector2Int WorldToCell(Vector3 world)
    {
        return new Vector2Int(Mathf.RoundToInt(world.x / gridSize), Mathf.RoundToInt(world.z / gridSize));
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * gridSize, level * gridSize, cell.y * gridSize);
    }

    private string Key(string prefix, Vector2Int cell) => $"{prefix}_{level}_{cell.x}_{cell.y}";

    private void LoadDefaults(bool force = false)
    {
        if (force || floorPiece == null) floorPiece = Load("floor-square.fbx");
        if (force || wallPiece == null) wallPiece = Load("wall.fbx");
        if (force || cornerPiece == null) cornerPiece = Load("wall-corner.fbx");
        if (force || junctionPiece == null) junctionPiece = Load("column.fbx");
        if (force || propPiece == null) propPiece = Load("stairs.fbx");
        Repaint();
    }

    private static GameObject Load(string file) => AssetDatabase.LoadAssetAtPath<GameObject>(KitPath + file);

    private void DetectCellSize()
    {
        if (floorPiece == null) return;
        GameObject preview = Instantiate(floorPiece);
        preview.hideFlags = HideFlags.HideAndDontSave;
        preview.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        preview.transform.localScale = Vector3.one;
        Vector3 size = CalculateLocalBounds(preview).size;
        DestroyImmediate(preview);
        float detected = Mathf.Max(size.x, size.z);
        if (detected > 0.01f) gridSize = detected;
    }

    private static void AddBoundsCollider(GameObject visual)
    {
        Bounds bounds = CalculateLocalBounds(visual);
        if (bounds.size.sqrMagnitude < 0.0001f) return;
        BoxCollider collider = Undo.AddComponent<BoxCollider>(visual);
        collider.center = bounds.center;
        collider.size = bounds.size;
    }

    private static Bounds CalculateLocalBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
        Matrix4x4 toLocal = root.transform.worldToLocalMatrix;
        Bounds result = default;
        bool initialized = false;
        foreach (Renderer renderer in renderers)
        {
            Bounds world = renderer.bounds;
            for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            for (int z = 0; z <= 1; z++)
            {
                Vector3 point = new Vector3(x == 0 ? world.min.x : world.max.x,
                    y == 0 ? world.min.y : world.max.y,
                    z == 0 ? world.min.z : world.max.z);
                point = toLocal.MultiplyPoint3x4(point);
                if (!initialized)
                {
                    result = new Bounds(point, Vector3.zero);
                    initialized = true;
                }
                else result.Encapsulate(point);
            }
        }
        return result;
    }
}
#endif
