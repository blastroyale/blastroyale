using System;
using System.Text;
using System.Threading.Tasks;
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
		
		private Label _nameLabel;
		private VisualElement _content;
		private VisualElement _loadingSpinner;
		private VisualElement _avatarImageLoadingSpinner;
		private VisualElement _pfpImage;
		
		private Label[] _statLabels;
		private Label[] _statValues;
		private VisualElement[] _statContainers;
		private int _pfpRequestHandle = -1;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_statLabels = new Label[6];
			_statValues = new Label[6];
			_statContainers = new VisualElement[6];
			
			root.Q<ImageButton>("CloseButton").clicked += Data.OnCloseClicked;
			root.Q<VisualElement>("Background")
				.RegisterCallback<ClickEvent, StateData>((_, data) => data.OnCloseClicked(), Data);

			_pfpImage = root.Q<VisualElement>("PfpImage").Required();
			_avatarImageLoadingSpinner = root.Q<VisualElement>("SpinnerHolder").Required();
			_content = root.Q<VisualElement>("Content").Required();
			_nameLabel = root.Q<Label>("NameLabel").Required();
			_loadingSpinner = root.Q<AnimatedImageElement>("LoadingSpinner").Required();

			for (int i = 0; i < 6; i++)
			{
				_statContainers[i] = root.Q<VisualElement>($"StatsContainer{i}").Required();
				_statLabels[i] = root.Q<Label>($"StatName{i}").Required();
				_statValues[i] = root.Q<Label>($"StatValue{i}").Required();

				_statContainers[i].visible = false;
			}

			_content.visible = false;
			_loadingSpinner.visible = true;
			
			root.SetupClicks(_services);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			SetupPopup();
		}

		protected override async Task OnClosed()
		{
			base.OnClosed();
			
			_services.RemoteTextureService.CancelRequest(_pfpRequestHandle);
		}

		private void SetupPopup()
		{
			var t = new PlayerProfileService(MainInstaller.ResolveServices().GameBackendService);
			
			t.GetPlayerPublicProfile(Data.PlayerId, (result) =>
			{
				_nameLabel.text = result.Name;

				var i = 0;
				foreach (var s in result.Statistics)
				{
					_statContainers[i].visible = true;
					_statLabels[i].text = s.Name;
					_statValues[i].text = s.Value.ToString();
					i++;
					Debug.Log($"{s.Name} = {s.Value}");
				}

				if (!string.IsNullOrEmpty(result.AvatarUrl))
				{
					_pfpRequestHandle = _services.RemoteTextureService.RequestTexture(
						result.AvatarUrl,
						tex =>
						{
							_pfpImage.SetDisplay(true);
							_pfpImage.style.backgroundImage = new StyleBackground(tex);
							_avatarImageLoadingSpinner.SetDisplay(false);
						},
						() =>
						{
							_avatarImageLoadingSpinner.SetDisplay(false);
						});
				}
				else
				{
					_pfpImage.SetDisplay(true);
					_avatarImageLoadingSpinner.SetDisplay(false);
				}

				_content.visible = true;
				_loadingSpinner.visible = false;
			});
		}
	}
}