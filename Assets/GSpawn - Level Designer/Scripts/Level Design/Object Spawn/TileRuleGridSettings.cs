#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class TileRuleGridSettings : PluginSettings<TileRuleGridSettings>
    {
        private static Vector3                  _minCellSize                    = new Vector3(1e-1f, 1e-1f, 1e-1f);

        [SerializeField]
        private string                          _tileRuleProfileName            = defaultTileRuleProfileName;
        [SerializeField]
        private TileRuleNeighborRadius          _tileRuleNeighborRadius         = defaultTileRuleNeighborRadius;
        [SerializeField]
        private Vector3                         _cellSize                       = defaultCellSize;
        [SerializeField]
        private Color                           _wireColor                      = defaultWireColor;
        [SerializeField]
        private Color                           _fillColor                      = defaultFillColor;

        public string                           tileRuleProfileName             { get { return _tileRuleProfileName; } set { UndoEx.record(this); _tileRuleProfileName = value; EditorUtility.SetDirty(this); } }
        public TileRuleProfile                  tileRuleProfile
        {
            get
            {
                var profile = TileRuleProfileDb.instance.findProfile(_tileRuleProfileName);
                if (profile == null) profile = TileRuleProfileDb.instance.defaultProfile;
                return profile;
            }
        }
        public TileRuleNeighborRadius           tileRuleNeighborRadius          { get { return _tileRuleNeighborRadius; } set { UndoEx.record(this); _tileRuleNeighborRadius = value; EditorUtility.SetDirty(this); } }
        public Vector3                          cellSize                        { get { return _cellSize; } set { UndoEx.record(this); _cellSize = Vector3.Max(value, _minCellSize); EditorUtility.SetDirty(this); } }
        public Color                            wireColor                       { get { return _wireColor; } set { UndoEx.record(this); _wireColor = value; EditorUtility.SetDirty(this); } }
        public Color                            fillColor                       { get { return _fillColor; } set { UndoEx.record(this); _fillColor = value; EditorUtility.SetDirty(this); } }

        public static string                    defaultTileRuleProfileName      { get { return TileRuleProfileDb.defaultProfileName; } }
        public static TileRuleNeighborRadius    defaultTileRuleNeighborRadius   { get { return TileRuleNeighborRadius.One; } }
        public static Vector3                   defaultCellSize                 { get { return Vector3.one; } }
        public static Color                     defaultWireColor                { get { return Color.black; } }
        public static Color                     defaultFillColor                { get { return Color.gray.createNewAlpha(0.0f); } }

        public static Vector3                   minCellSize                     { get { return _minCellSize; } } 

        public void copy(TileRuleGridSettings src)
        {
            if (src == this) return;

            _tileRuleProfileName    = src._tileRuleProfileName;
            tileRuleNeighborRadius  = src._tileRuleNeighborRadius;
            cellSize                = src.cellSize;
            wireColor               = src.wireColor;
            fillColor               = src.fillColor;

            EditorUtility.SetDirty(this);
        }

        public override void useDefaults()
        {
            _tileRuleProfileName    = defaultTileRuleProfileName;
            tileRuleNeighborRadius  = defaultTileRuleNeighborRadius;
            cellSize                = defaultCellSize;
            wireColor               = defaultWireColor;
            fillColor               = defaultFillColor;

            EditorUtility.SetDirty(this);
        }

        public void buildUI(VisualElement parent)
        {
            const float labelWidth = 150.0f;

            IMGUIContainer prefabProfileContainer = UI.createIMGUIContainer(parent);
            prefabProfileContainer.onGUIHandler = () =>
            {
                string newName = EditorUIEx.profileNameSelectionField<TileRuleProfileDb, TileRuleProfile>
                    (TileRuleProfileDb.instance, "Tile rule profile", labelWidth, _tileRuleProfileName);
                if (newName != _tileRuleProfileName)
                {
                    UndoEx.record(this);
                    _tileRuleProfileName = newName;
                    EditorUtility.SetDirty(this);
                }
            };

            var neighRadiusField    = UI.createEnumField(typeof(TileRuleNeighborRadius), "_tileRuleNeighborRadius", serializedObject, "Neighbor radius", 
                "This is the radius that will be used to check for adjacent tile neighbors.", parent);
            neighRadiusField.setChildLabelWidth(labelWidth);

            var cellSizeField   = UI.createVector3Field("_cellSize", serializedObject, "Cell size", "The grid cell size.", _minCellSize, parent);
            cellSizeField.setChildLabelWidth(labelWidth);

            var colorField      = UI.createColorField("_wireColor", serializedObject, "Wire color", "The grid cell line color.", parent);
            colorField.setChildLabelWidth(labelWidth);
            colorField          = UI.createColorField("_fillColor", serializedObject, "Fill color", "The grid cell area color.", parent);
            colorField.setChildLabelWidth(labelWidth);

            UI.createUseDefaultsButton(() => { useDefaults(); }, parent);
        }
    }
}
#endif