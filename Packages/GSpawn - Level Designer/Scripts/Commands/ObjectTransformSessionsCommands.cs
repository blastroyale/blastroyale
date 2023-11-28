#if UNITY_EDITOR
namespace GSpawn
{
    public class ObjectTransformSession_ModularSnap_VerticalStepUp : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId      = GSpawn.active.levelDesignToolId;
            var command     = new ObjectModularSnapSessionCommand() 
            { id = ObjectModularSnapSessionCommandId.VerticalStep, verticalStepDirection = VerticalStepDirection.Up };

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.executeModularSnapSessionCommand(command);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.executeModularSnapSessionCommand(command);
        }
    }

    public class ObjectTransformSession_ModularSnap_VerticalStepDown : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId      = GSpawn.active.levelDesignToolId;
            var command     = new ObjectModularSnapSessionCommand() 
            { id = ObjectModularSnapSessionCommandId.VerticalStep, verticalStepDirection = VerticalStepDirection.Down };

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.executeModularSnapSessionCommand(command);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.executeModularSnapSessionCommand(command);
        }
    }

    public class ObjectTransformSession_ModularSnap_ResetVerticalStep : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId      = GSpawn.active.levelDesignToolId;
            var command     = new ObjectModularSnapSessionCommand() 
            { id = ObjectModularSnapSessionCommandId.ResetVerticalStep };

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.executeModularSnapSessionCommand(command);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.executeModularSnapSessionCommand(command);
        }
    }

    public class ObjectTransformSession_ModularSnap_ResetVerticalStepToOriginal : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId      = GSpawn.active.levelDesignToolId;
            var command     = new ObjectModularSnapSessionCommand() 
            { id = ObjectModularSnapSessionCommandId.ResetVerticalStepToOriginal };

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.executeModularSnapSessionCommand(command);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.executeModularSnapSessionCommand(command);
        }
    }

    public class ObjectTransformSession_ModularSnap_ToggleHalfSpace : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId      = GSpawn.active.levelDesignToolId;
            var command     = new ObjectModularSnapSessionCommand() 
            { id = ObjectModularSnapSessionCommandId.ToggleSnapHalfSpace };

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.executeModularSnapSessionCommand(command);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.executeModularSnapSessionCommand(command);
        }
    }

    public class ObjectTransformSession_ModularSnap_ToggleObject2ObjectSnap : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId      = GSpawn.active.levelDesignToolId;
            var command     = new ObjectModularSnapSessionCommand() 
            { id = ObjectModularSnapSessionCommandId.ToggleObject2ObjectSnap };

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.executeModularSnapSessionCommand(command);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.executeModularSnapSessionCommand(command);
        }
    }

    public class ObjectTransformSession_ModularSnap_ToggleGridSnapObjectClimb : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            var command = new ObjectModularSnapSessionCommand()
            { id = ObjectModularSnapSessionCommandId.ToggleGridSnapObjectClimb };

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.executeModularSnapSessionCommand(command);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.executeModularSnapSessionCommand(command);
        }
    }

    public class ObjectTransformSession_ModularSnap_ToggleAlignmentHighlights : PluginCommand
    {
        protected override void onEnter() 
        {
            UndoEx.saveEnabledState();
            UndoEx.enabled = false;
            ObjectTransformSessionPrefs.instance.modularSnapDrawAlingmentHighlights = !ObjectTransformSessionPrefs.instance.modularSnapDrawAlingmentHighlights;
            UndoEx.restoreEnabledState();
        }
    }

    public class ObjectTransformSession_ModularSnap_ToggleAlignmentHints : PluginCommand
    {
        protected override void onEnter()
        {
            UndoEx.saveEnabledState();
            UndoEx.enabled = false;
            ObjectTransformSessionPrefs.instance.modularSnapShowAlignmentHints = !ObjectTransformSessionPrefs.instance.modularSnapShowAlignmentHints;
            UndoEx.restoreEnabledState();
        }
    }

    public class ObjectTransformSession_SurfaceSnap_ResetMouseOffsetFromSurface : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId      = GSpawn.active.levelDesignToolId;
            var command     = new ObjectSurfaceSnapSessionCommand() 
            { id = ObjectSurfaceSnapSessionCommandId.SetOffsetFromSurface, appliedOffsetFromSurface = 0.0f };

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.executeSurfaceSnapSessionCommand(command);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.executeSurfaceSnapSessionCommand(command);
        }
    }

    public class ObjectTransformSession_SurfaceSnap_ToggleAxisAlignment : PluginCommand
    {
        protected override void onEnter()
        {
            var toolId = GSpawn.active.levelDesignToolId;
            var command = new ObjectSurfaceSnapSessionCommand()
            { id = ObjectSurfaceSnapSessionCommandId.ToggleAxisAlignment };

            if (toolId == LevelDesignToolId.ObjectSpawn) ObjectSpawn.instance.executeSurfaceSnapSessionCommand(command);
            else if (toolId == LevelDesignToolId.ObjectSelection) ObjectSelection.instance.executeSurfaceSnapSessionCommand(command);
        }
    }
}
#endif