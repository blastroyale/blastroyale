#if UNITY_EDITOR
namespace GSpawn
{
    public static class GlobalShortcutNames
    {
        public static string objectSpawn                                { get { return "Object Spawn"; } }
        public static string objectSelection                            { get { return "Object Selection"; } }
        public static string objectErase                                { get { return "Object Erase"; } }
        public static string grid_VerticalStepUp                        { get { return "Grid/Vertical Step Up"; } }
        public static string grid_VerticalStepDown                      { get { return "Grid/Vertical Step Down"; } }
        public static string grid_SnapToPickedObject                    { get { return "Grid/Snap to Picked Object"; } }
        public static string transform_RotateAroundX                    { get { return "Transform/Rotate Around X"; } }
        public static string transform_RotateAroundY                    { get { return "Transform/Rotate Around Y"; } }
        public static string transform_RotateAroundZ                    { get { return "Transform/Rotate Around Z"; } }
        public static string transform_RotateAroundXAroundCenter        { get { return "Transform/Rotate Around X Around Center"; } }
        public static string transform_RotateAroundYAroundCenter        { get { return "Transform/Rotate Around Y Around Center"; } }
        public static string transform_RotateAroundZAroundCenter        { get { return "Transform/Rotate Around Z Around Center"; } }
        public static string transform_ResetRotationToOriginal          { get { return "Transform/Reset Rotation to Original"; } }
        public static string transform_ResetScaleToOriginal             { get { return "Transform/Reset Scale to Original"; } }
        public static string mirrorGizmo_Toggle                         { get { return "Mirror Gizmo/Toggle"; } }
        public static string mirrorGizmo_SnapToView                     { get { return "Mirror Gizmo/Snap to View"; } }
        public static string selection_FrameSelected                    { get { return "Selection/Frame Selected"; } }
        public static string selection_DeleteSelected                   { get { return "Selection/Delete Selected"; } }
        public static string selection_DuplicateSelected                { get { return "Selection/Duplicate Selected"; } }
    }

    public static class ObjectTransformSessionsShortcutNames
    {
        public static string modularSnap_VerticalStepUp                 { get { return "Modular Snap/Vertical Step Up"; } }
        public static string modularSnap_VerticalStepDown               { get { return "Modular Snap/Vertical Step Down"; } }
        public static string modularSnap_ResetVerticalStep              { get { return "Modular Snap/Reset Vertical Step"; } }
        public static string modularSnap_ResetVerticalStepToOriginal    { get { return "Modular Snap/Reset Vertical Step to Original"; } }
        public static string modularSnap_ToggleHalfSpace                { get { return "Modular Snap/Toggle Half-Space"; } }
        public static string modularSnap_ToggleObject2ObjectSnap        { get { return "Modular Snap/Toggle Object-to-Object Snap"; } }
        public static string modularSnap_ToggleGridSnapClimb            { get { return "Modular Snap/Toggle Grid Snap Climb"; } }
        public static string modularSnap_ToggleAlignmentHighlights      { get { return "Modular Snap/Toggle Alignment Highlights"; } }
        public static string modularSnap_ToggleAlignmentHints           { get { return "Modular Snap/Toggle Alignment Hints"; } }
        public static string surfaceSnap_ResetMouseOffsetFromSurface    { get { return "Surface Snap/Reset Mouse Offset from Surface"; } }
        public static string surfaceSnap_ToggleAxisAlignment            { get { return "Surface Snap/Toggle Axis Alignment"; } }
    }

    public static class ObjectSpawnShortcutNames
    {
        public static string spawnGuide_SyncGridCellSize                { get { return "Spawn Guide/Sync Grid Cell Size"; } }
        public static string spawnGuide_ToggleDecorRules                { get { return "Spawn Guide/Toggle Decor Rules"; } }
        public static string tileRuleSpawn_Paint                        { get { return "Tile Rule Spawn/Paint"; } }
        public static string tileRuleSpawn_Ramp                         { get { return "Tile Rule Spawn/Ramp"; } }
        public static string tileRuleSpawn_Erase                        { get { return "Tile Rule Spawn/Erase"; } }
        public static string tileRuleSpawn_Connect                      { get { return "Tile Rule Spawn/Connect"; } }
        public static string tileRuleSpawn_Connect_ChangeMajorAxis      { get { return "Tile Rule Spawn/Connect/Change Major Axis"; } }
        public static string tileRuleSpawn_BoxBrush                     { get { return "Tile Rule Spawn/Box Brush"; } }
        public static string tileRuleSpawn_FlexiBoxBrush                { get { return "Tile Rule Spawn/Flexi Box Brush"; } }
        public static string tileRuleSpawn_SegmentsBrush                { get { return "Tile Rule Spawn/Segments Brush"; } }
        public static string curveSpawn_SelectAllControlPoints          { get { return "Curve Spawn/Select All Control Points"; } }
        public static string curveSpawn_InsertControlPoint              { get { return "Curve Spawn/Insert Control Point"; } }
        public static string curveSpawn_ProjectSelectedControlPoints    { get { return "Curve Spawn/Project Selected Control Points"; } }
        public static string curveSpawn_MoveGizmo                       { get { return "Curve Spawn/Move Gizmo"; } }
        public static string curveSpawn_RotationGizmo                   { get { return "Curve Spawn/Rotation Gizmo"; } }
        public static string curveSpawn_ScaleGizmo                      { get { return "Curve Spawn/Scale Gizmo"; } }
    }

    public static class ObjectSelectionShortcutNames
    {
        public static string snapAllAxes                                { get { return "Snap All Axes"; } }
        public static string selectSimilarPrefabs                       { get { return "Select Similar Prefabs"; } }
        public static string selectPrefabsInManager                     { get { return "Select Prefabs in Manager"; } }
        public static string projectOnGrid                              { get { return "Project On Grid"; } }
        public static string projectOnObject                            { get { return "Project On Object"; } }
        public static string moveGizmo                                  { get { return "Move Gizmo"; } }
        public static string rotationGizmo                              { get { return "Rotation Gizmo"; } }
        public static string scaleGizmo                                 { get { return "Scale Gizmo"; } }
        public static string universalGizmo                             { get { return "Universal Gizmo"; } }
        public static string extrudeGizmo                               { get { return "Extrude Gizmo"; } }
        public static string selectionRect                              { get { return "Selection Rectangle"; } }
        public static string selectionSegments                          { get { return "Selection Segments"; } }
        public static string selectionBox                               { get { return "Selection Box"; } }
        public static string mirrorSelected                             { get { return "Mirror Selected"; } }
        public static string vertexSnap                                 { get { return "Vertex Snap"; } }
        public static string boxSnap                                    { get { return "Box Snap"; } }
        public static string surfaceSnap                                { get { return "Surface Snap"; } }
        public static string modularSnap                                { get { return "Modular Snap"; } }
        public static string filterOutOfView                            { get { return "Filter Out of View"; } }
        public static string grow                                       { get { return "Grow"; } }
    }

    public static class ObjectEraseShortcutNames
    {
        public static string eraseCursor                                { get { return "Erase Cursor"; } }
        public static string eraseBrush2D                               { get { return "Erase Brush 2D"; } }
        public static string eraseBrush3D                               { get { return "Erase Brush 3D"; } }
    }
}
#endif