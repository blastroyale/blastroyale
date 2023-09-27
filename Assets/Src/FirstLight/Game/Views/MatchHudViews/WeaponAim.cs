using System.Linq;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Photon.Deterministic;
using Quantum;
using Quantum.Physics3D;
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
	public unsafe class WeaponAim : Vfx<VfxId>
	{
		private const QueryOptions _hitQuery = QueryOptions.HitDynamics | QueryOptions.HitKinematics | QueryOptions.HitStatics;
	
		[Required, SerializeField] private LineRenderer _centerLineRenderer;
		[Required, SerializeField] private LineRenderer _upperLineRenderer;
		[Required, SerializeField] private LineRenderer _lowerLineRenderer;

		private const int _minAngleVariation = 15;
		private readonly Color _sideLineStartColor = new (0.13f, 0.13f, 0.13f);
		private readonly Color _sideLineEndColor = new (0.02f, 0.02f, 0.02f);
		private readonly Color _mainLineColor = Color.white;
		
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
			_range = f.Get<Stats>(entity).GetStatData(StatType.AttackRange).StatValue;
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
			
			var f = QuantumRunner.Default.Game.Frames.Predicted;
			if (!f.Unsafe.TryGetPointer<PlayerCharacter>(_view.EntityRef, out var playerCharacter))
			{
				return;
			}

			_aim = _aim.Normalized;

			var playerTransform = _view.transform;
			var origin = playerTransform.position.ToFPVector3();
			var end = (_aim * _range).XOY;

			var offset = playerTransform.rotation * playerCharacter->ProjectileSpawnOffset.ToUnityVector3();
			origin += offset.ToFPVector3();
			
			if (FeatureFlags.BULLET_CAMERA_ADJUSTMENT)
			{
				origin += BulletMonoComponent.CameraCorrectionOffset.ToFPVector3();
			}

			DrawAimLine(f, _centerLineRenderer, _view.EntityRef, origin, end);
			
			if (_angleVariation > _minAngleVariation)
			{
				end = FPVector2.Rotate(_aim, -_angleVariation * FP.Deg2Rad).XOY * _variationRange;
				DrawAimLine(f, _lowerLineRenderer, _view.EntityRef, origin, end);
				
				end = FPVector2.Rotate(_aim, _angleVariation * FP.Deg2Rad).XOY * _variationRange;
				DrawAimLine(f, _upperLineRenderer, _view.EntityRef, origin, end);
			}

			_lastFrameUpdate = Time.frameCount;
		}

		private Vector3 GetHit(Frame f, EntityRef entity, FPVector3 origin, FPVector3 end)
		{
			var hits = f.Physics3D.LinecastAll(origin, origin+end, -1, _hitQuery);
			if (hits.Count > 0)
			{
				hits.SortCastDistance();
				var hit = hits.ToArray().FirstOrDefault(hit => IsValidRaycastHit(f, hit, entity));
				if (hit.Point != FPVector3.Zero)
				{
					return hit.Point.ToUnityVector3();
				}
			}
			return Vector3.zero;
		}

		private void DrawAimLine(Frame f, LineRenderer line, EntityRef entity, FPVector3 origin, FPVector3 end)
		{
			var originUnity = origin.ToUnityVector3();
			var lineEnd = originUnity + end.ToUnityVector3();
			
			line.SetPosition(0, originUnity);
			var hit = GetHit(f, entity, origin, end);
			if (hit != Vector3.zero) lineEnd = hit;
;			line.SetPosition(1, lineEnd);
		}

		private bool IsValidRaycastHit(Frame f, Hit3D hit, EntityRef shooter)
		{
			return hit.Point != FPVector3.Zero && (hit.Entity.IsValid && hit.Entity != shooter || !hit.IsDynamic) && !TeamHelpers.HasSameTeam(f, shooter, hit.Entity);
		}
	}
}