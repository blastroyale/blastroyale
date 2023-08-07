#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public class ObjectOutline
    {
        private List<GameObject>    _objectGather   = new List<GameObject>();
        private GameObject[]        _objectArray    = new GameObject[1];

        public List<GameObject>     objectGather    { get { return _objectGather; } }

        public void setGatherObjects(List<GameObject> gameObjects)
        {
            _objectGather.Clear();
            _objectGather.AddRange(gameObjects);
        }

/*
        public void drawHandlesIndividually(Color highlightColor)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (_objectGather.Count == 0) return;

            if (_objectArray.Length != 1) _objectArray = new GameObject[1];
            foreach (var go in _objectGather)
            {
                _objectArray[0] = go;
                Handles.DrawOutline(_objectArray, highlightColor, 0.0f);
            }
        }*/

        public void drawHandles(Color highlightColor)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (_objectGather.Count == 0) return;

#if UNITY_2022_1
            if (_objectArray.Length != _objectGather.Count) _objectArray = new GameObject[_objectGather.Count];
            _objectGather.CopyTo(_objectArray);

            Handles.DrawOutline(_objectArray, highlightColor, 0.0f);
#else
            int numObjects = _objectGather.Count;
            for (int i = 0; i < numObjects; ++i)
            {
                GameObject go = _objectGather[i];
                Mesh mesh     = go.getMesh();
                if (mesh != null) HandlesEx.drawMeshWireTriangles(mesh, go.transform, highlightColor);
                else
                {
                    Sprite sprite = go.getSprite();
                    if (sprite != null) HandlesEx.drawSpriteWireTriangles(sprite, go.transform, highlightColor);
                }
            }
#endif
        }

        private List<GameObject> _parentBuffer  = new List<GameObject>();
        private List<GameObject> _childBuffer   = new List<GameObject>();
        public void drawHandles(Color parentColor, Color childColor, float opacity)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (_objectGather.Count == 0) return;

#if UNITY_2022_1
            if (_objectArray.Length != _objectGather.Count) _objectArray = new GameObject[_objectGather.Count];
            _objectGather.CopyTo(_objectArray);

            // Note: Use the overload which accepts a GameObject array. The overload which accepts List<GameObject> is buggy.
            //       Renderer array also works, but is not suitable for cases where the selection contains empty game objects
            //       with children that have renderers. Haven't tried the others.
            Handles.DrawOutline(_objectArray, parentColor, childColor, opacity);
#else
            GameObjectEx.getParents(_objectGather, _parentBuffer);
            int numParents = _parentBuffer.Count;
            for (int i = 0; i < numParents; ++i)
            {
                GameObject parent   = _parentBuffer[i];
                Mesh mesh           = parent.getMesh();
                if (mesh != null) HandlesEx.drawMeshWireTriangles(mesh, parent.transform, parentColor);
                else
                {
                    Sprite sprite   = parent.getSprite();
                    if (sprite != null) HandlesEx.drawSpriteWireTriangles(sprite, parent.transform, parentColor);
                }

                parent.getAllChildren(false, false, _childBuffer);
                int numChildren = _childBuffer.Count;
                for (int j = 0; j < numChildren; ++j) 
                {
                    GameObject child    = _childBuffer[j];
                    mesh                = parent.getMesh();
                    if (mesh != null) HandlesEx.drawMeshWireTriangles(mesh, child.transform, childColor);
                    else
                    {
                        Sprite sprite   = child.getSprite();
                        if (sprite != null) HandlesEx.drawSpriteWireTriangles(sprite, child.transform, childColor);
                    }
                }
            }
#endif
        }
    }
}
#endif