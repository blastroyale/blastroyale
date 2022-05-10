using System.Collections;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
    public enum TooltipArrowPosition
    {
        Top,
        Bottom
    }
    
	/// /// <summary>
	/// This Presenter handles the tooltips for the UI 
	/// </summary>
	public class UiTooltipPresenter : UiPresenter
	{
        [SerializeField, Required] private Transform _tooltipHelperTransform;
        [SerializeField, Required] private RectTransform _rectTransform;
        [SerializeField, Required] private Canvas _canvas;
        [SerializeField, Required] private Animator _animator;
        [SerializeField, Required] private TextMeshProUGUI _tooltipText;
        [SerializeField, Required] private RectTransform _tooltipArrow;
        [SerializeField, Required] private CanvasGroup _canvasGroup;
        
        [SerializeField] private int _arrowYOffset;
        
        private bool _isActive;
        
        /// <summary>
        /// Show a tool tip graphic at world and arrow position
        /// </summary>
        public void ShowTooltip(string locTag, Vector3 worldPosition, TooltipArrowPosition tooltipArrowPosition)
        {
            _canvas.enabled = false;
            _isActive = false;
            _canvasGroup.alpha = 0;
            StartCoroutine(SetActiveCoroutine(locTag, worldPosition, tooltipArrowPosition));
        }

        private void SetTooltipPosition(Vector3 worldPosition, TooltipArrowPosition position)
        {
            _tooltipHelperTransform.position = worldPosition;
            
            if (position == TooltipArrowPosition.Top)
            {
                _tooltipHelperTransform.localPosition += new Vector3(0f, -(_rectTransform.rect.height + _tooltipArrow.rect.height), 0f);
            }
        }

        private void SetArrowPosition(Vector3 worldPosition, TooltipArrowPosition tooltipArrowPosition)
        {
            switch (tooltipArrowPosition)
            {
                case TooltipArrowPosition.Top:
                    _tooltipArrow.anchorMax = new Vector3(0.5f, 1f);
                    _tooltipArrow.anchorMin = new Vector3(0.5f, 1f);
                    _tooltipArrow.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 180f));
                    _tooltipArrow.anchoredPosition = new Vector3(0f, -_arrowYOffset);
                    break;
                case TooltipArrowPosition.Bottom:
                    _tooltipArrow.anchorMax = new Vector3(0.5f, 0f);
                    _tooltipArrow.anchorMin = new Vector3(0.5f, 0f);
                    _tooltipArrow.localRotation = Quaternion.identity;
                    _tooltipArrow.anchoredPosition = new Vector3(0f, _arrowYOffset);
                    break;
            }

            _tooltipArrow.position = new Vector3(worldPosition.x, _tooltipArrow.position.y, _tooltipArrow.position.z);
        }

        private IEnumerator SetActiveCoroutine(string locTag, Vector3 worldPosition, TooltipArrowPosition tooltipArrowPosition)
        {
            _tooltipText.text = locTag;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) _tooltipHelperTransform.parent);

            yield return new WaitForEndOfFrame();

            SetTooltipPosition(worldPosition, tooltipArrowPosition);
            ClampToWindow();
            
            SetArrowPosition(worldPosition, tooltipArrowPosition);

            _canvas.enabled = true;
            
            if (gameObject.activeSelf)
            {
                _animator.SetTrigger("Show");
            }
            
            _isActive = true;
        }

        private void HideTooltip()
        {
            _canvas.enabled = false;
            _isActive = false;
            _canvasGroup.alpha = 1;
        }

        private void Update()
        {
            if (_isActive && UnityEngine.Input.GetMouseButtonUp(0))
            {
                HideTooltip();
            }
        }

        private void ClampToWindow()
        {
            var parentRectTransform = (RectTransform) _tooltipHelperTransform.parent;
            
            var minPosition = parentRectTransform.rect.min - _rectTransform.rect.min;
            var maxPosition = parentRectTransform.rect.max - _rectTransform.rect.max;

            var pos = _rectTransform.localPosition;
            pos.x = Mathf.Clamp(_rectTransform.localPosition.x, minPosition.x, maxPosition.x);
            pos.y = Mathf.Clamp(_rectTransform.localPosition.y, minPosition.y, maxPosition.y);

            _rectTransform.localPosition = pos;
        }
    }
}