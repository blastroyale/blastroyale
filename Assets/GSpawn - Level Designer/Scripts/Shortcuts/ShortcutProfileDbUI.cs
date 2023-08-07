#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace GSpawn
{
    public class ShortcutProfileDbUI : PluginUI
    {
        [SerializeField]
        private ListViewState                                           _categoryViewState;
        [NonSerialized]
        private ListView<UIShortcutCategoryItem, ShortcutCategory>      _categoryView;
        [SerializeField]
        private ListViewState                                           _shortcutViewState;
        [NonSerialized]
        private ListView<UIShortcutItem, Shortcut>                      _shortcutView;
        [NonSerialized]
        private VisualElement                                           _viewContainer;
        [NonSerialized]
        private ProfileSelectionUI<ShortcutProfileDb, ShortcutProfile>  _profileSelectionUI;

        public static ShortcutProfileDbUI                               instance                { get { return ShortcutProfileDb.instance.ui; } }

        protected override void onRefresh()
        {
            _categoryView.refreshUI();
            _shortcutView.refreshUI();
        }

        protected override void onBuild()
        {
            _profileSelectionUI                 = new ProfileSelectionUI<ShortcutProfileDb, ShortcutProfile>();
            _profileSelectionUI.build(ShortcutProfileDb.instance, "shortcut", contentContainer);
            contentContainer.style.flexGrow     = 1.0f;

            _viewContainer                      = new VisualElement();
            contentContainer.Add(_viewContainer);
            _viewContainer.style.flexDirection  = FlexDirection.Row;
            _viewContainer.style.flexGrow       = 1.0f;

            createCategoryView();
            populateCategoryView();

            createShortcutView();
            populateShortcutView();

            UI.createUseDefaultsButton(new List<Action>() { () => { ShortcutProfileDb.instance.activeProfile.useDefaults(); populateShortcutView(); } }, contentContainer);
        }

        protected override void onEnabled()
        {
            ShortcutProfileDb.instance.activeProfileChanged += onActiveProfileChanged;

            if (_categoryViewState == null)
            {
                _categoryViewState      = ScriptableObject.CreateInstance<ListViewState>();
                _categoryViewState.name = GetType().Name + "_CategoryViewState";
                AssetDbEx.addObjectToAsset(_categoryViewState, ShortcutProfileDb.instance);
            }
            if (_shortcutViewState == null)
            {
                _shortcutViewState      = ScriptableObject.CreateInstance<ListViewState>();
                _shortcutViewState.name = GetType().Name + "_ShortcutViewState";
                AssetDbEx.addObjectToAsset(_shortcutViewState, ShortcutProfileDb.instance);
            }
        }

        protected override void onDisabled()
        {
            ShortcutProfileDb.instance.activeProfileChanged -= onActiveProfileChanged;
        }

        protected override void onUndoRedo()
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            if (_categoryView != null) populateCategoryView();
            if (_shortcutView != null) populateShortcutView();
        }

        protected override void onDestroy()
        {
            ScriptableObjectEx.destroyImmediate(_categoryViewState);
            ScriptableObjectEx.destroyImmediate(_shortcutViewState);
        }

        private void createCategoryView()
        {
            var container                   = new VisualElement();
            _viewContainer.Add(container);

            var header                      = UI.createColumnHeader("Category", container);
            header.style.width              = 180.0f;
            header.style.borderBottomWidth  = 0.0f;
            header.style.borderTopWidth     = 0.0f;

            _categoryView                   = new ListView<UIShortcutCategoryItem, ShortcutCategory>(_categoryViewState, container);
            _categoryView.style.setBorderWidth(1.0f);
            _categoryView.style.setBorderColor(UIValues.listViewBorderColor);
            _categoryView.style.width       = header.style.width;

            _categoryView.selectionChanged  += onCategorySelectionChanged;
        }

        private void populateCategoryView()
        {
            if (_categoryView == null) return;

            _categoryViewState.clearSelectionInfo();
            _categoryView.onBeginBuild();

            var categories = new List<ShortcutCategory>();
            ShortcutProfileDb.instance.activeProfile.getShortcutCategories(categories);
            foreach (var category in categories)
                _categoryView.addItem(category, true);

            _categoryView.onEndBuild();
        }

        private void createShortcutView()
        {
            var container               = new VisualElement();
            container.style.flexGrow    = 1.0f;
            container.style.marginLeft  = 2.0f;
            _viewContainer.Add(container);

            var headerColumns = new List<UI.HeaderColumnDesc>()
            {
                new UI.HeaderColumnDesc { text = "Command", width = UIShortcutItem.shortcutNameWidth + TexturePool.instance.refresh.width - 3.0f },
                new UI.HeaderColumnDesc { text = "Shortcut" }
            };
            var header = UI.createColumnHeader(headerColumns, container);
            header.style.borderBottomWidth      = 0.0f;
            header.style.borderTopWidth         = 0.0f;

            _shortcutView = new ListView<UIShortcutItem, Shortcut>(_shortcutViewState, container);
            _shortcutView.style.setBorderWidth(1.0f);
            _shortcutView.style.setBorderColor(UIValues.listViewBorderColor);
        }

        private void populateShortcutView()
        {
            if (_shortcutView == null) return;

            _shortcutViewState.clearSelectionInfo();
            _shortcutView.onBeginBuild();

            var selectedCategories = new List<ShortcutCategory>();
            _categoryView.getSelectedItemData(selectedCategories);
            if (selectedCategories.Count != 0)
            {
                var category = selectedCategories[0];
                var shortcuts = new List<Shortcut>();
                category.getShortcuts(shortcuts);

                foreach (var s in shortcuts)
                    _shortcutView.addItem(s, true);
            }

            _shortcutView.onEndBuild();
        }

        private void onActiveProfileChanged(ShortcutProfile newActiveProfile)
        {
            if (_profileSelectionUI != null)
                _profileSelectionUI.refresh();

            populateCategoryView();
            populateShortcutView();
        }

        private void onCategorySelectionChanged(ListView<UIShortcutCategoryItem, ShortcutCategory> listView)
        {
            populateShortcutView();
        }
    }
}
#endif