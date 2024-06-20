#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectToObjectSnapData
    {
        private static Box3DFace[]      _allBoxFaces        = Box3D.facesArrayCopy;
        private static List<AABB>       _overlapAABBBuffer  = new List<AABB>();
        private static List<Vector3>    _vector3Buffer      = new List<Vector3>();

        private AABB[]                  _snapSocketAABB     = new AABB[Enum.GetValues(typeof(Box3DFace)).Length];

        public static ObjectToObjectSnapData create(GameObject gameObject)
        {
            if (gameObject == null) return null;

            Mesh mesh       = gameObject.getMesh();
            Sprite sprite   = gameObject.getSprite();
            if (mesh == null && sprite == null) return null;

            PluginMesh pluginMesh = PluginMeshDb.instance.getPluginMesh(mesh);
            if (pluginMesh == null && sprite == null) return null;

            bool useMesh    = pluginMesh != null;
            AABB modelAABB  = useMesh ? pluginMesh.aabb : ObjectBounds.calcSpriteModelAABB(gameObject);
            calcOverlapAABBs(modelAABB, _overlapAABBBuffer);

            var snapData = new ObjectToObjectSnapData();
            if (useMesh)
            {
                foreach (var aabbFace in _allBoxFaces)
                {
                    ObjectVertexOverlap.overlapMeshModelVerts(gameObject, _overlapAABBBuffer[(int)aabbFace], _vector3Buffer);
                    Plane facePlane = Box3D.calcFacePlane(modelAABB.center, modelAABB.size, aabbFace);
                    facePlane.projectPoints(_vector3Buffer);
                    snapData._snapSocketAABB[(int)aabbFace] = new AABB(_vector3Buffer);
                }
            }
            else
            {
                foreach (var aabbFace in _allBoxFaces)
                {
                    if (aabbFace != Box3DFace.Front && aabbFace != Box3DFace.Back)
                    {
                        Box3D.calcFaceCorners(modelAABB.center, modelAABB.size, Quaternion.identity, aabbFace, _vector3Buffer);
                        snapData._snapSocketAABB[(int)aabbFace] = new AABB(_vector3Buffer);
                    }
                    else snapData._snapSocketAABB[(int)aabbFace] = AABB.getInvalid();
                }
            }

            return snapData;
        }

        public Box3DFaceAreaDesc getSnapSocketWorldAreaDesc(Box3DFace boxFace, Transform worldTransform)
        {
            return Box3D.getFaceAreaDesc(Vector3.Scale(_snapSocketAABB[(int)boxFace].size, worldTransform.lossyScale.abs()), boxFace);
        }

        public OBB calcSnapSocketWorldOBB(Box3DFace boxFace, Transform worldTransform)
        {
            return new OBB(_snapSocketAABB[(int)boxFace], worldTransform);
        }

        private static void calcOverlapAABBs(AABB modelAABB, List<AABB> overlapAABBs)
        {
            overlapAABBs.Clear();
            for (int boxFaceIndex = 0; boxFaceIndex < _allBoxFaces.Length; ++boxFaceIndex)
            {
                // Note: We have to GUESS an overlap size big enough to capture as much vertex data as possible, but not too big
                //       as to capture vertices which are irrelevant for the current face.
                //       When the vertices are collected, they will be projected on the face plane so the size only matters when collecting the verts.
                Vector3 faceNormal  = Box3D.calcFaceNormal(modelAABB.center, modelAABB.size, Quaternion.identity, _allBoxFaces[boxFaceIndex]);
                float overlapSize   = modelAABB.extents.absDot(faceNormal) * 0.1f;

                AABB overlapAABB    = modelAABB.calcInwardFaceExtrusion(_allBoxFaces[boxFaceIndex], overlapSize);
                overlapAABBs.Add(overlapAABB);
            }
        }
    }
}
#endif