#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GSpawn
{
    public class ObjectTransformUI : ScriptableObject
    {
        private IEnumerable<GameObject>         _targetObjects;
        private List<Transform>                 _targetTransforms   = new List<Transform>();
        //private List<Transform> _targetParentTransforms = new List<Transform>();
        private TransformDiffCheck.DiffInfo     _transformDiffInfo;
        private ObjectLayerDiff                 _objectLayerDiff;
        private bool[]                          _positionDiff       = new bool[3];
        private bool[]                          _rotationDiff       = new bool[3];
        private bool[]                          _scaleDiff          = new bool[3];
        private IMGUIContainer                  _settingsContainer;
        private VisualElement                   _positionContainer;
        private VisualElement                   _rotationContainer;
        private VisualElement                   _scaleContainer;
        private VisualElement                   _snapButtonsContainer;
        private VisualElement                   _alignButtonsContainer;
        private VisualElement                   _noObjectsAvailable;
        private Button                          _snapAllAxesBtn;

        public Func<bool>                       visibilityCondition { get; set; }

        public void onTargetObjectsChanged()
        {
            TransformEx.getTransforms(_targetObjects, _targetTransforms);
            _transformDiffInfo  = TransformDiffCheck.checkLocalDiff(_targetTransforms);
            _objectLayerDiff    = ObjectLayerDiffCheck.checkDiff(_targetTransforms);
        }

        public void removeObjects(List<GameObject> gameObjects)
        {
            _targetTransforms.RemoveAll(item => gameObjects.Contains(item.gameObject));
            _transformDiffInfo  = TransformDiffCheck.checkLocalDiff(_targetTransforms);
            _objectLayerDiff    = ObjectLayerDiffCheck.checkDiff(_targetTransforms);
        }

        public void removeNullObjects()
        {
            _targetTransforms.RemoveAll(item => item == null);
            _transformDiffInfo  = TransformDiffCheck.checkLocalDiff(_targetTransforms);
            _objectLayerDiff    = ObjectLayerDiffCheck.checkDiff(_targetTransforms);
        }

        public void refresh()
        {
            onTargetObjectsChanged();
            refreshTooltips();
        }

        public void refreshTooltips()
        {
            if (_snapAllAxesBtn != null)
                _snapAllAxesBtn.tooltip = ShortcutProfileDb.instance.activeProfile.getShortcutUITooltip(ObjectSelectionShortcutNames.snapAllAxes, "Snap objects to the closest grid point along all 3 axes.");
        }

        public void build(IEnumerable<GameObject> targetObjects, VisualElement parent)
        {
            _targetObjects = targetObjects;
            onTargetObjectsChanged();

            createNoObjectsAvailableLabel(parent);

            var dummyContainer = new IMGUIContainer();
            dummyContainer.style.height = 0.0f;
            parent.Add(dummyContainer);
            dummyContainer.onGUIHandler += () =>
            {
                if (visibilityCondition != null && !visibilityCondition()) return;
                if (_targetTransforms.Count == 0)
                {
                    _settingsContainer.setDisplayVisible(false);
                    _positionContainer.setDisplayVisible(false);
                    _rotationContainer.setDisplayVisible(false);
                    _scaleContainer.setDisplayVisible(false);
                    _snapButtonsContainer.setDisplayVisible(false);
                    _alignButtonsContainer.setDisplayVisible(false);
                    _noObjectsAvailable.setDisplayVisible(true);
                }
                else
                {
                    _settingsContainer.setDisplayVisible(true);
                    _positionContainer.setDisplayVisible(true);
                    _rotationContainer.setDisplayVisible(true);
                    _scaleContainer.setDisplayVisible(true);
                    _snapButtonsContainer.setDisplayVisible(true);
                    _alignButtonsContainer.setDisplayVisible(true);
                    _noObjectsAvailable.setDisplayVisible(false);
                }
            };

            _settingsContainer                  = UI.createIMGUIContainer(parent);
            _settingsContainer.style.alignSelf  = Align.FlexStart;
            _settingsContainer.style.height     = 20.0f;
            _settingsContainer.style.width      = 200.0f;
            _settingsContainer.style.marginTop  = 0.0f;
            _settingsContainer.style.marginLeft = UIValues.smallIconSize + 6.0f;
            _settingsContainer.onGUIHandler     += () =>
            {
                if (visibilityCondition != null && !visibilityCondition()) return;
                if (_targetTransforms.Count != 0)
                {
                    const float labelWidth = 40.0f;
                    EditorUIEx.saveLabelWidth();
                    EditorUIEx.saveShowMixedValue();
                    EditorGUIUtility.labelWidth = labelWidth;

                    // Layers
                    EditorGUI.showMixedValue = _objectLayerDiff.layer;
                    int layer = _objectLayerDiff.layer ? LayerEx.getMaxLayer() + 1 : _targetTransforms[0].gameObject.layer;

                    if (!_objectLayerDiff.layer)
                    {
                        const float top = 3.0f;
                        float left = -UIValues.smallIconSize - 2.0f;
                     
                        if (PluginObjectLayerDb.instance.isLayerTerrainMesh(layer))
                        {
                            Rect rect = new Rect(left, top - 1.0f, UIValues.smallIconSize, UIValues.smallIconSize);
                            GUI.DrawTexture(rect, TexturePool.instance.terrain);
                        }
                        else if (PluginObjectLayerDb.instance.isLayerSphericalMesh(layer))
                        {
                            Rect rect = new Rect(left, top - 1.0f, UIValues.smallIconSize, UIValues.smallIconSize);
                            GUI.DrawTexture(rect, TexturePool.instance.greenSphere);
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    int newLayer = EditorGUILayout.LayerField("Layer ", layer, GUILayout.Width(150.0f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var t in _targetTransforms)
                        {
                            UndoEx.record(t.gameObject);
                            t.gameObject.layer = newLayer;
                        }

                        // Note: Needs to be updated after layers are changed.
                        _objectLayerDiff = ObjectLayerDiffCheck.checkDiff(_targetTransforms);
                    }

                    EditorUIEx.restoreShowMixedValue();
                    EditorUIEx.restoreLabelWidth();
                }
            };

            const float floatFieldWidth = 80.0f;
            _positionContainer = createChannelRow(TransformChannel.Position,
                () => 
                {
                    // Note: When objects are deleted from the scene this needs to be here.
                    //       It seems that putting this in the 'dummyContainer.onGUIHandler'
                    //       doesn't work. 
                    _targetTransforms.RemoveAll(item => item == null);
               
                    if (_targetTransforms.Count == 0) return;                   
                    _transformDiffInfo.getPositionDiff(_positionDiff);
                    var result = EditorUIEx.vector3FieldEx(_targetTransforms[0].localPosition, _positionDiff, floatFieldWidth);
                    if (result.hasChanged)
                    {
                        UndoEx.recordTransforms(_targetTransforms);
                        foreach (var transform in _targetTransforms)
                        {
                            Vector3 newVal = transform.localPosition;
                            newVal[result.changedAxisIndex] = result.newAxisValue;
                            transform.localPosition = newVal;
                        }

                        ObjectEvents.onObjectsTransformedByUI();
                    }
                }, parent);
            _rotationContainer = createChannelRow(TransformChannel.Rotation,
                () =>
                {
                    if (_targetTransforms.Count == 0) return;
                    _transformDiffInfo.getRotationDiff(_rotationDiff);
                    var result = EditorUIEx.vector3FieldEx(_targetTransforms[0].localEulerAngles, _rotationDiff, floatFieldWidth);
                    if (result.hasChanged)
                    {
                        UndoEx.recordTransforms(_targetTransforms);
                        foreach (var transform in _targetTransforms)
                        {
                            Vector3 newVal = transform.localEulerAngles;
                            newVal[result.changedAxisIndex] = result.newAxisValue;
                            transform.localEulerAngles = newVal;
                        }

                        ObjectEvents.onObjectsTransformedByUI();
                    }
                }, parent);
            _scaleContainer = createChannelRow(TransformChannel.Scale,
                () =>
                {
                    if (_targetTransforms.Count == 0) return;
                    _transformDiffInfo.getScaleDiff(_scaleDiff);
                    var result = EditorUIEx.vector3FieldEx(_targetTransforms[0].localScale, _scaleDiff, floatFieldWidth);
                    if (result.hasChanged)
                    {
                        UndoEx.recordTransforms(_targetTransforms);
                        foreach (var transform in _targetTransforms)
                        {
                            Vector3 newVal = transform.localScale;
                            newVal[result.changedAxisIndex] = result.newAxisValue;
                            transform.localScale = newVal;
                        }

                        ObjectEvents.onObjectsTransformedByUI();
                    }
                }, parent);

            _snapButtonsContainer = createSnapButtons(parent);
            _alignButtonsContainer = createAlignButtons(parent);

            refreshTooltips();
        }

        private void createNoObjectsAvailableLabel(VisualElement parent)
        {
            _noObjectsAvailable = new Label("No objects available.");
            _noObjectsAvailable.style.unityFontStyleAndWeight = FontStyle.Bold;
            _noObjectsAvailable.style.marginLeft = 5.0f;
            _noObjectsAvailable.style.color = UIValues.infoLabelColor;
            parent.Add(_noObjectsAvailable);
        }

        private Button createResetButton(string tooltip, VisualElement parent)
        {
            var button = UI.createButton(TexturePool.instance.refresh, UI.ButtonStyle.Push, parent);
            button.style.setBackgroundImage(TexturePool.instance.refresh, true);
            button.style.unityBackgroundImageTintColor = Color.white;
            button.tooltip = tooltip;
            parent.Add(button);

            return button;
        }

        private Label createTransformChannelLabel(string text, VisualElement parent)
        {
            var label               = new Label(text);
            label.style.marginTop   = 1.0f;
            label.style.width       = 50.0f;
            label.style.flexGrow    = 1.0f;
            parent.Add(label);

            return label;
        }

        private VisualElement createChannelRow(TransformChannel channel, Action fieldIMGUIHandler, VisualElement parent)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            parent.Add(row);

            string channelName = "position";
            if (channel == TransformChannel.Rotation) channelName = "rotation";
            else if (channel == TransformChannel.Scale) channelName = "scale";

            Button resetBtn = createResetButton("Reset " + channelName + (channel != TransformChannel.Scale ? " to 0." : " to 1."), row);
            if (channel == TransformChannel.Position)
            {
                resetBtn.clicked += () =>
                {
                    UndoEx.recordTransforms(_targetTransforms);
                    foreach (var transform in _targetTransforms)
                        transform.localPosition = Vector3.zero;

                    ObjectEvents.onObjectsTransformedByUI();
                };
            }
            else
            if (channel == TransformChannel.Rotation)
            {
                resetBtn.clicked += () =>
                {
                    UndoEx.recordTransforms(_targetTransforms);
                    foreach (var transform in _targetTransforms)
                        transform.localRotation = Quaternion.identity;

                    ObjectEvents.onObjectsTransformedByUI();
                };
            }
            else 
            if (channel == TransformChannel.Scale)
            {
                resetBtn.clicked += () =>
                {
                    UndoEx.recordTransforms(_targetTransforms);
                    foreach (var transform in _targetTransforms)
                        transform.localScale = Vector3.one;

                    ObjectEvents.onObjectsTransformedByUI();
                };
            }

            createTransformChannelLabel(channelName.replaceAt(0, Char.ToUpper(channelName[0])), row);

            var container = new IMGUIContainer();
            row.Add(container);
            container.style.overflow = Overflow.Hidden;
            container.onGUIHandler += fieldIMGUIHandler;

            return row;
        }

        private VisualElement createSnapButtons(VisualElement parent)
        {
            var snapRow                 = new VisualElement();
            parent.Add(snapRow);
            snapRow.style.flexDirection = FlexDirection.Row;

            const float buttonWidth = 90.0f;

            var snapBtn         = new Button();
            snapRow.Add(snapBtn);
            snapBtn.text        = "Snap X";
            snapBtn.tooltip     = "Snap objects to the closest grid point along the X axis.";
            snapBtn.style.width = buttonWidth * 0.6f;
            snapBtn.clicked     += () =>
            {
                UndoEx.recordTransforms(_targetTransforms);
                PluginScene.instance.grid.snapTransformsAxis(_targetTransforms, 0);
                ObjectEvents.onObjectsTransformedByUI();
            };

            snapBtn             = new Button();
            snapRow.Add(snapBtn);
            snapBtn.text        = "Snap Y";
            snapBtn.tooltip     = "Snap objects to the closest grid point along the Y axis.";
            snapBtn.style.width = buttonWidth;
            snapBtn.style.width = buttonWidth * 0.6f;
            snapBtn.style.marginLeft = UIValues.actionButtonLeftMargin;
            snapBtn.clicked     += () =>
            {
                UndoEx.recordTransforms(_targetTransforms);
                PluginScene.instance.grid.snapTransformsAxis(_targetTransforms, 1);
                ObjectEvents.onObjectsTransformedByUI();
            };

            snapBtn             = new Button();
            snapRow.Add(snapBtn);
            snapBtn.text        = "Snap Z";
            snapBtn.tooltip     = "Snap objects to the closest grid point along the Z axis.";
            snapBtn.style.width = buttonWidth;
            snapBtn.style.width = buttonWidth * 0.6f;
            snapBtn.style.marginLeft = UIValues.actionButtonLeftMargin;
            snapBtn.clicked     += () =>
            {
                UndoEx.recordTransforms(_targetTransforms);
                PluginScene.instance.grid.snapTransformsAxis(_targetTransforms, 2);
                ObjectEvents.onObjectsTransformedByUI();
            };

            _snapAllAxesBtn             = new Button();
            snapRow.Add(_snapAllAxesBtn);
            _snapAllAxesBtn.text        = "Snap all axes";
            _snapAllAxesBtn.style.width = buttonWidth;
            _snapAllAxesBtn.style.marginLeft = UIValues.actionButtonLeftMargin;
            _snapAllAxesBtn.clicked     += () =>
            {
                UndoEx.recordTransforms(_targetTransforms);
                PluginScene.instance.grid.snapTransformsAllAxes(_targetTransforms);
                ObjectEvents.onObjectsTransformedByUI();
            };

            return snapRow;
        }

        private VisualElement createAlignButtons(VisualElement parent)
        {
            var alignRow = new VisualElement();
            parent.Add(alignRow);
            alignRow.style.flexDirection = FlexDirection.Row;

            const float buttonWidth = 90.0f;

            var alignButton         = new Button();
            alignRow.Add(alignButton);
            alignButton.text        = "Align X";
            alignButton.tooltip     = "Align the object positions along the X axis.";
            alignButton.style.width = buttonWidth * 0.6f;           
            alignButton.clicked     += () =>
            {
                ObjectAlignment.alignObjects(_targetObjects, 0, true);
                ObjectEvents.onObjectsTransformedByUI();
            };

            alignButton             = new Button();
            alignRow.Add(alignButton);
            alignButton.text        = "Align Y";
            alignButton.tooltip     = "Align the object positions along the Y axis.";
            alignButton.style.width = buttonWidth * 0.6f;
            alignButton.style.marginLeft = UIValues.actionButtonLeftMargin;
            alignButton.clicked     += () =>
            {
                ObjectAlignment.alignObjects(_targetObjects, 1, true);
                ObjectEvents.onObjectsTransformedByUI();
            };

            alignButton             = new Button();
            alignRow.Add(alignButton);
            alignButton.text        = "Align Z";
            alignButton.tooltip     = "Align the object positions along the Z axis.";
            alignButton.style.width = buttonWidth * 0.6f;
            alignButton.style.marginLeft = UIValues.actionButtonLeftMargin;
            alignButton.clicked     += () =>
            {
                ObjectAlignment.alignObjects(_targetObjects, 2, true);
                ObjectEvents.onObjectsTransformedByUI();
            };

            return alignRow;
        }
    }
}
#endif