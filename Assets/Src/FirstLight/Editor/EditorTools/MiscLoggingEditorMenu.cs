using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
    public class MiscLoggingEditorMenu
    {
        private const string ENABLE_STATE_MACHINE_DEBUG = "EnableStateMachineDebug";
        
        static MiscLoggingEditorMenu()
        {
            if (!EditorPrefs.HasKey(ENABLE_STATE_MACHINE_DEBUG))
            {
                EditorPrefs.SetBool(ENABLE_STATE_MACHINE_DEBUG, false);
            }
        }
        
        [MenuItem("FLG/Logging/Enable State Machine Debug")]
        private static void EnableStateMachineDebug()
        {
            EditorPrefs.SetBool(ENABLE_STATE_MACHINE_DEBUG, true);
            Debug.Log("State machine debug has been ENABLED.");
        }

        [MenuItem("FLG/Logging/Disable State Machine Debug")]
        private static void DisableStateMachineDebug()
        {
            EditorPrefs.SetBool(ENABLE_STATE_MACHINE_DEBUG, false);
            Debug.Log("State machine debug has been DISABLED.");
        }
    }
}
