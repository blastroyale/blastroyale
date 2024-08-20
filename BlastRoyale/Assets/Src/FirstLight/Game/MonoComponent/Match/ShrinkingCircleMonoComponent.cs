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
		[SerializeField, Required] private Transform _fireVfxZoneTransform;
		[SerializeField, Required] private ParticleSystem _ringOfFireParticle;
		[SerializeField, Required] private Material _damageMaterial;
		[SerializeField, Required] private MapData _mapData;
		
		private QuantumShrinkingCircleConfig _config;
		private IGameServices _services;
		
		// Ring of Fire FX config values
		private static readonly int _ringOfFireSegments = 32; // The more segments the smoother the ring of fire will be
		private static readonly FP _anglePerSegment = 360 / _ringOfFireSegments;
		private static readonly FP _ringOfFireWidth = FP._0_04;
		private FP _lastInnerRadius;
		private FP _innerRadius;
		private FP _outerRadius;
		private const int MaxParticles = 50;
		private Vector3[] _vertices;

		private Mesh _mesh;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			QuantumEvent.Subscribe<EventOnGameEnded>(this, HandleGameEnded);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, HandleUpdateView);
			_shrinkingCircleLinerRenderer.gameObject.SetActive(false);
			_safeAreaCircleLinerRenderer.gameObject.SetActive(false);
			_damageZoneTransform.gameObject.SetActive(false);
			_fireVfxZoneTransform.gameObject.SetActive(false);
			_ringOfFireParticle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
		}

		private void Start()
		{
			CreateDamageZoneMeshData();
		}

		private void CreateDamageZoneMeshData()
		{
			var meshFilter = _damageZoneTransform.gameObject.AddComponent<MeshFilter>();
			var meshRenderer = _damageZoneTransform.gameObject.AddComponent<MeshRenderer>();
			_mesh= meshFilter.mesh;
			_mesh.Clear();
			
			meshRenderer.material = _damageMaterial;
			var circleVertexResolution = 120;
			var cornerPositionSize = 4;
			
			var totalSize = circleVertexResolution + cornerPositionSize;
			
			var circlePositions = new Vector3[circleVertexResolution];
			
			var angle = 2 * Mathf.PI / circleVertexResolution;

			for (var i = 0; i < circleVertexResolution; i++)
			{
				var cos = Mathf.Cos(angle * i);
				var sin = Mathf.Sin(angle * i);
				
				var rotationMatrix = new Matrix4x4(new Vector4(cos, sin, 0, 0),
					new Vector4(-sin, cos, 0, 0),
					new Vector4(0, 0, 1, 0),
					new Vector4(0, 0, 0, 1));
				
				circlePositions[i] = rotationMatrix.MultiplyPoint(new Vector3(0, 1, 0));
			}

			_vertices= new Vector3[totalSize];
			for (var i = 0; i < circleVertexResolution; i++)
			{
				_vertices[i].Set(circlePositions[i].x, circlePositions[i].z, circlePositions[i].y);
			}
			
			// corner points
			_vertices[totalSize-1] = new Vector3(-1, 0, 1);
			_vertices[totalSize-2] = new Vector3(-1, 0, -1);
			_vertices[totalSize-3] = new Vector3(1, 0, -1);
			_vertices[totalSize-4] = new Vector3(1, 0, 1);
			
			var segmentResolution = circleVertexResolution / 4;
			
			var totalResolution = segmentResolution * 4;
			var triangleCount = totalResolution * 3;
			var triangles = new int[triangleCount + 12];
			
			for (var i = 0; i < totalResolution; i++)
			{
				triangles[i * 3] = i;
				triangles[i * 3 + 1] = (i + 1 == circleVertexResolution) ? 0 : i + 1;
				triangles[i * 3 + 2] = totalSize - (i / segmentResolution) - 1;
			}
			
			triangles[triangleCount] = 0;
			triangles[triangleCount+1] = totalSize-1;
			triangles[triangleCount+2] = totalSize-4;
			
			triangles[triangleCount+3] = segmentResolution * 3;
			triangles[triangleCount+4] = totalSize-3;
			triangles[triangleCount+5] = totalSize-4;
			
			triangles[triangleCount+6] = segmentResolution * 2;
			triangles[triangleCount+7] = totalSize-3;
			triangles[triangleCount+8] = totalSize-2;
			
			triangles[triangleCount+9] = segmentResolution;
			triangles[triangleCount+10] = totalSize-2;
			triangles[triangleCount+11] = totalSize-1;

			_mesh.vertices = _vertices;
			_mesh.triangles = triangles;
		}

		private void HandleGameEnded(EventOnGameEnded callback)
		{
			QuantumCallback.UnsubscribeListener(this);
		}
		
		private unsafe void HandleUpdateView(CallbackUpdateView callback)
		{
			var frame = callback.Game.Frames.Predicted;
			if (!frame.Unsafe.TryGetPointerSingleton<ShrinkingCircle>(out var circle) || circle->Step < 0)
			{
				return;
			}
			
			_shrinkingCircleLinerRenderer.gameObject.SetActive(true);
			_safeAreaCircleLinerRenderer.gameObject.SetActive(true);
			_damageZoneTransform.gameObject.SetActive(true);
			_fireVfxZoneTransform.gameObject.SetActive(true);

			
			var targetCircleCenter = circle->TargetCircleCenter.ToUnityVector2();
			var targetRadius = circle->TargetRadius.AsFloat;
			
			circle->GetMovingCircle(frame, out var centerFP, out var radiusFP);
			var radius = radiusFP.AsFloat;
			var center = centerFP.ToUnityVector2();
			
	
			var cornerPtSize = _mapData.Asset.Settings.WorldSize * 2;
			var pt1 = new Vector3(-1 * cornerPtSize, 0, 1 * cornerPtSize) / radius;
			var pt2 = new Vector3(-1 * cornerPtSize, 0, -1 * cornerPtSize) / radius;
			var pt3 = new Vector3(1 * cornerPtSize, 0, -1 * cornerPtSize) / radius;
			var pt4 = new Vector3(1 * cornerPtSize, 0, 1 * cornerPtSize) / radius;
			
			var vertexLength = _vertices.Length;
			_vertices[vertexLength - 1] = pt1;
			_vertices[vertexLength - 2] = pt2;
			_vertices[vertexLength - 3] = pt3;
			_vertices[vertexLength - 4] = pt4;
			_mesh.vertices = _vertices;

			
			var cachedShrinkingCircleLineTransform = _shrinkingCircleLinerRenderer.transform;
			var cachedSafeAreaCircleLine = _safeAreaCircleLinerRenderer.transform;
			
			var position = new Vector3(center.x, cachedShrinkingCircleLineTransform.position.y, center.y);
			
			_mesh.bounds = new Bounds(position, new Vector3(cornerPtSize, 1, cornerPtSize));
			
			if (_config == null || _config.Step != circle->Step)
			{
				_config = frame.Context.MapShrinkingCircleConfigs[Math.Clamp(circle->Step - 1,
				                                                             0,
				                                                             frame.Context.MapShrinkingCircleConfigs.Count - 1)];
			}
			
			cachedShrinkingCircleLineTransform.position = position;
			cachedShrinkingCircleLineTransform.localScale = new Vector3(radius, radius, 1f);
			_shrinkingCircleLinerRenderer.WidthMultiplier = 1f / radius;
			
			_damageZoneTransform.position = position;
			_damageZoneTransform.localScale = new Vector3(radius, _damageZoneTransform.localScale.y, radius);

			_fireVfxZoneTransform.position = position;
			_fireVfxZoneTransform.localScale = new Vector3(radius  * 2f, _fireVfxZoneTransform.localScale.y, radius * 2f);
			
			if (frame.Time < circle->ShrinkingStartTime - _config.WarningTime)
			{
				// We set white circle to be as big as red one at the first step to avoid it sitting small in the center of the map
				// It's because the safe zone position/scale is not revealed for a few seconds at the beginning of a match
				if (circle->Step == 1)
				{
					cachedSafeAreaCircleLine.localScale = new Vector3(radius, radius, 1f);
					_safeAreaCircleLinerRenderer.WidthMultiplier = 0f;
				}
				return;
			}
			
			cachedSafeAreaCircleLine.position = new Vector3(targetCircleCenter.x, cachedSafeAreaCircleLine.position.y, targetCircleCenter.y);
			cachedSafeAreaCircleLine.localScale = new Vector3(targetRadius, targetRadius, 1f);
			_safeAreaCircleLinerRenderer.WidthMultiplier = 1f / targetRadius;
			
			
			// Update ring of fire particle FX
			_innerRadius = radiusFP/FP._10;
			_outerRadius = _innerRadius + _ringOfFireWidth;
			if (_lastInnerRadius != _innerRadius)
			{
				_lastInnerRadius = _innerRadius;
				
				var emission = _ringOfFireParticle.emission;
				var shape = _ringOfFireParticle.shape;
				shape.radius = radius;
				shape.radiusThickness = 0;
				emission.rateOverTime = Math.Min(MaxParticles, radius * 2f);
				
				if (_ringOfFireParticle.isStopped)
				{
					_ringOfFireParticle.Play();
				}
			}
		}
	}
}