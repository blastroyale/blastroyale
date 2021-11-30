using FirstLight.Game.Utils;
using SRF;
using UnityEngine;

namespace FirstLight.Game.Views.TooltipView
{
    /// <summary>
    /// This class manages registering, building and showing of UI tooltips.
    /// </summary>
    public class TooltipManager : MonoSingleton<TooltipManager>
    {
        [SerializeField] private GameObject _tooltipHelperPrefab;
        
        private TooltipHelper _tooltipHelper;
 
        
        protected override void _Start()
        {
            var go  = Instantiate(_tooltipHelperPrefab);
            _tooltipHelper = go.GetComponent<TooltipHelper>();
            _tooltipHelper.transform.SetParent(transform);
            _tooltipHelper.transform.ResetLocal();
        }
        
        public void ShowTooltipHelper(string locTag, Vector3 worldPos, TooltipHelper.TooltipArrowPosition tooltipArrowPosition)
        {
            _tooltipHelper.ShowTooltip(locTag, worldPos, tooltipArrowPosition);
        }
    }
}


