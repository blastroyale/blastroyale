using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views
{
	public class GameModeSelectionButtonView : IUIView
	{
		private const string SELECTED_CLASS = "Selected";
		
		private VisualElement _root;
		private Button _button;
		private Label _matchTypeLabel;
		private Label _gameModeLabel;
		private bool _selected;

		public GameModeInfo GameModeInfo { get; set; }
		public event Action<GameModeSelectionButtonView> Clicked;
		
		public bool Selected
		{
			get => _selected;
			set
			{
				_selected = value;

				if (_selected)
				{
					_button.AddToClassList(SELECTED_CLASS);
				}
				else
				{
					_button.RemoveFromClassList(SELECTED_CLASS);
				}
			}
		}
		
		public void Attached(VisualElement element)
		{
			_root = element;
			
			_button = _root.Q<Button>().Required();
			
			
			_matchTypeLabel = _root.Q<Label>("MatchTypeLabel").Required();
			_gameModeLabel = _root.Q<Label>("GameModeLabel").Required();
		}

		public void SubscribeToEvents()
		{
			_button.clicked += () => Clicked?.Invoke(this);
		}

		public void UnsubscribeFromEvents()
		{
		}

		public void SetData(GameModeInfo gameModeInfo)
		{
			GameModeInfo = gameModeInfo;
			
			_matchTypeLabel.text = gameModeInfo.Entry.MatchType.ToString().ToUpper();
			_gameModeLabel.text = gameModeInfo.Entry.GameModeId.ToUpper();
		}
	}
}
