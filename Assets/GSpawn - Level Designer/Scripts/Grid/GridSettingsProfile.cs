#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
#if UNITY_2021
using UnityEditor.UIElements;
#endif

namespace GSpawn
{
    public enum GridOrientation
    {
        XY = 0,
        YZ,
        ZX,
        [Obsolete]
        Custom
    }

    public class GridSettingsProfile : Profile
    {
        [SerializeField]
        private GridOrientation         _orientation                = defaultOrientation;
        [SerializeField]
        private Vector3                 _customRotation             = defaultCustomRotation;
        [SerializeField]
        private float                   _localOriginYOffset         = defaultLocalOriginYOffset;
        [SerializeField]
        private Vector3                 _cellSize                   = defaultCellSize;

        private SerializedObject        _serializedObject;
        private SerializedObject        serializedObject
        {
            get
            {
                if (_serializedObject == null) _serializedObject = new SerializedObject(this);
                return _serializedObject;
            }
        }

        public GridOrientation          orientation                 { get { return _orientation; } set { UndoEx.record(this); _orientation = value; EditorUtility.SetDirty(this); } }
        public Vector3                  customRotation              { get { return _customRotation; } set { UndoEx.record(this); _customRotation = value; EditorUtility.SetDirty(this); } }
        public float                    localOriginYOffset          { get { return _localOriginYOffset; } set { UndoEx.record(this); _localOriginYOffset = value; EditorUtility.SetDirty(this); } }
        public Vector3                  localOriginOffset           { get { return new Vector3(0.0f, localOriginYOffset, 0.0f); } }
        public Vector3                  cellSize                    { get { return _cellSize; } set { UndoEx.record(this); _cellSize = Vector3.Max(value, Vector3Ex.create(DefaultSystemValues.minGridCellSize)); EditorUtility.SetDirty(this); } }
        public float                    cellSizeX                   { get { return _cellSize.x; } set { UndoEx.record(this); _cellSize.x = Mathf.Max(value, DefaultSystemValues.minGridCellSize); EditorUtility.SetDirty(this); } }
        public float                    cellSizeY                   { get { return _cellSize.y; } set { UndoEx.record(this); _cellSize.y = Mathf.Max(value, DefaultSystemValues.minGridCellSize); EditorUtility.SetDirty(this); } }
        public float                    cellSizeZ                   { get { return _cellSize.z; } set { UndoEx.record(this); _cellSize.z = Mathf.Max(value, DefaultSystemValues.minGridCellSize); EditorUtility.SetDirty(this); } }

        public static GridOrientation   defaultOrientation          { get { return GridOrientation.ZX; } }
        public static Vector3           defaultCustomRotation       { get { return Vector3.zero; } }
        public static float             defaultLocalOriginYOffset   { get { return 0.0f; } }
        public static Vector3           defaultCellSize             { get { return Vector3.one; } }

        #pragma warning disable 0612
        public Quaternion getOrientationRotation()
        {
            if (orientation == GridOrientation.Custom)  return Quaternion.Euler(customRotation);
            else if (orientation == GridOrientation.ZX) return Quaternion.identity;
            else if (orientation == GridOrientation.XY) return Quaternion.AngleAxis(-90.0f, Vector3.right);
            else return Quaternion.AngleAxis(-90.0f, Vector3.forward);
        }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 150.0f;

            Vector3Field customRotationField = null;
            EnumField orientationField = UI.createEnumField(typeof(GridOrientation), "_orientation", serializedObject, "Orientation", 
                "The grid orientation.", parent);
            orientationField.setChildLabelWidth(labelWidth);
            orientationField.RegisterValueChangedCallback(p => 
            {
                customRotationField.setDisplayVisible(orientation == GridOrientation.Custom);
            });

            customRotationField = UI.createVector3Field("_customRotation", serializedObject, "Custom rotation", 
                "The custom grid rotation expressed in degrees.", parent);
            customRotationField.setChildLabelWidth(labelWidth);
            customRotationField.setDisplayVisible(orientation == GridOrientation.Custom);

            VisualElement ctrl = UI.createFloatField("_localOriginYOffset", serializedObject, "Local Y offset", 
                "Allows you to specify an offset for the grid origin along its local Y axis.", parent);
            ctrl.setChildLabelWidth(labelWidth);
            ctrl = UI.createVector3Field("_cellSize", serializedObject, "Cell size", "The grid cell size.", parent);
            ctrl.setChildLabelWidth(labelWidth);

            UI.createUseDefaultsButton(() => useDefaults(), parent);
        }

        public void useDefaults()
        {
            orientation         = defaultOrientation;
            customRotation      = defaultCustomRotation;
            localOriginYOffset  = defaultLocalOriginYOffset;
            cellSize            = defaultCellSize;

            EditorUtility.SetDirty(this);
        }
        #pragma warning restore 0612
    }
}
#endif