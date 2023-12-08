using DG.Tweening;
using FirstLight.Services;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Displays an airdrop icon on the minimap, with a timer.
	/// </summary>
	public class MinimapAirdropView : MonoBehaviour, IPoolEntitySpawn, IPoolEntityDespawn
	{
		/// <summary>
		/// The position of this AirDrop in minimap viewport coordinates.
		/// </summary>
		public Vector3 ViewportPosition { get; private set; }

		/// <summary>
		/// The quantum's <see cref="AirDrop"/> data for this view
		/// </summary>
		public AirDrop AirDrop { get; private set; }

		/// <summary>
		/// The quantum's <see cref="EntityRef"/> representing this airdrop
		/// </summary>
		public EntityRef Entity { get; private set; }

		[SerializeField, Required, Title("Refs")]
		private Image _timerImage;

		[SerializeField, Required] private RectTransform _container;
		[SerializeField, Required] private RectTransform _rectTransform;

		[SerializeField, Required] private Image _timerBackground;
		[SerializeField, Required] private Image _icon;
		[SerializeField, Required] private Image _innerCircle;

		[SerializeField, Title("Colors")] private Color _iconLandedColor;
		[SerializeField] private Color _glowLandedColor;
		[SerializeField] private float _innerCircleWithinBoundsOpacity;

		[SerializeField, Title("Animation")] private Ease _showEase = Ease.OutSine;
		[SerializeField] private float _showDuration = 0.3f;
		[SerializeField] private Ease _hideEase = Ease.InSine;
		[SerializeField] private float _hideDuration = 0.3f;

		public void OnSpawn()
		{
			_rectTransform.DOKill();

			gameObject.SetActive(true);

			_timerImage.enabled = true;
			_timerBackground.enabled = true;

			var endScale = _rectTransform.localScale;
			_rectTransform.localScale = Vector3.zero;
			_rectTransform.DOScale(endScale, _showDuration).SetEase(_showEase);
		}

		public void OnDespawn()
		{
			_rectTransform.DOScale(Vector3.zero, _hideDuration).SetEase(_hideEase)
				.OnComplete(() => { gameObject.SetActive(false); });
		}

		/// <summary>
		/// Updates the airdrop loading animations with Quantum time.
		/// </summary>
		public void UpdateTime(FP time)
		{
			if (AirDrop.Duration > FP._0)
			{
				_timerImage.fillAmount =
					Mathf.Clamp01(((time - AirDrop.StartTime - AirDrop.Delay) / AirDrop.Duration).AsFloat);
			}
		}

		/// <summary>
		/// Sets the AirDrop and it's minimap viewport position.
		/// </summary>
		public void SetAirdrop(AirDrop airDrop, EntityRef entity, Vector3 viewportPosition)
		{
			AirDrop = airDrop;
			Entity = entity;
			ViewportPosition = viewportPosition;
		}

		/// <summary>
		/// Updates the anchoredPosition, clamping it to a circle within the container.
		/// </summary>
		public void SetPosition(Vector2 position)
		{
			var rect = _container.rect;
			var clampedPos = Vector2.ClampMagnitude(position, rect.width / 2f);
			_rectTransform.anchoredPosition = clampedPos;

			if (!Mathf.Approximately(position.magnitude, clampedPos.magnitude))
			{
				// Outside of the minimap - do tanya thing
				_innerCircle.color = _innerCircle.color.Alpha(_innerCircleWithinBoundsOpacity);
			}
			else
			{
				_innerCircle.color = _innerCircle.color.Alpha(1f);
			}
		}

		/// <summary>
		/// Puts the indicator into the "landed" visual state.
		/// </summary>
		public void OnLanded()
		{
			_timerImage.enabled = false;
			_timerBackground.enabled = false;
			_icon.DOColor(_iconLandedColor, 0.3f);
		}
	}
}