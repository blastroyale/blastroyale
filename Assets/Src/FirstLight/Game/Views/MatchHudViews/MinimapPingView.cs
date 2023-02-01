using DG.Tweening;
using FirstLight.Services;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Displays a squad ping on the minimap.
	/// </summary>
	public class MinimapPingView : MonoBehaviour, IPoolEntitySpawn, IPoolEntityDespawn
	{
		/// <summary>
		/// The position of this Ping in minimap viewport coordinates.
		/// </summary>
		public Vector3 ViewportPosition { get; set; }

		[SerializeField, Required] private RectTransform _container;
		[SerializeField, Required] private RectTransform _rectTransform;

		[SerializeField, Title("Animation")] private Ease _showEase = Ease.OutSine;
		[SerializeField] private float _showDuration = 0.3f;
		[SerializeField] private float _shownScale = 0.4f;
		[SerializeField] private Ease _hideEase = Ease.InSine;
		[SerializeField] private float _hideDuration = 0.3f;

		public void OnSpawn()
		{
			_rectTransform.DOKill();

			gameObject.SetActive(true);
			
			_rectTransform.localScale = Vector3.zero;
			_rectTransform.DOScale(Vector3.one * _shownScale, _showDuration).SetEase(_showEase);
		}

		public void OnDespawn()
		{
			_rectTransform.DOScale(Vector3.zero, _hideDuration).SetEase(_hideEase)
				.OnComplete(() => { gameObject.SetActive(false); });
		}
		
		/// <summary>
		/// Updates the anchoredPosition, clamping it to a circle within the container.
		/// </summary>
		public void SetPosition(Vector2 position)
		{
			var rect = _container.rect;
			_rectTransform.anchoredPosition = Vector2.ClampMagnitude(position, rect.width / 2f);
		}
	}
}