#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public static class FixedShortcuts
    {
        public static double _modifierDelay = 0.1;

        public static bool grid_enableChangeCellSize(Event e)
        {
             return e.shift && e.control && e.alt && !e.command;
        }

        public static bool selection_ReplaceOnClick(Event e)
        {
            return e.clickCount == 1 && e.altStrict() && e.button == (int)MouseButton.LeftMouse;
        }

        public static bool selection_EnableAppend(Event e)
        {
            return e.controlStrict();
        }

        public static bool selection_EnableMultiDeselect(Event e)
        {
            return e.shiftStrict();
        }

        public static bool modularSnap_EnableLockSnapAxis(Event e)
        {
            return e.control && !e.alt && !e.command;
        }

        public static bool modularSnap_EnableLockSnapAxisInvert(Event e)
        {
            return e.shift && !e.alt && !e.command;
        }

        public static bool modularSnap_VerticalStep_ScrollWheel(Event e)
        {
            return e.shift && e.control && !e.alt && !e.command && e.isScrollWheel;
        }

        public static bool modularSnap_Rotate_ScrollWheel(Event e)
        {
            return e.shift && !e.control && !e.alt && !e.command && e.isScrollWheel;
        }

        public static bool selectionSegments_EnableStepBack(Event e)
        {
            return e.shiftStrict();
        }

        public static bool structureBuild_EnableCommitOnLeftClick(Event e)
        {
            return e.isLeftMouseButtonDownEvent() && e.shiftStrict();
        }

        public static bool objectSpawnStructure_UpdateHeightByScrollWheel(Event e)
        {
            return e.shiftStrict() && e.isScrollWheel;
        }

        public static bool boxObjectSpawn_ChangePatternDirectionByScrollWheel(Event e)
        {
            return e.altStrict() && e.isScrollWheel;
        }

        public static bool boxObjectSpawn_EnableEqualSize(Event e)
        {
            return e.controlStrict();
        }

        public static bool curveObjectSpawn_EnableControlPointSnapToGrid(Event e)
        {
            return e.controlStrict();
        }

        public static bool enableMouseRotateObjects(Event e)
        {
            return e.shiftStrict() /*&& Mouse.instance.noButtonsDown*/;
        }

        public static bool checkMouseRotateDelay()
        {
            return Keyboard.instance.shiftTimeSinceDown > _modifierDelay;
        }

        public static bool enableMouseScaleObjects(Event e)
        {
            return e.controlStrict() /*&& Mouse.instance.noButtonsDown*/;
        }

        public static bool checkMouseScaleDelay()
        {
            return Keyboard.instance.ctrlTimeSinceDown > _modifierDelay;
        }

        public static bool enableMouseOffsetFromSurface(Event e)
        {
            return e.altStrict() && Mouse.instance.noButtonsDown;
        }

        public static bool checkMouseOffsetFromSurfaceDelay()
        {
            return Keyboard.instance.altTimeSinceDown > _modifierDelay;
        }

        public static bool enableMouseOrbitAroundPoint(Event e)
        {
            return e.control && e.shift && !e.alt && !e.command && Mouse.instance.noButtonsDown;
        }

        public static bool checkMouseOrbitDelay()
        {
            return Keyboard.instance.ctrlTimeSinceDown > _modifierDelay &&
                    Keyboard.instance.shiftTimeSinceDown > _modifierDelay;
        }

        public static bool enableMouseAdjustAnchor(Event e)
        {
            return e.control && e.alt && !e.shift && !e.command && Mouse.instance.noButtonsDown;
        }

        public static bool checkMouseAdjustAnchorDelay()
        {
            return Keyboard.instance.ctrlTimeSinceDown > _modifierDelay &&
                    Keyboard.instance.altTimeSinceDown > _modifierDelay;
        }

        public static bool enableMouseOffsetFromPoint(Event e)
        {
            return e.shift && e.alt && !e.control && !e.command && Mouse.instance.noButtonsDown;
        }

        public static bool checkMouseOffsetFromPointDelay()
        {
            return Keyboard.instance.shiftTimeSinceDown > _modifierDelay &&
                    Keyboard.instance.altTimeSinceDown > _modifierDelay;
        }

        public static bool cancelAction(Event e)
        {
            return e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape;
        }

        public static bool cancelAction(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.Escape;
        }

        public static bool enableSnapAllAxes(Event e)
        {
            return e.shiftStrict();
        }

        public static bool changeDecorRuleIndex(Event e)
        {
            return e.altStrict() && e.isScrollWheel;
        }

        public static bool changeRadiusByScrollWheel(Event e)
        {
            return e.controlStrict() && e.isScrollWheel;
        }

        public static bool changeHeightByScrollWheel(Event e)
        {
            return e.shiftStrict() && e.isScrollWheel;
        }

        public static bool changeOffsetByScrollWheel(Event e)
        {
            return e.control && e.alt && !e.command && !e.shift && e.isScrollWheel;
        }

        public static bool pickYOffsetOnClick(Event e)
        {
            return e.isLeftMouseButtonDownEvent() && e.control && e.alt && !e.command && !e.shift;
        }

        public static bool extensionPlane_ChangeByScrollWheel(Event e)
        {
            return e.controlStrict() && e.isScrollWheel;
        }

        public static bool extensionPlane_EnablePickOnClick(Event e)
        {
            return e.control && e.alt && !e.command && !e.shift;
        }

        public static bool eraseCullPlaneToggle(Event e)
        {
            return e.shiftStrict();
        }

        public static bool enablePickSpawnGuidePrefabFromScene(Event e)
        {
            return e.altStrict();
        }

        public static bool ui_BeginRename(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.F2;
        }

        public static bool ui_DropAsCopy(Event e)
        {
            return e.altStrict();
        }

        public static bool ui_Copy(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.C && e.controlStrict();
        }

        public static bool ui_Cut(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.X && e.controlStrict();
        }

        public static bool ui_Paste(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.V && e.controlStrict();
        }

        public static bool ui_SpawnPrefabOnDblClick(MouseDownEvent e)
        {
            return e.clickCount == 2 && e.noShiftCtrlCmdAlt() && e.button == (int)MouseButton.LeftMouse;
        }

        public static bool ui_ReplaceSelectionOnClick(MouseDownEvent e)
        {
            return e.clickCount == 1 && e.altKey && e.noShiftCtrlCmd() && e.button == (int)MouseButton.LeftMouse;
        }

        public static bool ui_selection_ReplaceWithSelectedPrefabsInManager(MouseDownEvent e)
        {
            return e.clickCount == 1 && e.altKey && e.ctrlKey && !e.shiftKey && !e.commandKey && e.button == (int)MouseButton.LeftMouse;
        }

        public static bool ui_BeginPreviewRotationOnClick(MouseDownEvent e)
        {
            return e.clickCount == 1 && e.noShiftCtrlCmdAlt() && e.button == (int)MouseButton.MiddleMouse;
        }

        public static bool ui_BeginHotkeyAssignmentOnDblClick(MouseDownEvent e)
        {
            return e.clickCount == 2 && e.noShiftCtrlCmdAlt() && e.button == (int)MouseButton.LeftMouse;
        }

        public static bool ui_BeginItemRenameOnClick(MouseDownEvent e)
        {
            return e.clickCount == 1 && e.noShiftCtrlCmdAlt() && e.button == (int)MouseButton.LeftMouse;
        }

        public static bool ui_DeleteSelected(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.Delete && e.noShiftCtrlCmdAlt();
        }

        public static bool ui_DuplicateSelected(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.D && e.noShiftCmdAlt() && e.ctrlKey;
        }

        public static bool ui_DetachSelectedFromParents(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.D && e.noShiftCtrlCmdAlt();
        }

        public static bool ui_TreeViewPlaceItemsAbove(DragPerformEvent e)
        {
            return e.altStrict();
        }

        public static bool ui_SelectUp(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.UpArrow;
        }

        public static bool ui_SelectDown(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.DownArrow;
        }

        public static bool ui_SelectLeft(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.LeftArrow;
        }

        public static bool ui_SelectRight(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.RightArrow;
        }

        public static bool ui_EnableDirectionalSelectAppend(KeyDownEvent e)
        {
            return e.shiftKey || e.ctrlKey && e.noCmdAlt();
        }

        public static bool ui_SelectAll(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.A && e.noShiftCmdAlt() && e.ctrlKey;
        }

        public static bool ui_ExpandSelected(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.W && e.noShiftCtrlCmdAlt();
        }

        public static bool ui_CollapseSelected(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.Q && e.noShiftCtrlCmdAlt();
        }

        public static bool ui_CreateChildrenForSelected(KeyDownEvent e)
        {
            return e.keyCode == KeyCode.N && e.noShiftCtrlCmdAlt();
        }

        public static bool ui_EnableClearAll()
        {
            return Event.current.shiftStrict();
        }

        public static bool ui_EnableClearAllOnMouseDown(MouseDownEvent e)
        {
            return e.shiftStrict();
        }

        public static bool ui_EnableClearAllOnMouseUp(MouseUpEvent e)
        {
            return e.shiftStrict();
        }
    }
}
#endif