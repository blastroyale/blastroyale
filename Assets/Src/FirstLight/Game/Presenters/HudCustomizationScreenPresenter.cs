using System;
using System.Collections.Generic;
using FirstLight.Game.Data;
using FirstLight.Game.Presenters.Manipulators;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class HudCustomizationScreenPresenter : UiToolkitPresenterData<HudCustomizationScreenPresenter.StateData>
	{
		private readonly float MIN_SIZE = 0.2f;
		private readonly float MIN_OPACITY = 0.05f;
		private readonly string USS_CUSTOMIZABLE_ELEMENT = "customizable-hud";
		private readonly string USS_NON_CUSTOMIZABLE_ELEMENT = "non-customizable-hud";
		private readonly string USS_CUSTOMIZING_CLASS = "customizing-element";
		private readonly string USS_MENU_CLOSED = "menu__options--closed";
		private readonly string USS_OPENER_CLOSED = "menu__opener-icon--closed";
		private readonly string USS_DISABLED = "item-disabled";
		
		private List<SerializedVisualElementSetup> _originals = new();
		private HashSet<VisualElement> _customizable = new ();
		private Vector2 _lastPos;
		private VisualElement _selected;
		private IGameServices _services;
		private Slider _opacity;
		private Slider _size;
		private VisualElement _options;
		private Button _close;
		private Button _save;
		private Button _reset;
		private VisualElement _openCloseIcon;
		private VisualElement _menu;
		private ImageButton _openButton;
		private Label _scaleLabel;
		private Label _opacityLabel;
		private bool _open = true;
		private float _optionsSize = 400;
		private List<VisualElement> _disabled = new();
		
		public struct StateData
		{
			public Action<IReadOnlyCollection<VisualElement>> OnSave;
			public Action OnClose;
		}

		protected override void QueryElements(VisualElement root)
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_opacity = root.Q<Slider>("OpacitySlider").Required();
			_size = root.Q<Slider>("SizeSlider").Required();
			_opacity.RegisterValueChangedCallback(OnOpacityChange);
			_size.RegisterValueChangedCallback(OnSizeChange);
			_options = root.Q("MenuOptions").Required();
			_openCloseIcon = root.Q("OpenCloseIcon").Required();
			_openButton = root.Q<ImageButton>("OpenCloseButton").Required();
			_opacityLabel = root.Q<Label>("OpacityValue").Required();
			_reset = root.Q<Button>("ResetButton").Required();
			_save = root.Q<Button>("SaveButton").Required();
			_close = root.Q<Button>("CloseButton").Required();
			_menu = root.Q("Menu").Required();

			_openButton.clicked += ToggleOpen;
			_close.clicked += Data.OnClose;
			_save.clicked += () => Data.OnSave(_customizable);
			_reset.clicked += Reset;

			_disabled = root.Query(className: USS_DISABLED).Build().ToList();
			root.Query(className: USS_NON_CUSTOMIZABLE_ELEMENT).Build().ForEach(Remove);
			root.Query(className:  USS_CUSTOMIZABLE_ELEMENT).Build().ForEach(MakeCustomizable);

			// Joysticks are snowflakes because they handled the events on parent
			// if we block events on parent to drag without moving them, the propagation stops
			// and we cannot drag them
			var joy1 = root.Q<JoystickElement>("MovementJoystick");
			var joy2 = root.Q<JoystickElement>("ShootingJoystick");
			joy1.RemoveListeners();
			joy2.RemoveListeners();
			MakeCustomizable(joy1, onlyInsideParent:true);
			MakeCustomizable(joy2, onlyInsideParent:true);
			
			_services.ControlsSetup.SetControlPositions(root);
		}
		
		private void EnableAll()
		{
			foreach (var d in _disabled)
			{
				d.RemoveFromClassList(USS_DISABLED);
			}
			_disabled.Clear();
		}


		private void ToggleOpen()
		{
			_open = !_open;
			if (_open)
			{
				_options.RemoveFromClassList(USS_MENU_CLOSED);
				_openCloseIcon.RemoveFromClassList(USS_OPENER_CLOSED);
			}
			else
			{
				_options.AddToClassList(USS_MENU_CLOSED);
				_openCloseIcon.AddToClassList(USS_OPENER_CLOSED);
			}
		}
		
		private void Reset()
		{
			foreach (var o in _originals)
			{
				var e = Root.Q(o.ElementId);
				if (e == null) continue;
				o.ToElement(e);
			}
		}

		private void OnSizeChange(ChangeEvent<float> ev)
		{
			if (_selected == null) return;
			var value = Math.Max(MIN_SIZE, ev.newValue) * 2;
			_selected.style.scale = new Scale(new Vector2(value, value));
		}
		
		private void OnOpacityChange(ChangeEvent<float> ev)
		{
			if (_selected == null) return;
			_selected.style.opacity = Math.Max(ev.newValue, MIN_OPACITY);
			_opacityLabel.text = $"{Mathf.RoundToInt(_selected.style.opacity.value * 100)}%";
		}

		private void MakeCustomizable(VisualElement e)
		{
			MakeCustomizable(e, false);
		}
		
		private void MakeCustomizable(VisualElement e, bool onlyInsideParent)
		{
			_originals.Add(new SerializedVisualElementSetup().FromElement(e));
			_customizable.Add(e);
			var manipulator = new DragDropManipulator();
			manipulator.OnlyInsideParent = onlyInsideParent;
			manipulator.OnStartDrag += OnStartDrag;
			e.AddManipulator(manipulator);
		}

		private void OnStartDrag(VisualElement element)
		{
			if(_selected != null) _selected.RemoveFromClassList(USS_CUSTOMIZING_CLASS);
			element.AddToClassList(USS_CUSTOMIZING_CLASS);
			_selected = element;
			EnableAll();
			_opacity.value = element.style.opacity.value == 0 ? 1 : element.style.opacity.value;
			_size.value = element.style.scale.value.value.x == 0 ? 0.5f : element.style.scale.value.value.x / 2;
		}

		private void Remove(VisualElement e)
		{
			RemoveEvents(e);
			e.style.display = DisplayStyle.None;
		}

		private void RemoveEvents(VisualElement e)
		{
			e.RegisterCallback<PointerDownEvent>(CancelEvent, TrickleDown.TrickleDown);
			e.RegisterCallback<PointerUpEvent>(CancelEvent, TrickleDown.TrickleDown);
			e.RegisterCallback<PointerMoveEvent>(CancelEvent, TrickleDown.TrickleDown);
		}

		private void CancelEvent(EventBase e)
		{
			e.PreventDefault();
			e.StopImmediatePropagation();
		}
	}
}