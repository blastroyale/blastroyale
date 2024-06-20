#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class IntPatternDbUI : PluginUI
    {
        private const float         _minFontSize        = 10.0f;
        private const float         _maxFontSize        = 30.0f;

        private VisualElement       _viewSplitter;
        private TextField           _codeField;
        private TextField           _newPatternNameField;
        private Button              _compileBtn;

        [SerializeField]
        private float               _codeFontSize       = 14.0f;

        [SerializeField]
        private ListViewState                           _patternViewState;
        private ListView<UIIntPatternItem, IntPattern>  _patternView;
        private EntitySearchField                       _patternSearchField;

        [NonSerialized]
        private List<IntPattern>        _patternBuffer  = new List<IntPattern>();

        public float                    codeFontSize    { get { return _codeFontSize; } set { UndoEx.record(this); _codeFontSize = Mathf.Max(_minFontSize, value); } }

        public static IntPatternDbUI    instance        { get { return IntPatternDb.instance.ui; } }

        protected override void onRefresh()
        {
            if (_patternView != null) _patternView.refreshUI();
        }

        protected override void onBuild()
        {
            contentContainer.style.flexGrow     = 1.0f;
            createSearchToolbar();

            _viewSplitter = new VisualElement();
            _viewSplitter.style.flexGrow        = 1.0f;
            _viewSplitter.style.flexDirection   = FlexDirection.Row;
            contentContainer.Add(_viewSplitter);

            createPatternView();
            populatePatternView();
            createCodeArea();
            createBottomToolbar();
        }

        private void createSearchToolbar()
        {
            var toolbar = new Toolbar();
            toolbar.style.flexShrink = 0.0f;
            contentContainer.Add(toolbar);

            _patternSearchField = new EntitySearchField(toolbar, (entityNames) =>
            { IntPatternDb.instance.getPatternNames(entityNames, null); },
            (name) => { _patternView.filterItems(filterPattern); });
        }

        private void createPatternView()
        {
            _patternView = new ListView<UIIntPatternItem, IntPattern>(_patternViewState, _viewSplitter);
            _patternView.canDelete                  = true;
            _patternView.canRenameItems             = true;
            _patternView.canMultiSelect             = true;

            _patternView.selectedItemsWillBeDeleted += onSelectedPatternItemsWillBeDeleted;
            _patternView.selectionChanged           += onPatternSelectionChanged;
            _patternView.selectionDeleted           += onPatternSelectionDeleted;
            _patternView.canDeleteItem              += onCanDeleteItem;

            _patternView.style.setBorderWidth(1.0f);
            _patternView.style.setBorderColor(UIValues.listViewBorderColor);
            _patternView.style.width                = 200.0f;
            _patternView.style.flexGrow             = 0.0f;
        }

        private void createCodeArea()
        {
            _codeField                  = new TextField();
            _viewSplitter.Add(_codeField);

            _codeField.multiline        = true;
            _codeField.style.flexGrow   = 1.0f;
            _codeField.style.fontSize   = _codeFontSize;

            updateCodeField();
        }

        private void createBottomToolbar()
        {
            var bottomToolbar                   = new Toolbar();
            bottomToolbar.style.flexShrink      = 0.0f;
            contentContainer.Add(bottomToolbar);

            var createPatternButton             = UI.createSmallCreateNewToolbarButton(bottomToolbar);
            createPatternButton.clicked         += () => { createNewPattern(); };
            createPatternButton.tooltip         = "Create a new pattern with the specified name.";

            _newPatternNameField                = UI.createToolbarTextField(bottomToolbar);
            _newPatternNameField.style.flexGrow = 0.0f;
            _newPatternNameField.style.width    = _patternView.style.width.value.value - 22.0f;

            var codeFontSizeSlider = UI.createSlider("_codeFontSize", serializedObject, string.Empty, "Code font size [" + codeFontSize + "]", _minFontSize, _maxFontSize, bottomToolbar);
            codeFontSizeSlider.style.width = 80.0f;
            codeFontSizeSlider.RegisterValueChangedCallback
                ((p) =>
                {
                    _codeField.style.fontSize = _codeFontSize;
                    codeFontSizeSlider.tooltip = "Code font size [" + codeFontSize + "]";
                });

            VisualElement indent = UI.createHorizontalSpacer(bottomToolbar);
            bottomToolbar.Add(indent);

            _compileBtn             = new Button();
            _compileBtn.text        = "Compile";
            _compileBtn.tooltip     = "Compile the selected pattern.";
            bottomToolbar.Add(_compileBtn);

            _compileBtn.clicked += () => 
            {
                var selectedPatterns = new List<IntPattern>();
                _patternView.getSelectedItemData(selectedPatterns);
                if (selectedPatterns.Count == 1)
                {
                    if (selectedPatterns[0].compile(_codeField.text))
                        EditorUtility.DisplayDialog("Pattern Compilation Status", "Pattern compiled successfully.", "OK");
                    else EditorUtility.DisplayDialog("Pattern Compilation Status", "Pattern failed to compile. Please check the Console window.", "OK");
                }
            };

            updateCompileButton();
        }

        private void createNewPattern()
        {
            var newPattern = IntPatternDb.instance.createPattern(_newPatternNameField.text);
            if (newPattern != null)
            {
                _patternView.setAllItemsSelected(false, false, false);
                PluginGuid newPatternId = _patternView.addItem(newPattern, true);
                _patternView.setItemSelected(newPatternId, true, false);
                _patternView.scheduleScrollToItem(newPatternId);

                updateCodeField();
                updateCompileButton();
            }
        }

        private void populatePatternView()
        {
            if (_patternView == null) return;
            _patternSearchField.refreshMatchNames();

            _patternView.onBeginBuild();
            IntPatternDb.instance.getPatterns(_patternBuffer);

            foreach (var pattern in _patternBuffer)
                _patternView.addItem(pattern, filterPattern(pattern));

            _patternView.onEndBuild();
        }

        private void onSelectedPatternItemsWillBeDeleted(ListView<UIIntPatternItem, IntPattern> listView, List<PluginGuid> itemIds)
        {
            var patterns = new List<IntPattern>();
            _patternView.getItemData(itemIds, patterns);

            SegmentsObjectSpawnSettingsProfileDb.instance.onIntPatternsWillBeDeleted(patterns);
            ObjectSpawn.instance.tileRuleObjectSpawn.settings.onIntPatternsWillBeDeleted(patterns);
            IntPatternDb.instance.deletePatterns(patterns);

            updateCodeField();
            updateCompileButton();
        }

        private void onPatternSelectionChanged(ListView<UIIntPatternItem, IntPattern> listView)
        {
            updateCodeField();
            updateCompileButton();
        }

        private void onPatternSelectionDeleted(ListView<UIIntPatternItem, IntPattern> listView)
        {
            updateCodeField();
            updateCompileButton();

            PluginInspectorUI.instance.refresh();
        }

        private bool onCanDeleteItem(PluginGuid id)
        {
            return id != IntPatternDb.instance.defaultPattern.guid;
        }

        private bool filterPattern(IntPattern pattern)
        {
            if (!_patternSearchField.matchName(pattern.patternName)) return false;
            return true;
        }

        private void updateCodeField()
        {
            if (_patternView == null || _codeField == null) return;
            if (IntPatternDb.instance.numPatterns == 0 || 
                _patternView.numSelectedItems > 1 ||
                _patternView.numSelectedItems == 0) _codeField.SetEnabled(false);
            else
            {
                _patternView.getSelectedItemData(_patternBuffer);
                _codeField.SetValueWithoutNotify(_patternBuffer[0].text);
                if (_patternBuffer.Count == 1)
                {
                    if (_patternBuffer[0] == IntPatternDb.instance.defaultPattern)
                    {
                        _codeField.SetEnabled(false);
                        return;
                    }
                }
                _codeField.SetEnabled(true);
            }
        }

        private void updateCompileButton()
        {
            if (_patternView == null || _compileBtn == null) return;
            if (IntPatternDb.instance.numPatterns == 0 || 
                _patternView.numSelectedItems > 1 ||
                _patternView.numSelectedItems == 0) _compileBtn.SetEnabled(false);
            else _compileBtn.SetEnabled(true);
        }

        protected override void onUndoRedo()
        {
            if (_patternView != null)
                populatePatternView();

            updateCodeField();
            updateCompileButton();
        }

        protected override void onEnabled()
        {
            if (_patternViewState == null)
            {
                _patternViewState       = ScriptableObject.CreateInstance<ListViewState>();
                _patternViewState.name  = GetType().Name + "_PatternViewState";
                AssetDbEx.addObjectToAsset(_patternViewState, IntPatternDb.instance);
            }
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_patternViewState);
        }
    }
}
#endif
