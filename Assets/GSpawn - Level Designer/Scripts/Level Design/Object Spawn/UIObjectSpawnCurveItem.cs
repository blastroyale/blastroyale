#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class UIObjectSpawnCurveItem : ListViewItem<ObjectSpawnCurve>
    {
        public override PluginGuid      guid                { get { return getItemId(data); } }
        public override string          displayName         { get { return data.curveName; } }

        public static PluginGuid getItemId(ObjectSpawnCurve curve) 
        {
            return curve.guid; 
        }

        public static void getItemIds(List<ObjectSpawnCurve> curves, List<PluginGuid> ids)
        {
            ids.Clear();
            foreach (var curve in curves)
                ids.Add(getItemId(curve));
        }

        protected override void onRefreshUI()
        {
            applyVariableStates();
        }

        protected override void onBuildUIBeforeDisplayName()
        {
             UI.createIcon(TexturePool.instance.curveSpawn, 16.0f, this);
        }

        protected override void onBuildUIAfterDisplayName()
        {
            _displayNameLabel.style.flexGrow                = 1.0f;
            _displayNameLabel.style.marginLeft              = 5.0f;
            _displayNameLabel.style.color                   = UIValues.listItemTextColor;
            _displayNameLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            _displayNameLabel.RegisterCallback<MouseDownEvent>(p => 
            {
                if (p.clickCount == 2)
                    data.frame();
            });

            _renameField.style.flexGrow                     = 1.0f;

            var refreshCurveButton                          = UI.createButton(TexturePool.instance.refresh, UI.ButtonStyle.Push, UIValues.smallButtonSize, this);
            refreshCurveButton.tooltip                      = "Refresh curve.";
            refreshCurveButton.clicked                      += () => { data.refresh(ObjectSpawnCurveRefreshReason.Refresh); };
            UI.useDefaultMargins(refreshCurveButton);
            refreshCurveButton.style.marginTop              = -0.5f;
            refreshCurveButton.style.marginRight            = 0.0f;

            applyVariableStates();
        }

        protected override void onBeginRename()
        {
            _renameField.SetValueWithoutNotify(displayName);
        }

        protected override void onEndRename(bool commit)
        {
            if (commit)
            {
                ObjectSpawnCurveDb.instance.renameCurve(data, _renameField.text);
            }
        }

        private void applyVariableStates()
        {
            _displayNameLabel.text = displayName;
        }
    }
}
#endif