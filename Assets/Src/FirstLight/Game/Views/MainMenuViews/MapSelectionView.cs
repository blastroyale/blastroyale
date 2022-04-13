using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This View allows the player to select a spawn point on the map
	/// </summary>
	public class MapSelectionView : MonoBehaviour, IPointerClickHandler
	{
		[SerializeField] private TextMeshProUGUI _selectedDropAreaText;
		[SerializeField] private RectTransform _selectedPoint;
		[SerializeField] private Camera _uiCamera;
		[SerializeField] private AspectRatioFitter _aspectRatioFitter;	
		[SerializeField] private Image _mapImage;
		
		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private RectTransform _rectTransform;
		private bool _selectionEnabled = false;
		
		/// <summary>
		/// Returns the player's selected point on the map in a normalized state
		/// </summary>
		public Vector2 NormalizedSelectionPoint { get; private set; }

		public async void Start()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_rectTransform = transform as RectTransform;
		
			_mapImage.enabled = false;
			_mapImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(_dataProvider.AppDataProvider.CurrentMapConfig.Map, false);
			_mapImage.enabled = true;
			
			_selectionEnabled = _dataProvider.AppDataProvider.SelectedGameMode.Value == GameMode.BattleRoyale;

			_selectedDropAreaText.gameObject.SetActive(_selectionEnabled);
			_selectedPoint.gameObject.SetActive(_selectionEnabled);
			
			// Aspect ratio has to be calculated and set in ARF per-map, as the rect size is crucial in grid
			// selection calculations. If you flat out set the ratio on ARF to something like 3-4, it will fit all map 
			// images on the UI, but then landing location grid will be completely broken for BR game mode
			float aspectRatioPercent = (_mapImage.preferredWidth / _mapImage.preferredHeight);
			_aspectRatioFitter.aspectRatio = aspectRatioPercent;

			if (_selectionEnabled)
			{
				SetGridPosition(GetRandomGridPosition());
			}
		}
		/// <inheritdoc />
		public void OnPointerClick(PointerEventData eventData)
		{
			SetGridPosition(ScreenToGridPosition(eventData.position));
		}

		private void SetGridPosition(Vector2Int pos)
		{
			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var gridConfig = mapGridConfigs.GetConfig(pos.x, pos.y);

			if (!gridConfig.IsValid || !_selectionEnabled)
			{
				return;
			}

			var localPosition = GridToAnchoredPosition(pos);
			var localSize = _rectTransform.sizeDelta;
			_selectedPoint.anchoredPosition = localPosition;
			_selectedDropAreaText.text = mapGridConfigs.GetTranslation(gridConfig.AreaName);
			NormalizedSelectionPoint = new Vector2(localPosition.x / localSize.x, localPosition.y / localSize.y);
		}

		private Vector2Int GetRandomGridPosition()
		{
			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var gridSize = mapGridConfigs.GetSize() - Vector2Int.one;
			var availableGridPositions = new HashSet<MapGridConfig>(gridSize.x * gridSize.y);

			for (var x = 0; x < gridSize.x; x++)
			{
				for (var y = 0; y < gridSize.y; y++)
				{
					var config = mapGridConfigs.GetConfig(x, y);
					if (config.IsValid)
					{
						availableGridPositions.Add(config);
					}
				}
			}

			var position = availableGridPositions.ElementAt(Random.Range(0, availableGridPositions.Count));
			
			return new Vector2Int(position.X, position.Y);
		}

		private Vector2Int ScreenToGridPosition(Vector2 pointer)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, pointer, _uiCamera,
			                                                        out var localPos);

			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var size = mapGridConfigs.GetSize()  - Vector2Int.one;
			var sizeDelta = _rectTransform.sizeDelta;
			var calcPos = new Vector2(localPos.x + sizeDelta.x / 2f, Mathf.Abs(localPos.y - sizeDelta.y / 2f));

			return new Vector2Int(Mathf.RoundToInt(size.x * calcPos.x / sizeDelta.x),
			                      Mathf.RoundToInt(size.y * calcPos.y / sizeDelta.y));
		}

		private Vector2 GridToAnchoredPosition(Vector2Int pos)
		{
			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var size = mapGridConfigs.GetSize() - Vector2Int.one;
			var sizeDelta = _rectTransform.sizeDelta;
			var normalizedPos = new Vector2((float) pos.x / size.x, (float) pos.y / size.y);
			var positionInRectangle = sizeDelta * normalizedPos;
			
			positionInRectangle.x -= sizeDelta.x / 2f;
			positionInRectangle.y = sizeDelta.y - positionInRectangle.y - sizeDelta.y / 2f;

			return positionInRectangle;
		}
	}
}