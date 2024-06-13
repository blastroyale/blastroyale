using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace FirstLight.Game.MonoComponent.MainMenu
{
	/// <inheritdoc cref="CharacterEquipmentMonoComponent"/>
	public class MainMenuCharacterViewComponent : CharacterEquipmentMonoComponent, IDragHandler, IPointerClickHandler
	{
		public event Action<PointerEventData> Clicked;

		public bool PlayEnterAnimation { get; set; } = false;

		private const float MIN_FLARE_DELAY = 10f;
		private const float MAX_FLARE_DELAY = 25f;

		private float _nextFlareTime = -1f;
		private bool _playedFirstFlareAnim;
		private float _inertia;

		protected override void Awake()
		{
			base.Awake();
			_nextFlareTime = Time.time + Random.Range(MIN_FLARE_DELAY / 2, MAX_FLARE_DELAY / 2);
		}

		private void Start()
		{
			_skin.Meta = true;
			if (PlayEnterAnimation)
			{
				_skin.TriggerEnter();
			}
		}

		private void Update()
		{
			if (Time.time > _nextFlareTime)
			{
				_skin.TriggerFlair();
				_nextFlareTime = Time.time + Random.Range(MIN_FLARE_DELAY, MAX_FLARE_DELAY);
			}
			if (_inertia != 0)
			{
				const float DRAG = 0.93f;
				_skin.transform.Rotate(Vector3.up, _inertia * Time.deltaTime);
				_inertia *= DRAG;
				if (Mathf.Abs(_inertia) < 0.01f)
				{
					_inertia = 0;
				}
			}
		}
		

		public void OnDrag(PointerEventData eventData)
		{
			_inertia = -eventData.delta.x * 10f;
			transform.parent.Rotate(0, -eventData.delta.x, 0, Space.Self);
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			Clicked?.Invoke(eventData);
		}
	}
}