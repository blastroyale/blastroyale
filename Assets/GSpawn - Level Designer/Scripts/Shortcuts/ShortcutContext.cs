#if UNITY_EDITOR
using System.Collections.Generic;

namespace GSpawn
{
    public abstract class ShortcutContext
    {
        private ShortcutContext         _parent;
        private List<ShortcutContext>   _children       = new List<ShortcutContext>();
        private bool                    _active         = false;

        public bool                     active          { get { return _active; } }
        public virtual bool             alwaysActive    { get { return false; } }

        public void setParentContext(ShortcutContext parentContext)
        {
            if (parentContext == _parent || parentContext == this) return;

            if (_parent != null) _parent._children.Remove(this);

            if (parentContext != null)
            {
                _parent = parentContext;
                _parent._children.Add(this);
            }
            else _parent = null;
        }

        public bool evaluateHierarchy()
        {
            _active = alwaysActive || evaluate();
            if (!_active)
            {
                // Note: If this context is not active, all it's children can be
                //       marked as inactive. If we don't do this, children might 
                //       be treated as being active even though they are not.
                setChildrenActiveRecurse(this, false);
                return false;
            }

            bool foundActiveChild = false;
            foreach(var child in _children)
            {
                if (child.evaluateHierarchy())
                {
                    foundActiveChild = true;

                    // Note: When a child is found active, we will deactivate the parent
                    //       because we always want to activate the context deepest
                    //       down the hierarchy.
                    if (!alwaysActive) _active = false;
                }
            }

            return foundActiveChild || _active;
        }

        protected abstract bool evaluate();

        private void setChildrenActiveRecurse(ShortcutContext parent, bool active)
        {
            foreach (var child in parent._children)
            {
                child._active = active;
                setChildrenActiveRecurse(child, active);
            }
        }
    }
}
#endif