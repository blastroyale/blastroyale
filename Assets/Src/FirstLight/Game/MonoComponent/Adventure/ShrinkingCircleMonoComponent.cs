using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Adventure
{
	/// <summary>
	/// This Mono Component controls shrinking circle visuals behaviour
	/// </summary>
	public class ShrinkingCircleMonoComponent : MonoBehaviour
	{
		[SerializeField] private CircleLineRenderer _shrinkingCircleLinerRenderer;
		[SerializeField] private CircleLineRenderer _safeAreaCircleLinerRenderer;
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
			var frame = callback.Game.Frames.Verified;
			var circle = frame.GetSingleton<ShrinkingCircle>();
			var targetCircleCenter = circle.TargetCircleCenter.ToUnityVector2();
			var targetRadius = circle.TargetRadius.AsFloat;
			var safeArea = new Vector3(targetCircleCenter.x, _safeAreaCircleLinerRenderer.transform.position.y, targetCircleCenter.y);
			var lerp = Mathf.Max(0, (frame.Time.AsFloat - circle.ShrinkingStartTime.AsFloat) / circle.ShrinkingDurationTime.AsFloat);
			var radius = Mathf.Lerp(circle.CurrentRadius.AsFloat, targetRadius, lerp);
			var center = Vector2.Lerp(circle.CurrentCircleCenter.ToUnityVector2(), targetCircleCenter, lerp);
			var position = new Vector3(center.x, _shrinkingCircleLinerRenderer.transform.position.y, center.y);
			var cachedTransform = _damageZoneTransform;

			if (_config.Step != circle.Step)
			{
				_config = _services.ConfigsProvider.GetConfig<QuantumShrinkingCircleConfig>(circle.Step);
			}
			
			cachedTransform.position = position;
			cachedTransform.localScale = new Vector3(radius * 2f, cachedTransform.localScale.y, radius * 2f);
			
			_shrinkingCircleLinerRenderer.Draw(position, radius);

			if (frame.Time < circle.ShrinkingStartTime - _config.WarningTime)
			{
				return;
			}
			
			_safeAreaCircleLinerRenderer.Draw(safeArea, targetRadius);
		}
	}
}