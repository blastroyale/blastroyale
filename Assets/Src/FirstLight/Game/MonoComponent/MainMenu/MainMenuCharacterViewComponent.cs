using System;
using FirstLight.Game.Messages;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <inheritdoc cref="CharacterEquipmentMonoComponent"/>
	public class MainMenuCharacterViewComponent : CharacterEquipmentMonoComponent, IDragHandler, IPointerClickHandler
	{
		public event Action<PointerEventData> Clicked;
		
		private const float MIN_FLARE_DELAY = 10f;
		private const float MAX_FLARE_DELAY = 25f;

		private float _nextFlareTime = -1f;
		private bool _playedFirstFlareAnim;
		private bool _disableRotation = true;

		protected override void Awake()
		{
			base.Awake();
			_nextFlareTime = Time.time + Random.Range(MIN_FLARE_DELAY / 2, MAX_FLARE_DELAY / 2);
		}

		public void EnableRotation()
		{
			_disableRotation = false;
		}


		private void Start()
		{
			_skin.Meta = true;
		}

		private void Update()
		{
			if (Time.time > _nextFlareTime)
			{
				_skin.TriggerFlair();
				_nextFlareTime = Time.time + Random.Range(MIN_FLARE_DELAY, MAX_FLARE_DELAY);
			}
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (_disableRotation) return;
			transform.parent.Rotate(0, -eventData.delta.x, 0, Space.Self);
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			Clicked?.Invoke(eventData);
		}
	}
}