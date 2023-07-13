using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// This Mono Component controls shrinking circle visuals behaviour
	/// </summary>
	public class ShrinkingCircleMonoComponent : MonoBehaviour
	{
		[SerializeField, Required] private CircleLineRendererMonoComponent _shrinkingCircleLinerRenderer;
		[SerializeField, Required] private CircleLineRendererMonoComponent _safeAreaCircleLinerRenderer;
		[SerializeField, Required] private Transform _damageZoneTransform;

		private QuantumShrinkingCircleConfig _config;
		private IGameServices _services;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			QuantumEvent.Subscribe<EventOnGameEnded>(this, HandleGameEnded);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, HandleUpdateView);
			_shrinkingCircleLinerRenderer.gameObject.SetActive(false);
			_safeAreaCircleLinerRenderer.gameObject.SetActive(false);
			_damageZoneTransform.gameObject.SetActive(false);
		}

		private void HandleGameEnded(EventOnGameEnded callback)
		{
			QuantumCallback.UnsubscribeListener(this);
		}

		private void HandleUpdateView(CallbackUpdateView callback)
		{
			var frame = callback.Game.Frames.Predicted;
			if (!frame.TryGetSingleton<ShrinkingCircle>(out var circle) || circle.Step < 0)
			{
				return;
			}
			
			_shrinkingCircleLinerRenderer.gameObject.SetActive(true);
			_safeAreaCircleLinerRenderer.gameObject.SetActive(true);
			_damageZoneTransform.gameObject.SetActive(true);
			
			var targetCircleCenter = circle.TargetCircleCenter.ToUnityVector2();
			var targetRadius = circle.TargetRadius.AsFloat;
			
			circle.GetMovingCircle(frame, out var centerFP, out var radiusFP);
			var radius = radiusFP.AsFloat;
			var center = centerFP.ToUnityVector2();
			
			var cachedShrinkingCircleLineTransform = _shrinkingCircleLinerRenderer.transform;
			var cachedSafeAreaCircleLine = _safeAreaCircleLinerRenderer.transform;
			
			var position = new Vector3(center.x, cachedShrinkingCircleLineTransform.position.y, center.y);
			
			if (_config.Step != circle.Step)
			{
				_config = frame.Context.MapShrinkingCircleConfigs[circle.Step];
			}
			
			cachedShrinkingCircleLineTransform.position = position;
			cachedShrinkingCircleLineTransform.localScale = new Vector3(radius, radius, 1f);
			_shrinkingCircleLinerRenderer.WidthMultiplier = 1f / radius;

			_damageZoneTransform.position = position;
			_damageZoneTransform.localScale = new Vector3(radius * 2f, _damageZoneTransform.localScale.y, radius * 2f);
			
			if (frame.Time < circle.ShrinkingStartTime - _config.WarningTime)
			{
				// We set white circle to be as big as red one at the first step to avoid it sitting small in the center of the map
				// It's because the safe zone position/scale is not revealed for a few seconds at the beginning of a match
				if (circle.Step == 1)
				{
					cachedSafeAreaCircleLine.localScale = new Vector3(radius, radius, 1f);
					_safeAreaCircleLinerRenderer.WidthMultiplier = 0f;
				}
				
				return;
			}
			
			cachedSafeAreaCircleLine.position = new Vector3(targetCircleCenter.x, cachedSafeAreaCircleLine.position.y, targetCircleCenter.y);
			cachedSafeAreaCircleLine.localScale = new Vector3(targetRadius, targetRadius, 1f);
			_safeAreaCircleLinerRenderer.WidthMultiplier = 1f / targetRadius;
		}
	}
}