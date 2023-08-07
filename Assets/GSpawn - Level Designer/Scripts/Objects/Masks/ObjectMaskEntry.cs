#if UNITY_EDITOR
using UnityEngine;
using System;

namespace GSpawn
{
    public class ObjectMaskEntry : ScriptableObject, IUIItemStateProvider
    {
        [SerializeField]
        private PluginGuid      _guid               = new PluginGuid(Guid.NewGuid());
        [SerializeField]
        private GameObject      _gameObject;
        [SerializeField]
        private bool            _uiSelected         = false;
        [NonSerialized]
        private CopyPasteMode   _uiCopyPasteMode    = CopyPasteMode.None;

        public PluginGuid       guid                { get { return _guid; } }
        public GameObject       gameObject          { get { return _gameObject; } set { _gameObject = value; } }
        public bool             uiSelected          { get { return _uiSelected; } set { UndoEx.record(this); _uiSelected = value; } }
        public CopyPasteMode    uiCopyPasteMode     { get { return _uiCopyPasteMode; } set { _uiCopyPasteMode = value; } }
    }
}
#endif