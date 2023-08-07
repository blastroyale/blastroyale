#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace GSpawn
{
    public static class UIValues
    {
        private static StyleSheet   _buttonStyles;

        public static StyleSheet    buttonStyles
        {
            get
            {
                if (_buttonStyles == null) _buttonStyles = Resources.Load<StyleSheet>("USS/ButtonStyles");
                return _buttonStyles;
            }
        }

        #region General
        public static bool  isProSkin                   { get { return EditorGUIUtility.isProSkin; } }
        public static float pluginWindowMargin          { get { return 10.0f; } }
        public static float wndMargin                   { get { return 5.0f; } }
        public static Color activeButtonColor           { get { return ColorEx.create(255, 152, 73, 255); } }
        public static Color inactiveButtonColor         { get { return ColorEx.create(255, 255, 255, 255).createNewAlpha(0.0f); } }
        public static Color inactiveButtonTintColor     { get { return Color.white; } }
        public static Color infoLabelColor              { get { return isProSkin ? Color.white : Color.black; } }
        public static Color importantInfoLabelColor     { get { return isProSkin ? ColorEx.darkOrange : Color.magenta; } }
        public static Color sceneViewSectionLabelColor  { get { return isProSkin ? Color.green : Color.blue; } }
        public static Color uiSectionBkColor            { get { return isProSkin ? ColorEx.create(34, 34, 34, 255) : ColorEx.create(200, 200, 200, 255); } }
        public static float uiSectionSeparatorSize      { get { return 1.0f; } }
        public static float actionButtonLeftMargin      { get { return -3.0f; } }
        public static Color lineSeparatorBorderColor    { get { return Color.gray.createNewAlpha(0.5f); } }
        public static float settingsMarginLeft          { get { return 8.0f; } }
        public static float settingsMarginRight         { get { return 8.0f; } }
        public static float settingsMarginBottom        { get { return 8.0f; } }
        public static float settingsMarginTop           { get { return 8.0f; } }
        public static float prefsTitleMarginTop         { get { return 3.0f; } }
        public static float inlineToggleWidth           { get { return 45.0f; } }
        public static float useDefaultsButtonWidth      { get { return 110.0f; } }
        public static float smallButtonSize             { get { return 16.0f; } }
        public static float smallIconSize               { get { return 16.0f; } }
        public static float smallHeaderIconSize         { get { return 16.0f; } }
        public static float mediumHeaderIconSize        { get { return 24.0f; } }
        public static float imGUIContainerMarginLeft    { get { return 3.0f; } }
        public static Color unavailableTextColor        { get { return Color.yellow; } }
        public static Color disabledColor               { get { return Color.gray.createNewAlpha(disabledOpacity); } }
        public static float disabledOpacity             { get { return 0.3f; } }
        #endregion

        #region Prefab Previews
        public static float minPrefabPreviewScale       { get { return 0.5f; } }
        public static float maxPrefabPreviewScale       { get { return 2.0f; } }
        public static float defaultPrefabPreviewScale   { get { return 0.7f; } }
        public static Color prefabPreviewNameLabelColor { get { return UIValues.isProSkin ? Color.white : Color.black; } }
        public static Color unusedPrefabLabelColor      { get { return importantInfoLabelColor; } }
        #endregion

        #region Toolbar
        public static float smallToolbarButtonSize      { get { return 16.0f; } }
        public static float mediumToolbarButtonSize     { get { return 24.0f; } }
        public static float toolbarTextFieldHeight      { get { return 16.0f; } }
        public static Color toolbarBkColor              { get { return isProSkin ? ColorEx.create(34, 34, 34, 255) : ColorEx.create(200, 200, 200, 255); } }
        public static Color toolbarBorderColor          { get { return isProSkin ? ColorEx.create(56, 56, 56, 255) : ColorEx.create(56, 56, 56, 255); } }
        public static Color toolbarSpacerColor          { get { return isProSkin ? ColorEx.create(32, 32, 32, 255) : ColorEx.create(56, 56, 56, 255); } }
        #endregion

        #region List_Grid_Tree_View
        public static Color selectedListItemColor       { get { return isProSkin ? ColorEx.create(62, 95, 150, 255) : ColorEx.create(61, 128, 223, 255); } }
        public static Color unselectedListItemColor     { get { return isProSkin ? Color.white.createNewAlpha(0.0f) : ColorEx.create(229, 229, 229, 255); } }
        public static Color listItemSeparatorColor      { get { return Color.white.createNewAlpha(0.0f); } }
        public static Color dropDestinationItemColor    { get { return ColorEx.create(68, 68, 68, 255); } }
        public static Color listItemTextColor           { get { return isProSkin ? ColorEx.create(188, 188, 188, 255) : Color.black; } }
        public static Color listViewBorderColor         { get { return isProSkin ? ColorEx.create(37, 37, 37, 255) : ColorEx.create(100, 100, 100, 255); } }
        public static Color copySourceListItemTextColor { get { return Color.green; } }
        public static Color copySourceGridItemTextColor { get { return Color.green; } }
        public static float listItemLeftMargin          { get { return 5.0f; } }
        public static float listItemRightMargin         { get { return 5.0f; } }
        public static float itemClickRenameDelay        { get { return 1.1f; } }
        #endregion

        #region Header
        public static Color focusedHeaderColor          { get { return ColorEx.create(0, 74, 127); } }
        public static Color unfocusedHeaderColor        { get { return Color.gray; } }
        public static Color headerTextColor             { get { return Color.white; } }
        public static float headerHeight                { get { return 20.0f; } }
        public static Color headerBkColor               { get { return isProSkin ? ColorEx.create(50, 50, 50, 255) : ColorEx.create(229, 229, 229, 255); } }
        #endregion
    }
}
#endif