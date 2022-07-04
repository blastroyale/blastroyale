using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// View for controlling the minimap view.
	/// </summary>
	public class MiniMapView : MonoBehaviour
	{
		[SerializeField, Required]
		[ValidateInput("@!_minimapCamera.gameObject.activeSelf", "Camera should be disabled!")]
		private Camera _minimapCamera;

		[SerializeField, Required] private RawImage _minimapImage;
		[SerializeField, Range(0f, 1f)] private float _viewportSize = 0.2f;
		[SerializeField] private int _cameraHeight = 10;

		private IGameServices _services;
		private IEntityViewUpdaterService _entityViewUpdaterService;

		private EntityView _playerEntityView;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
		}

		[Button]
		private void RenderMinimap()
		{
			FLog.Verbose("Rendering MiniMap camera.");
			_minimapCamera.transform.position = new Vector3(0, _cameraHeight, 0);
			_minimapCamera.Render();
		}

		private void UpdateMinimapViewport(float _)
		{
			var viewportPoint = _minimapCamera.WorldToViewportPoint(_playerEntityView.transform.position);
			_minimapImage.uvRect = new Rect(viewportPoint.x - _viewportSize / 2f,
			                                viewportPoint.y - _viewportSize / 2f,
			                                _viewportSize, _viewportSize);
		}

		private void OnDestroy()
		{
			_services?.TickService?.UnsubscribeOnUpdate(UpdateMinimapViewport);
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			_services?.TickService?.UnsubscribeOnUpdate(UpdateMinimapViewport);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			RenderMinimap();
			_playerEntityView = _entityViewUpdaterService.GetManualView(callback.Entity);
			_services.TickService.SubscribeOnUpdate(UpdateMinimapViewport);
		}
	}
}