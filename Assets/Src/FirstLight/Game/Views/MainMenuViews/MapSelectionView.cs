using System.Diagnostics.CodeAnalysis;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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

			var size = _rectTransform.sizeDelta;
			var randomPos = new Vector2(Random.Range(-size.x, size.x), Random.Range(-size.y, size.y));

			SetPosition(randomPos / 2f);
		}
		
		/// <inheritdoc />
		public void OnPointerClick(PointerEventData eventData)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, _uiCamera, 
			                                                        out var localPos);
			
			SetPosition(localPos);
		}

		private void SetPosition(Vector2 localPos)
		{
			var mapConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var size = mapConfigs.GetSize();
			var sizeDelta = _rectTransform.sizeDelta;
			var normalizedPoint = new Vector2(localPos.x / sizeDelta.x, localPos.y / sizeDelta.y);
			var calcPos = new Vector2(localPos.x + sizeDelta.x / 2f, Mathf.Abs(localPos.y - sizeDelta.y / 2f));
			var xPos = Mathf.FloorToInt(size.Key * calcPos.x / sizeDelta.x);
			var yPos = Mathf.FloorToInt(size.Value * calcPos.y / sizeDelta.y);
			var config = mapConfigs.GetConfig(xPos, yPos);

			if (!config.IsValid)
			{
				return;
			}

			_selectedPoint.anchoredPosition = localPos;
			_selectedDropAreaText.text = mapConfigs.GetTranslation(config.AreaName);
			NormalizedSelectionPoint = normalizedPoint;
		}
	}
}