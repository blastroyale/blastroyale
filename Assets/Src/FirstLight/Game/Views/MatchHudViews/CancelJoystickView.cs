using System;
using FirstLight.Game.Input;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Onscreen view to cancel <see cref="JoystickView"/> 
	/// </summary>
	public class CancelJoystickView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField, Required] private UnityInputScreenControl _onScreenCancelPointerDown;
		
		private PointerEventData _pointerDownData;

		private int? CurrentPointerId => _pointerDownData?.pointerId;

		private void OnDisable()
		{
			_pointerDownData = null;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (CurrentPointerId.HasValue)
			{
				return;
			}
			
			_pointerDownData = eventData;
			
			_onScreenCancelPointerDown.SendValueToControl(1f);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (CurrentPointerId != eventData.pointerId)
			{
				return;
			}
			
			_pointerDownData = null;
			
			_onScreenCancelPointerDown.SendValueToControl(0f);
		}
	}
}