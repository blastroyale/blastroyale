using DG.Tweening;
using FirstLight.Services;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Displays a friendly player indicator.
	/// </summary>
	public class MinimapFriendlyView : MonoBehaviour, IPoolEntitySpawn, IPoolEntityDespawn
	{
		/// <summary>
		/// The last known position of the player.
		/// </summary>
		public Vector3 PlayerTransformPosition
		{
			get
			{
				if (_playerTransform != null)
				{
					_playerTransformPosition = _playerTransform.position;
				}

				return _playerTransformPosition;
			}
		}

		/// <summary>
		/// The quantum's <see cref="EntityRef"/> representing the friendly player.
		/// </summary>
		public EntityRef Entity { get; private set; }

		/// <summary>
		/// If the state of this element is alive or dead.
		/// </summary>
		public bool Alive { get; private set; } = true;

		[SerializeField, Required] private RectTransform _container;
		[SerializeField, Required] private RectTransform _rectTransform;
		[SerializeField, Required] private GameObject _alive;
		[SerializeField, Required] private GameObject _dead;
		[SerializeField, Required] private Image _aliveIcon;
		[SerializeField, Required] private Image _deadIcon;
		[SerializeField, Required] private Image _characterAvatar;

		private Transform _playerTransform;
		private Vector3 _playerTransformPosition;
		private bool _positionSet;

		public void OnSpawn()
		{
			// We don't enable the GameObject here because we need to wait for the position to be set for the first time.

			_alive.SetActive(true);
			_dead.SetActive(false);
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
			_playerTransform = playerTransform;
		}

		/// <summary>
		/// Sets the color of the player.
		/// </summary>
		public void SetColor(Color color)
		{
			_aliveIcon.color = color;
			_deadIcon.color = color;
		}
		
		/// <summary>
		/// Sets the color of the player.
		/// </summary>
		public void SetAvatar(Sprite avatar)
		{
			_characterAvatar.sprite = avatar;
		}



		/// <summary>
		/// Sets the visual state as alive or dead.
		/// </summary>
		public void SetAlive(bool alive)
		{
			Alive = alive;
			_alive.SetActive(alive);
			_dead.SetActive(!alive);

			if (!alive)
			{
				_dead.transform.DOPunchScale(Vector3.one * 1.4f, 0.3f, 0, 0);
			}
		}

		/// <summary>
		/// Updates the anchoredPosition, clamping it to a circle within the container.
		/// </summary>
		public void SetPosition(Vector2 position)
		{
			if (!_positionSet)
			{
				gameObject.SetActive(true);
				_positionSet = true;
			}

			var rect = _container.rect;
			_rectTransform.anchoredPosition = Vector2.ClampMagnitude(position, rect.width / 2f);
		}
	}
}