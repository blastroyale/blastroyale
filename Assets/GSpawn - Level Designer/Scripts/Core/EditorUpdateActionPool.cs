#if UNITY_EDITOR
using System.Collections.Generic;

namespace GSpawn
{
    public class EditorUpdateActionPool
    {
        private List<EditorUpdateAction>    _actionsToRemove   = new List<EditorUpdateAction>();
        private List<EditorUpdateAction>    _actions           = new List<EditorUpdateAction>();

        public void registerAction(EditorUpdateAction action)
        {
            _actions.Add(action);
        }

        public void onEditorUpdate()
        {
            if (_actions.Count != 0)
            {
                foreach(var action in _actions)
                {
                    if (action.attemptExecute())
                        _actionsToRemove.Add(action);
                }

                foreach (var action in _actionsToRemove)
                    _actions.Remove(action);

                _actionsToRemove.Clear();
            }
        }
    }
}
#endif