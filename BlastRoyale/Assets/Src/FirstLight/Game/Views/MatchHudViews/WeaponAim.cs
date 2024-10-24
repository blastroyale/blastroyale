 using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Domains.VFX;
using FirstLight.Game.Ids;
 using FirstLight.Game.MonoComponent.EntityPrototypes;
 using FirstLight.Game.MonoComponent.EntityViews;
 using FirstLight.Game.Utils;
using FirstLight.Services;
using Photon.Deterministic;
using Quantum;
 using Quantum.Physics2D;
 using Quantum.Physics3D;
using Quantum.Systems;
using Sirenix.OdinInspector;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

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
	public unsafe class WeaponAim : VfxMonoBehaviour
	{
		private const QueryOptions _hitQuery = QueryOptions.HitDynamics | QueryOptions.HitKinematics | QueryOptions.HitStatics | QueryOptions.ComputeDetailedInfo;
	
		[Required, SerializeField] private LineRenderer _centerLineRenderer;
		[Required, SerializeField] private LineRenderer _upperLineRenderer;
		[Required, SerializeField] private LineRenderer _lowerLineRenderer;

		private const int _minAngleVariation = 90;
		private readonly Color _sideLineStartColor = new (0.13f, 0.13f, 0.13f);
		private readonly Color _sideLineEndColor = new (0.02f, 0.02f, 0.02f);
		private readonly Color _mainLineColor = Color.white;

		private PlayerCharacterViewMonoComponent _playerView;
		private HitCollection _hits;
		private Shape2D _shape;
		private FP _variationRange;
		private FP _range;
		private FP _angleVariation = 0;
		private EntityView _view;
		private FPVector2 _aim;
		private FPVector2 _lastAim;
		private int _lastFrameUpdate;

		public void SetView(EntityView view)
		{
			_view = view;
			transform.parent = _view.transform;
			_playerView = _view.GetComponent<PlayerCharacterMonoComponent>().PlayerView;
		}
		
		void OnEnable()
		{
			_upperLineRenderer.gameObject.SetActive(_angleVariation > _minAngleVariation);
			_lowerLineRenderer.gameObject.SetActive(_angleVariation > _minAngleVariation);
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
			if (!f.TryGet<Stats>(entity, out var stats)) return;
			
			_range = stats.GetStatData(StatType.AttackRange).StatValue;
			_angleVariation = newWeapon.MinAttackAngle;
			AdjustDottedLine(_centerLineRenderer);
			
			// X and Y are similar to the main bullet collider
			var colliderSize = new FPVector2(FP._0_10 + FP._0_01, _range / 2);
			_shape = Shape2D.CreateBox(colliderSize);
			
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
		/// </summary>
		public void UpdateAimAngle(Frame f, EntityRef entity, FPVector2 aimDirection)
		{
			_lastAim = _aim;
			_aim = aimDirection;

			if(ShouldReRenderLines()) ReRenderLines();
		}
		
		/// <summary>
		/// Sets the color of line renderers.
		/// </summary>
		public void SetColor(Color c)
		{
			_centerLineRenderer.startColor = c;
			_centerLineRenderer.endColor = c;
			_lowerLineRenderer.startColor = c;
			_lowerLineRenderer.endColor = c;
			_upperLineRenderer.startColor = c;
			_upperLineRenderer.endColor = c;
		}

		/// <summary>
		/// Resets the color of line renderers back to original color
		/// </summary>
		public void ResetColor()
		{
			_centerLineRenderer.startColor = _mainLineColor;
			_centerLineRenderer.endColor = _mainLineColor;
			_lowerLineRenderer.startColor = _sideLineStartColor;
			_lowerLineRenderer.endColor = _sideLineEndColor;
			_upperLineRenderer.startColor = _sideLineStartColor;
			_upperLineRenderer.endColor = _sideLineEndColor;
		}
		
		/// <summary>
		// We attempt to render the line update on the begining of the frame it was changed
		// to avoid rendering the update on the next frame if the update was done after rendering was already done
		private bool ShouldReRenderLines()
		{
			return (_lastFrameUpdate != Time.frameCount || _lastAim != _aim);
		}

		private void Update()
		{
			if(ShouldReRenderLines()) ReRenderLines();
		}

		private void ReRenderLines()
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning()) return;
			
			var f = QuantumRunner.Default.Game.Frames.Verified;
			if (!f.Unsafe.TryGetPointer<PlayerCharacter>(_view.EntityRef, out var playerCharacter))
			{
				return;
			}

			_aim = _aim.Normalized;

			var playerTransform = _playerView.transform;
			var origin = playerTransform.position.ToFPVector2();
			var end = (_aim * _range);

			var offset = FPVector2.Rotate(playerCharacter->ProjectileSpawnOffset, playerTransform.rotation.ToFPRotation2D()); // rotates according to view, not simulation
			origin += offset + ProjectileSystem.CAMERA_CORRECTION;

			DrawAimLine(f, _centerLineRenderer, _view.EntityRef, origin, end);
			
			if (_angleVariation > _minAngleVariation)
			{
				end = FPVector2.Rotate(_aim, (-_angleVariation / FP._2) * FP.Deg2Rad) * _variationRange;
				DrawAimLine(f, _lowerLineRenderer, _view.EntityRef, origin, end);
				
				end = FPVector2.Rotate(_aim, (_angleVariation / FP._2) * FP.Deg2Rad) * _variationRange;
				DrawAimLine(f, _upperLineRenderer, _view.EntityRef, origin, end);
			}

			_lastFrameUpdate = Time.frameCount;
		}

		/// <summary>
		/// Replaced by hit aimcast because quantum is not returning the hit point correctly on 2d
		/// </summary>
		private FPVector2 GetHit(Frame f, EntityRef entity, FPVector2 origin, FPVector2 direction)
		{
			var directionNormalized = direction.Normalized;
			var centerForShape = origin + (direction / 2);

			var rot = directionNormalized.ToRotation();
			
			_hits = f.Physics2D.OverlapShape(centerForShape, rot, _shape, f.Layers.GetLayerMask(PhysicsLayers.PLAYERS, PhysicsLayers.OBSTACLES), _hitQuery);
			
			if (_hits.Count > 0)
			{
				var closestHit = FPVector2.Zero;
				var smallestDistanceSqr = FP.MaxValue;
				for (var i = 0; i < _hits.Count; i++)
				{
					var hit = _hits[i];
					
					var checkSqrDistance = FPVector2.DistanceSquared(_hits[i].Point, origin);
					if (checkSqrDistance < smallestDistanceSqr && IsValidRaycastHit(f, &hit, entity))
					{
						smallestDistanceSqr = checkSqrDistance;
						closestHit = _hits[i].Point;
					}
				}
				if (closestHit != FPVector2.Zero)
				{
					return closestHit;
				}
			}
			return FPVector2.Zero;
		}
		
		/// <summary>
		/// TODO: Remove this method and use the above function when quantum fixes getting the collision point
		/// </summary>
		private FPVector2 GetHitLinecast(Frame f, EntityRef entity, FPVector2 origin, FPVector2 direction)
		{
			var hit = f.Physics2D.Linecast(origin, origin + direction, f.Layers.GetLayerMask(PhysicsLayers.PLAYERS, PhysicsLayers.OBSTACLES), _hitQuery);
			if (hit == null)
			{
				return FPVector2.Zero;
			}
			else
			{
				var v = hit.Value;
				var ptr = &v;
				if (!IsValidRaycastHit(f, ptr, entity))
				{
					return FPVector2.Zero;
				}
				return v.Point;
			}
			
		}

		private void DrawAimLine(Frame f, LineRenderer line, EntityRef entity, FPVector2 origin, FPVector2 direction)
		{
			var originUnity = origin;
			line.SetPosition(0, originUnity.ToUnityVector3());
			
			var hit = GetHitLinecast(f, entity, origin, direction);
			var lineEnd = originUnity + direction;

			if (hit != FPVector2.Zero)
			{
				lineEnd = hit;
			}
;			line.SetPosition(1, lineEnd.ToUnityVector3());
		}

		private bool IsValidRaycastHit(Frame f, Hit* hit, EntityRef shooter)
		{
			return hit->Point != FPVector2.Zero && (hit->Entity.IsValid && hit->Entity != shooter || !hit->IsDynamic) && (!QuantumFeatureFlags.TEAM_IGNORE_COLLISION || !TeamSystem.HasSameTeam(f, shooter, hit->Entity));
		}
	}
}