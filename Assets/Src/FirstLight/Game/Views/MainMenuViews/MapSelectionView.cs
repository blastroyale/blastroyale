using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

		/// <summary>
		/// Returns the player's selected point on the map in a normalized state
		/// </summary>
		public Vector2 NormalizedSelectionPoint { get; private set; }

		private IGameServices _services;
		private RectTransform _rectTransform;

		[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_rectTransform = transform as RectTransform;

			SetGridPosition(GetRandomGridPosition());
		}

		/// <inheritdoc />
		public void OnPointerClick(PointerEventData eventData)
		{
			SetGridPosition(ScreenToGridPosition(eventData.position));
		}

		private void SetGridPosition(Vector2Int pos)
		{
			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var mapGridSize = mapGridConfigs.GetSize();
			var gridConfig = mapGridConfigs.GetConfig(pos.x, pos.y);

			Debug.Log($"Selecting grid position ({pos.x}, {pos.y}) - {(gridConfig.IsValid ? "Valid" : "Invalid")}");
			if (!gridConfig.IsValid) return;

			_selectedPoint.anchoredPosition = GridToAnchoredPosition(pos);
			_selectedDropAreaText.text = mapGridConfigs.GetTranslation(gridConfig.AreaName);
			NormalizedSelectionPoint = new Vector2((float) pos.x / mapGridSize.Key, (float) pos.y / mapGridSize.Value);
		}

		private Vector2Int GetRandomGridPosition()
		{
			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var gridSize = mapGridConfigs.GetSize();

			var availableGridPositions = new HashSet<MapGridConfig>(gridSize.Key * gridSize.Value);

			for (int x = 0; x < gridSize.Key; x++)
			{
				for (int y = 0; y < gridSize.Value; y++)
				{
					var config = mapGridConfigs.GetConfig(x, y);
					if (config.IsValid) availableGridPositions.Add(config);
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
			var size = mapGridConfigs.GetSize();
			var sizeDelta = _rectTransform.sizeDelta;
			var calcPos = new Vector2(localPos.x + sizeDelta.x / 2f, Mathf.Abs(localPos.y - sizeDelta.y / 2f));

			return new Vector2Int(
				Mathf.RoundToInt(size.Key * calcPos.x / sizeDelta.x),
				Mathf.RoundToInt(size.Value * calcPos.y / sizeDelta.y)
			);
		}

		private Vector2 GridToAnchoredPosition(Vector2Int pos)
		{
			var mapGridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var size = mapGridConfigs.GetSize();
			var sizeDelta = _rectTransform.sizeDelta;
			var normalizedPos = new Vector2((float) pos.x / size.Key, (float) pos.y / size.Value);

			var positionInRectangle = sizeDelta * normalizedPos;
			positionInRectangle.x -= sizeDelta.x / 2f;
			positionInRectangle.y = sizeDelta.y - positionInRectangle.y - sizeDelta.y / 2f;

			return positionInRectangle;
		}
	}
}
