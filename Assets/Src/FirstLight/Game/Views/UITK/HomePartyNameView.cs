using System;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles currency display on the screen. Because of legacy reasons all the logic
	/// is still handled in the CurrencyDisplayElement.
	/// </summary>
	public class HomePartyNameView : UIView
	{
		public Action OnClicked;

		private Vector3 _worldAnchor;
		private readonly float _labelScale;

		private VisualElement _crownElement;
		private VisualElement _readyElement;
		private VisualElement _notReadyElement;
		public Label PlayerNameLabel { private set; get; }

		public HomePartyNameView(Vector3 worldAnchor, float labelScale)
		{
			_worldAnchor = worldAnchor;
			_labelScale = labelScale;
		}


		protected override void Attached()
		{
			Element.Q<VisualElement>("PlayerNameContainer").contentContainer.style.scale = new StyleScale(new Vector2(_labelScale, _labelScale));
			Element.pickingMode = PickingMode.Ignore;
			PlayerNameLabel = Element.Q<Label>("PlayerNameLabel").Required();
			_crownElement = Element.Q<VisualElement>("Crown").Required();
			_readyElement = Element.Q<VisualElement>("ReadyStatus").Required();
			_notReadyElement = Element.Q<VisualElement>("NotReadyStatus").Required();
			Element.RegisterCallback<ClickEvent>(OnPlayerNameClicked);
			Element.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			UpdatePosition();
		}

		public void Disable()
		{
			Element.SetDisplay(false);
		}

		public void Enable(string playerName, bool leader, bool isReady)
		{
			Element.SetDisplay(true);
			PlayerNameLabel.text = playerName;
			_crownElement.SetDisplay(leader);
			_readyElement.SetDisplay(isReady && !leader);
			_notReadyElement.SetDisplay(!isReady && !leader);
		}


		public void UpdatePosition()
		{
			Element.SetPositionBasedOnWorldPosition(_worldAnchor);
		}


		private void OnPlayerNameClicked(ClickEvent evt)
		{
			OnClicked?.Invoke();
		}
	}
}