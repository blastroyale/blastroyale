using System.Diagnostics.CodeAnalysis;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This View allows the player to select a spawn point on the map
	/// </summary>
	public class MapSelectionView : MonoBehaviour
	{
		[SerializeField, Required] private AspectRatioFitter _aspectRatioFitter;
		[SerializeField, Required] private Image _mapImage;
		[SerializeField, Required] private Transform _dropzoneLayout;
		
		private IGameServices _services;
		private RectTransform _rectTransform;
		private float _dropSelectionSize;

		/// <summary>
		/// Requests the state of selecting a drop point in the map
		/// </summary>
		public bool SelectionEnabled { get; set; }

		[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_rectTransform = transform as RectTransform;
		}

		/// <summary>
		/// Setup the map visuals to look awesome on the screen and selects a random point in battle royale mode
		/// </summary>
		public async void SetupMapView(string gameModeId, int mapId)
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumMapConfig>(mapId);
			var gameModeConfig = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId);

			if (config.DropSelectionSize == 0)
			{
				Debug.LogWarning("Map "+config.Map+" has a Drop Selection of 0 which would make the zoom infinite.");
			}
			
			_dropSelectionSize = config.DropSelectionSize>0?config.DropSelectionSize:1f;

			_mapImage.enabled = false;
			_mapImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(config.Map, false);
			_mapImage.enabled = true;
			_mapImage.rectTransform.localScale = Vector3.one / _dropSelectionSize;
			
			// Aspect ratio has to be calculated and set in ARF per-map, as the rect size is crucial in grid
			// selection calculations. If you flat out set the ratio on ARF to something like 3-4, it will fit all map 
			// images on the UI, but then landing location grid will be completely broken for BR game mode
			_aspectRatioFitter.aspectRatio = (_mapImage.preferredWidth / _mapImage.preferredHeight);
		
		}
	}
}