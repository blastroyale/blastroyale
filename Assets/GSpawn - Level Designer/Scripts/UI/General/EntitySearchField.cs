#if UNITY_EDITOR
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System;

namespace GSpawn
{
    public class EntitySearchField
    {
        private List<string>            _entityNameBuffer   = new List<string>();
        private string                  _searchName         = string.Empty;
        private HashSet<string>         _matchNames         = new HashSet<string>();
        private ToolbarSearchField      _searchField;

        private Action<List<string>>    _onFetchEntityNames;
        private Action<string>          _onFilterEntities;

        public IStyle                   style               { get { return _searchField.style; } }

        public EntitySearchField(VisualElement parent, Action<List<string>> onFetchEntityNames, Action<string> onFilterEntities)
        {
            _onFetchEntityNames = onFetchEntityNames;
            _onFilterEntities   = onFilterEntities;

            _searchField = new ToolbarSearchField();
            parent.Add(_searchField);
            _searchField.style.flexGrow = 1.0f;

            _searchField.RegisterValueChangedCallback((p) =>
            {
                _searchName = _searchField.value;
                findMatchNames();
                _onFilterEntities(_searchName);
            });
        }

        public void clearSearchName(bool filterEntities)
        {
            _searchField.value = "";
            findMatchNames();

            if (filterEntities)
                _onFilterEntities(_searchName);
        }

        public void refreshMatchNames()
        {
            findMatchNames();
        }

        public void refreshMatchNames(List<string> entityNames)
        {
            findMatchNames();
            findMatchNames(entityNames);
        }

        public bool matchName(string name)
        {
            if (string.IsNullOrEmpty(_searchName)) return true;
            return _matchNames.Contains(name);
        }

        private void findMatchNames()
        {
            if (!string.IsNullOrEmpty(_searchName))
            {
                _entityNameBuffer.Clear();
                _onFetchEntityNames(_entityNameBuffer);
                _matchNames = new HashSet<string>(StringMatch.match(_entityNameBuffer, _searchName, StringMatch.Case.Insensitive));
            }
        }

        private void findMatchNames(List<string> entityNames)
        {
            _entityNameBuffer.Clear();
            _entityNameBuffer.AddRange(entityNames);

            if (!string.IsNullOrEmpty(_searchName))
            {
                _matchNames = new HashSet<string>(StringMatch.match(_entityNameBuffer, _searchName, StringMatch.Case.Insensitive));
            }
        }
    }
}
#endif