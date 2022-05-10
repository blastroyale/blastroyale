using FirstLight.Services;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This Mono Component is responsible for displaying an on screen arrow graphic (bound by the screen dimensions) rotated towards
	/// an off screen transform in the environment, text indicates distance from the source to the target in metres 
	/// </summary>
	public class IndicatorView : MonoBehaviour, IPoolEntityDespawn
	{
		// Check on I2Localization the Term reference
		[SerializeField, Required] private CanvasGroup _canvasGroup;
		[SerializeField] private float _clampScreenPadding;
		
		private EntityView _targetEntityView;
		private Rect _safeArea;
		private Rect _screenRect;
		private Camera _camera;

		/// <summary>
		/// The current target entity
		/// </summary>
		public EntityRef Target { get; private set; }

		private void OnValidate()
		{
			_canvasGroup = _canvasGroup ? _canvasGroup : GetComponent<CanvasGroup>();
		}

		private void Awake()
		{
			_safeArea = Screen.safeArea;
			_screenRect = new Rect(0, 0, Screen.width, Screen.height);

			_safeArea.min += _clampScreenPadding * Vector2.one;
			_safeArea.max -= _clampScreenPadding * Vector2.one;
		}

		/// <inheritdoc />
		public void OnDespawn()
		{
			_targetEntityView = null;
			Target = EntityRef.None;
		}
		
		/// <summary>
		/// Sets the given  <see cref="targetEntityView"/> of this indicator
		/// </summary>
		public void Activate(EntityView targetEntityView)
		{
			_targetEntityView = targetEntityView;
			_camera = Camera.main;
			
			Target = _targetEntityView.EntityRef;

			if (!gameObject.activeSelf)
			{
				gameObject.SetActive(true);
			}
			
			UpdateView();
		}

		/// <summary>
		/// Deactivates this View
		/// </summary>
		public void Deactivate()
		{
			if (gameObject.activeSelf)
			{
				gameObject.SetActive(false);
				OnDespawn();
			}
		}

		/// <summary>
		/// Updates the Indicator direction and screen position
		/// </summary>
		public void UpdateView()
		{
			if (!Target.IsValid)
			{
				return;
			}
			
			var destinationPos = _targetEntityView.transform.position;
			var newPos = _camera.WorldToScreenPoint(destinationPos);
			
			if (_screenRect.Contains(newPos))
			{
				_canvasGroup.alpha = 0;
				
				return;
			}
			
			var targetPosLocal = _camera.transform.InverseTransformPoint(destinationPos);
			var cacheTransform = transform;
			
			cacheTransform.position = GetIndicatorPosition(newPos);
			cacheTransform.localEulerAngles = new Vector3(0, 0, -Mathf.Atan2(targetPosLocal.x, targetPosLocal.y) * Mathf.Rad2Deg);
			_canvasGroup.alpha = 1;
		}

		// Uses the line intersection algorithm
		// https://github.com/jinincarnate/off-screen-indicator/blob/master/Off%20Screen%20Indicator/Assets/Scripts/OffScreenIndicatorCore.cs
		private Vector3 GetIndicatorPosition(Vector3 position)
		{
			var screenCentre = (Vector3) _screenRect.size / 2f;
			
			position -= screenCentre;
			
			if (position.z < 0)
			{
				position *= -1;
			}
			
			var slope = Mathf.Tan(Mathf.Atan2(position.y, position.x));
			position = position.x > 0 ? new Vector3(screenCentre.x, screenCentre.x * slope, 0) : new Vector3(-screenCentre.x, -screenCentre.x * slope, 0);
			
			if (position.y > screenCentre.y)
			{
				position = new Vector3(screenCentre.y / slope, screenCentre.y, 0);
			}
			else if (position.y < -screenCentre.y)
			{
				position = new Vector3(-screenCentre.y / slope, -screenCentre.y, 0);
			}
			
			position += screenCentre;
			return Rect.NormalizedToPoint(_safeArea, Rect.PointToNormalized(_safeArea, position));
		}
	}
}