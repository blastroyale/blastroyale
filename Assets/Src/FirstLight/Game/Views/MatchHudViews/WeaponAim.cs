using System.Linq;
using System.Numerics;
using FirstLight.Game.Ids;
using FirstLight.Services;
using Photon.Deterministic;
using Quantum;
using Quantum.Physics3D;
using Sirenix.OdinInspector;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Vfx class that handles weapon aimining in isolation.
	/// There are three lines, the main one which tells where your aimint center is and two aux lines for the max
	/// shot angle variation.
	/// The main line is always calculated in world space for precision.
	/// The aux lines are in local space and are only meant to give the player an idea on the max range variation
	/// of his weapon. This is so we don't need to keep updating the variation lines every frame.
	/// </summary>
	public unsafe class WeaponAim : Vfx<VfxId>
	{
		private const QueryOptions _hitQuery = QueryOptions.HitDynamics | QueryOptions.HitKinematics | QueryOptions.HitStatics;
	
		[Required, SerializeField] private LineRenderer _centerLineRenderer;
		[Required, SerializeField] private LineRenderer _upperLineRenderer;
		[Required, SerializeField] private LineRenderer _lowerLineRenderer;

		private const int _minAngleVariation = 15;
		
		private FP _variationRange;
		private FP _range;
		private FP _angleVariation = 0;
		private EntityView _view;
		
		public void SetView(EntityView view)
		{
			_view = view;
			transform.parent = _view.transform;
		}
		
		void OnEnable()
		{
			_upperLineRenderer.gameObject.SetActive(true);
			_lowerLineRenderer.gameObject.SetActive(true);
		}

		private void AdjustDottedLine(LineRenderer lineRenderer)
		{
			lineRenderer.material.mainTextureScale = new Vector2(1f / lineRenderer.startWidth / 2, 1.0f);
		}

		/// <summary>
		/// Updates the weapon equipped and recalculate weapon specific metrics.
		/// </summary>
		public void UpdateWeapon(Frame f, EntityRef entity, QuantumWeaponConfig newWeapon)
		{
			var rangeStat = f.Get<Stats>(entity).GetStatData(StatType.AttackRange).StatValue;
			_range = newWeapon.AttackRangeAimBonus + rangeStat;
			_angleVariation = newWeapon.MinAttackAngle;
			AdjustDottedLine(_centerLineRenderer);
			
			if (_angleVariation > _minAngleVariation)
			{
				_upperLineRenderer.gameObject.SetActive(true);
				_lowerLineRenderer.gameObject.SetActive(true);
				_variationRange = _range * FP._0_75;
			}
			else
			{
				_upperLineRenderer.gameObject.SetActive(false);
				_lowerLineRenderer.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// Updates the local aim indicator.
		/// It will use the simulation physics, transforms and colliders as opposed to Unity to detect if the aim line
		/// should collide with something this means it will use predicted frame data.
		/// Will not calculate weapon angle variations and range to save resources.
		/// Will also not update if the aim does not change to save resources.
		/// </summary>
		public void UpdateAimAngle(Frame f, EntityRef entity, FPVector2 aimDirection)
		{
			if (!f.Unsafe.TryGetPointer<PlayerCharacter>(entity, out var playerCharacter))
			{
				return;
			}

			aimDirection = aimDirection.Normalized;
			
			var origin = _view.transform.position.ToFPVector3();
			var end = (aimDirection * _range).XOY;

			var offset = transform.rotation * playerCharacter->ProjectileSpawnOffset.ToUnityVector3();
			origin += offset.ToFPVector3();
			
			DrawAimLine(f, _centerLineRenderer, entity, origin, end);
			if (_angleVariation > _minAngleVariation)
			{
				end = FPVector2.Rotate(aimDirection, -_angleVariation * FP.Deg2Rad).XOY * _variationRange;
				DrawAimLine(f, _lowerLineRenderer, entity, origin, end);
				
				end = FPVector2.Rotate(aimDirection, _angleVariation * FP.Deg2Rad).XOY * _variationRange;
				DrawAimLine(f, _upperLineRenderer, entity, origin, end);
			}
		}

		private void DrawAimLine(Frame f, LineRenderer line, EntityRef entity, FPVector3 origin, FPVector3 end)
		{
			var originUnity = origin.ToUnityVector3();
			var lineEnd = originUnity + end.ToUnityVector3();
			var hits = f.Physics3D.LinecastAll(origin, origin+end, f.Context.TargetAllLayerMask, _hitQuery);
			line.SetPosition(0, originUnity);
			if (hits.Count > 0)
			{
				var hit = hits.ToArray().FirstOrDefault(hit => IsValidRaycastHit(hit, entity));
				if (hit.Point != FPVector3.Zero)
				{
					lineEnd = hit.Point.ToUnityVector3();
				}
			} 
			line.SetPosition(1, lineEnd);
		}

		private bool IsValidRaycastHit(Hit3D hit, EntityRef shooter)
		{
			return hit.Point != FPVector3.Zero && (hit.Entity.IsValid && hit.Entity != shooter || !hit.IsDynamic);
		}
	}
}