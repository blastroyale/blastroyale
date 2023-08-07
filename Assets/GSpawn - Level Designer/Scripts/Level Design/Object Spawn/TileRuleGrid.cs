#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GSpawn
{
    public class TileRuleGridRayHit
    {
        private TileRuleGrid        _hitGrid;
        private float               _hitEnter;
        private Vector3             _hitPoint;
        private Vector3             _hitNormal;
        private Vector3Int          _hitCellCoords;

        public TileRuleGrid         hitGrid             { get { return _hitGrid; } }
        public float                hitEnter            { get { return _hitEnter; } }
        public Vector3              hitPoint            { get { return _hitPoint; } }
        public Vector3              hitNormal           { get { return _hitNormal; } }
        public Vector3Int           hitCellCoords       { get { return _hitCellCoords; } }

        public TileRuleGridRayHit(Ray hitRay, TileRuleGrid hitGrid, Vector3 hitNormal, float hitEnter, Vector3Int hitCellCoords)
        {
            _hitGrid            = hitGrid;
            _hitEnter           = hitEnter;
            _hitPoint           = hitRay.GetPoint(hitEnter);
            _hitNormal          = hitNormal;
            _hitCellCoords      = hitCellCoords;
        }
    }

    public struct TileRuleGridCellRange
    {
        private Vector3Int _min;
        private Vector3Int _max;

        public Vector3Int min { get { return _min; } set { _min = value; sortCoords(); } }
        public Vector3Int max { get { return _max; } set { _max = value; sortCoords(); } }

        public TileRuleGridCellRange(Vector3Int minCoords, Vector3Int maxCoords)
        {
            _min = minCoords;
            _max = maxCoords;
            sortCoords();
        }

        private void sortCoords()
        {
            if (_min.x > _max.x) { int t = _min.x; _min.x = _max.x; _max.x = t; }
            if (_min.y > _max.y) { int t = _min.y; _min.y = _max.y; _max.y = t; }
            if (_min.z > _max.z) { int t = _min.z; _min.z = _max.z; _max.z = t; }
        }
    }

    [Serializable]
    public class TileRuleGridRampCells : SerializableHashSet<Vector3Int> { }

    [Serializable]
    public class TileRuleGridState
    {
        [SerializeField]
        public TileRuleGridRampCells rampCells = new TileRuleGridRampCells();

        public void clear()
        {
            rampCells.Clear();
        }
    }

    public class TileRuleGrid : ScriptableObject, IUIItemStateProvider
    {
        private class EditData
        {
            public HashSet<Vector3Int> addedRampCells       = new HashSet<Vector3Int>();
            public HashSet<Vector3Int> removedRampCells     = new HashSet<Vector3Int>();

            public void clear()
            {
                addedRampCells.Clear();
                removedRampCells.Clear();
            }
        }

        private enum TilePaintReason
        {
            Paint = 0,
            Erase,
            Connect,
            Refresh,
        }

        private class TilePaintParams
        {
            public Vector3Int       cellCoords          = new Vector3Int();
            public bool             paintingRamp        = false;
            public TilePaintReason  paintReason         = TilePaintReason.Paint;

            public void clear()
            {
                paintingRamp    = false;
                paintReason     = TilePaintReason.Paint;
            }
        }

        private class TileSpawnData
        {
            [NonSerialized]
            public TileRule         rule;
            [NonSerialized]
            public TileRulePrefab   rulePrefab;
            [NonSerialized]
            public Quaternion       rotation        = Quaternion.identity;
            [NonSerialized]
            public Vector3          scale           = Vector3.one;
            [NonSerialized]
            public bool             flipSpriteX     = false;
            [NonSerialized]
            public bool             flipSpriteY     = false;
            [NonSerialized]
            public bool             isRamp          = false;

            public void reset()
            {
                rule        = null;
                rulePrefab  = null;
                rotation    = Quaternion.identity;
                scale       = Vector3.one;
                flipSpriteX = false;
                flipSpriteY = false;
                isRamp      = false;
            }
        }

        static TileRuleGrid()
        {
            _neighOffsets_R1 = new Vector2Int[8];
            _neighOffsets_R2 = new Vector2Int[24];
            _neighOffsets_R3 = new Vector2Int[48];

            int arrayIndex = 0;
            for (int r = -1; r <= 1; ++r)
            {
                for(int c = -1; c <= 1; ++c)
                {
                    if (r != 0 || c != 0)
                    {
                        _neighOffsets_R1[arrayIndex++] = new Vector2Int(c, r);
                    }
                }
            }

            arrayIndex = 0;
            for (int r = -2; r <= 2; ++r)
            {
                for (int c = -2; c <= 2; ++c)
                {
                    if (r != 0 || c != 0)
                    {
                        _neighOffsets_R2[arrayIndex++] = new Vector2Int(c, r);
                    }
                }
            }

            arrayIndex = 0;
            for (int r = -3; r <= 3; ++r)
            {
                for (int c = -3; c <= 3; ++c)
                {
                    if (r != 0 || c != 0)
                    {
                        _neighOffsets_R3[arrayIndex++] = new Vector2Int(c, r);
                    }
                }
            }
        }

        private static Vector2Int[]     _neighOffsets_R1;
        private static Vector2Int[]     _neighOffsets_R2;
        private static Vector2Int[]     _neighOffsets_R3;

        private Dictionary<Vector3Int, GameObject>  _tileMap        = new Dictionary<Vector3Int, GameObject>();
        
        [SerializeField]
        private GameObject              _gameObject;
        [NonSerialized]
        private Transform               _transform;
        [SerializeField]
        private TileRuleGridSettings    _settings;
        [SerializeField]
        private PluginGuid              _guid                       = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private string                  _gridName                   = string.Empty;
        [SerializeField]
        private bool                    _uiSelected;
        [NonSerialized]
        private CopyPasteMode           _uiCopyPasteMode            = CopyPasteMode.None;
        [SerializeField]
        private bool                    _uiExpanded                 = false;
        [SerializeField]
        private TileRuleGridState       _state                      = new TileRuleGridState();
        [NonSerialized]
        private EditData                _editData                   = new EditData();
        [SerializeField]
        private bool                    _usingSprites               = false;

        [SerializeField]
        private bool                        _mirroringEnabled       = false;
        [SerializeField]
        private ObjectMirrorGizmo           _mirrorGizmo            = null;
        [SerializeField]
        private ObjectMirrorGizmoSettings   _mirrorGizmoSettings    = null;

        [NonSerialized]
        private Vector2Int[]            _neighborOffsets            = null;
        [NonSerialized]
        private TileRuleProfile         _ruleProfile;
        [NonSerialized]
        private HashSet<Vector3Int>     _occupiedCells              = new HashSet<Vector3Int>();
        [NonSerialized]
        private List<Vector3Int>        _cellsAroundVertBorder      = new List<Vector3Int>();
        [NonSerialized]
        private List<Vector3Int>        _cellsBelow                 = new List<Vector3Int>();
        [NonSerialized]
        private HashSet<Vector3Int>     _eraseBrushCells            = new HashSet<Vector3Int>();
        [NonSerialized]
        private TilePaintParams             _tilePaintParams        = new TilePaintParams();
        [NonSerialized]
        private TileSpawnData               _tileSpawnData          = new TileSpawnData();
        [NonSerialized]
        private TileRulePrefabPickParams    _prefabPickParams       = new TileRulePrefabPickParams();

        [NonSerialized]
        private List<TileRule>          _sortedStdRules             = new List<TileRule>();
        [NonSerialized]
        private List<TileRule>          _sortedPlatformRules        = new List<TileRule>();
        [NonSerialized]
        private List<TileRule>          _sortedRampRules            = new List<TileRule>();

        [NonSerialized]
        private List<GameObject>        _objectBuffer               = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>        _prefabInstanceRoots        = new List<GameObject>();
        [NonSerialized]
        private Func<GameObject, bool>  _prefabInstanceRootFilter;
        [NonSerialized]
        private SceneRaycastFilter      _pickCellCoordsFilter       = new SceneRaycastFilter();
        [NonSerialized]
        private ObjectOverlapFilter     _foreignEraseOverlapFilter  = new ObjectOverlapFilter();
        [NonSerialized]
        private List<GameObject>        _foreignObjectBuffer        = new List<GameObject>();
        [NonSerialized]
        private List<Vector3>           _shadowCorners              = new List<Vector3>();
        [NonSerialized]
        private List<Vector3Int>        _cellCoordsBuffer           = new List<Vector3Int>();
        [NonSerialized]
        private List<GameObject>        _meshObjectBuffer           = new List<GameObject>();
        [NonSerialized]
        private List<GameObject>        _objectOverlapBuffer        = new List<GameObject>();
    
        [NonSerialized]
        private Vector3[]               _tileFrame                  = new Vector3[3];

        private TileRuleObjectSpawnSettings     spawnSettings       { get { return ObjectSpawn.instance.tileRuleObjectSpawn.settings; } }

        public PluginGuid               guid                        { get { return _guid; } }
        public TileRuleGridSettings     settings                    { get { return _settings; } }
        public string                   gridName                    { get { return _gridName; } set { if (!string.IsNullOrEmpty(value)) { UndoEx.record(this); _gridName = value; } } }
        public bool                     uiSelected                  { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public CopyPasteMode            uiCopyPasteMode             { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }
        public bool                     uiExpanded                  { get { return _uiExpanded; } set { _uiExpanded = value; EditorUtility.SetDirty(this); } }

        public Vector3                  gridOrigin                  { get { return _transform.position; } set { UndoEx.record(_transform); _transform.position = value; } }
        public Vector3                  gridRight                   { get { return _transform.right; } }
        public Vector3                  gridUp                      { get { return _transform.up; } }
        public Vector3                  gridLook                    { get { return _transform.forward; } }
        public Quaternion               gridRotation                { get { return _transform.rotation; } set { UndoEx.record(_transform); _transform.rotation = value; } }
        public Plane                    gridPlane                   { get { return new Plane(gridUp, gridOrigin); } }
        public GameObject               gameObject                  { get { return _gameObject; } }
        public Transform                transform                   { get { return _transform; } }

        public bool                         mirroringEnabled        { get { return _mirroringEnabled; } set { UndoEx.record(this); _mirroringEnabled = value; EditorUtility.SetDirty(this); } }
        public ObjectMirrorGizmo            mirrorGizmo             { get { return _mirrorGizmo; } }
        public ObjectMirrorGizmoSettings    mirrorGizmoSettings     { get { return _mirrorGizmoSettings; } }
        public Vector3Int                   mirrorGizmoCellCoords   { get { return worldPointToCellCoords(_mirrorGizmo.position); } set { UndoEx.record(_mirrorGizmo); _mirrorGizmo.position = cellCoordsToCellPosition(value); SceneView.RepaintAll(); } }

        public static void getCellsAroundVerticalBorder(Vector3Int coords, List<Vector3Int> cellCoords)
        {
            cellCoords.Clear();
            cellCoords.Add(new Vector3Int(coords.x - 1, coords.y, coords.z));
            cellCoords.Add(new Vector3Int(coords.x + 1, coords.y, coords.z));
            cellCoords.Add(new Vector3Int(coords.x, coords.y, coords.z - 1));
            cellCoords.Add(new Vector3Int(coords.x, coords.y, coords.z + 1));
            cellCoords.Add(new Vector3Int(coords.x - 1, coords.y, coords.z - 1));
            cellCoords.Add(new Vector3Int(coords.x - 1, coords.y, coords.z + 1));
            cellCoords.Add(new Vector3Int(coords.x + 1, coords.y, coords.z + 1));
            cellCoords.Add(new Vector3Int(coords.x + 1, coords.y, coords.z - 1));
        }

        public static void getCellsAroundVerticalBorder(Vector3Int coords, int radius, List<Vector3Int> cellCoords)
        {
            cellCoords.Clear();
            if (radius < 1) return;

            int minX = coords.x - radius;
            int maxX = coords.x + radius;
            int minZ = coords.z - radius;
            int maxZ = coords.z + radius;

            for (int x = minX; x <= maxX; ++x)
            {
                for (int z = minZ; z <= maxZ; ++z)
                {
                    if (x == coords.x && z == coords.z) continue;

                    cellCoords.Add(new Vector3Int(x, coords.y, z));
                }
            }
        }

        public void initialize(TileRuleGridSettings initialGridSettings)
        {
            _settings.copy(initialGridSettings);

            _gameObject = new GameObject(gridName);
            UndoEx.registerCreatedObject(_gameObject);
            EditorUtility.SetDirty(_gameObject);

            _transform  = _gameObject.transform;

            _mirrorGizmoSettings.moveSnapStep       = settings.cellSize;
            _mirrorGizmoSettings.hasRotationHandles = false;
            _mirrorGizmoSettings.mirrorRotation     = false;
            _mirrorGizmoSettings.mirrorSpanning     = false;

            _usingSprites   = false;
            var ruleProfile = initialGridSettings.tileRuleProfile;
            int numRules    = ruleProfile.numTileRules;
            for (int i = 0; i < numRules; ++i)
            {
                var rule        = ruleProfile.getTileRule(i);
                int numPrefabs  = rule.numPrefabs;
                for (int j = 0; j < numPrefabs; ++j)
                {
                    var prefab  = rule.getPrefab(j);
                    if (prefab.prefabAsset.hierarchyHasOnlySprites(false, false))
                    {
                        _usingSprites = true;
                        break;
                    }
                }
            }
        }

        public void rotateRamp(Vector3Int cellCoords)
        {
            if (isRamp(cellCoords))
            {
                var rampTile = _tileMap[cellCoords];
                UndoEx.recordTransform(rampTile.transform);
                rampTile.transform.rotateAround(Quaternion.AngleAxis(90.0f, gridUp), rampTile.transform.position);
            }
        }

        public void snapMirrorGizmoToView(bool enableGizmo)
        {
            if (enableGizmo && !mirrorGizmo.enabled) mirrorGizmo.enabled = true;
            if (mirrorGizmo.enabled)
            {
                mirrorGizmo.snapToView();
                snapMirrorGizmoPositionToCellBaseCenter();
            }
        }

        public void deleteAllTiles()
        {
            UndoEx.record(this);
            _state.clear();

            _gameObject.getAllChildren(true, true, _objectBuffer);
            foreach(var go in _objectBuffer)
            {
                if (!ObjectGroupDb.instance.isObjectGroup(go))
                {
                    // Note: Might have been previously deleted.
                    if (go != null) UndoEx.destroyGameObjectImmediate(go);
                }
            }

            _tileMap.Clear();
        }

        public void refreshTiles()
        {
            prepareForTileUpdate();
            _editData.clear();

            var tileMapPairs        = _tileMap.ToList();
            int numTileMapPairs     = tileMapPairs.Count;
            bool hasRamps           = _sortedRampRules.Count > 0;
            if (hasRamps)
            {
                int numRampPrefabs = 0;
                foreach (var rule in _sortedRampRules)
                    numRampPrefabs += rule.numPrefabs;

                if (numRampPrefabs == 0) hasRamps = false;
            }

            PluginProgressDialog.begin("Refreshing");
            _tilePaintParams.clear();
            _tilePaintParams.paintReason = TilePaintReason.Refresh;
            for (int pairIndex = 0; pairIndex < numTileMapPairs; ++pairIndex)
            {
                var pair = tileMapPairs[pairIndex];
                PluginProgressDialog.updateProgress("Tile " + pairIndex, (pairIndex + 1) / (float)(numTileMapPairs));

                _tilePaintParams.cellCoords = pair.Key;
                if (isRamp(_tilePaintParams.cellCoords))
                {
                    if (hasRamps)
                    {
                        // Note: Store rotation because the ramp might have been rotated using the keyboard.
                        Quaternion rampRotation = pair.Value.transform.rotation;
                        _tilePaintParams.paintingRamp = true;
                        GameObject ramp = paintTile(_tilePaintParams);

                        // Store ramp rotation.
                        // Note: This can produce incorrect results when multiple ramp prefabs are used which open up
                        //       in different directions in their model pose.
                        if (ramp != null) ramp.transform.rotation = rampRotation;
                    }
                    else
                    {
                        eraseTile(pair.Key);
                        updateSurroundingTiles_IgnoreRamps(pair.Key);
                    }
                }
                else
                {
                    _tilePaintParams.paintingRamp = false;
                    paintTile(_tilePaintParams);
                }
            }

            // Note: Commit edit data here. It has to be done this way for Undo/Redo to work.
            commitEditData();

            PluginProgressDialog.end();
            ObjectSelection.instance.onSelectedObjectsMightHaveBeenDeleted(true);
        }

        [NonSerialized]
        private List<Vector3> _obbCornerBuffer          = new List<Vector3>();
        public void fixObjectOverlaps()
        {
            if (_usingSprites) return;

            UndoEx.saveEnabledState();
            UndoEx.enabled = false;

            var tileMapPairs    = _tileMap.ToList();
            int numTileMapPairs = tileMapPairs.Count;

            ObjectOverlapConfig overlapConfig   = ObjectOverlapConfig.defaultConfig;
            ObjectOverlapFilter overlapFilter   = new ObjectOverlapFilter();
            overlapFilter.objectTypes           = GameObjectType.Mesh;
            // Note: Only children of the tile rule grid will be taken into account.
            overlapFilter.customFilter          = (GameObject go) => { return go.transform.IsChildOf(_transform); };

            ObjectBounds.QueryConfig boundsQConfig  = ObjectBounds.QueryConfig.defaultConfig;
            boundsQConfig.objectTypes               = GameObjectType.Mesh;

            PluginProgressDialog.begin("Fixing Overlaps");
            for (int pairIndex = 0; pairIndex < numTileMapPairs; ++pairIndex)
            {
                var pair        = tileMapPairs[pairIndex];
                GameObject tile = pair.Value;
                PluginProgressDialog.updateProgress("Tile: " + tile.name, (pairIndex + 1) / (float)(numTileMapPairs));

                // Get all meshes in the tiles hierarchy
                tile.getMeshObjectsInHierarchy(false, false, _meshObjectBuffer);
                
                // Loop through each mesh
                foreach (var meshObject in _meshObjectBuffer)
                {
                    OBB obb = ObjectBounds.calcWorldOBB(meshObject, boundsQConfig);
                    if (!obb.isValid) continue;

                    obb.calcCorners(_obbCornerBuffer, false);

                    GameObject prefabAsset = meshObject.getPrefabAsset();
                    if (prefabAsset == null) continue;  // Note: Should not happen.

                    // Gather overlapping objects
                    PluginScene.instance.overlapBox(obb, overlapFilter, overlapConfig, _objectOverlapBuffer);
                    
                    // Loop through each overlapped object and disable its renderer if:
                    //  -if it's not a child of the current tile we are processing;
                    //  -is part of the same prefab asset;
                    //  -it has the same position;
                    foreach (var go in _objectOverlapBuffer)
                    {
                        if (!go.transform.IsChildOf(tile.transform))
                        {
                            // Calculate object OBB
                            OBB otherOBB = ObjectBounds.calcWorldOBB(go, boundsQConfig);
                            if (!obb.isValid) continue;

                            // Same center?
                            const float posEps = 1e-3f;
                            if (Vector3.Magnitude(otherOBB.center - obb.center) < posEps)
                            {
                                // Sizes should roughly match along the grid X and Z axes
                                const float sizeEsp = 1e-2f;
                                float s0 = Vector3Ex.getSizeAlongAxis(obb.size, obb.rotation, gridRight);
                                float s1 = Vector3Ex.getSizeAlongAxis(otherOBB.size, otherOBB.rotation, gridRight);
                                if (Mathf.Abs(s0 - s1) > sizeEsp) continue;

                                s0  = Vector3Ex.getSizeAlongAxis(obb.size, obb.rotation, gridLook);
                                s1  = Vector3Ex.getSizeAlongAxis(otherOBB.size, otherOBB.rotation, gridLook);
                                if (Mathf.Abs(s0 - s1) > sizeEsp) continue;

                                // All conditions are met. Disable renderer and colliders.
                                go.setMeshOrSkinnedMeshRendererEnabled(false);
                                go.setAllCollidersEnabled(false);
                            }
                        }
                    }
                }
            }

            UndoEx.restoreEnabledState();
            PluginProgressDialog.end();
        }

        public bool calcShadowCasterOBBCorners(OBB obb, int cellY, List<Vector3> corners)
        {
            corners.Clear();

            if (cellY <= -1) Box3D.calcFaceCorners(obb.center, obb.size, obb.rotation, Box3DFace.Top, corners);
            else if (cellY >= 1) Box3D.calcFaceCorners(obb.center, obb.size, obb.rotation, Box3DFace.Bottom, corners);
            else return false;

            return true;
        }

        public Plane getGridPlane(int yOffset)
        {
            return new Plane(gridUp, gridOrigin + yOffset * gridUp * settings.cellSize.y);
        }

        public bool pickTileCellCoords(Ray ray, bool pickAdjacent, out Vector3Int cellCoords)
        {
            cellCoords = Vector3Int.zero;
            if (pickAdjacent) return pickTileCellCoordsAdjacent(ray, out cellCoords);
            else return pickTileCellCoords(ray, out cellCoords);
        }

        public bool pickTileCellCoords(Ray ray, out Vector3Int cellCoords)
        {
            cellCoords = Vector3Int.zero;

            _pickCellCoordsFilter.raycastGrid = false;
            _pickCellCoordsFilter.objectTypes = GameObjectType.Mesh | GameObjectType.Sprite;

            var raycastConfig = ObjectRaycastConfig.defaultConfig;
            raycastConfig.raycastPrecision = ObjectRaycastPrecision.BestFit;

            var sceneHit = PluginScene.instance.raycastClosest(ray, _pickCellCoordsFilter, raycastConfig);
            if (sceneHit.wasObjectHit && sceneHit.objectHit.hitObject.transform.IsChildOf(_transform))
            {
                Vector3 hitPt   = sceneHit.objectHit.hitPoint;
                hitPt           -= sceneHit.objectHit.hitNormal * 1e-2f;
                cellCoords      = worldPointToVisualCellCoords(hitPt);
                return true;
            }

            return false;
        }

        public bool pickTileCellCoordsAdjacent(Ray ray, out Vector3Int cellCoords)
        {
            cellCoords = Vector3Int.zero;

            _pickCellCoordsFilter.raycastGrid = false;
            _pickCellCoordsFilter.objectTypes = GameObjectType.Mesh | GameObjectType.Sprite;

            var raycastConfig = ObjectRaycastConfig.defaultConfig;
            raycastConfig.raycastPrecision = ObjectRaycastPrecision.BestFit;

            var sceneHit = PluginScene.instance.raycastClosest(ray, _pickCellCoordsFilter, raycastConfig);
            if (sceneHit.wasObjectHit && sceneHit.objectHit.hitObject.transform.IsChildOf(_transform))
            {
                Vector3 hitPt   = sceneHit.objectHit.hitPoint;
                hitPt           += sceneHit.objectHit.hitNormal * 1e-2f;
                cellCoords      = worldPointToVisualCellCoords(hitPt);
                return true;
            }

            return false;
        }

        public bool pickCellCoords(Ray ray, int yOffset, out Vector3Int cellCoords)
        {
            cellCoords = Vector3Int.zero;

            _pickCellCoordsFilter.raycastGrid = false;
            _pickCellCoordsFilter.objectTypes = GameObjectType.Mesh | GameObjectType.Sprite;

            var sceneHit = PluginScene.instance.raycastClosest(ray, _pickCellCoordsFilter, ObjectRaycastConfig.defaultConfig);
            if (sceneHit.wasObjectHit)
            {
                Vector3 hitPt = sceneHit.objectHit.hitPoint;
                hitPt += gridUp * 1e-3f;        // Note: When clicking near the top of a cell, favor sitting on top.

                cellCoords = worldPointToCellCoords(hitPt);
                return true;
            }
            else
            {
                var gridHit = raycast(ray, yOffset);
                if (gridHit != null)
                {
                    cellCoords = gridHit.hitCellCoords;
                    return true;
                }
            }

            return false;
        }

        public TileRuleGridRayHit raycast(Ray ray)
        {
            float t;
            if (gridPlane.Raycast(ray, out t))
            {
                var cellCoords = worldPointToCellCoords(ray.GetPoint(t));
                cellCoords.y = 0;

                return new TileRuleGridRayHit(ray, this, gridPlane.normal, t, cellCoords);
            }

            return null;
        }

        public TileRuleGridRayHit raycast(Ray ray, int yOffset)
        {
            float t;
            Plane plane = getGridPlane(yOffset);
            if (plane.Raycast(ray, out t))
            {
                var cellCoords = worldPointToCellCoords(ray.GetPoint(t));
                cellCoords.y = yOffset;

                return new TileRuleGridRayHit(ray, this, plane.normal, t, cellCoords);
            }

            return null;
        }

        public OBB calcVisualCellOBB(Vector3Int cellCoords)
        {
            return new OBB(cellCoordsToVisualCellPosition(cellCoords), settings.cellSize, gridRotation);
        }

        public OBB calcCellRangeOBB(Vector3Int minCell, Vector3Int maxCell)
        {
            OBB obb             = new OBB(true);

            Plane plane         = gridPlane;
            obb.center          = plane.projectPoint(cellCoordsToCellPosition(minCell));
            obb.center          += plane.projectPoint(cellCoordsToCellPosition(maxCell));
            obb.center          *= 0.5f;

            int width           = maxCell.x - minCell.x + 1;
            int height          = maxCell.y - minCell.y + 1;
            int depth           = maxCell.z - minCell.z + 1;

            Vector3 cellSize    = settings.cellSize;
            obb.center          += plane.normal * height * 0.5f * cellSize.y;
            obb.center          += plane.normal * minCell.y * cellSize.y;
            obb.size            = new Vector3(width * cellSize.x, height * cellSize.y, depth * cellSize.z);
            obb.rotation        = gridRotation;

            return obb;
        }

        public Vector3Int worldPointToCellCoords(Vector3 pt)
        {
            Vector3 cellSize        = settings.cellSize;
            Vector3 toPt            = pt - gridOrigin;

            return new Vector3Int   (Mathf.RoundToInt(Vector3.Dot(toPt, gridRight)  / cellSize.x),
                                     Mathf.RoundToInt(Vector3.Dot(toPt, gridUp)     / cellSize.y),
                                     Mathf.RoundToInt(Vector3.Dot(toPt, gridLook)   / cellSize.z));
        }

        public Vector3Int worldPointToVisualCellCoords(Vector3 pt)
        {
            Vector3 cellSize    = settings.cellSize;
            Vector3 toPt        = pt - gridOrigin;

            float dotY          = Vector3.Dot(toPt, gridUp);

            return new Vector3Int(Mathf.RoundToInt(Vector3.Dot(toPt, gridRight) / cellSize.x),
                                  Mathf.RoundToInt((dotY - cellSize.y * 0.5f) / cellSize.y),
                                  Mathf.RoundToInt(Vector3.Dot(toPt, gridLook) / cellSize.z));
        }

        public Vector3 cellCoordsToCellPosition(Vector3Int cellCoords)
        {
            Vector3 cellSize        = settings.cellSize;
            return  gridOrigin      + gridRight * (cellCoords.x * cellSize.x)
                                    + gridUp    * (cellCoords.y * cellSize.y)
                                    + gridLook  * (cellCoords.z * cellSize.z);
        }

        public Vector3 cellCoordsToCellPosition(int x, int y, int z)
        {
            Vector3 cellSize        = settings.cellSize;
            return  gridOrigin      + gridRight * (x * cellSize.x)
                                    + gridUp    * (y * cellSize.y)
                                    + gridLook  * (z * cellSize.z);
        }

        public Vector3 cellCoordsToVisualCellPosition(Vector3Int cellCoords)
        {
            Vector3 cellSize        = settings.cellSize;
            return gridOrigin       + gridRight * (cellCoords.x * cellSize.x)
                                    + gridUp    * (cellCoords.y * cellSize.y + cellSize.y * 0.5f)
                                    + gridLook  * (cellCoords.z * cellSize.z);
        }

        public Vector3 cellCoordsToVisualCellPosition(int x, int y, int z)
        {
            Vector3 cellSize        = settings.cellSize;
            return gridOrigin       + gridRight * (x * cellSize.x)
                                    + gridUp    * (y * cellSize.y + cellSize.y * 0.5f)
                                    + gridLook  * (z * cellSize.z);
        }

        public void onSceneGUI(int gridYOffset)
        {
            prepareForTileUpdate();
            _mirrorGizmo.enabled    = _mirroringEnabled;

            Event e = Event.current;
            if (e.isLeftMouseButtonDownEvent()) _editData.clear();
            else if (e.isLeftMouseButtonUpEvent()) commitEditData();

            draw(gridYOffset);
        }

        public void paintTiles(TileRuleBrush brush)
        {
            _tilePaintParams.clear();
            _tilePaintParams.paintReason = TilePaintReason.Paint;

            // Update tiles inside brush
            brush.getCellCoords(_occupiedCells);
            foreach (var cellCoords in _occupiedCells)
            {
                _tilePaintParams.cellCoords = cellCoords;
                paintTile(_tilePaintParams);
            }

            // Update platforms
            brush.getCellCoordsBelowBrush(_cellsBelow);
            foreach (var cellCoords in _cellsBelow)
            {
                if (getTileObject(cellCoords) != null)
                {
                    _tilePaintParams.cellCoords = cellCoords;
                    paintTile(_tilePaintParams);
                }
            }

            // Update tiles around brush borders
            brush.getCellsAroundVerticalBorder((int)settings.tileRuleNeighborRadius, _cellsAroundVertBorder);
            foreach (var cellCoords in _cellsAroundVertBorder)
            {
                // Note: We don't update ramps.
                if ((getTileObject(cellCoords) != null) && !isRamp(cellCoords))
                {
                    _tilePaintParams.cellCoords = cellCoords;
                    paintTile(_tilePaintParams);
                }
            }

            _occupiedCells.Clear();
        }

        public void paintRamps(TileRuleBrush brush)
        {
            if (_sortedRampRules.Count == 0) return;

            _tilePaintParams.clear();
            _tilePaintParams.paintReason    = TilePaintReason.Paint;
            _tilePaintParams.paintingRamp   = true;

            // Update tiles inside brush
            bool paintedRamp = false;
            brush.getCellCoords(_occupiedCells);
            foreach (var cellCoords in _occupiedCells)
            {
                _tilePaintParams.cellCoords = cellCoords;
                if (paintTile(_tilePaintParams) != null) paintedRamp = true;
            }

            // Update platforms
            _tilePaintParams.paintingRamp = false;
            brush.getCellCoordsBelowBrush(_cellsBelow);
            foreach (var cellCoords in _cellsBelow)
            {
                if (getTileObject(cellCoords) != null &&
                    getTileObject(new Vector3Int(cellCoords.x, cellCoords.y + 1, cellCoords.z)) != null)
                {
                    _tilePaintParams.cellCoords = cellCoords;
                    paintTile(_tilePaintParams);
                }
            }

            if (paintedRamp)
            {
                // Update tiles around brush borders
                brush.getCellsAroundVerticalBorder((int)settings.tileRuleNeighborRadius, _cellsAroundVertBorder);
                foreach (var cellCoords in _cellsAroundVertBorder)
                {
                    // Note: We don't update ramps.
                    if ((getTileObject(cellCoords) != null) && !isRamp(cellCoords))
                    {
                        _tilePaintParams.cellCoords = cellCoords;
                        paintTile(_tilePaintParams);
                    }
                }
            }

            _occupiedCells.Clear();
        }

        public void eraseTiles(TileRuleBrush brush)
        {
            _tilePaintParams.clear();
            _tilePaintParams.paintReason = TilePaintReason.Erase;

            // Delete tiles inside brush
            brush.getCellCoords(_eraseBrushCells);
            if (spawnSettings.eraseForeignObjects)
            {
                foreach (var cellCoords in _eraseBrushCells)
                {
                    eraseForeignObjects(cellCoords);
                    eraseTile(cellCoords);
                }
            }
            else
            {
                foreach (var cellCoords in _eraseBrushCells)
                    eraseTile(cellCoords);
            }
            _eraseBrushCells.Clear();

            // Update tiles around brush borders
            brush.getCellsAroundVerticalBorder((int)settings.tileRuleNeighborRadius, _cellsAroundVertBorder);
            foreach (var cellCoords in _cellsAroundVertBorder)
            {
                // Note: We don't update ramps.
                if ((getTileObject(cellCoords) != null) && !isRamp(cellCoords))
                {
                    _tilePaintParams.cellCoords = cellCoords;
                    paintTile(_tilePaintParams);
                }
            }
            _cellsAroundVertBorder.Clear();
        }

        public void connect(TileRuleConnect tileRuleConnect)
        {
            int numConnectionPaths = tileRuleConnect.numConnectionPaths;
            for (int i = 0; i < numConnectionPaths; ++i)
                connect(tileRuleConnect.getConnectionPath(i));
        }

        private void connect(TileRuleConnectionPath connectionPath)
        {
            if (connectionPath.cells.Count == 0) return;

            _tilePaintParams.clear();
            _tilePaintParams.paintReason = TilePaintReason.Connect;

            int numConnectionCells = connectionPath.cells.Count;
            if (numConnectionCells == 0) return;

            // Note: Need to fill the occupied cell set for correctly updating the tiles.
            _occupiedCells.Clear();
            foreach (var cell in  connectionPath.cells)
                _occupiedCells.Add(cell);

            // Store needed data
            Vector3Int platformCoords   = Vector3Int.zero;
            bool fillCorners            = spawnSettings.connectFillCorners;
            bool generateRamps          = spawnSettings.connectGenerateRamps && _sortedRampRules.Count != 0;
            bool movingUp               = connectionPath.firstCell.y < connectionPath.lastCell.y;
            if (connectionPath.firstCell.y == connectionPath.lastCell.y)
            {
                fillCorners     = false;
                generateRamps   = false;
            }

            // Paint tiles
            for (int i = 0; i < numConnectionCells; ++i)
            {
                _tilePaintParams.paintingRamp   = false;
                _tilePaintParams.cellCoords     = connectionPath.cells[i];
                paintTile(_tilePaintParams);

                // Update tiles around this tile. Don't update ramps.
                updateSurroundingTiles_IgnoreRamps(_tilePaintParams.cellCoords);

                // Update the tile below (i.e. turn it into a platform)
                convertTileBelowToPlatform(connectionPath.cells[i]);
            
                // Generate ramp if necessary
                if (generateRamps)
                {
                    // Check if a ramp must be generated
                    bool paintRamp = false;
                    if (movingUp) paintRamp = (i < numConnectionCells - 1) && (connectionPath.cells[i + 1].y - connectionPath.cells[i].y == 1);
                    else paintRamp = (i >= 1) && (connectionPath.cells[i - 1].y - connectionPath.cells[i].y == 1);

                    if (paintRamp)
                    {
                        // Generate ramp
                        _tilePaintParams.paintingRamp   = true;
                        _tilePaintParams.cellCoords     = connectionPath.cells[i];
                        ++_tilePaintParams.cellCoords.y;
                        paintTile(_tilePaintParams);

                        // Update tiles
                        updateSurroundingTiles_IgnoreRamps(_tilePaintParams.cellCoords);
                        convertTileBelowToPlatform(_tilePaintParams.cellCoords);

                        // We need to check if the ramp is sitting in a corner, in which case,
                        // we need to paint tiles in order to make the ramp accessible.
                        if (i >= 1 && i < numConnectionCells - 1) 
                        {
                            Vector3Int currentCoords    = connectionPath.cells[i];
                            Vector3Int prevCoords       = connectionPath.cells[i - 1];
                            Vector3Int nextCoords       = connectionPath.cells[i + 1];
                            if (movingUp)
                            {
                                if (currentCoords.z == prevCoords.z && currentCoords.z != nextCoords.z)
                                {
                                    int zCoord = currentCoords.z + (int)Mathf.Sign(currentCoords.z - nextCoords.z);

                                    _cellCoordsBuffer.Clear();
                                    _cellCoordsBuffer.Add(new Vector3Int(currentCoords.x, currentCoords.y, zCoord));
                                    _cellCoordsBuffer.Add(new Vector3Int(prevCoords.x, currentCoords.y, zCoord));
                                    paintTilesAndUpdateSurroundings(_cellCoordsBuffer);
                                }
                                else
                                if (currentCoords.x == prevCoords.x && currentCoords.x != nextCoords.x)
                                {
                                    int xCoord = currentCoords.x + (int)Mathf.Sign(currentCoords.x - nextCoords.x);

                                    _cellCoordsBuffer.Clear();
                                    _cellCoordsBuffer.Add(new Vector3Int(xCoord, currentCoords.y, currentCoords.z));
                                    _cellCoordsBuffer.Add(new Vector3Int(xCoord, currentCoords.y, prevCoords.z));
                                    paintTilesAndUpdateSurroundings(_cellCoordsBuffer);
                                }
                            }
                            else
                            {
                                if (currentCoords.z == prevCoords.z && currentCoords.z != nextCoords.z)
                                {
                                    int xCoord = currentCoords.x + (int)Mathf.Sign(currentCoords.x - prevCoords.x);

                                    _cellCoordsBuffer.Clear();
                                    _cellCoordsBuffer.Add(new Vector3Int(xCoord, currentCoords.y, currentCoords.z));
                                    _cellCoordsBuffer.Add(new Vector3Int(xCoord, currentCoords.y, nextCoords.z));
                                    paintTilesAndUpdateSurroundings(_cellCoordsBuffer);
                                }
                                else
                                if (currentCoords.x == prevCoords.x && currentCoords.x != nextCoords.x)
                                {
                                    int zCoord = currentCoords.z + (int)Mathf.Sign(currentCoords.z - prevCoords.z);

                                    _cellCoordsBuffer.Clear();
                                    _cellCoordsBuffer.Add(new Vector3Int(currentCoords.x, currentCoords.y, zCoord));
                                    _cellCoordsBuffer.Add(new Vector3Int(nextCoords.x, currentCoords.y, zCoord));
                                    paintTilesAndUpdateSurroundings(_cellCoordsBuffer);
                                }
                            }
                        }
                    }
                }

                // Generate tiles below to fill corners
                if (fillCorners)
                {
                    // Is the previous or next tile lower?
                    bool paintTileBelow     = (i >= 1 && connectionPath.cells[i - 1].y < connectionPath.cells[i].y);
                    paintTileBelow          |= (i < (numConnectionCells - 1) && connectionPath.cells[i + 1].y < connectionPath.cells[i].y);

                    // Paint tile if necessary
                    if (paintTileBelow)
                    {
                        // Paint tile
                        platformCoords = connectionPath.cells[i];
                        --platformCoords.y;
                        _tilePaintParams.cellCoords = platformCoords;
                        paintTile(_tilePaintParams);

                        // Update tiles
                        updateSurroundingTiles_IgnoreRamps(_tilePaintParams.cellCoords);
                        convertTileBelowToPlatform(_tilePaintParams.cellCoords);
                    }
                }
            }
        }

        [NonSerialized]
        private TilePaintParams _paintParams_PaintAndUpdate = new TilePaintParams();
        private void paintTilesAndUpdateSurroundings(List<Vector3Int> cellCoords)
        {
            foreach (var coords in cellCoords)
            {
                _paintParams_PaintAndUpdate.paintingRamp   = false;
                _paintParams_PaintAndUpdate.cellCoords     = coords;
                paintTile(_paintParams_PaintAndUpdate);
                updateSurroundingTiles_IgnoreRamps(_paintParams_PaintAndUpdate.cellCoords);
                convertTileBelowToPlatform(coords);
            }
        }

        [NonSerialized]
        private TilePaintParams _paintParams_UpdateSurrounding = new TilePaintParams();
        private void updateSurroundingTiles_IgnoreRamps(Vector3Int tileCoords)
        {
            _paintParams_UpdateSurrounding.paintingRamp = false;
            getCellsAroundVerticalBorder(tileCoords, (int)settings.tileRuleNeighborRadius, _cellsAroundVertBorder);
            foreach (var cellCoords in _cellsAroundVertBorder)
            {
                if ((getTileObject(cellCoords) != null) && !isRamp(cellCoords))
                {
                    _paintParams_UpdateSurrounding.cellCoords = cellCoords;
                    paintTile(_paintParams_UpdateSurrounding);
                }
            }
        }

        [NonSerialized]
        private TilePaintParams _paintParams_ConvertToPlatform = new TilePaintParams();
        private void convertTileBelowToPlatform(Vector3Int tileAbove)
        {
            _paintParams_ConvertToPlatform.paintingRamp = false;
            Vector3Int cellBelow = new Vector3Int(tileAbove.x, tileAbove.y - 1, tileAbove.z);
            if (getTileObject(cellBelow) != null)
            {
                _paintParams_ConvertToPlatform.cellCoords = cellBelow;
                paintTile(_paintParams_ConvertToPlatform);
            }
        }

        private void draw(int yOffset)
        {
            GridHandles.DrawConfig drawConfig   = new GridHandles.DrawConfig();
            drawConfig.cellSizeX                = settings.cellSize.x;
            drawConfig.cellSizeZ                = settings.cellSize.z;
            drawConfig.wireColor                = settings.wireColor;
            drawConfig.fillColor                = settings.fillColor;
            drawConfig.origin                   = gridOrigin + gridUp * yOffset * settings.cellSize.y;
            drawConfig.right                    = gridRight;
            drawConfig.look                     = gridLook;
            drawConfig.planeNormal              = gridUp;
            drawConfig.drawCoordSystem          = GridPrefs.instance.drawCoordSystem;
            drawConfig.xAxisColor               = GridPrefs.instance.xAxisColor;
            drawConfig.yAxisColor               = GridPrefs.instance.yAxisColor;
            drawConfig.zAxisColor               = GridPrefs.instance.zAxisColor;
            drawConfig.finiteAxisLength         = GridPrefs.instance.finiteAxisLength;
            drawConfig.infiniteXAxis            = GridPrefs.instance.infiniteXAxis;
            drawConfig.infiniteYAxis            = GridPrefs.instance.infiniteYAxis;
            drawConfig.infiniteZAxis            = GridPrefs.instance.infiniteXAxis;
            GridHandles.drawInfinite(drawConfig, PluginCamera.camera);

            // Note: Draw the gizmo over the grid.
            _mirrorGizmo.rotation               = gridRotation;
            _mirrorGizmo.tileRuleGrid           = this;
            _mirrorGizmo.tileRuleGridYOffset    = yOffset;
            _mirrorGizmo.midSnapMode            = MirrorGizmoMidSnapMode.TileRuleGrid;
            _mirrorGizmo.onSceneGUI();

            // Note: Always force the gizmo to sit at the base of a cell. Random positions not allowed inside a tile grid.
            snapMirrorGizmoPositionToCellBaseCenter();

            if (_mirrorGizmo.isDraggingHandles) TileRuleObjectSpawnUI.instance.refresh();
        }

        public void drawShadow(List<Vector3> shadowCasterCorners, Color shadowLineColor, Color shadowColor)
        {
            HandlesEx.saveColor();
            Handles.color = shadowLineColor;

            Plane plane = gridPlane;
            _shadowCorners.Clear();
            foreach (var pt in shadowCasterCorners)
            {
                Vector3 prjPt = plane.projectPoint(pt);
                Handles.DrawLine(pt, prjPt);

                _shadowCorners.Add(prjPt);
            }

            Handles.color = shadowColor;
            Handles.DrawLine(_shadowCorners[0], _shadowCorners[1]);
            Handles.DrawLine(_shadowCorners[1], _shadowCorners[2]);
            Handles.DrawLine(_shadowCorners[2], _shadowCorners[3]);
            Handles.DrawLine(_shadowCorners[3], _shadowCorners[0]);

            HandlesEx.restoreColor();
        }

        private void snapMirrorGizmoPositionToCellBaseCenter()
        {
            _mirrorGizmo.position = cellCoordsToCellPosition(worldPointToCellCoords(_mirrorGizmo.position));
        }

        private GameObject paintTile(TilePaintParams paintParams)
        {
            // Must we paint a platform?
            Vector3Int cellCoords = paintParams.cellCoords;
            bool paintingPlatform = _sortedPlatformRules.Count != 0 && mustPaintPlatform(cellCoords);

            // Destroy the old tile that resides at this position
            GameObject oldTile = getTileObject(cellCoords);
            if (oldTile != null)
            {
                // Remove the tile record and destroy the game object
                removeTileRecord(cellCoords);
                UndoEx.destroyGameObjectImmediate(oldTile);

                // Note: If we are painting ramps, don't do anything. The tile
                //       will just be replaced with another ramp. Otherwise,
                //       we need to update the edit data accordingly.
                if (!paintParams.paintingRamp)
                {
                    if (isRamp(cellCoords))
                        _editData.removedRampCells.Add(cellCoords);
                }
            }
            else
            {
                // Just in case tile objects were deleted using means other than the tile rule interface
                if (!paintParams.paintingRamp)
                {
                    if (isRamp(cellCoords))
                        _editData.removedRampCells.Add(cellCoords);
                }
                removeTileRecordIfExists(cellCoords);
            }

            // Identify the tile rule list that we're going to use
            Vector3Int neighborMaskCoords = cellCoords;
            var tileRules = _sortedStdRules;
            if (paintingPlatform)
            {
                tileRules = _sortedPlatformRules;

                /*var moveUpCoords = neighborMaskCoords;
                ++moveUpCoords.y;
                while (getTileObject(moveUpCoords) != null)
                {
                    neighborMaskCoords = moveUpCoords;
                    ++moveUpCoords.y;
                }*/
            }
            else if (paintParams.paintingRamp) tileRules = _sortedRampRules;

            // Check if we have a matching rule for this position. If not, we can exit.
            ulong neighborMask  = calcNeighborMask(neighborMaskCoords, paintParams);
            TileRuleMaskMatchResult matchResult;
            TileRule tileRule   = matchRule(neighborMask, tileRules, !paintParams.paintingRamp, out matchResult);
            if (tileRule == null) return null;

            // Setup the prefab pick params
            _prefabPickParams.grid          = this;
            _prefabPickParams.cellCoords    = cellCoords;

            // Finally, we can spawn a new tile. Calculate the spawn data.
            _tileSpawnData.reset();
            _tileSpawnData.rule         = tileRule;
            _tileSpawnData.rulePrefab   = tileRule.pickPrefab(_prefabPickParams);
            if (_tileSpawnData.rulePrefab == null) return null;

            _tileSpawnData.isRamp       = paintParams.paintingRamp && !paintingPlatform;
            // Note: Always apply rotation even if mirroring was used. We need to orient the object
            //       so that it sits on the grid plane.
            //Quaternion baseRotation         = _tileSpawnData.rulePrefab.prefabAsset.transform.rotation;
            if (_usingSprites) _tileSpawnData.rotation = Quaternion.LookRotation(-gridUp, TileRuleMask.maskRotationToLookAxis(matchResult.maskRotation, gridRotation));// * baseRotation;
            else _tileSpawnData.rotation = Quaternion.LookRotation(TileRuleMask.maskRotationToLookAxis(matchResult.maskRotation, gridRotation), gridUp);// * baseRotation;

            // Check if mirroring was used to match the rule. In that case
            // we will have to apply scale to the object.
            if (matchResult.maskMirrorAxis != TileRuleMaskMirrorAxis.None)
            {
                _tileSpawnData.scale = _tileSpawnData.rulePrefab.prefabAsset.transform.localScale;
                _tileFrame[0] = _tileSpawnData.rotation * Vector3.right;
                _tileFrame[1] = _tileSpawnData.rotation * Vector3.up;
                _tileFrame[2] = _tileSpawnData.rotation * Vector3.forward;

                int affectedAxisIndex = 0;
                switch (matchResult.maskMirrorAxis)
                {
                    case TileRuleMaskMirrorAxis.X:

                        affectedAxisIndex = TransformEx.findIndexOfMostAlignedAxis(_tileFrame, gridRight);
                        break;

                    case TileRuleMaskMirrorAxis.Z:

                        affectedAxisIndex = TransformEx.findIndexOfMostAlignedAxis(_tileFrame, gridLook);
                        break;
                }

                if (_usingSprites)
                {
                    if (affectedAxisIndex == 0) _tileSpawnData.flipSpriteX = true;
                    else if (affectedAxisIndex == 1) _tileSpawnData.flipSpriteY = true;
                }
                else _tileSpawnData.scale[affectedAxisIndex] = -_tileSpawnData.scale[affectedAxisIndex];
            }
            else _tileSpawnData.scale = _tileSpawnData.rulePrefab.prefabAsset.transform.localScale;

            // Spawn the tile and return
            return spawnTile(cellCoords, _tileSpawnData);
        }

        private TilePaintParams _eraseTile_PaintParams = new TilePaintParams();
        private void eraseTile(Vector3Int cellCoords)
        {
            // Destroy tile if present
            if (destroyTileForErase(cellCoords))
            {
                // A tile was present. We need to account for any platform that
                // might have been sitting below it.
                Vector3Int platformCoords = new Vector3Int(cellCoords.x, cellCoords.y - 1, cellCoords.z);
                if (getTileObject(platformCoords) != null)
                {
                    _eraseTile_PaintParams.cellCoords = platformCoords;
                    paintTile(_eraseTile_PaintParams);
                }
            }
        }

        private void eraseForeignObjects(Vector3Int cellCoords)
        {
            var overlapOBB              = calcVisualCellOBB(cellCoords);
            overlapOBB.inflate(-1e-2f);
            var overlapConfig           = ObjectOverlapConfig.defaultConfig;
            overlapConfig.prefabMode    = ObjectOverlapPrefabMode.OnlyPrefabInstanceRoot;

            PluginScene.instance.overlapBox(overlapOBB, _foreignEraseOverlapFilter, overlapConfig, _foreignObjectBuffer);
            foreach (var go in _foreignObjectBuffer)
                UndoEx.destroyGameObjectImmediate(go);
        }

        private void commitEditData()
        {
            UndoEx.record(this);

            foreach (var cell in _editData.removedRampCells)
                _state.rampCells.Remove(cell);

            foreach (var cell in _editData.addedRampCells)
                _state.rampCells.Add(cell);

            _editData.clear();
        }

        private void prepareForTileUpdate()
        {
            #pragma warning disable 0612
            _ruleProfile = settings.tileRuleProfile;

            switch (settings.tileRuleNeighborRadius)
            {
                case TileRuleNeighborRadius.One:

                    _neighborOffsets = _neighOffsets_R1;
                    break;

                case TileRuleNeighborRadius.Two:

                    _neighborOffsets = _neighOffsets_R2;
                    break;

                case TileRuleNeighborRadius.Three:

                    _neighborOffsets = _neighOffsets_R3;
                    break;
            }

            // Note: Sort the tile rules based on the number of bits which are set to 1.
            _ruleProfile.getTileRules(TileRuleType.Standard, _sortedStdRules);
            _sortedStdRules.RemoveAll(item => item.numReqOnBitsSet == 0);
            sortRulesDescending(_sortedStdRules);

            _ruleProfile.getTileRules(TileRuleType.Platform, _sortedPlatformRules);
            _sortedPlatformRules.RemoveAll(item => item.numReqOnBitsSet == 0);
            sortRulesDescending(_sortedPlatformRules);

            _ruleProfile.getTileRules(TileRuleType.Ramp, _sortedRampRules);
            _sortedRampRules.RemoveAll(item => item.numReqOnBitsSet == 0);
            sortRulesDescending(_sortedRampRules);
            #pragma warning restore 0612
        }

        private void sortRulesDescending(List<TileRule> tileRules)
        {
            tileRules.Sort(delegate (TileRule r0, TileRule r1) { return r1.numReqOnBitsSet.CompareTo(r0.numReqOnBitsSet); });
        }

        private bool mustPaintPlatform(Vector3Int cellCoords)
        {
            if (_sortedPlatformRules.Count == 0) return false;

            // First, check if we are below any of the cells that are marked as occupied
            var checkCell = new Vector3Int(cellCoords.x, cellCoords.y + 1, cellCoords.z);
            if (_occupiedCells.Contains(checkCell)) return true;

            // Just check if there is an object above
            return getTileObject(checkCell) != null;
        }

        private bool isRamp(Vector3Int cell)
        {
            if (_editData.removedRampCells.Contains(cell)) return false;
            return _editData.addedRampCells.Contains(cell) || _state.rampCells.Contains(cell);
        }

        private TileRule matchRule(ulong ruleMask, List<TileRule> tileRules, bool pickFirstWithPrefabs, out TileRuleMaskMatchResult matchResult)
        {
            matchResult     = new TileRuleMaskMatchResult(false);
            int numRules    = tileRules.Count;
            if (numRules == 0) return null;

            for (int i = 0; i < numRules; ++i)
            {
                var tileRule = tileRules[i];

                // Skip rules with no prefabs
                if (tileRule.numPrefabs == 0) continue;

                matchResult = tileRule.match(ruleMask);
                if (matchResult.matched) return tileRule;
            }

            if (pickFirstWithPrefabs)
            {
                foreach (var tileRule in tileRules)
                {
                    if (tileRule.numPrefabs != 0)
                    {
                        matchResult.matched         = true;
                        matchResult.maskRotation    = TileRuleMaskRotation.None;
                        return tileRule;
                    }
                }
            }

            return null;
        }

        private GameObject spawnTile(Vector3Int cellCoords, TileSpawnData spawnData)
        {
            var rulePrefab = spawnData.rulePrefab;
            if (rulePrefab != null)
            {
                Vector3 position        = cellCoordsToCellPosition(cellCoords);
                GameObject tileObject   = rulePrefab.pluginPrefab.spawn(position, spawnData.rotation, spawnData.scale);
        
                // Flip sprite if necessary
                if (spawnData.flipSpriteX)
                {
                    var spriteRenderer      = tileObject.getSpriteRendererInChildren();
                    spriteRenderer.flipX    = !spriteRenderer.flipX;       // Note: It may already be flipped. So always flip relative to the current flip state.
                }
                else
                if (spawnData.flipSpriteY)
                {
                    var spriteRenderer      = tileObject.getSpriteRendererInChildren();
                    spriteRenderer.flipY    = !spriteRenderer.flipY;
                }

                // If the rule prefab is associated with an object group, we need to ensure
                // that the object group is a child of the grid. Otherwise, we detach the
                // object from the group and attach it to the grid.
                if (tileObject.transform.parent != _transform)
                {
                    // The object is attached to an object group or it doesn't have a parent.
                    if (tileObject.transform.parent == null || !tileObject.transform.parent.IsChildOf(_transform))
                        tileObject.transform.parent = _transform;
                }

                // Store the new tile
                addTileRecord(cellCoords, tileObject);
              
                // If we are spawning a ramp, store the cell coordinates of the ramp
                if (spawnData.isRamp) _editData.addedRampCells.Add(cellCoords);

                return tileObject;
            }

            return null;
        }

        private bool destroyTileForErase(Vector3Int cellCoords)
        {
            var tileObject = getTileObject(cellCoords);
            if (tileObject != null)
            {
                if (isRamp(cellCoords))
                    _editData.removedRampCells.Add(cellCoords);

                removeTileRecord(cellCoords);
                UndoEx.destroyGameObjectImmediate(tileObject);

                return true;
            }
            else
            {
                if (isRamp(cellCoords))
                    _editData.removedRampCells.Add(cellCoords);

                removeTileRecordIfExists(cellCoords);
            }

            return false;
        }

        private ulong calcNeighborMask(Vector3Int cellCoords, TilePaintParams paintParams)
        {
            ulong ruleMask = TileRuleMask.defaultReqOnMask;
            for (int i = 0; i < _neighborOffsets.Length; ++i)
            {
                var offset  = _neighborOffsets[i];

                var coords  = cellCoords;
                coords.x    += offset.x;
                coords.z    += offset.y;

                var tile    = getTileObject(coords);

                if ((tile != null && (!paintParams.paintingRamp || !isRamp(coords))) || _occupiedCells.Contains(coords))
                {
                    // Note: Subtract offset.y instead of adding, because in bit mask space, rows decrease upwards.
                    ruleMask |= TileRuleMask.setBit(ruleMask, TileRuleMask.middleBitRow - offset.y, TileRuleMask.middleBitCol + offset.x);
                }
            }

            return ruleMask;
        }

        private void addTileRecord(Vector3Int cellCoords, GameObject tileObject)
        {
            _tileMap.Add(cellCoords, tileObject);
        }

        private GameObject getTileObject(Vector3Int cellCoords)
        {
            GameObject tileObject = null;
            _tileMap.TryGetValue(cellCoords, out tileObject);

            return tileObject;
        }

        private void removeTileRecord(Vector3Int cellCoords)
        {
            _tileMap.Remove(cellCoords);
        }

        private void removeTileRecordIfExists(Vector3Int cellCoords)
        {
            if (_tileMap.ContainsKey(cellCoords))
                _tileMap.Remove(cellCoords);
        }

        private void onUndoRedo()
        {
            registerTilesWithGrid();
        }

        private void onHierarchyChanged()
        {
            if (_gameObject == null)
            {
                UndoEx.saveEnabledState();
                UndoEx.enabled = false;
                TileRuleGridDb.instance.deleteGrid(this);
                UndoEx.restoreEnabledState();
                TileRuleObjectSpawnUI.instance.refresh();
                return;
            }
            else
            if (_gameObject.name != _gridName)
            {
                UndoEx.record(this);
                _gridName= _gameObject.name;
                TileRuleObjectSpawnUI.instance.refresh();
            }
        }

        private void registerTilesWithGrid()
        {
            // Note: Could happen if the grid has just been created (it's being called from OnEnable).
            if (_gameObject == null) return;

            _tileMap.Clear();

            getChildTileRulePrefabInstances(_prefabInstanceRoots);
            foreach (var go in _prefabInstanceRoots)
            {
                Vector3Int cellCoords = worldPointToCellCoords(go.transform.position);
                addTileRecord(cellCoords, go);
            }
        }

        private void getChildTileRulePrefabInstances(List<GameObject> tileRulePrefabInstances)
        {
            _prefabInstanceRootFilter = (GameObject go) =>
            { return !ObjectGroupDb.instance.isObjectGroup(go) && _ruleProfile.containsPrefab(go.getPrefabAsset()); };
            _gameObject.getAllChildren(true, true, _objectBuffer);
            GameObjectEx.getOutermostPrefabInstanceRoots(_objectBuffer, tileRulePrefabInstances, _prefabInstanceRootFilter);
        }

        private void OnEnable()
        {
            if (_settings == null)      _settings       = ScriptableObject.CreateInstance<TileRuleGridSettings>();
            if (_gameObject != null)    _transform      = _gameObject.transform;

            EditorApplication.hierarchyChanged  += onHierarchyChanged;
            Undo.undoRedoPerformed              += onUndoRedo;

            _ruleProfile = settings.tileRuleProfile;
            registerTilesWithGrid();

            _foreignEraseOverlapFilter.objectTypes  = GameObjectType.All & ~(GameObjectType.Light | GameObjectType.Camera | GameObjectType.Terrain);
            _foreignEraseOverlapFilter.customFilter = (GameObject go) => { return !go.transform.IsChildOf(_transform); };

            if (_mirrorGizmo == null)           _mirrorGizmo            = ScriptableObject.CreateInstance<ObjectMirrorGizmo>();
            if (_mirrorGizmoSettings == null)   _mirrorGizmoSettings    = ScriptableObject.CreateInstance<ObjectMirrorGizmoSettings>();

            _mirrorGizmo.sharedSettings = _mirrorGizmoSettings;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged  -= onHierarchyChanged;
            Undo.undoRedoPerformed              -= onUndoRedo;
        }

        private void OnDestroy()
        {
            if (_settings != null)              UndoEx.destroyObjectImmediate(_settings);
            if (_mirrorGizmo != null)           UndoEx.destroyObjectImmediate(_mirrorGizmo);
            if (_mirrorGizmoSettings != null)   UndoEx.destroyObjectImmediate(_mirrorGizmoSettings);
        }
    }
}
#endif