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

		[SerializeField, Required, Title("Refs")]
		private Image _timerImage;

		[SerializeField, Required] private Image _timerBackground;
		[SerializeField, Required] private Image _glow;
		[SerializeField, Required] private Image _icon;

		[SerializeField, Title("Colors")] private Color _iconDroppingColor;
		[SerializeField] private Color _iconLandedColor;
		[SerializeField] private Color _glowLandedColor;

		[SerializeField, Title("Animation")] private Ease _showEase = Ease.OutSine;
		[SerializeField] private float _showDuration = 0.3f;
		[SerializeField] private Ease _hideEase = Ease.InSine;
		[SerializeField] private float _hideDuration = 0.3f;

		private RectTransform _container;
		private RectTransform _rectTransform;

		private AirDrop _airDrop;

		private void Awake()
		{
			_rectTransform = GetComponent<RectTransform>();
			_container = _rectTransform.parent.GetComponent<RectTransform>();
		}

		private void Update()
		{
			_glow.transform.localScale = Vector3.one + Vector3.one * ((Mathf.Sin(Time.time * 2f) + 1f) / 2f * 0.2f);
		}

		public void OnSpawn()
		{
			_rectTransform.DOKill();

			gameObject.SetActive(true);

			_timerImage.enabled = true;
			_timerBackground.enabled = true;
			_glow.enabled = false;
			_glow.color = new Color(1f, 1f, 1f, 0f);
			_icon.color = _iconDroppingColor;

			_rectTransform.localScale = Vector3.zero;
			_rectTransform.DOScale(Vector3.one * 0.6f, _showDuration).SetEase(_showEase);
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
			if (_airDrop.Duration > FP._0)
			{
				_timerImage.fillAmount =
					Mathf.Clamp01(((time - _airDrop.StartTime - _airDrop.Delay) / _airDrop.Duration).AsFloat);
			}
		}

		/// <summary>
		/// Sets the AirDrop and it's minimap viewport position.
		/// </summary>
		public void SetAirdrop(AirDrop airDrop, Vector3 viewportPosition)
		{
			_airDrop = airDrop;
			ViewportPosition = viewportPosition;
		}

		/// <summary>
		/// Updates the anchoredPosition, clamping it to a circle within the container.
		/// </summary>
		public void SetPosition(Vector2 position)
		{
			var rect = _container.rect;
			_rectTransform.anchoredPosition = Vector2.ClampMagnitude(position, rect.width / 2f);
		}

		/// <summary>
		/// Puts the indicator into the "landed" visual state.
		/// </summary>
		public void OnLanded()
		{
			_timerImage.enabled = false;
			_timerBackground.enabled = false;
			_glow.enabled = true;
			_glow.DOColor(_glowLandedColor, 0.3f);
			_icon.DOColor(_iconLandedColor, 0.3f);
		}
	}
}