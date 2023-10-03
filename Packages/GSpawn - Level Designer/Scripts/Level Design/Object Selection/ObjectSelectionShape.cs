#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace GSpawn
{
    public abstract class ObjectSelectionShape
    {
        public enum Type
        {
            Rect = 0,
            Segments,
            Box
        }

        protected UndoConfig            _undoConfig             = new UndoConfig { allowUndoRedo = true, collapseToGroup = true };
        protected List<GameObject>      _overlappedObjects      = new List<GameObject>();

        public abstract bool            selecting               { get; }
        public abstract Type            shapeType               { get; }

        public void onSceneGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (FixedShortcuts.cancelAction(e))
                {
                    cancel();

                    _overlappedObjects.Clear();
                    ObjectSelection.instance.setMultiSelectedObjects(_overlappedObjects, shapeType, _undoConfig);
                    return;
                }
            }

            update();
            if (selecting)
            {
                detectOverlappedObjects();
                ObjectSelection.instance.setMultiSelectedObjects(_overlappedObjects, shapeType, _undoConfig);
                _overlappedObjects.Clear();
            }

            draw();
        }

        public abstract void cancel();

        protected abstract void update();
        protected abstract void detectOverlappedObjects();
        protected abstract void draw();
    }
}
#endif