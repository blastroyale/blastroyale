using System;
using System.Text;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.UIElements;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles the player statistics screen.
	/// </summary>
	[LoadSynchronously]
	public class PlayerStatisticsPopupPresenter : UiToolkitPresenterData<PlayerStatisticsPopupPresenter.StateData>
	{
		public struct StateData
		{
			public string PlayerId;
			public Action OnCloseClicked;
		}
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private Label _label;
		private VisualElement _loadingSpinner;
		

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
			root.Q<ImageButton>("CloseButton").clicked += Data.OnCloseClicked;
			root.Q<VisualElement>("Background")
				.RegisterCallback<ClickEvent, StateData>((_, data) => data.OnCloseClicked(), Data);

			_label = root.Q<Label>("StatsLabel").Required();
			_loadingSpinner = root.Q<AnimatedImageElement>("LoadingSpinner").Required();
			
			_loadingSpinner.SetDisplay(true);
			
			root.SetupClicks(_services);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			SetupPopup();
		}

		private void SetupPopup()
		{
			var t = new PlayerProfileService(MainInstaller.ResolveServices().GameBackendService);
			t.GetPlayerPublicProfile(Data.PlayerId, (result) =>
			{
				var sbTerms = new StringBuilder();
				sbTerms.Append($"{result.Name}\n\n");
				
				foreach (var s in result.Statistics)
				{
					sbTerms.Append($"{s.Name} = {s.Value}\n");
					Debug.Log($"{s.Name} = {s.Value}");
				}
				
				_label.visible = true;
				_label.text = sbTerms.ToString();
				
				_loadingSpinner.SetDisplay( false);
			});
		}
	}
}