using System;
using System.Diagnostics.CodeAnalysis;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using SRF;
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
		[SerializeField, Required] private GameObject _selectedDropAreaRoot;
		[SerializeField, Required] private TextMeshProUGUI _selectedDropAreaText;
		[SerializeField, Required] private RectTransform _selectedPoint;
		[SerializeField, Required] private Camera _uiCamera;
		[SerializeField, Required] private AspectRatioFitter _aspectRatioFitter;
		[SerializeField, Required] private Image _mapImage;
		[SerializeField, Required] private RectTransform _gridOverlay;
		[SerializeField] private Color _unavailableGridColor;

		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private RectTransform _rectTransform;
		private bool _selectionEnabled = false;

		/// <summary>
		/// Returns the player's selected point on the map in a normalized state
		/// </summary>
		public Vector2 NormalizedSelectionPoint { get; private set; }

		[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_rectTransform = transform as RectTransform;
		}

		/// <summary>
		/// Setup the map visuals to look awesome on the screen and selects a random point in battle royale mode
		/// </summary>
		public async void SetupMapView(string gameModeId, int mapId)
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumMapConfig>(mapId);
			var gameModeConfig = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId.GetHashCode());

			_mapImage.enabled = false;
			_mapImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(config.Map, false);
			_mapImage.enabled = true;
			_selectionEnabled = gameModeConfig.SpawnSelection && !config.IsTestMap;

			_selectedDropAreaText.gameObject.SetActive(_selectionEnabled);
			_selectedPoint.gameObject.SetActive(_selectionEnabled);

			// Aspect ratio has to be calculated and set in ARF per-map, as the rect size is crucial in grid
			// selection calculations. If you flat out set the ratio on ARF to something like 3-4, it will fit all map 
			// images on the UI, but then landing location grid will be completely broken for BR game mode
			_aspectRatioFitter.aspectRatio = (_mapImage.preferredWidth / _mapImage.preferredHeight);

			if (_selectionEnabled)
			{
				var gridPosition = GetRandomGridPosition();
				_services.AnalyticsService.MatchCalls.DefaultDropPosition = gridPosition;
				SetGridPosition(gridPosition, false);
				
				if (TryGetDropPattern(out var pattern))
				{
					var gridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
					var containerSize = _gridOverlay.rect.size;
					var gridSize = gridConfigs.GetSize();
					string gridPath = "(";
					for (int y = 0; y < gridSize.y; y++)
					{
						for (int x = 0; x < gridSize.x; x++)
						{
							if (pattern[x][y])
							{
								gridPath += "(" + x + "," + y + "),";
								continue;
							}

							var go = new GameObject($"[{x},{y}]");
							go.transform.parent = _gridOverlay.transform;
							go.transform.localScale = Vector3.one;

							var image = go.AddComponent<RawImage>();
							image.color = _unavailableGridColor;

							var rt = go.GetComponent<RectTransform>();
							rt.anchoredPosition = new Vector2(containerSize.x / gridSize.x * x,
							                                  containerSize.y / gridSize.y * (gridSize.y - y - 1)) -
							                      containerSize / 2f + (containerSize / gridSize / 2);
							rt.sizeDelta = containerSize / gridSize;
						}
					}

					gridPath += ")";
				
					_services.AnalyticsService.MatchCalls.PresentedMapPath = gridPath;
				}
			}
		}

		/// <summary>
		/// Cleans up entities that aren't required anymore.
		/// </summary>
		public void CleanupMapView()
		{
			_gridOverlay.DestroyChildren();
		}

		/// <inheritdoc />
		public void OnPointerClick(PointerEventData eventData)
		{
			if (!_selectionEnabled) return;

			SetGridPosition(ScreenToGridPosition(eventData.position), true);
		}

		public void SelectWaterPosition()
		{
			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();

			for (int y = 0; y < mapGridConfigs.GetSize().y; y++)
			{
				var pos = new Vector2Int(0, y);
				if (IsValidPosition(pos, true))
				{
					SetGridPosition(pos, true);
					return;
				}
			}
		}

		private void SetGridPosition(Vector2Int pos, bool includeWater)
		{
			if (!IsValidPosition(pos, includeWater))
			{
				return;
			}

			_services.AnalyticsService.MatchCalls.SelectedDropPosition = pos;
			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var gridConfig = mapGridConfigs.GetConfig(pos.x, pos.y);

			var localPosition = GridToAnchoredPosition(pos);
			var localSize = _rectTransform.sizeDelta;
			_selectedPoint.anchoredPosition = localPosition;
			_selectedDropAreaText.text = mapGridConfigs.GetTranslation(gridConfig.AreaName);
			NormalizedSelectionPoint = new Vector2(localPosition.x / localSize.x, localPosition.y / localSize.y);

			_selectedDropAreaRoot.SetActive(gridConfig.IsValidNamedArea);
		}

		private Vector2Int GetRandomGridPosition()
		{
			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var gridSize = mapGridConfigs.GetSize();

			Vector2Int position;
			do
			{
				position = new Vector2Int(Random.Range(0, gridSize.x), Random.Range(0, gridSize.y));
			} while (!IsValidPosition(position, false));

			return position;
		}

		private Vector2Int ScreenToGridPosition(Vector2 pointer)
		{
			var pointerVec3 = new Vector3(pointer.x, pointer.y, 0);
			var screenPoint = RectTransformUtility.WorldToScreenPoint(_uiCamera, pointerVec3);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPoint, _uiCamera,
			                                                        out var localPos);

			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var size = mapGridConfigs.GetSize() - Vector2Int.one;
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

		private bool IsValidPosition(Vector2Int position, bool includeWater)
		{
			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();

			return (!TryGetDropPattern(out var pattern) || pattern[position.x][position.y]) &&
			       (includeWater || mapGridConfigs.GetConfig(position.x,position.y).IsValidNamedArea);
		}

		private bool TryGetDropPattern(out bool[][] pattern)
		{
			if (_services.NetworkService.QuantumClient.CurrentRoom.CustomProperties
			             .TryGetValue(GameConstants.Network.ROOM_PROPS_DROP_PATTERN, out var dropPattern))
			{
				pattern = (bool[][]) dropPattern;
				return true;
			}

			pattern = Array.Empty<bool[]>();
			return false;
		}
	}
}