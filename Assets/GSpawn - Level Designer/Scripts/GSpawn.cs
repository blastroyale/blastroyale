#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    [ExecuteInEditMode]
    public class GSpawn : MonoBehaviour
    {
        private const int _menuItemPriorityStart_Initialize         = 10;
        private const int _menuItemPriorityStart_Actions            = 20;
        private const int _menuItemPriorityStart_Prefabs            = 40;
        private const int _menuItemPriorityStart_Misc               = 60;
        private const int _menuItemPriorityStart_Data               = 80;

        [SerializeField]
        private string _version = string.Empty;

        // Note: These 2 would normally have to reside in MeshCombineSettings. But because
        //       MeshCombineSettings is an asset, the game object references are not serialized
        //       properly (i.e. loosing refs between reloads). So they are stored here.
        [SerializeField][HideInInspector]
        private GameObject              _meshCombineSourceParent;
        [SerializeField][HideInInspector]
        private GameObject              _meshCombineDestinationParent;

        [SerializeField][HideInInspector]
        private bool                    _isDuplicate;

        [SerializeField][HideInInspector]
        private LevelDesignToolId       _levelDesignToolId  = LevelDesignToolId.ObjectSpawn;

        [SerializeField]
        private SceneViewPrefabDrop     _sceneViewPrefabDrop;

        [SerializeField][HideInInspector]
        private CreateNewEntityUI       _createNewEntityUI;
        [SerializeField][HideInInspector]
        private RenameEntityUI          _renameEntityUI;
        [SerializeField][HideInInspector]
        private DeleteEntityUI          _deleteEntityUI;

        [SerializeField][HideInInspector]
        private PluginInspectorUI       _inspectorUI;
        [SerializeField][HideInInspector]
        private ObjectSpawnUI           _objectSpawnUI;
        [SerializeField][HideInInspector]
        private ObjectSelectionUI       _objectSelectionUI;
        [SerializeField][HideInInspector]
        private ObjectEraseUI           _objectEraseUI;

        [SerializeField][HideInInspector]
        private PluginScene             _pluginScene;

        [SerializeField][HideInInspector]
        private ObjectSpawn             _objectSpawn;
        [SerializeField][HideInInspector]
        private ObjectSelection         _objectSelection;
        [SerializeField][HideInInspector]
        private ObjectErase             _objectErase;

        [SerializeField][HideInInspector]
        private TileRuleGridDb              _tileRuleGridDb;
        [SerializeField][HideInInspector]
        private TileRuleObjectSpawnUI       _tileRuleObjectSpawnUI;

        [SerializeField][HideInInspector]
        private ModularWallsObjectSpawnUI   _modularWallsObjectSpawnUI;

        [SerializeField][HideInInspector]
        private ObjectSpawnCurveDb      _objectSpawnCurveDb;
        [SerializeField][HideInInspector]
        private CurveObjectSpawnUI      _objectSpawnCurveDbUI;

        [SerializeField][HideInInspector]
        private DataExportSettings      _dataExportSettings;
        [SerializeField][HideInInspector]
        private DataExportUI            _dataExportUI;

        private EditorUpdateActionPool  _editorUpdateActionPool = new EditorUpdateActionPool();
        [NonSerialized]
        private PrefabPreviewFactory    _prefabPreviewFactory = new PrefabPreviewFactory();

        private SceneViewPrefabDrop sceneViewPrefabDrop
        {
            get
            {
                if (_sceneViewPrefabDrop == null)
                {
                    _sceneViewPrefabDrop = ScriptableObject.CreateInstance<SceneViewPrefabDrop>();
                    _sceneViewPrefabDrop.dragPerformed += onPrefabDroppedInSceneView;
                }
                return _sceneViewPrefabDrop;
            }
        }

        public GameObject meshCombineSourceParent 
        { 
            get { return _meshCombineSourceParent; } 
            set 
            {
                if (value != null && !value.isSceneObject()) return;
                UndoEx.record(this); 
                _meshCombineSourceParent = value; 
            } 
        }
        public GameObject meshCombineDestinationParent 
        { 
            get { return _meshCombineDestinationParent; } 
            set
            {
                if (value != null && !value.isSceneObject()) return;
                UndoEx.record(this); 
                _meshCombineDestinationParent = value; 
            } 
        }
        public LevelDesignToolId levelDesignToolId 
        { 
            get { return _levelDesignToolId; } 
            set 
            {
                if (_levelDesignToolId == value) return;

                var previousLevelDesignToolId = _levelDesignToolId;
                _levelDesignToolId = value;
                EditorUtility.SetDirty(this);
                if (previousLevelDesignToolId == LevelDesignToolId.ObjectSpawn) objectSpawn.onLevelDesignToolChanged();
                else if (previousLevelDesignToolId == LevelDesignToolId.ObjectSelection) objectSelection.onLevelDesignToolChanged();
              
                inspectorUI.refresh(); 
                SceneView.RepaintAll(); 
            } 
        }
        public CreateNewEntityUI createNewEntityUI
        {
            get
            {
                if (_createNewEntityUI == null) _createNewEntityUI = ScriptableObject.CreateInstance<CreateNewEntityUI>();
                return _createNewEntityUI;
            }
        }
        public RenameEntityUI renameEntityUI
        {
            get
            {
                if (_renameEntityUI == null) _renameEntityUI = ScriptableObject.CreateInstance<RenameEntityUI>();
                return _renameEntityUI;
            }
        }
        public DeleteEntityUI deleteEntityUI
        {
            get
            {
                if (_deleteEntityUI == null) _deleteEntityUI = ScriptableObject.CreateInstance<DeleteEntityUI>();
                return _deleteEntityUI;
            }
        }
        public PluginInspectorUI inspectorUI
        {
            get
            {
                if (_inspectorUI == null) _inspectorUI = ScriptableObject.CreateInstance<PluginInspectorUI>();
                return _inspectorUI;
            }
        }
        public ObjectSpawnUI                objectSpawnUI               { get { if (_objectSpawnUI == null) _objectSpawnUI = ScriptableObject.CreateInstance<ObjectSpawnUI>(); return _objectSpawnUI; } }
        public ObjectSelectionUI            objectSelectionUI           { get { if (_objectSelectionUI == null) _objectSelectionUI = ScriptableObject.CreateInstance<ObjectSelectionUI>(); return _objectSelectionUI; } }
        public ObjectEraseUI                objectEraseUI               { get { if (_objectEraseUI == null) _objectEraseUI = ScriptableObject.CreateInstance<ObjectEraseUI>(); return _objectEraseUI; } }
        public TileRuleGridDb               tileRuleGridDb              { get { if (_tileRuleGridDb == null) _tileRuleGridDb = ScriptableObject.CreateInstance<TileRuleGridDb>(); return _tileRuleGridDb; } }
        public TileRuleObjectSpawnUI        tileRuleObjectSpawnUI       { get { if (_tileRuleObjectSpawnUI == null) _tileRuleObjectSpawnUI = ScriptableObject.CreateInstance<TileRuleObjectSpawnUI>(); return _tileRuleObjectSpawnUI; } }
        public ModularWallsObjectSpawnUI    modularWallsObjectSpawnUI   { get { if (_modularWallsObjectSpawnUI == null) _modularWallsObjectSpawnUI = ScriptableObject.CreateInstance<ModularWallsObjectSpawnUI>(); return _modularWallsObjectSpawnUI; } }
        public ObjectSpawnCurveDb           objectSpawnCurveDb          { get { if (_objectSpawnCurveDb == null) _objectSpawnCurveDb = ScriptableObject.CreateInstance<ObjectSpawnCurveDb>(); return _objectSpawnCurveDb; } }
        public CurveObjectSpawnUI           curveObjectSpawnUI          { get { if (_objectSpawnCurveDbUI == null) _objectSpawnCurveDbUI = ScriptableObject.CreateInstance<CurveObjectSpawnUI>(); return _objectSpawnCurveDbUI; } }
        public DataExportSettings           dataExportSettings          { get { if (_dataExportSettings == null) _dataExportSettings = ScriptableObject.CreateInstance<DataExportSettings>(); return _dataExportSettings; } }
        public DataExportUI                 dataExportUI                { get { if (_dataExportUI == null) _dataExportUI = ScriptableObject.CreateInstance<DataExportUI>(); return _dataExportUI; } }     
        public PluginScene                  pluginScene
        {
            get
            {
                if (_pluginScene == null) _pluginScene = ScriptableObject.CreateInstance<PluginScene>();
                return _pluginScene;
            }
        }
        public ObjectSpawn objectSpawn
        {
            get
            {
                if (_objectSpawn == null) _objectSpawn = ScriptableObject.CreateInstance<ObjectSpawn>();
                return _objectSpawn;
            }
        }
        public ObjectSelection objectSelection
        {
            get
            {
                if (_objectSelection == null) _objectSelection = ScriptableObject.CreateInstance<ObjectSelection>();
                return _objectSelection;
            }
        }
        public ObjectErase objectErase
        {
            get
            {
                if (_objectErase == null) _objectErase = ScriptableObject.CreateInstance<ObjectErase>();
                return _objectErase;
            }
        }
        public PrefabPreviewFactory prefabPreviewFactory    { get { return _prefabPreviewFactory; } }

        public static GSpawn    active                      { get { return PluginInstanceData.instance != null ? PluginInstanceData.instance.activePlugin : null; } }
        public static bool      isActiveSelected            { get { return active != null && active.gameObject == Selection.activeGameObject; } }
        public static int       numPlugins                  { get { return PluginInstanceData.instance != null ? PluginInstanceData.instance.numPlugins : 0; } }
        public static string    pluginName                  { get { return "GSpawn"; } }
        public static bool      anyInstanceSelected         { get { return PluginInstanceData.instance.isPlugin(Selection.activeGameObject); } }
        public static string    currentVersion              { get { return "3.2.8"; } }

        [MenuItem("Tools/GSpawn/Initialize", false, _menuItemPriorityStart_Initialize)]
        public static void initialize()
        {
            if (FindObjectOfType<GSpawn>() != null)
            {
                Debug.LogWarning("Only a single plugin object can exist in the scene.");
                return;
            }

            PluginFolders.createDataFolderAndAssetsIfMissing();

            var pluginObject = new GameObject(pluginName);
            pluginObject.AddComponent<GSpawn>();
            UndoEx.registerCreatedObject(pluginObject);

            // Note: It seems that the object is not saved in the scene when Unity is
            //       closed if it's not marked as dirty.
            EditorUtility.SetDirty(pluginObject);

            Selection.activeObject = pluginObject;

            // Note: Undo/Redo throws exceptions.
            Undo.ClearAll();
        }

        [MenuItem("Tools/GSpawn/Actions/Select-Deselect &R", false, _menuItemPriorityStart_Actions)]
        public static void selectDeselectPlugin()
        {
            if (GSpawn.active == null) return;
            if (Selection.activeObject == GSpawn.active.gameObject) Selection.activeObject = null;
            else Selection.activeObject = GSpawn.active.gameObject;

            // Note: Keyboard state is not updated correctly when the ALT key is pressed
            //       in conjunction with another key. So clear the keyboard state whenever
            //       an ALT command is sent.
            Keyboard.instance.clearButtonStates();
        }

        [MenuItem("Tools/GSpawn/Actions/Transfer Selection &T", false, _menuItemPriorityStart_Actions + 1)]
        public static void transferSelection()
        {
            if (GSpawn.numPlugins == 0) return;
            if (SelectionEx.containsObjectsOfType<GSpawn>())
            {
                var selectedObjects = new List<GameObject>();
                ObjectSelection.instance.getSelectedObjects(selectedObjects);
                SelectionEx.clear();
                SelectionEx.appendGameObjects(selectedObjects);
            }
            else
            {
                var selectedObjects = new List<GameObject>();
                SelectionEx.getGameObjects(selectedObjects);
                SelectionEx.clear();
                SelectionEx.appendGameObject(GSpawn.active.gameObject);
                GSpawn.active.levelDesignToolId = LevelDesignToolId.ObjectSelection;
                ObjectSelection.instance.setSelectedObjects(selectedObjects);
            }

            // Note: Keyboard state is not updated correctly when the ALT key is pressed
            //       in conjunction with another key. So clear the keyboard state whenever
            //       an ALT command is sent.
            Keyboard.instance.clearButtonStates();
        }

        [MenuItem("Tools/GSpawn/Actions/Refresh Data Caches", false, _menuItemPriorityStart_Actions + 2)]
        public static void refresh()
        {
            PluginProgressDialog.begin("Refreshing Data Caches");
            PluginProgressDialog.updateProgress("Refreshing...", 0.0f);
            GameObjectDataDb.instance.refresh();
            PluginProgressDialog.updateProgress("Refreshing...", 0.5f);
            PrefabDataDb.instance.refresh();
            PluginProgressDialog.updateProgress("Refreshing...", 1.0f);
            PluginProgressDialog.end();

            PluginProgressDialog.begin("Refreshing Scene");
            PluginProgressDialog.updateProgress("Refreshing...", 0.0f);
            PluginScene.instance.refreshObjectTrees();
            PluginProgressDialog.updateProgress("Refreshing...", 1.0f);
            PluginProgressDialog.end();
        }

        [MenuItem("Tools/GSpawn/Actions/Refresh Prefab Previews", false, _menuItemPriorityStart_Actions + 3)]
        public static void refreshPrefabPreviews()
        {
            // Note: Necessary in order to destroy the old render texture and create a new one.
            //       This must be done when the color space changes.
            PrefabPreviewFactory.instance.cleanup();
            PrefabPreviewFactory.instance.initialize();

            int numProfiles = RandomPrefabProfileDb.instance.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                RandomPrefabProfileDb.instance.getProfile(i).regeneratePrefabPreviews();

            numProfiles = IntRangePrefabProfileDb.instance.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                IntRangePrefabProfileDb.instance.getProfile(i).regeneratePrefabPreviews();

            numProfiles = ScatterBrushPrefabProfileDb.instance.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                ScatterBrushPrefabProfileDb.instance.getProfile(i).regeneratePrefabPreviews();

            numProfiles = CurvePrefabProfileDb.instance.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                CurvePrefabProfileDb.instance.getProfile(i).regeneratePrefabPreviews();

            numProfiles = TileRuleProfileDb.instance.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                TileRuleProfileDb.instance.getProfile(i).regeneratePrefabPreviews();

            numProfiles = ModularWallPrefabProfileDb.instance.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                ModularWallPrefabProfileDb.instance.getProfile(i).regeneratePrefabPreviews();

            numProfiles = PrefabLibProfileDb.instance.numProfiles;
            for (int i = 0; i < numProfiles; ++i)
                PrefabLibProfileDb.instance.getProfile(i).regeneratePrefabPreviews();

            if (RandomPrefabProfileDbUI.instance.uiVisibleAndReady)         RandomPrefabProfileDbUI.instance.refresh();
            if (IntRangePrefabProfileDbUI.instance.uiVisibleAndReady)       IntRangePrefabProfileDbUI.instance.refresh();
            if (ScatterBrushPrefabProfileDbUI.instance.uiVisibleAndReady)   ScatterBrushPrefabProfileDbUI.instance.refresh();
            if (CurvePrefabProfileDbUI.instance.uiVisibleAndReady)          CurvePrefabProfileDbUI.instance.refresh();
            if (TileRuleProfileDbUI.instance.uiVisibleAndReady)             TileRuleProfileDbUI.instance.refresh();
            if (PluginPrefabManagerUI.instance.uiVisibleAndReady)           PluginPrefabManagerUI.instance.refresh();
            if (ModularWallPrefabProfileDbUI.instance.uiVisibleAndReady)    ModularWallPrefabProfileDbUI.instance.refresh();
        }

        [MenuItem("Tools/GSpawn/Windows/Prefab Library Manager...", false, _menuItemPriorityStart_Prefabs + 1)]
        public static void showPrefabLibraryManagerWindow()
        {
            var wnd = PluginWindow.show<PrefabLibProfileDbWindow>("Prefab Lib Db");
            wnd.setSize(new Vector2(300, 500));
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Prefab Manager...", false, _menuItemPriorityStart_Prefabs + 2)]
        public static void showPrefabManagerWindow()
        {
            var wnd = PluginWindow.show<PluginPrefabManagerWindow>("Prefab Manager");
            wnd.setSize(new Vector2(300, 500));
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Random Prefabs...", false, _menuItemPriorityStart_Prefabs + 3)]
        public static void showRandomPrefabProfileWindow()
        {
            var wnd = PluginWindow.show<RandomPrefabProfileDbWindow>("Random Prefabs");
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Integer Range Prefabs...", false, _menuItemPriorityStart_Prefabs + 4)]
        public static void showIntRangePrefabProfileWindow()
        {
            var wnd = PluginWindow.show<IntRangePrefabProfileDbWindow>("Integer Range Prefabs");
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Scatter Brush Prefabs...", false, _menuItemPriorityStart_Prefabs + 5)]
        public static void showScatterBrushPrefabsWindow()
        {
            var wnd = PluginWindow.show<ScatterBrushPrefabProfileDbWindow>("Scatter Brush Prefabs");
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Tile Rules...", false, _menuItemPriorityStart_Prefabs + 6)]
        public static void showTileRuleProfileWindow()
        {
            var wnd = PluginWindow.show<TileRuleProfileDbWindow>("Tile Rules");
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Modular Wall Prefabs...", false, _menuItemPriorityStart_Prefabs + 7)]
        public static void showModularWallPrefabsWindow()
        {
            var wnd = PluginWindow.show<ModularWallPrefabProfileDbWindow>("Modular Wall Prefabs");
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Curve Prefabs...", false, _menuItemPriorityStart_Prefabs + 8)]
        public static void showCurvePrefabsWindow()
        {
            var wnd = PluginWindow.show<CurvePrefabProfileDbWindow>("Curve Prefabs");
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Integer Patterns...", false, _menuItemPriorityStart_Misc + 2)]
        public static void showIntegerPatternsWindow()
        {
            var wnd = PluginWindow.show<IntPatternDbWindow>("Integer Patterns");
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Object Layers...", false, _menuItemPriorityStart_Misc)]
        public static void showObjectLayersWindow()
        {
            var wnd = PluginWindow.show<PluginObjectLayerDbWindow>("Object Layers");
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Object Groups...", false, _menuItemPriorityStart_Misc + 1)]
        public static void showObjectGroupsWindow()
        {
            var wnd = PluginWindow.show<ObjectGroupDbWindow>("Object Groups");
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Grid Settings...", false, _menuItemPriorityStart_Misc + 2)]
        public static void showGridSettingsWindow()
        {
            var wnd = PluginWindow.show<GridSettingsProfileDbWindow>("Grid Settings");
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Shortcuts...", false, _menuItemPriorityStart_Misc + 3)]
        public static void showShortcutsWindow()
        {
            var wnd = PluginWindow.show<ShortcutsWindow>("Shortcuts");
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Mesh Combine...", false, _menuItemPriorityStart_Misc + 4)]
        public static void showMeshCombineWindow()
        {
            var wnd = PluginWindow.show<MeshCombineWindow>("Mesh Combine");
            wnd.centerOnScreen();
        }

        [MenuItem("Tools/GSpawn/Windows/Export Data...", false, _menuItemPriorityStart_Data)]
        public static void showDataExportWindow()
        {
            var wnd = PluginWindow.show<DataExportWindow>("Data Export");
            wnd.centerOnScreen();
        }

        public void registerEditorUpdateAction(EditorUpdateAction action)
        {
            _editorUpdateActionPool.registerAction(action);
        }

        private void onEditorUpdate()
        {
            if (_destroyOnUpdate)
            {
                GameObject go = this.gameObject;
                GameObject.DestroyImmediate(this);
                EditorUtility.SetDirty(go);
                GameObject.DestroyImmediate(go);
                return;
            }

            if (_updatePluginFolders)
            {
                _updatePluginFolders = false;
                PluginFolders.createDataFolderAndAssetsIfMissing();
            }

            if (!gameObject.isEditorOnly()) gameObject.makeEditorOnly();
            PhysicsSimulation.instance.update();
            _editorUpdateActionPool.onEditorUpdate();
        }

        private void onSceneGUI(SceneView sceneView)
        {
            // Note: Don't don anything until all folders are in place.
            if (_updatePluginFolders) return;

            if (transform.parent != null) transform.parent = null;
            if (GSpawn.active == this) sceneViewPrefabDrop.onSceneGUI();

            if (GSpawn.active != this || Selection.activeGameObject == null ||
                Selection.activeGameObject.getPlugin() == null) return;
                      
            Event e = Event.current;

            Keyboard.instance.updateModifierInfo();
            switch (e.type)
            {
                case EventType.ExecuteCommand:

                    // Block commands if necessary
                    if (e.commandName == UnityEditorCommands.duplicateName)
                    {
                        if (SelectionEx.containsObjectsOfType<GSpawn>())
                        {
                            e.disable();
                            return;
                        }
                    }
                    else
                    if (e.commandName == UnityEditorCommands.softDeleteName)
                    {
                        if (SelectionEx.containsObjectsOfType<GSpawn>())
                        {
                            e.disable();
                            return;
                        }
                    }
                    else
                    if (e.commandName == UnityEditorCommands.deleteName)
                    {
                        if (SelectionEx.containsObjectsOfType<GSpawn>())
                        {
                            e.disable();
                            return;
                        }
                    }
                    else
                    if (e.commandName == UnityEditorCommands.frameSelectedName)
                    {
                        if (SelectionEx.containsObjectsOfType<GSpawn>())
                        {
                            e.disable();
                            return;
                        }
                    }
                    else
                    if (e.commandName == UnityEditorCommands.frameSelectedWithLockName)
                    {
                        if (SelectionEx.containsObjectsOfType<GSpawn>())
                        {
                            e.disable();
                            return;
                        }
                    }
                    else
                    if (e.commandName == UnityEditorCommands.selectAllName)
                    {
                        if (SelectionEx.containsObjectsOfType<GSpawn>())
                        {
                            e.disable();
                            return;
                        }
                    }
                    break;

                case EventType.KeyDown:
                   
                    Keyboard.instance.onKeyDown(e.keyCode);
                    
                    // Allow the camera to be rotated and panned
                    if (Mouse.instance.isButtonDown((int)MouseButton.RightMouse) ||
                        Mouse.instance.isButtonDown((int)MouseButton.MiddleMouse)) return;

                    // Note: Allow Unity commands to pass through
                    if (ShortcutManagerEx.isFileSave(e) || ShortcutManagerEx.isUndoRedo(e))
                        return;

                    // Allow misc commands
                    if (e.alt)
                    {
                        // ALT + R/D
                        if (e.keyCode == KeyCode.R || e.keyCode == KeyCode.D || e.keyCode == KeyCode.T) return;
                    }
                    
                    break;

                case EventType.KeyUp:

                    Keyboard.instance.onKeyUp(e.keyCode);
                    break;

                case EventType.MouseDown:

                    Mouse.instance.onButtonDown(e.button);
                    break;

                case EventType.MouseUp:

                    Mouse.instance.onButtonUp(e.button);
                    break;

                case EventType.MouseLeaveWindow:

                    // Note: Seems to be the only way to detect when the mouse is released over the Inspector
                    //       or an editor window.
                    Mouse.instance.onButtonUp(e.button);
                    break;
            }

            bool disableEvent = false;
            if (ShortcutProfileDb.instance.processEvent(e))
            {
                if (e.type == EventType.KeyDown || e.type == EventType.KeyUp || e.type == EventType.MouseDown || e.type == EventType.MouseUp)
                    disableEvent = true;
            }

            Tools.current = Tool.None;
            pluginScene.onSceneGUI(sceneView);
            if (_levelDesignToolId == LevelDesignToolId.ObjectSpawn) objectSpawn.onSceneGUI();
            if (_levelDesignToolId == LevelDesignToolId.ObjectSelection) objectSelection.onSceneGUI();
            else if (_levelDesignToolId == LevelDesignToolId.ObjectErase) objectErase.onSceneGUI();

            if (e.type == EventType.MouseDown)
            {
                // We disable the left mouse button to avoid deselecting the plugin,
                // but we do allow it in case the ALT key is pressed because we want
                // to be able to orbit the camera.
                if (e.button == (int)MouseButton.LeftMouse && !e.alt) disableEvent = true;
            }
            else if (e.type == EventType.KeyDown)
            {
                // Disable the V key always because we want to always hide the vertex snap
                // handle for position gizmos. It seems that they are useless.
                if (e.keyCode == KeyCode.V) disableEvent = true;
            }

            if (disableEvent) e.disable();

            ShortcutLogger.instance.onSceneGUI();
        }

        private void OnDrawGizmosSelected()
        {
            // Note: Don't don anything until all folders are in place.
            if (_updatePluginFolders) return;

            pluginScene.drawGizmos();
            if (_levelDesignToolId == LevelDesignToolId.ObjectSelection) objectSelection.drawGizmos();
        }

        private void deleteSpawnGuides()
        {
            // Note: Erase all spawn guides from the scene. This seems to be the most reliable
            //       way to ensure the guide does not survive between Unity sessions.
            ObjectSpawnGuideMono[] spawnGuides = FindObjectsOfType<ObjectSpawnGuideMono>();
            foreach (var sg in spawnGuides) DestroyImmediate(sg.gameObject);
        }

        private void Awake()
        {
            if (FindObjectsOfType<GSpawn>().Length > 1)
            {
                _isDuplicate = true;
                GameObject.DestroyImmediate(gameObject);
                Debug.LogWarning("Only a single plugin object can exist in the scene.");
                return;
            }
        }

        [SerializeField]
        private bool _destroyOnUpdate       = false;
        private bool _updatePluginFolders   = false;
        private void OnEnable()
        {
            #if UNITY_EDITOR
            if (!PluginFolders.isDataFolderValid() && _version == currentVersion)
            {
                EditorUtility.DisplayDialog("Missing/Invalid Data Folder",
                    "Detected missing or invalid data folder. The plugin object will be deleted from the scene.", "Ok");

                // Destroying from OnEnable doesn't seem to work correctly. Must use delayed strategy.
                _destroyOnUpdate = true;

                // Note: Must register handler for the update event to get a chance to destroy.
                EditorApplication.update            += onEditorUpdate;
                return;
            }

            if (_version != currentVersion)
            {
                // Note: Must create any missing assets that might have been added in this new version.
                _updatePluginFolders    = true;
                _version                = currentVersion;
                EditorUtility.SetDirty(this);
            }

            gameObject.makeEditorOnly();
            PluginInstanceData.instance.add(this);
            prefabPreviewFactory.initialize();

            EditorApplication.update            += onEditorUpdate;
            SceneView.duringSceneGui            += onSceneGUI;
            PluginDragAndDrop.began             += onPluginDragAndDropBegan;

            deleteSpawnGuides();

            Undo.undoRedoPerformed += onUndoRedo;
            #endif
        }

        private void OnDisable()
        {
            #if UNITY_EDITOR
            prefabPreviewFactory.cleanup();
            EditorApplication.update                -= onEditorUpdate;
            SceneView.duringSceneGui                -= onSceneGUI;
            PluginDragAndDrop.began                 -= onPluginDragAndDropBegan;
            if (_sceneViewPrefabDrop != null) _sceneViewPrefabDrop.dragPerformed -= onPrefabDroppedInSceneView;
            Undo.undoRedoPerformed -= onUndoRedo;
            #endif
        }

        private void OnDestroy()
        {
            #if UNITY_EDITOR
            Undo.undoRedoPerformed      -= onUndoRedo;
            EditorApplication.update    -= onEditorUpdate;
            SceneView.duringSceneGui    -= onSceneGUI;
            prefabPreviewFactory.cleanup();

            // Note: Data folder could have been deleted.
            if (PluginInstanceData.instance != null) PluginInstanceData.instance.remove(this);

            if (_sceneViewPrefabDrop != null) _sceneViewPrefabDrop.dragPerformed -= onPrefabDroppedInSceneView;

            if (!_isDuplicate)
            {
                ScriptableObjectEx.destroyImmediate(_sceneViewPrefabDrop);

                ScriptableObjectEx.destroyImmediate(_createNewEntityUI);
                ScriptableObjectEx.destroyImmediate(_renameEntityUI);
                ScriptableObjectEx.destroyImmediate(_deleteEntityUI);

                ScriptableObjectEx.destroyImmediate(_inspectorUI);
                ScriptableObjectEx.destroyImmediate(_objectSpawnUI);
                ScriptableObjectEx.destroyImmediate(_objectSelectionUI);
                ScriptableObjectEx.destroyImmediate(_objectEraseUI);

                ScriptableObjectEx.destroyImmediate(_pluginScene);
                ScriptableObjectEx.destroyImmediate(_objectSpawn);
                ScriptableObjectEx.destroyImmediate(_objectSelection);
                ScriptableObjectEx.destroyImmediate(_objectErase);

                ScriptableObjectEx.destroyImmediate(_tileRuleGridDb);
                ScriptableObjectEx.destroyImmediate(_tileRuleObjectSpawnUI);

                ScriptableObjectEx.destroyImmediate(_modularWallsObjectSpawnUI);

                ScriptableObjectEx.destroyImmediate(_objectSpawnCurveDb);
                ScriptableObjectEx.destroyImmediate(_objectSpawnCurveDbUI);

                ScriptableObjectEx.destroyImmediate(_dataExportSettings);
                ScriptableObjectEx.destroyImmediate(_dataExportUI);
            }
            #endif

            // Note: Undo/Redo after plugin is deleted may not be safe. So clear stack.
            Undo.ClearAll();
        }

        private void onPluginDragAndDropBegan()
        {
            if (PluginPrefabManagerUI.instance.dragAndDropInitiatedByPrefabView())
                sceneViewPrefabDrop.begin(PluginPrefabManagerUI.instance.getFirstDragAndDropPrefab());
        }

        private void onPrefabDroppedInSceneView(List<GameObject> spawnedInstances)
        {
            if (isActiveSelected)
            {
                if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection)
                    ObjectSelection.instance.setSelectedObjects(spawnedInstances);
            }
        }

        private void onUndoRedo()
        {
            // Note: Can be null when dragging materials onto objects.
            if (Event.current == null) return;

            if (Event.current.keyCode == KeyCode.Y)
                PluginScene.instance.destroyPhysicsSimulationMonos(false);
        }

        // Don't allow reset.
        // Source: https://answers.unity.com/questions/1383533/prevent-reset-from-clearing-out-serialized-fields.html
        [UnityEditor.MenuItem("CONTEXT/" + nameof(GSpawn) + "/Reset", true)]
        private static bool OnValidateReset()
        {
            return false;
        }
        [UnityEditor.MenuItem("CONTEXT/" + nameof(GSpawn) + "/Reset")]
        private static void OnReset()
        {
            Debug.LogWarning("Plugin doesn't support Reset.");
        }
    }
}
#endif