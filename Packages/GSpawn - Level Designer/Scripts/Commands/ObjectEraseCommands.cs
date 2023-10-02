#if UNITY_EDITOR
namespace GSpawn
{
    public class ObjectErase_EnableEraseCursor : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectErase)
                ObjectErase.instance.activeToolId = ObjectEraseToolId.Cursor;
        }
    }

    public class ObjectErase_EnableEraseBrush2D : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectErase)
                ObjectErase.instance.activeToolId = ObjectEraseToolId.Brush2D;
        }
    }

    public class ObjectErase_EnableEraseBrush3D : PluginCommand
    {
        protected override void onEnter()
        {
            if (GSpawn.active.levelDesignToolId == LevelDesignToolId.ObjectErase)
                ObjectErase.instance.activeToolId = ObjectEraseToolId.Brush3D;
        }
    }
}
#endif