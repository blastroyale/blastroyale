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
	public class MapSelectionView : MonoBehaviour, IPointerClickHandler
	{
		[SerializeField, Required] private GameObject _selectedDropAreaRoot;
		[SerializeField, Required] private TextMeshProUGUI _selectedDropAreaText;
		[SerializeField, Required] private RectTransform _selectedPoint;
		[SerializeField, Required] private Camera _uiCamera;
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
		public async void SetupMapView(string gameModeId, int mapId, Vector3 dropzonePosRot)
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
			SelectionEnabled = gameModeConfig.SpawnSelection && !config.IsTestMap;
			var selectionPattern = gameModeConfig.SpawnPattern;

			_selectedDropAreaText.gameObject.SetActive(SelectionEnabled);
			_selectedPoint.gameObject.SetActive(SelectionEnabled);
			_dropzoneLayout.gameObject.SetActive(selectionPattern);
			
			// Aspect ratio has to be calculated and set in ARF per-map, as the rect size is crucial in grid
			// selection calculations. If you flat out set the ratio on ARF to something like 3-4, it will fit all map 
			// images on the UI, but then landing location grid will be completely broken for BR game mode
			_aspectRatioFitter.aspectRatio = (_mapImage.preferredWidth / _mapImage.preferredHeight);

			if (SelectionEnabled)
			{
				var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
				var position = new Vector2Int(
					Mathf.FloorToInt(Random.value * mapGridConfigs.GetSize().x / 2), 
					Mathf.FloorToInt(Random.value * mapGridConfigs.GetSize().y) / 2);
				SetGridPosition(position, false);
				_dropzoneLayout.rotation = Quaternion.Euler(0,0,dropzonePosRot.z);

				_services.AnalyticsService.MatchCalls.DefaultDropPosition = position;
				_services.AnalyticsService.MatchCalls.PresentedMapPath = dropzonePosRot.ToString();
			}
		}

		/// <inheritdoc />
		public void OnPointerClick(PointerEventData eventData)
		{
			if (!SelectionEnabled) return;
			SetGridPosition(ScreenToGridPosition(eventData.position), true);
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
			var dropPosition = new Vector2(localPosition.x / localSize.x, localPosition.y / localSize.y) *
				_dropSelectionSize;
			_services.NetworkService.SetDropPosition(dropPosition);
			_selectedDropAreaRoot.SetActive(false);
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

			return (includeWater || mapGridConfigs.GetConfig(position.x,position.y).IsValidNamedArea);
		}
	}
}