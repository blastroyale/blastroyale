using System;
using System.Collections;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.MainMenu;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using SRF;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using LayerMask = UnityEngine.LayerMask;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Players Waiting Screen UI by:
	/// - Showing the loading status
	/// </summary>
	public class MatchmakingLoadingScreenPresenter : UiPresenterData<MatchmakingLoadingScreenPresenter.StateData>
	{
		public struct StateData
		{
			public IUiService UiService;
		}

		[SerializeField] private Transform _playerCharacterParent;
		[SerializeField] private Image _nextMapImage;
		[SerializeField] private Image [] _playersWaitingImage;
		[SerializeField] private Animation _animation;
		[SerializeField] private TextMeshProUGUI _selectedDropAreaText;
		[SerializeField] private TextMeshProUGUI _firstToXKillsText;
		[SerializeField] private TextMeshProUGUI _nextArenaText;
		[SerializeField] private TextMeshProUGUI _playersFoundText;
		[SerializeField] private TextMeshProUGUI _findingPlayersText;
		[SerializeField] private TextMeshProUGUI _getReadyToRumbleText;
		[SerializeField] private GameObject _selectedAreaHolder;
		
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		private float _rndWaitingTimeLowest;
		private float _rndWaitingTimeBiggest;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			foreach (var image in _playersWaitingImage)
			{
				image.gameObject.SetActive(false);
			}

			SceneManager.activeSceneChanged += OnSceneChanged;
		}

		private void OnDestroy()
		{
			SceneManager.activeSceneChanged -= OnSceneChanged;
		}

		/// <inheritdoc />
		protected override async void OnOpened()
		{
			var config = _gameDataProvider.AdventureDataProvider.SelectedMapConfig;
			
			_playersFoundText.text = $"{0}/{config.PlayersLimit.ToString()}" ;
			_nextMapImage.enabled = false;
			_rndWaitingTimeLowest = 2f / config.PlayersLimit;
			_rndWaitingTimeBiggest = 8f / config.PlayersLimit;
			
			_getReadyToRumbleText.gameObject.SetActive(false);
			transform.SetParent(null);
			SetLayerState(false);
			_animation.Rewind();
			_animation.Play();
			
			_nextMapImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(config.Map, false);
			_nextMapImage.enabled = true;
			
			StartCoroutine(TimeUpdateCoroutine(config));
		}

		protected override void OnClosed()
		{
			SetLayerState(true);
		}

		private void OnSceneChanged(Scene previous, Scene current)
		{
			// Ignore scene changes that are not levels
			if (current.buildIndex != -1)
			{
				return;
			}
			
			// Little hack to avoid UIs to spam over this screen
			for (var i = 0; i < Data.UiService.TotalLayers; i++)
			{
				if (!Data.UiService.TryGetLayer(i, out var layer))
				{
					continue;
				}

				layer.SetActive(true);
				
				foreach (var canvas in layer.GetComponentsInChildren<UiPresenter>(true))
				{
					// To force the UI awake calls
					canvas.gameObject.SetActive(true);
					canvas.gameObject.SetActive(false);
				}
				
				layer.SetActive(false);
			}
		}

		private IEnumerator TimeUpdateCoroutine(MapConfig config)
		{
			for (var i = 0; i < _playersWaitingImage.Length && i < config.PlayersLimit; i++)
			{
				_playersWaitingImage[i].gameObject.SetActive(true);
				_playersFoundText.text = $"{(i + 1).ToString()}/{config.PlayersLimit.ToString()}" ;
				
				yield return new WaitForSeconds(Random.Range(_rndWaitingTimeLowest, _rndWaitingTimeBiggest));
			}

			yield return new WaitForSeconds(0.5f);
			
			_getReadyToRumbleText.gameObject.SetActive(true);
			_findingPlayersText.enabled = false;
			_playersFoundText.enabled = false;
		}

		private void SetLayerState(bool state)
		{
			// Little hack to avoid UIs to spam over this screen
			for (var i = 0; i < Data.UiService.TotalLayers; i++)
			{
				if (!Data.UiService.TryGetLayer(i, out var layer))
				{
					continue;
				}

				layer.SetActive(state);
			}
		}

		private void OnDropAreaPressed()
		{
			var mapConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			Touch touch = UnityEngine.Input.GetTouch(0);

			// TODO Miguel: Please can you make this position relative to the screen / map image?
			_selectedAreaHolder.transform.localPosition = touch.position;
		}
		
	}
}