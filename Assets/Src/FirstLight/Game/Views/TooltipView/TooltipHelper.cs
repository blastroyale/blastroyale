using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;


namespace FirstLight.Game.Views.TooltipView
{
    /// <summary>
    /// This component controls tooltip on screen positioning and animation.
    /// </summary>
    public class TooltipHelper : MonoBehaviour
    {
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Animator _animator;
        [SerializeField] private TextMeshProUGUI _tooltipText;
        [SerializeField] private RectTransform _tooltipArrow;
        [SerializeField] private int _arrowYOffset;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private LayoutElement _layoutElement;

        private bool _isActive;
        
        public enum TooltipArrowPosition
        {
            Top,
            Bottom
        }

        private void OnValidate()
        {
            _rectTransform = _rectTransform ? _rectTransform : GetComponent<RectTransform>();
        }
        
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
            transform.position = worldPosition;
            
            if (position == TooltipArrowPosition.Top)
            {
                transform.localPosition += new Vector3(0f, -(_rectTransform.rect.height + _tooltipArrow.rect.height), 0f);
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
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) transform.parent);

            yield return new WaitForEndOfFrame();

            SetTooltipPosition(worldPosition, tooltipArrowPosition);
            ClampToWindow();
            
            SetArrowPosition(worldPosition, tooltipArrowPosition);

            _canvas.enabled = true;
            
            if (gameObject.activeSelf)
            {
                _animator.SetTrigger("Show");
            }

            yield return new WaitForEndOfFrame();
            
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
            var parentRectTransform = (RectTransform) transform.parent;
            
            var minPosition = parentRectTransform.rect.min - _rectTransform.rect.min;
            var maxPosition = parentRectTransform.rect.max - _rectTransform.rect.max;

            var pos = _rectTransform.localPosition;
            pos.x = Mathf.Clamp(_rectTransform.localPosition.x, minPosition.x, maxPosition.x);
            pos.y = Mathf.Clamp(_rectTransform.localPosition.y, minPosition.y, maxPosition.y);

            _rectTransform.localPosition = pos;
        }
    }
}
