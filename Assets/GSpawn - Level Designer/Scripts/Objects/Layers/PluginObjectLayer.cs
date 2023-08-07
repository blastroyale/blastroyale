#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace GSpawn
{
    public class PluginObjectLayer : ScriptableObject, IUIItemStateProvider
    {
        [SerializeField]
        private PluginGuid      _guid               = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private int             _layer;
        [SerializeField]
        private bool            _isErasable         = true;
        [SerializeField]
        private bool            _isTerrainMesh      = false;
        [SerializeField]
        private bool            _isSphericalMesh    = false;
        [SerializeField]
        private bool            _uiSelected         = false;
        [NonSerialized]
        private CopyPasteMode   _uiCopyPasteMode    = CopyPasteMode.None;

        public string           layerName           { get { return LayerMask.LayerToName(_layer); } }
        public bool             hasName             { get { return !string.IsNullOrEmpty(layerName); } }
        public int              layerIndex          { get { return _layer; } set { _layer = Mathf.Clamp(value, LayerEx.getMinlayer(), LayerEx.getMaxLayer()); } }
        public bool             isErasable          { get { return _isErasable; } set { UndoEx.record(this); _isErasable = value; EditorUtility.SetDirty(this); } }
        public bool             isTerrainMesh 
        { 
            get { return _isTerrainMesh; }
            set 
            { 
                if (value != _isTerrainMesh)
                {
                    UndoEx.record(this);
                    _isTerrainMesh = value;
                    if (_isTerrainMesh) _isSphericalMesh = false;
                    EditorUtility.SetDirty(this);

                    PluginScene.instance.onObjectLayerChangedTerrainMeshStatus(this);
                }
            } 
        }
        public bool             isSphericalMesh 
        { 
            get { return _isSphericalMesh; }
            set 
            { 
                if (value != _isSphericalMesh)
                {
                    UndoEx.record(this);
                    _isSphericalMesh = value;

                    bool wasTerrainMesh = _isTerrainMesh;
                    if (_isSphericalMesh) _isTerrainMesh = false;
                    EditorUtility.SetDirty(this);

                    if (wasTerrainMesh && !_isTerrainMesh) PluginScene.instance.onObjectLayerChangedTerrainMeshStatus(this);
                }
            } 
        }
        public PluginGuid       guid                { get { return _guid; } }
        public bool             uiSelected          { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public CopyPasteMode    uiCopyPasteMode     { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }
    }
}
#endif