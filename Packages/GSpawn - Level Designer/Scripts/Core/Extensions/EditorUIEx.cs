#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GSpawn
{
    public static class EditorUIEx
    {
        private static Stack<float>     _labelWidthStack        = new Stack<float>();
        private static Stack<bool>      _showMixedValueStack    = new Stack<bool>();
        private static Stack<bool>      _wideModeStack          = new Stack<bool>(); 

        private static string[]         _axesLabels             = new string[] { "X", "Y", "Z" };
        private static List<string>     _stringBuffer           = new List<string>();

        public struct Vector3FieldResult
        {
            public Vector3          newValue;
            public float            newAxisValue;
            public int              changedAxisIndex;
            public bool             hasChanged;

            public static readonly 
                Vector3FieldResult  defaultResult = new Vector3FieldResult()
            {
                newValue            = Vector3.zero,
                newAxisValue        = 0.0f,
                changedAxisIndex    = -1,
                hasChanged          = false,
            };
        }

        public static void saveLabelWidth()
        {
            _labelWidthStack.Push(EditorGUIUtility.labelWidth);
        }

        public static void restoreLabelWidth()
        {
            if (_labelWidthStack.Count != 0) EditorGUIUtility.labelWidth = _labelWidthStack.Pop();
        }

        public static void saveShowMixedValue()
        {
            _showMixedValueStack.Push(EditorGUI.showMixedValue);
        }

        public static void restoreShowMixedValue()
        {
            if (_showMixedValueStack.Count != 0) EditorGUI.showMixedValue = _showMixedValueStack.Pop();
        }

        public static void saveWideMode()
        {
            _wideModeStack.Push(EditorGUIUtility.wideMode);
        }

        public static void restoreWideMode()
        {
            if (_wideModeStack.Count != 0) EditorGUIUtility.wideMode = _wideModeStack.Pop();
        }

        public static void objectMirrorGizmoPlaneToggle(ObjectMirrorGizmoSettings settings)
        {
            const float colorQuadWidth = 20.0f;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            bool usePlane = EditorGUILayout.Toggle(string.Empty, settings.useXYPlane, GUILayout.Width(UIValues.inlineToggleWidth));
            if (EditorGUI.EndChangeCheck())
            {
                settings.useXYPlane = usePlane;
                SceneView.RepaintAll();
            }
            Rect rect   = GUILayoutUtility.GetLastRect();
            rect.x      += 20.0f;
            rect.width  = colorQuadWidth;
            GUIEx.saveColor();
            GUI.color   = GizmoPrefs.instance.mirrorXYPlaneColor;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUIEx.restoreColor();

            EditorGUI.BeginChangeCheck();
            usePlane = EditorGUILayout.Toggle(string.Empty, settings.useYZPlane, GUILayout.Width(UIValues.inlineToggleWidth));
            if (EditorGUI.EndChangeCheck())
            {
                settings.useYZPlane = usePlane;
                SceneView.RepaintAll();
            }
            rect            = GUILayoutUtility.GetLastRect();
            rect.x          += 20.0f;
            rect.width      = colorQuadWidth;
            GUIEx.saveColor();
            GUI.color       = GizmoPrefs.instance.mirrorYZPlaneColor;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUIEx.restoreColor();

            EditorGUI.BeginChangeCheck();
            usePlane = EditorGUILayout.Toggle(string.Empty, settings.useZXPlane);
            if (EditorGUI.EndChangeCheck())
            {
                settings.useZXPlane = usePlane;
                SceneView.RepaintAll();
            }
            rect        = GUILayoutUtility.GetLastRect();
            rect.x      += 20.0f;
            rect.width  = colorQuadWidth;
            GUIEx.saveColor();
            GUI.color   = GizmoPrefs.instance.mirrorZXPlaneColor;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUIEx.restoreColor();
            EditorGUILayout.EndHorizontal();
        }

        public static IntPattern intPatternSelectionField(string label, float labelWidth, IntPattern selectedPattern)
        {
            int selectedIndex = IntPatternDb.instance.indexOf(selectedPattern);
            if (selectedIndex == -1)
            {
                selectedPattern = IntPatternDb.instance.defaultPattern;
                selectedIndex = IntPatternDb.instance.indexOf(selectedPattern);
            }

            IntPattern result = selectedPattern;
            IntPatternDb.instance.getPatternNames(_stringBuffer, null);

            saveLabelWidth();
            EditorGUIUtility.labelWidth = labelWidth;
            int newIndex = EditorGUILayout.Popup(label, selectedIndex, _stringBuffer.ToArray());
            if (newIndex != selectedIndex) result = IntPatternDb.instance.findPattern(_stringBuffer[newIndex]);
            restoreLabelWidth();

            return result;
        }

        public static string profileNameSelectionField<TProfileDb, TProfile>(TProfileDb profileDb, string label, 
            float labelWidth, string selectedName, bool showMixedValue = false) 
            where TProfileDb : ProfileDb<TProfile>
            where TProfile : Profile
        {
            string result = selectedName;

            profileDb.getProfileNames(_stringBuffer, null);
            int selectedIndex = _stringBuffer.IndexOf(selectedName);
            if (selectedIndex < 0)
            {
                if (showMixedValue)
                {
                    // Note: This is needed for situations in which multiple items are selected
                    //       and the user needs to specify a profile for the selection. In that
                    //       case a dummy string will be specified to allow for selection changes
                    //       to occur. So add the dummy string in the buffer and pretend it's OK :)
                    _stringBuffer.Add(selectedName);
                    selectedIndex = _stringBuffer.Count - 1;
                }
                else
                {
                    selectedIndex = 0;
                    result = _stringBuffer[0];
                }
            }

            saveLabelWidth();
            saveShowMixedValue();

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.showMixedValue    = showMixedValue;
            int newSelectedIndex        = EditorGUILayout.Popup(label, selectedIndex, _stringBuffer.ToArray());
            if (newSelectedIndex != selectedIndex) result = _stringBuffer[newSelectedIndex];

            restoreShowMixedValue();
            restoreLabelWidth();         

            return result;
        }

        public static Vector3FieldResult vector3FieldEx(Vector3 val, bool[] axisDiff, float floatFieldWidth)
        {
            saveLabelWidth();
            saveShowMixedValue();

            EditorGUI.showMixedValue            = false;
            const float axisLabelWidth          = 12.0f;
            Vector3FieldResult result           = Vector3FieldResult.defaultResult;

            EditorGUILayout.BeginHorizontal();
            float newFloat; Vector3 newVec3     = val;
            for (int axisIndex = 0; axisIndex < 3; ++axisIndex)
            {
                if (axisDiff[axisIndex])
                {
                    EditorGUI.showMixedValue    = true;
                    EditorGUIUtility.labelWidth = axisLabelWidth;
                    EditorGUI.BeginChangeCheck();
                    newFloat = EditorGUILayout.FloatField(_axesLabels[axisIndex], newVec3[axisIndex], GUILayout.Width(floatFieldWidth), GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        newVec3[axisIndex]      = newFloat;
                        result.changedAxisIndex = axisIndex;
                        result.newAxisValue     = newFloat;
                        result.hasChanged       = true;
                    }
                    EditorGUI.showMixedValue    = false;
                    EditorGUIUtility.labelWidth = 0.0f;
                }
                else
                {
                    EditorGUIUtility.labelWidth = axisLabelWidth;
                    EditorGUI.BeginChangeCheck();
                    newFloat = EditorGUILayout.FloatField(_axesLabels[axisIndex], newVec3[axisIndex], GUILayout.Width(floatFieldWidth), GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        newVec3[axisIndex]      = newFloat;
                        result.changedAxisIndex = axisIndex;
                        result.newAxisValue     = newFloat;
                        result.hasChanged       = true;
                    }
                    EditorGUIUtility.labelWidth = 0.0f;
                }
            }
            EditorGUILayout.EndHorizontal();

            restoreShowMixedValue();
            restoreLabelWidth();

            result.newValue = newVec3;
            return result;
        }

        public static Vector3FieldResult vector3FieldEx(Vector3 val, float floatFieldWidth)
        {
            saveLabelWidth();
            saveShowMixedValue();

            EditorGUI.showMixedValue = false;
            const float axisLabelWidth = 12.0f;
            Vector3FieldResult result = Vector3FieldResult.defaultResult;

            EditorGUILayout.BeginHorizontal();
            float newFloat; Vector3 newVec3 = val;
            for (int axisIndex = 0; axisIndex < 3; ++axisIndex)
            {
                EditorGUIUtility.labelWidth = axisLabelWidth;
                EditorGUI.BeginChangeCheck();
                newFloat = EditorGUILayout.FloatField(_axesLabels[axisIndex], newVec3[axisIndex], GUILayout.Width(floatFieldWidth), GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    newVec3[axisIndex]      = newFloat;
                    result.changedAxisIndex = axisIndex;
                    result.newAxisValue     = newFloat;
                    result.hasChanged       = true;
                }
                EditorGUIUtility.labelWidth = 0.0f;
            }
            EditorGUILayout.EndHorizontal();

            restoreShowMixedValue();
            restoreLabelWidth();

            result.newValue = newVec3;
            return result;
        }
    }
}
#endif