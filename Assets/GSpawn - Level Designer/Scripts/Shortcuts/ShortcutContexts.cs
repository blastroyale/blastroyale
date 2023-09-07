#if UNITY_EDITOR

namespace GSpawn
{
    public class GlobalShortcutContext : ShortcutContext
    {
        private static GlobalShortcutContext                    _instance = null;

        private ObjectSpawnShortcutContext                      _objectSpawnContext                             = new ObjectSpawnShortcutContext();
        private ObjectSpawn_ModularWalls_BuildShortcutContext   _objectSpawn_ModularWalls_BuildContext          = new ObjectSpawn_ModularWalls_BuildShortcutContext();
        private ObjectSpawn_Segments_BuildShortcutContext       _objectSpawn_Segments_BuildShortcutContext      = new ObjectSpawn_Segments_BuildShortcutContext();
        private ObjectSpawn_Box_BuildShortcutContext            _objectSpawn_Box_BuildShortcutContext           = new ObjectSpawn_Box_BuildShortcutContext();
        private ObjectSpawn_TileRules_ShortcutContext           _objectSpawn_TileRules_ShortcutContext          = new ObjectSpawn_TileRules_ShortcutContext();
        private ObjectSpawn_Curve_ShortcutContext               _objectSpawn_Curve_ShortcutContext              = new ObjectSpawn_Curve_ShortcutContext();

        private ObjectSelectionShortcutContext                  _objectSelectionContext                         = new ObjectSelectionShortcutContext();
        private ObjectEraseShortcutContext                      _objectEraseContext                             = new ObjectEraseShortcutContext();
        private ObjectModularSnapShortcutContext                _objectModularSnapShortcutContext               = new ObjectModularSnapShortcutContext();
        private ObjectSurfaceSnapShortcutContext                _objectSurfaceSnapShortcutContext               = new ObjectSurfaceSnapShortcutContext();
        
        public ObjectSpawnShortcutContext                       objectSpawnContext                              { get { return _objectSpawnContext; } }
        public ObjectSpawn_ModularWalls_BuildShortcutContext    objectSpawn_ModularWalls_BuildContext           { get { return _objectSpawn_ModularWalls_BuildContext; } }
        public ObjectSpawn_Segments_BuildShortcutContext        objectSpawn_Segments_BuildShortcutContext       { get { return _objectSpawn_Segments_BuildShortcutContext; } }
        public ObjectSpawn_Box_BuildShortcutContext             objectSpawn_Box_BuildShortcutContext            { get { return _objectSpawn_Box_BuildShortcutContext; } }
        public ObjectSpawn_TileRules_ShortcutContext            objectSpawn_TileRules_ShortcutContext           { get { return _objectSpawn_TileRules_ShortcutContext; } }
        public ObjectSpawn_Curve_ShortcutContext                objectSpawn_Curve_ShortcutContext               { get { return _objectSpawn_Curve_ShortcutContext; } }

        public ObjectSelectionShortcutContext                   objectSelectionContext                      { get { return _objectSelectionContext; } }
        public ObjectEraseShortcutContext                       objectEraseContext                          { get { return _objectEraseContext; } }
        public ObjectModularSnapShortcutContext                 objectModularSnapShortcutContext            { get { return _objectModularSnapShortcutContext; } }
        public ObjectSurfaceSnapShortcutContext                 objectSurfaceSnapShortcutContext            { get { return _objectSurfaceSnapShortcutContext; } }
        public override bool                                    alwaysActive                                { get { return true; } }

        public static GlobalShortcutContext instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GlobalShortcutContext();
                    _instance._objectSpawnContext.setParentContext(_instance);
                    _instance.objectSpawn_ModularWalls_BuildContext.setParentContext(_instance._objectSpawnContext);
                    _instance._objectSpawn_Segments_BuildShortcutContext.setParentContext(_instance._objectSpawnContext);
                    _instance._objectSpawn_Box_BuildShortcutContext.setParentContext(_instance._objectSpawnContext);
                    _instance._objectSpawn_TileRules_ShortcutContext.setParentContext(_instance._objectSpawnContext);
                    _instance._objectSpawn_Curve_ShortcutContext.setParentContext(_instance._objectSpawnContext);
                    _instance._objectSelectionContext.setParentContext(_instance);
                    _instance._objectEraseContext.setParentContext(_instance);
                    _instance._objectModularSnapShortcutContext.setParentContext(_instance);
                    _instance._objectSurfaceSnapShortcutContext.setParentContext(_instance);
                }
                return _instance;
            }
        }

        protected override bool evaluate()
        {
            return true;
        }
    }

    public class ObjectSpawnShortcutContext : ShortcutContext
    {
        protected override bool evaluate()
        {
            return GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn;
        }
    }

    public class ObjectSpawn_ModularWalls_BuildShortcutContext : ShortcutContext
    {
        protected override bool evaluate()
        {
            return GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                   ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.ModularWalls &&
                   ObjectSpawn.instance.modularWallObjectSpawn.isBuildingWalls;
        }
    }

    public class ObjectSpawn_Segments_BuildShortcutContext : ShortcutContext
    {
        protected override bool evaluate()
        {
            return GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                   ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Segments &&
                   ObjectSpawn.instance.segmentsObjectSpawn.isBuildingSegments;
        }
    }

    public class ObjectSpawn_Box_BuildShortcutContext : ShortcutContext
    {
        protected override bool evaluate()
        {
            return GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                   ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Box &&
                   ObjectSpawn.instance.boxObjectSpawn.isBuildingBox;
        }
    }

    public class ObjectSpawn_TileRules_ShortcutContext : ShortcutContext
    {
        protected override bool evaluate()
        {
            return GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.TileRules;
        }
    }

    public class ObjectSpawn_Curve_ShortcutContext : ShortcutContext
    {
        protected override bool evaluate()
        {
            return GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSpawn &&
                ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Curve;
        }
    }

    public class ObjectSelectionShortcutContext : ShortcutContext
    {
        protected override bool evaluate()
        {
            return GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectSelection &&
                !ObjectSelection.instance.isAnyTransformSessionActive;
        }
    }

    public class ObjectEraseShortcutContext : ShortcutContext
    {
        protected override bool evaluate()
        {
            return GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectErase;
        }
    }

    public class ObjectModularSnapShortcutContext : ShortcutContext
    {
        protected override bool evaluate()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            if (toolId == LevelDesignToolId.ObjectSpawn)
            {
                return ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.ModularSnap || 
                        (ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.ModularWalls && !ObjectSpawn.instance.modularWallObjectSpawn.isBuildingWalls) ||
                        (ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Segments && !ObjectSpawn.instance.segmentsObjectSpawn.isBuildingSegments) ||
                        (ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Box && !ObjectSpawn.instance.boxObjectSpawn.isBuildingBox);
            }
            else if (toolId == LevelDesignToolId.ObjectSelection) return ObjectSelection.instance.isTransformSessionActive(ObjectTransformSessionType.ModularSnap);
            else return false;
        }
    }

    public class ObjectSurfaceSnapShortcutContext : ShortcutContext
    {
        protected override bool evaluate()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            if (toolId == LevelDesignToolId.ObjectSpawn) return ObjectSpawn.instance.activeToolId == ObjectSpawnToolId.Props;
            else if (toolId == LevelDesignToolId.ObjectSelection) return ObjectSelection.instance.isTransformSessionActive(ObjectTransformSessionType.SurfaceSnap);
            else return false;
        }
    }
}
#endif