#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public class ScatterBrushNode
    {
        public OBB obb = OBB.getInvalid();
    }

    public class ScatterBrushObjectSpawner
    {
        private struct SpawnData
        {
            public Vector3      center;
            public Quaternion   rotation;
            public Vector3      scale;
            public Plane        pushPlane;
            public int          insertAfterNode;
        }

        private class PrefabData
        {
            public GameObject   dummyObject;     // Needed to avoid repetitive spawning and destroying of objects during a spawn session
        }

        private ScatterBrushObjectSpawnSettings             _objectSpawnSettings;
        private ScatterBrushPrefabProfile                   _prefabProfile;
        private CircleBrush3D                               _circleBrush;
        private ObjectProjectionSettings                    _projectionSettings;
        private List<ScatterBrushNode>                      _nodes                  = new List<ScatterBrushNode>();
        private ScatterBrushNodeTree                        _nodeTree               = new ScatterBrushNodeTree();
        private ObjectBounds.QueryConfig                    _objectBoundsQConfig    = ObjectBounds.QueryConfig.defaultConfig;
        private ObjectOverlapConfig                         _overlapConfig          = ObjectOverlapConfig.defaultConfig;
        private ObjectOverlapFilter                         _overlapFilter          = new ObjectOverlapFilter();
        private Dictionary<ScatterBrushPrefab, PrefabData>  _prefabToDataMap        = new Dictionary<ScatterBrushPrefab, PrefabData>();
        private bool                                        _isSpawnSessionActive;
        
        [NonSerialized]
        private List<Vector3>                               _vector3Buffer          = new List<Vector3>();

        public int                                          numNodes                { get { return _nodes.Count; } }
        public int                                          numSegments             { get { return _nodes.Count > 1 ? _nodes.Count + 1 : 0; } }
        public bool                                         isSpawnSessionActive    { get { return _isSpawnSessionActive; } }

        public bool beginSpawnSession(ScatterBrushObjectSpawnSettings objectSpawnSettings, CircleBrush3D circleBrush, ObjectProjectionSettings projectionSettings)
        {
            if (_isSpawnSessionActive) return false;

            _prefabProfile = objectSpawnSettings.scatterBrushPrefabProfile;
            if (!_prefabProfile.isAnyPrefabUsed()) return false;

            _prefabToDataMap.Clear();
            _nodeTree.initialize();
            _objectSpawnSettings    = objectSpawnSettings;
            _circleBrush            = circleBrush;
            _projectionSettings     = projectionSettings;

            int numPrefabs = _prefabProfile.numPrefabs;
            for (int i = 0; i < numPrefabs; ++i)
            {
                ScatterBrushPrefab brushPrefab = _prefabProfile.getPrefab(i);
                if (brushPrefab.used)
                {
                    GameObject dummyObject  = brushPrefab.pluginPrefab.prefabAsset.instantiatePrefab();
                    dummyObject.SetActive(false);
                    dummyObject.makeEditorOnly();
                    dummyObject.hideFlags = HideFlags.HideInHierarchy;

                    PrefabData prefabData   = new PrefabData();
                    prefabData.dummyObject  = dummyObject;
                    _prefabToDataMap.Add(brushPrefab, prefabData);
                }
            }
        
            _objectBoundsQConfig.objectTypes = GameObjectType.Mesh | GameObjectType.Sprite | 
                GameObjectType.Camera | GameObjectType.Light | GameObjectType.ParticleSystem;
            _isSpawnSessionActive = true;
            return true;
        }

        public void endSpawnSession()
        {
            _isSpawnSessionActive = false;
            _nodeTree.clear();

            foreach(var pair in _prefabToDataMap)
            {
                var prefabData = pair.Value;
                if (prefabData.dummyObject != null)
                    GameObject.DestroyImmediate(prefabData.dummyObject);
            }
            _prefabToDataMap.Clear();
        }

        public void spawnObjects()
        {
            _nodes.Clear();

            if (!_prefabProfile.isAnyPrefabUsed()) return;

            int maxNumObjects   = _objectSpawnSettings.maxNumObjects;
            int maxNumTries     = Mathf.Min(100, maxNumObjects * 2);
            int numTries        = 0;

            for (int objectIndex = 0; objectIndex < maxNumObjects && numTries < maxNumTries; )
            {
                ScatterBrushPrefab brushPrefab = _prefabProfile.pickPrefab();
                if (trySpawnObject(brushPrefab) != null) ++objectIndex;
                else ++numTries;
            }
        }

        private GameObject trySpawnObject(ScatterBrushPrefab brushPrefab)
        {
            if (!_isSpawnSessionActive) return null;
            prepareProjectionSettings(brushPrefab);
    
            OBB prefabOBB = ObjectBounds.calcHierarchyWorldOBB(brushPrefab.prefabAsset, _objectBoundsQConfig);
            if (!prefabOBB.isValid) return null;

            SpawnData spawnData = generateSpawnData(brushPrefab, prefabOBB);
            Vector3 objectPosition = ObjectPositionCalculator.calcRootPosition(brushPrefab.prefabAsset, prefabOBB, spawnData.center, spawnData.scale, spawnData.rotation);

            // Note: Perform all calculations using the dummy object. Also, activate it
            //       because certain queries such as OBB calculation will depend on whether
            //       objects are active or not. 
            var dummyObject = _prefabToDataMap[brushPrefab].dummyObject;
            dummyObject.SetActive(true);

            // Initialize dummy object transform data.
            // Note: Initially, place the object in the same position, rotation and scale as the prefab in order
            //       to force it to have the same OBB as the prefab. We need the OBB to rotate the object around
            //       its center. Simply setting the rotation (e.g. transform.rotation = ... ) might produce
            //       incorrect results for hierarchies whose root objects are placed at a certain distance away
            //       from its children.
            Transform dummyTransform    = dummyObject.transform;
            dummyTransform.position     = brushPrefab.prefabAsset.transform.position;
            dummyTransform.rotation     = brushPrefab.prefabAsset.transform.rotation;
            dummyTransform.localScale   = brushPrefab.prefabAsset.transform.localScale;
            dummyTransform.rotateAround(spawnData.rotation, prefabOBB.center);

            dummyTransform.position     = objectPosition;
            dummyTransform.localScale   = spawnData.scale;

            // Project the hierarchy, rotate it around the surface normal and then correct its position
            // so that it doesn't overlap existing nodes.
            var projectionResult = projectHierarchyOnSurface(dummyObject);
            applyFinalRotation(dummyObject, brushPrefab, projectionResult.projectionPlane.normal);
            avoidOverlapsWithNodes(dummyObject, spawnData);

            // Note: Project again because we were pushed away from the nodes and apply the
            //       rotation again using the new projection surface normal.
            projectionResult = projectHierarchyOnSurface(dummyObject);
            applyFinalRotation(dummyObject, brushPrefab, projectionResult.projectionPlane.normal);

            // Note: Deactivate the object AFTER the OBB is calculated. This is because
            //       the query config is configured to ignore inactive objects.
            OBB hierarchyOBB    = ObjectBounds.calcHierarchyWorldOBB(dummyObject, _objectBoundsQConfig);
            OBB nodeOBB         = calcNodeOBB(dummyObject, hierarchyOBB, spawnData, brushPrefab);
            dummyObject.SetActive(false);

            // Reject object if necessary
            if (isObjectRejected(dummyObject, nodeOBB, hierarchyOBB, brushPrefab, projectionResult)) return null;

            // The object is valid, so create a new node 
            var node = new ScatterBrushNode();
            node.obb = nodeOBB;

            // Inset the node in the correct position and ensure that there are no nodes that generate concavities
            if (spawnData.insertAfterNode < 0) _nodes.Add(node);
            else _nodes.Insert((spawnData.insertAfterNode + 1) % numNodes, node);
            _nodeTree.addNode(node);
            removeNodesWhichGenerateConcavities();

            // Spawn the actual object and return it
            var spawnedObject = brushPrefab.pluginPrefab.spawn(dummyTransform.position, dummyTransform.rotation, dummyTransform.localScale);
            return spawnedObject;
        }

        private OBB calcNodeOBB(GameObject gameObject, OBB hierarchyOBB, SpawnData spawnData, ScatterBrushPrefab brushPrefab)
        {
            float volumeSize = brushPrefab.volumeRadius * 2.0f * spawnData.scale.getMaxAbsComp();

            // Note: The radius will replace the OBB size components that run parallel to the circle plane.
            OBB nodeOBB = hierarchyOBB;
            if (brushPrefab.alignAxis)
            {
                var axisDesc = TransformEx.flexiToLocalAxisDesc(gameObject.transform, nodeOBB, brushPrefab.alignmentAxis, brushPrefab.invertAlignmentAxis);
                nodeOBB.size = nodeOBB.size.replaceOther(axisDesc.index, volumeSize);
            }
            else
            {
                int axisIndex   = TransformEx.findIndexOfMostAlignedAxis(gameObject.transform, _circleBrush.normal);
                nodeOBB.size    = nodeOBB.size.replaceOther(axisIndex, volumeSize);
            }

            return nodeOBB;
        }

        private void prepareProjectionSettings(ScatterBrushPrefab brushPrefab)
        {
            _projectionSettings.alignAxis           = brushPrefab.alignAxis;
            _projectionSettings.alignmentAxis       = (FlexiAxis)brushPrefab.alignmentAxis;
            _projectionSettings.invertAlignmentAxis = brushPrefab.invertAlignmentAxis;
            _projectionSettings.embedInSurface      = brushPrefab.embedInSurface;
            _projectionSettings.inFrontOffset       = brushPrefab.offsetFromSurface;
            _projectionSettings.halfSpace           = ObjectProjectionHalfSpace.InFront;
        }

        private SpawnData generateSpawnData(ScatterBrushPrefab brushPrefab, OBB prefabOBB)
        {
            var spawnData                   = new SpawnData();
            spawnData.rotation              = generateInitialSpawnRotation(brushPrefab, prefabOBB);
            spawnData.scale                 = generateScale(brushPrefab);
            spawnData.insertAfterNode       = -1;

            if (_nodes.Count > 1)
            {
                int randSegment             = findBestSpawnSegment();
                int firstNodeIndex          = wrapNodeIndex(randSegment);
                spawnData.insertAfterNode   = firstNodeIndex;
                int secondNodeIndex         = wrapNodeIndex(firstNodeIndex + 1);
                ScatterBrushNode firstNode              = _nodes[firstNodeIndex];
                ScatterBrushNode secondNode             = _nodes[secondNodeIndex];

                Vector3 segmentDir          = (secondNode.obb.center - firstNode.obb.center);
                float segmentLength         = segmentDir.magnitude;
                segmentDir.Normalize();

                // Note: Always pick the middle of the segment. Generating random values seems
                //       to produce a less uniform distribution.
                float t                     = 0.5f;
                Vector3 center              = firstNode.obb.center + segmentDir * segmentLength * t;
                Plane pushPlane             = new Plane(Vector3.Cross(segmentDir, _circleBrush.normal).normalized, center);

                // Note: We need to adjust the plane such that it is not intersecting the 2 nodes we just picked.
                firstNode.obb.calcCorners(_vector3Buffer, false);
                secondNode.obb.calcCorners(_vector3Buffer, true);
                int ptIndex = pushPlane.findIndexOfFurthestPointInFront(_vector3Buffer);
                if (ptIndex >= 0) pushPlane = new Plane(pushPlane.normal, _vector3Buffer[ptIndex]);

                spawnData.center            = center;
                spawnData.pushPlane         = pushPlane;
            }
            else
            { 
                if (_nodes.Count == 0) spawnData.center = _circleBrush.calcRandomPoint();
                else
                {
                    float theta                 = UnityEngine.Random.Range(0.0f, 1.0f) * Mathf.PI * 2.0f;
                    Vector3 randDir             = (Mathf.Cos(theta) * _circleBrush.u + Mathf.Sin(theta) * _circleBrush.v).normalized;

                    OBB nodeOBB                 = _nodes[0].obb;
                    Box3DFace mostAlignedFace   = Box3D.findMostAlignedFace(nodeOBB.center, nodeOBB.size, nodeOBB.rotation, randDir);
                    Box3DFaceDesc boxFaceDesc   = Box3D.getFaceDesc(nodeOBB.center, nodeOBB.size, nodeOBB.rotation, mostAlignedFace);
                    Vector3 projectedFaceNormal = new Plane(_circleBrush.normal, 0.0f).projectPoint(boxFaceDesc.plane.normal);
                    projectedFaceNormal.Normalize();

                    Plane provPushPlane             = new Plane(projectedFaceNormal, boxFaceDesc.center);
                    Box3D.calcFaceCorners(nodeOBB.center, nodeOBB.size, nodeOBB.rotation, mostAlignedFace, _vector3Buffer);
                    int ptIndex = provPushPlane.findIndexOfFurthestPointInFront(_vector3Buffer);
                    if (ptIndex >= 0) provPushPlane = new Plane(projectedFaceNormal, _vector3Buffer[ptIndex]);

                    spawnData.center            = boxFaceDesc.center;
                    spawnData.pushPlane         = provPushPlane;
                }
            }

            return spawnData;
        }

        private Quaternion generateInitialSpawnRotation(ScatterBrushPrefab brushPrefab, OBB prefabOBB)
        {
            if (brushPrefab.alignToStroke)
            {
                Transform prefabTransform   = brushPrefab.prefabAsset.transform;
                Vector3 alignmentAxis       = prefabTransform.flexiToLocalAxis(prefabOBB, brushPrefab.strokeAlignmentAxis, brushPrefab.invertStrokeAlignmentAxis);
                return prefabTransform.calcAlignmentRotation(alignmentAxis, _circleBrush.avgProjectedStrokeDirection);
            }
            else
            if (brushPrefab.randomizeRotation)
            {
                float minRotation = brushPrefab.minRandomRotation;
                float maxRotation = brushPrefab.maxRandomRotation;

                // Note: We ignore surface normal because we don't have access to it at this point.
                if (brushPrefab.rotationRandomizationAxis == ScatterBrushPrefabRotationRandomizationAxis.X)
                    return Quaternion.Euler(UnityEngine.Random.Range(minRotation, maxRotation), 0.0f, 0.0f);
                else if (brushPrefab.rotationRandomizationAxis == ScatterBrushPrefabRotationRandomizationAxis.Y)
                    return Quaternion.Euler(0.0f, UnityEngine.Random.Range(minRotation, maxRotation), 0.0f);
                else if (brushPrefab.rotationRandomizationAxis == ScatterBrushPrefabRotationRandomizationAxis.Z)
                    return Quaternion.Euler(0.0f, 0.0f, UnityEngine.Random.Range(minRotation, maxRotation));
            }

            return Quaternion.identity;
        }

        private Vector3 generateScale(ScatterBrushPrefab brushPrefab)
        {
            Vector3 prefabScale = brushPrefab.prefabAsset.transform.lossyScale;
            if (brushPrefab.randomizeScale)
                return Vector3.Scale(prefabScale, Vector3Ex.create(UnityEngine.Random.Range(brushPrefab.minRandomScale, brushPrefab.maxRandomScale)));
            
            return prefabScale;
        }

        private ObjectProjectionResult projectHierarchyOnSurface(GameObject root)
        {
            if (_circleBrush.isSurfaceObject)
                return ObjectProjection.projectHierarchyOnObject(root, _circleBrush.surfaceGameObject, _circleBrush.surfaceGameObjectType, _circleBrush.surfacePlane, _projectionSettings);
            else if (_circleBrush.isSurfaceGrid)
                return ObjectProjection.projectHierarchyOnPlane(root, _circleBrush.surfacePlane, _projectionSettings);

            return ObjectProjectionResult.notProjectedResult;
        }

        private void avoidOverlapsWithNodes(GameObject root, SpawnData spawnData)
        {
            // Note: When generating the first node, no correction is needed.
            if (_nodes.Count == 0) return;

            OBB obb = ObjectBounds.calcHierarchyWorldOBB(root, _objectBoundsQConfig);
            obb.calcCorners(_vector3Buffer, false);
            int ptIndex = spawnData.pushPlane.findIndexOfFurthestPointBehind(_vector3Buffer);
            if (ptIndex < 0) return;        // Can happen when using offset from surface other than 0

            Vector3 furthestPt  = _vector3Buffer[ptIndex];
            Vector3 prjPoint    = spawnData.pushPlane.projectPoint(_vector3Buffer[ptIndex]);
            Vector3 moveDir     = (prjPoint - furthestPt);
            float moveAmount    = moveDir.magnitude;
            moveDir.Normalize();

            root.transform.position += moveDir * moveAmount;
        }

        private void applyFinalRotation(GameObject root, ScatterBrushPrefab brushPrefab, Vector3 surfaceNormal)
        {
            if (!brushPrefab.randomizeRotation) return;

            OBB hierarchyOBB = ObjectBounds.calcHierarchyWorldOBB(root, _objectBoundsQConfig);
            if (brushPrefab.rotationRandomizationAxis == ScatterBrushPrefabRotationRandomizationAxis.SurfaceNormal)
            {
                Vector3 rotationAxis = surfaceNormal;
                if (!brushPrefab.alignAxis)
                {
                    if (_circleBrush.isSurfaceUnityTerrain) rotationAxis = Vector3.up;
                    else if (_circleBrush.isSurfaceTerrainMesh) rotationAxis = ObjectPrefs.instance.getTerrainMeshUp(_circleBrush.surfaceGameObject);
                }
                root.transform.rotateAround(Quaternion.AngleAxis(UnityEngine.Random.Range(brushPrefab.minRandomRotation, brushPrefab.maxRandomRotation), rotationAxis), hierarchyOBB.center);
            }
            else
            {
                if (brushPrefab.alignToStroke)
                {
                    float minRotation = brushPrefab.minRandomRotation;
                    float maxRotation = brushPrefab.maxRandomRotation;

                    if (brushPrefab.rotationRandomizationAxis == ScatterBrushPrefabRotationRandomizationAxis.X)
                        root.transform.rotateAround(Quaternion.Euler(UnityEngine.Random.Range(minRotation, maxRotation), 0.0f, 0.0f), hierarchyOBB.center);
                    else if (brushPrefab.rotationRandomizationAxis == ScatterBrushPrefabRotationRandomizationAxis.Y)
                        root.transform.rotateAround(Quaternion.Euler(0.0f, UnityEngine.Random.Range(minRotation, maxRotation), 0.0f), hierarchyOBB.center);
                    else if (brushPrefab.rotationRandomizationAxis == ScatterBrushPrefabRotationRandomizationAxis.Z)
                        root.transform.rotateAround(Quaternion.Euler(0.0f, 0.0f, UnityEngine.Random.Range(minRotation, maxRotation)), hierarchyOBB.center);
                }
            }
        }

        private void removeNodesWhichGenerateConcavities()
        {
            int nodeIndex = 0;
            while (nodeIndex < numNodes && numNodes > 3)
            {
                if (nodeGeneratesConcavity(nodeIndex)) _nodes.RemoveAt(nodeIndex);
                else ++nodeIndex;
            }
        }

        private bool nodeGeneratesConcavity(int nodeIndex)
        {
            ScatterBrushNode nodeBefore = _nodes[wrapNodeIndex(nodeIndex - 1)];
            ScatterBrushNode node       = _nodes[nodeIndex];
            ScatterBrushNode nodeAfter  = _nodes[wrapNodeIndex(nodeIndex + 1)];

            Vector3 planeNormal = Vector3.Cross((nodeAfter.obb.center - nodeBefore.obb.center).normalized, _circleBrush.normal).normalized;
            Plane segmentPlane  = new Plane(planeNormal, nodeBefore.obb.center);

            return segmentPlane.GetDistanceToPoint(node.obb.center) < 0.0f;
        }

        private int wrapNodeIndex(int nodeIndex)
        {
            if (numNodes == 0) return -1;

            nodeIndex %= numNodes;
            if (nodeIndex < 0) return numNodes + nodeIndex;
            return nodeIndex;
        }

        private bool isObjectRejected(GameObject root, OBB nodeOBB, OBB hierarchyOBB, ScatterBrushPrefab brushPrefab, ObjectProjectionResult projectionResult)
        {
            // Reject the object if it doesn't pass the slope test
            if (brushPrefab.enableSlopeCheck)
            {
                // Note: Only for terrains and spheres.
                bool isTerrain = _circleBrush.isSurfaceUnityTerrain || _circleBrush.isSurfaceTerrainMesh;
                if (isTerrain || _circleBrush.isSurfaceSphericalMesh)
                {
                    Vector3 upAxis = Vector3.up;
                    if (_circleBrush.isSurfaceTerrainMesh) upAxis = ObjectPrefs.instance.getTerrainMeshUp(_circleBrush.surfaceGameObject);

                    Vector3 surfaceNormal = projectionResult.projectionPlane.normal;
                    if (isTerrain) surfaceNormal = projectionResult.terrainNormal;

                    float angle = Vector3.Angle(surfaceNormal, upAxis);
                    if (angle < brushPrefab.minSlope || angle > brushPrefab.maxSlope) return true;
                }
            }

            // Reject the object if it lies outside the brush bounds. Use the original
            // hierarchy OBB. The node OBB may be smaller and in that case we can get objects
            // being spawned outside the brush.
            // Note: We will use a simple check where at least one of the corner points
            //       of the object's OBB must reside inside the circle area. It is not
            //       robust and can generate false negatives but seems to produce good results.
            hierarchyOBB.calcCorners(_vector3Buffer, false);
            _circleBrush.plane.projectPoints(_vector3Buffer);
            Vector3 circleCenter = _circleBrush.plane.projectPoint(_circleBrush.surfacePickPoint);
            bool foundPtInsideBrush = false;
            foreach (var pt in _vector3Buffer)
            {
                if ((pt - circleCenter).magnitude <= _circleBrush.radius)
                {
                    foundPtInsideBrush = true;
                    break;
                }
            }
            if (!foundPtInsideBrush) return true;

            // Reject the object if we are using a terrain surface and the object is outside of
            // the terrain bounds.
            if (_circleBrush.surfaceType == CircleBrush3DSurfaceType.UnityTerrain)
            {
                Terrain terrain = _circleBrush.surfaceGameObject.getTerrain();
                if (!terrain.isWorldOBBCompletelyInsideTerrainArea(nodeOBB)) return true;
            }
            else
            if (_circleBrush.surfaceType == CircleBrush3DSurfaceType.TerrainMesh)
            {
                OBB meshOBB = new OBB(_circleBrush.surfaceGameObject.getMeshOrSkinnedMeshRenderer().bounds);
                meshOBB.inflate(1e-2f); // Note: In case the terrain mesh is flat.
                if (!meshOBB.intersectsOBB(nodeOBB)) return true;
            }

            // Reject the object if it intersects other objects in the scene
            _overlapFilter.objectTypes = GameObjectType.Mesh | GameObjectType.Sprite;
            _overlapFilter.setIgnoredObject(_circleBrush.surfaceGameObject);
            OBB overlapOBB = nodeOBB;
            overlapOBB.inflate(-1e-1f);

            if (_objectSpawnSettings.overlapTestPrecision == ScatterBrushOverlapPrecision.BoundsVSBounds)
            {
                if (PluginScene.instance.overlapBox(overlapOBB, _overlapFilter, _overlapConfig)) return true;
            }
            else if (PluginScene.instance.overlapBoxAgainstMeshTriangles(overlapOBB, _overlapFilter, _overlapConfig)) return true;

            // Reject if it overlaps any nodes that were spawned during the same session
            if (_nodeTree.checkBoxOverlap(nodeOBB)) return true;

            return false;
        }

        private int findBestSpawnSegment()
        {
            // Note: A random value seems to work nicely.
            return UnityEngine.Random.Range(0, numSegments);
        }
    }
}
#endif