#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace GSpawn
{
    public class ObjectErase : ScriptableObject
    {
        [NonSerialized]
        private ObjectEraseCursorSettings   _eraseCursorSettings;
        [NonSerialized]
        private ObjectEraseBrush2DSettings  _eraseBrush2DSettings;
        [NonSerialized]
        private ObjectEraseBrush3DSettings  _eraseBrush3DSettings;

        [SerializeField]
        private ObjectMask                  _eraseMask;

        [SerializeField]
        private ObjectEraseToolId           _activeToolId       = ObjectEraseToolId.Cursor;
        [NonSerialized]
        private ObjectEraseTool[]           _eraseTools         = new ObjectEraseTool[] { new ObjectEraseCursor(), new ObjectEraseBrush2D(), new ObjectEraseBrush3D() };
     
        public ObjectEraseCursorSettings    eraseCursorSettings
        {
            get
            {
                if (_eraseCursorSettings == null) _eraseCursorSettings = AssetDbEx.loadScriptableObject<ObjectEraseCursorSettings>(PluginFolders.settings);
                return _eraseCursorSettings;
            }
        }
        public ObjectEraseBrush2DSettings   eraseBrush2DSettings
        {
            get
            {
                if (_eraseBrush2DSettings == null) _eraseBrush2DSettings = AssetDbEx.loadScriptableObject<ObjectEraseBrush2DSettings>(PluginFolders.settings);
                return _eraseBrush2DSettings;
            }
        }
        public ObjectEraseBrush3DSettings   eraseBrush3DSettings
        {
            get
            {
                if (_eraseBrush3DSettings == null) _eraseBrush3DSettings = AssetDbEx.loadScriptableObject<ObjectEraseBrush3DSettings>(PluginFolders.settings);
                return _eraseBrush3DSettings;
            }
        }
        public ObjectMask                   eraseMask       { get { if (_eraseMask == null) _eraseMask = ScriptableObject.CreateInstance<ObjectMask>(); return _eraseMask; } }
        public ObjectEraseToolId            activeToolId    { get { return _activeToolId; } set { _activeToolId = value; PluginInspectorUI.instance.refresh(); } }

        public static ObjectErase           instance        { get { return GSpawn.active.objectErase; } }

        public bool canEraseObject(GameObject gameObject)
        {
            if (canErase(gameObject))
            {           
                GameObject outerInstance = gameObject.getOutermostPrefabInstanceRoot();
                if (outerInstance != null) return canErase(outerInstance);
                else return true;
            }
            else return false;
        }

        public void onSceneGUI()
        {
            getEraseTool().onSceneGUI();
        }

        private ObjectEraseTool getEraseTool()
        {
            return _eraseTools[(int)_activeToolId];
        }

        private bool canErase(GameObject gameObject)
        {
            if (gameObject == null || PluginInstanceData.instance.isPlugin(gameObject)) return false;
            if (!PluginObjectLayerDb.instance.getLayer(gameObject.layer).isErasable) return false;
            if (gameObject.isTerrainMesh() || gameObject.isSphericalMesh()) return false;
            if (SceneVisibilityManager.instance.IsPickingDisabled(gameObject, false)) return false;
            if (LayerEx.isLayerHidden(gameObject.layer) /*|| LayerEx.isPickingDisabled(gameObject.layer)*/) return false;
            if (SceneVisibilityManager.instance.IsHidden(gameObject, false) /*|| SceneVisibilityManager.instance.IsPickingDisabled(gameObject, false)*/) return false;
            if (eraseMask.isObjectMasked(gameObject)) return false;
            if (ObjectGroupDb.instance.isObjectGroup(gameObject)) return false;
            if (TileRuleGridDb.instance.isObjectChildOfTileRuleGrid(gameObject)) return false;

            //GameObjectType gameObjectType = GameObjectDataDb.instance.gameObjectType(gameObject);
            //if (gameObjectType != GameObjectType.Mesh && gameObjectType != GameObjectType.Sprite) return false;

            return true;
        }

        private void OnDestroy()
        {
            if (_eraseMask != null) UndoEx.destroyObjectImmediate(_eraseMask);
        }
    }
}
#endif