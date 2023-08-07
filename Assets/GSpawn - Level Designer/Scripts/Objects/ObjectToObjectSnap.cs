#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public static class ObjectToObjectSnap
    {
        private struct SnapData
        {
            public GameObject   srcObject;
            public GameObject   destObject;

            public Box3DFace    srcSnapFace;
            public Box3DFace    destSnapFace;

            public Vector3      snapSource;
            public Vector3      snapDestination;
            public float        snapDistance;

            public bool         foundDestination;
            public float        minSnapDistance;
        }

        public enum SnapFailReson
        {
            None = 0,
            NoDestinationFound
        }

        public struct SnapResult
        {
            private bool            _success;
            private SnapFailReson   _failReason;
            private Vector3         _snapPivot;
            private Vector3         _snapDestination;
            private float           _snapDistance;

            public bool             success             { get { return _success; } }
            public SnapFailReson    failReason          { get { return _failReason; } }
            public Vector3          snapPivot           { get { return _snapPivot; } }
            public Vector3          snapDestination     { get { return _snapDestination; } }
            public float            snapDistance        { get { return _snapDistance; } }

            public SnapResult(SnapFailReson failReson)
            {
                _success            = false;
                _snapPivot          = Vector3.zero;
                _snapDestination    = Vector3.zero;
                _snapDistance       = 0.0f;
                _failReason         = failReson;
            }

            public SnapResult(Vector3 snapPivot, Vector3 snapDestination, float snapDistance)
            {
                _success            = true;
                _snapPivot          = snapPivot;
                _snapDestination    = snapDestination;
                _snapDistance       = snapDistance;
                _failReason         = SnapFailReson.None;
            }
        }

        public struct Config
        {
            public int      destinationLayers;
            public float    snapRadius;
        }

        private static ObjectOverlapFilter      _overlapFilter              = new ObjectOverlapFilter();
        private static Box3DFace[]              _allSnapFaces               = Box3D.facesArrayCopy;
        private static List<GameObject>         _sourceObjectBuffer         = new List<GameObject>();
        private static List<GameObject>         _nearbyObjectBuffer         = new List<GameObject>();
        private static List<Vector3>            _srcSocketPtBuffer          = new List<Vector3>();
        private static List<Vector3>            _destSocketPtBuffer         = new List<Vector3>();
        private static ObjectBounds.QueryConfig _nearbyGatherBoundsQConfig  = new ObjectBounds.QueryConfig()
        {
            objectTypes = GameObjectType.Mesh | GameObjectType.Sprite,
            includeInactive = false,
            includeInvisible = false,
            volumelessSize = Vector3.zero
        };

        public static int maxNumSourceObjects { get { return 100; } }

        public static SnapResult snap(List<GameObject> parents, Config snapConfig)
        {
            var snapResult = calcSnapResult(parents, snapConfig);
            if (snapResult.success)
            {
                Vector3 snapVector = snapResult.snapDestination - snapResult.snapPivot;
                foreach (var parent in parents)
                    parent.transform.position += snapVector;
            }

            return snapResult;
        }

        public static SnapResult calcSnapResult(List<GameObject> parents, Config snapConfig)
        {
            GameObjectEx.getAllObjectsInHierarchies(parents, false, false, _sourceObjectBuffer);

            SnapData snapData = new SnapData();
            snapData.minSnapDistance = float.MaxValue;

            // Setup the overlap filter to ignore the source objects and also any
            // objects that were specified via the snap config.
            _overlapFilter.layerMask = snapConfig.destinationLayers;
            _overlapFilter.setIgnoredObjects(_sourceObjectBuffer);

            OBB nearbyOverlapOBB = ObjectBounds.calcHierarchiesWorldOBB(parents, _nearbyGatherBoundsQConfig);
            nearbyOverlapOBB.inflate(snapConfig.snapRadius * 2.0f);
            PluginScene.instance.overlapBox(nearbyOverlapOBB, _overlapFilter, ObjectOverlapConfig.defaultConfig, _nearbyObjectBuffer);
            if (_nearbyObjectBuffer.Count == 0) return new SnapResult(SnapFailReson.NoDestinationFound);

            // Loop through all source objects
            foreach (var sourceObject in _sourceObjectBuffer)
            {
                // Retrieve the snap data which is needed for snapping. If the data is not available, we skip.
                var sourceSnapData = GameObjectDataDb.instance.getSnapData(sourceObject);
                if (sourceSnapData == null) continue;

                Transform srcTransform = sourceObject.transform;
                snapData.srcObject = sourceObject;

                // Loop through all snap faces in the current source object
                foreach (var srcSnapFace in _allSnapFaces)
                {
                    // Retrieve the area descriptor for the current snap face
                    var srcAreaDesc = sourceSnapData.getSnapSocketWorldAreaDesc(srcSnapFace, srcTransform);
                    if (srcAreaDesc.areaType == Box3DFaceAreaType.Invalid) continue;

                    // Calculate the snap socket OBB and socket face plane
                    var srcSnapSocketOBB = sourceSnapData.calcSnapSocketWorldOBB(srcSnapFace, srcTransform);
                    Plane srcSocketFacePlane = Box3D.calcFacePlane(srcSnapSocketOBB.center, srcSnapSocketOBB.size, srcSnapSocketOBB.rotation, srcSnapFace);
                   
                    snapData.srcSnapFace = srcSnapFace;
                   
                    // Extrude the socket face from center
                    OBB srcOverlapOBB = srcSnapSocketOBB.calcFaceExtrusionFromFaceCenter(srcSnapFace, snapConfig.snapRadius * 2.0f);
                    srcOverlapOBB.inflate(snapConfig.snapRadius * 2.0f, Box3D.getFaceAxisIndex(srcSnapFace));

                    // Loop through all nearby objects
                    foreach (var destObject in _nearbyObjectBuffer)
                    {
                        // Retrieve the snap data which is needed for snapping. If the data is not available, we skip.
                        var destSnapData = GameObjectDataDb.instance.getSnapData(destObject);
                        if (destSnapData == null) continue;

                        Transform destTransform = destObject.transform;
                        snapData.destObject = destObject;

                        // Loop through all snap faces in the current destination object
                        foreach (var destSnapFace in _allSnapFaces)
                        {
                            // Retrieve the area descriptor of the current snap face
                            var destAreaDesc = destSnapData.getSnapSocketWorldAreaDesc(destSnapFace, destTransform);
                            if (destAreaDesc.areaType == Box3DFaceAreaType.Invalid) continue;

                            // Extrude the destination face as we did for the source object
                            var destSnapSocketOBB = destSnapData.calcSnapSocketWorldOBB(destSnapFace, destTransform);
                            OBB destOverlapOBB = destSnapSocketOBB.calcFaceExtrusionFromFaceCenter(destSnapFace, snapConfig.snapRadius * 2.0f);
                            destOverlapOBB.inflate(snapConfig.snapRadius * 2.0f, Box3D.getFaceAxisIndex(destSnapFace));

                            // If the OBBs associated with the 2 extrusions (source and destination) do not overlap, it means 
                            // the current destination snap face does not reside with the snap radius so we can ignore it.
                            if (!destOverlapOBB.intersectsOBB(srcOverlapOBB)) continue;

                            snapData.destSnapFace = destSnapFace;
                            Plane destSocketFacePlane = Box3D.calcFacePlane(destSnapSocketOBB.center, destSnapSocketOBB.size, destSnapSocketOBB.rotation, destSnapFace);

                            // Find the closest points between the source and destination points
                            // that belong to the corner points of the source and destination socket OBBs.
                            srcSnapSocketOBB.calcCorners(_srcSocketPtBuffer, false);
                            destSnapSocketOBB.calcCorners(_destSocketPtBuffer, false);
                            foreach (var srcPt in _srcSocketPtBuffer)
                            {
                                foreach (var destPt in _destSocketPtBuffer)
                                {
                                    float d = (destPt - srcPt).magnitude;
                                    if (d <= snapConfig.snapRadius && d < snapData.minSnapDistance)
                                    {
                                        snapData.foundDestination = true;
                                        snapData.minSnapDistance = d;
                                        snapData.snapDistance = d;
                                        snapData.snapDestination = destPt;
                                        snapData.snapSource = srcPt;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return snapData.foundDestination ? new SnapResult(snapData.snapSource, snapData.snapDestination, snapData.snapDistance) :
                new SnapResult(SnapFailReson.NoDestinationFound);
        }
    }
}
#endif