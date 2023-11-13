using FirstLight.Services;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Displays a friendly player indicator.
	/// </summary>
	public class MinimapFriendlyView : MonoBehaviour, IPoolEntitySpawn, IPoolEntityDespawn
	{
		/// <summary>
		/// The transform of the friendly player.
		/// </summary>
		public Transform PlayerTransform { get; private set; }

		/// <summary>
		/// The quantum's <see cref="EntityRef"/> representing the friendly player.
		/// </summary>
		public EntityRef Entity { get; private set; }

		[SerializeField, Required] private RectTransform _container;
		[SerializeField, Required] private RectTransform _rectTransform;
		[SerializeField, Required] private Image _color;

		public void OnSpawn()
		{
			gameObject.SetActive(true);
		}

		public void OnDespawn()
		{
			gameObject.SetActive(false);
		}

		/// <summary>
		/// Sets the player entity and transform.
		/// </summary>
		public void SetPlayer(EntityRef entity, Transform playerTransform)
		{
			Entity = entity;
			PlayerTransform = playerTransform;
		}

		/// <summary>
		/// Sets the color of the player.
		/// </summary>
		public void SetColor(Color color)
		{
			_color.color = color;
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