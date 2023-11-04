using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
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
		[SerializeField, Required] private GameObject _ringOfFireDrawMesh;
		[SerializeField, Required] private ParticleSystem _ringOfFireParticle; 

		private QuantumShrinkingCircleConfig _config;
		private IGameServices _services;
		
		// Ring of Fire FX config values
		private static readonly int _ringOfFireSegments = 32; // The more segments the smoother the ring of fire will be
		private static readonly FP _anglePerSegment = 360 / _ringOfFireSegments;
		private static readonly FP _ringOfFireWidth = FP._0_04;
		private FP _lastInnerRadius;
		private FP _innerRadius;
		private FP _outerRadius;
		private Mesh _mesh;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			QuantumEvent.Subscribe<EventOnGameEnded>(this, HandleGameEnded);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, HandleUpdateView);
			_shrinkingCircleLinerRenderer.gameObject.SetActive(false);
			_safeAreaCircleLinerRenderer.gameObject.SetActive(false);
			_damageZoneTransform.gameObject.SetActive(false);
			
			_mesh = new Mesh();
			_ringOfFireDrawMesh.GetComponent<MeshFilter>().mesh = _mesh;
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
					
					// We also move Ring of Fire very far so we don't see the flame particle at the center of the map in the initial delay phase
					_ringOfFireDrawMesh.transform.position = new Vector3(radius * 2f, radius * 2f, radius * 2f);
				}
				
				return;
			}
			
			cachedSafeAreaCircleLine.position = new Vector3(targetCircleCenter.x, cachedSafeAreaCircleLine.position.y, targetCircleCenter.y);
			cachedSafeAreaCircleLine.localScale = new Vector3(targetRadius, targetRadius, 1f);
			_safeAreaCircleLinerRenderer.WidthMultiplier = 1f / targetRadius;
			
			// Update ring of fire particle FX
			_ringOfFireDrawMesh.transform.position = _damageZoneTransform.position;
			_innerRadius = radiusFP/FP._10;
			_outerRadius = _innerRadius + _ringOfFireWidth;
			if (_lastInnerRadius != _innerRadius)
			{
				_lastInnerRadius = _innerRadius;
				//UpdateRingOfFireMesh();
				
				// In case we need to have fewer particles for smaller circle (the code below doesn't really work, it's just to show the idea)
				// var emission = _ringOfFireParticle.emission;
				// FP _radiusToEmissionMultiplier = FP._10 + FP._5;
				// emission.rateOverTime = FPMath.CeilToInt(radiusFP * _radiusToEmissionMultiplier);
			}
		}

		// TODO: We can bake this mesh calculations in a LUT (lookup table) 
		private void UpdateRingOfFireMesh()
		{
			_mesh.Clear();
			Vector3[] vertices = new Vector3[((_ringOfFireSegments + 1) * 2)];
			int[] triangles = new int[_ringOfFireSegments * 6];
			
			for (int i = 0; i <= _ringOfFireSegments; i++)
			{
				var rad = FP.Deg2Rad * (i * _anglePerSegment);
				var c = FPMath.Cos(rad); // used FPMatch because it uses Lookup tables
				var s = FPMath.Sin(rad);
				vertices[i * 2] = new FPVector2(_innerRadius * c, _innerRadius * s).ToUnityVector2();
				vertices[i * 2 + 1] = new FPVector2(_outerRadius * c, _outerRadius * s).ToUnityVector2();

				if (i < _ringOfFireSegments)
				{
					int j = i * 6;
					triangles[j] = i * 2;
					triangles[j + 1] = triangles[j + 4] = (i + 1) * 2;
					triangles[j + 2] = triangles[j + 3] = i * 2 + 1;
					triangles[j + 5] = (i + 1) * 2 + 1;
				}
			}
			_mesh.vertices = vertices;
			_mesh.triangles = triangles;
			_mesh.RecalculateNormals();
		}
	}
}