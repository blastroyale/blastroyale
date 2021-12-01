using DG.Tweening;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.Game.Views.TooltipView;
using FirstLight.Services;
using FirstLight.UiService;
using SRF;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// /// <summary>
	/// This Presenter handles the tooltips for the UI 
	/// </summary>
	public class UiTooltipPresenter : UiPresenter
	{
		[SerializeField] private Transform _tooltipParentTransform;
		[SerializeField] private TooltipHelper _tooltipHelper;
		
		/// <summary>
		/// Show a tool tip graphic at world and arrow position
		/// </summary>
		public void ShowTooltipHelper(string locTag, Vector3 worldPos, TooltipHelper.TooltipArrowPosition tooltipArrowPosition)
		{
			_tooltipHelper.ShowTooltip(locTag, worldPos, tooltipArrowPosition);
		}
	}
}