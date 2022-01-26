using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// This Mono Component controls shrinking circle visuals behaviour
	/// </summary>
	public class ShrinkingCircleMonoComponent : MonoBehaviour
	{
		[SerializeField] private CircleLineRendererMonoComponent _shrinkingCircleLinerRenderer;
		[SerializeField] private CircleLineRendererMonoComponent _safeAreaCircleLinerRenderer;
		[SerializeField] private Transform _damageZoneTransform;

		private QuantumShrinkingCircleConfig _config;
		private IGameServices _services;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			QuantumCallback.Subscribe<CallbackUpdateView>(this, HandleUpdateView);
		}

		private void HandleUpdateView(CallbackUpdateView callback)
		{
			var frame = callback.Game.Frames.Predicted;
			var circle = frame.GetSingleton<ShrinkingCircle>();
			var targetCircleCenter = circle.TargetCircleCenter.ToUnityVector2();
			var targetRadius = circle.TargetRadius.AsFloat;
			var lerp = Mathf.Max(0, (frame.Time.AsFloat - circle.ShrinkingStartTime.AsFloat) / circle.ShrinkingDurationTime.AsFloat);
			var radius = Mathf.Lerp(circle.CurrentRadius.AsFloat, targetRadius, lerp);
			var center = Vector2.Lerp(circle.CurrentCircleCenter.ToUnityVector2(), targetCircleCenter, lerp);

			var cachedShrinkingCircleLineTransform = _shrinkingCircleLinerRenderer.transform;
			var cachedSafeAreaCircleLine = _safeAreaCircleLinerRenderer.transform;
			
			var position = new Vector3(center.x, cachedShrinkingCircleLineTransform.position.y, center.y);
			
			if (_config.Step != circle.Step)
			{
				_config = _services.ConfigsProvider.GetConfig<QuantumShrinkingCircleConfig>(circle.Step);
			}
			
			cachedShrinkingCircleLineTransform.position = position;
			cachedShrinkingCircleLineTransform.localScale = new Vector3(radius, radius, 1f);
			_shrinkingCircleLinerRenderer.WidthMultiplier = 1f / radius;

			_damageZoneTransform.position = position;
			_damageZoneTransform.localScale = new Vector3(radius * 2f, _damageZoneTransform.localScale.y, radius * 2f);
			
			if (frame.Time < circle.ShrinkingStartTime - _config.WarningTime)
			{
				return;
			}
			
			cachedSafeAreaCircleLine.position = new Vector3(targetCircleCenter.x, cachedSafeAreaCircleLine.position.y, targetCircleCenter.y);
			cachedSafeAreaCircleLine.localScale = new Vector3(targetRadius, targetRadius, 1f);
			_safeAreaCircleLinerRenderer.WidthMultiplier = 1f / targetRadius;
		}
	}
}