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
		[SerializeField] private GameObject _tooltipHelperPrefab;
		
		private TooltipHelper _tooltipHelper;
		
		protected void Awake()
		{
			var go  = Instantiate(_tooltipHelperPrefab);
			_tooltipHelper = go.GetComponent<TooltipHelper>();
			_tooltipHelper.transform.SetParent(_tooltipParentTransform);
			_tooltipHelper.transform.ResetLocal();
		}

		public void ShowTooltipHelper(string locTag, Vector3 worldPos, TooltipHelper.TooltipArrowPosition tooltipArrowPosition)
		{
			_tooltipHelper.ShowTooltip(locTag, worldPos, tooltipArrowPosition);
		}
	}
}