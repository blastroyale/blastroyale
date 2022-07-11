using System;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable CheckNamespace

namespace FirstLight.UiService
{
	/// <summary>
	/// This view helper translate anchored views based on device safe area (screens witch a notch)
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class SafeAreaHelperView : MonoBehaviour
	{
		[Tooltip("If true then will only shift the anchor when the pivot is out of the safe area. If inside the safe area" +
		         "will remain in the same anchor position")]
		[SerializeField] private bool _checkAreaBounds = false;
		[SerializeField] private RectTransform _rectTransform;
		[SerializeField] private bool _ignoreHeight = false;
		[SerializeField] private bool _ignoreWidth = false;
		[SerializeField] private bool _onUpdate = false;
		[SerializeField] private Vector2 _refResolution;

		private Vector2 _initAnchoredPosition;
		private Vector2 _initSizeDelta;
		private Rect _resolution;
		private Rect _safeArea;

		internal void OnValidate()
		{
			try
			{
				_rectTransform = _rectTransform ? _rectTransform : GetComponent<RectTransform>();
				_refResolution = transform.root.GetComponent<CanvasScaler>().referenceResolution;
				_initAnchoredPosition = _rectTransform.anchoredPosition;
				_initSizeDelta = _rectTransform.sizeDelta;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
			
		}

		private void Awake()
		{
			_initAnchoredPosition = _rectTransform.anchoredPosition;
			_initSizeDelta = _rectTransform.sizeDelta;
			_resolution = new Rect(0,0, Screen.currentResolution.width, Screen.currentResolution.height);
			_safeArea = Screen.safeArea;
		}

		private void OnEnable()
		{
			UpdatePositions();
		}

		private void Update()
		{
			if (_onUpdate)
			{
				UpdatePositions();
			}
		}

		internal void UpdatePositions()
		{
			
#if UNITY_EDITOR
			// Because Unity Device Simulator and Game View have different screen resolution configs and sometimes use Desktop resolution
			_safeArea = Screen.safeArea;
			_resolution = new Rect(0, 0, Screen.width, Screen.height);
			_resolution = _resolution == _safeArea ? _resolution : new Rect(0,0, Screen.currentResolution.width, Screen.currentResolution.height); 
#endif

			if (_safeArea == _resolution)
			{
				return;
			}
			
			var anchorMax = _rectTransform.anchorMax;
			var anchorMin = _rectTransform.anchorMin;
			var anchoredPosition = _initAnchoredPosition;

			// Check if it is stretched on all sides
			if (anchorMin == Vector2.zero && anchorMax == Vector2.one)
			{
				AllStretchedAnchoring();
				return;
			}
			
			// Check if anchored to top or bottom
			if (!_ignoreHeight && Mathf.Approximately(anchorMax.y,anchorMin.y) && !CheckHeightAreaBounds(anchoredPosition))
			{
				// bottom
				if (anchorMax.y < Mathf.Epsilon)
				{
					anchoredPosition.y += (_safeArea.yMin - _resolution.yMin) * _refResolution.y / _resolution.height; 
				}
				else // top
				{
					anchoredPosition.y += (_safeArea.yMax - _resolution.yMax) * _refResolution.y / _resolution.height;
				}
			}
			
			// Check if anchored to left or right
			if (!_ignoreWidth && Mathf.Approximately(anchorMax.x,anchorMin.x) && !CheckWidthAreaBounds(anchoredPosition))
			{
				// left
				if (anchorMax.x < Mathf.Epsilon)
				{
					anchoredPosition.x += (_safeArea.xMin - _resolution.xMin) * _refResolution.x / _resolution.width;
				}
				else // right
				{
					anchoredPosition.x += (_safeArea.xMax - _resolution.xMax) * _refResolution.x / _resolution.width;
				}
			}
			
			_rectTransform.anchoredPosition = anchoredPosition;
		}
	
		private bool CheckWidthAreaBounds(Vector2 anchoredPosition)
		{
			return _checkAreaBounds && anchoredPosition.x > _safeArea.xMin && anchoredPosition.x < _safeArea.xMax;
		}
	
		private bool CheckHeightAreaBounds(Vector2 anchoredPosition)
		{
			return _checkAreaBounds && anchoredPosition.y > _safeArea.yMin && anchoredPosition.y < _safeArea.yMax;
		}

		private void AllStretchedAnchoring()
		{
			var anchoredPosition = _initAnchoredPosition;
			var sizeDelta = _initSizeDelta;
			
			// Check if anchored to top or bottom
			if (!_ignoreHeight && !CheckHeightAreaBounds(anchoredPosition))
			{
				var yDelta = (_safeArea.yMax - _resolution.yMax) * _refResolution.y / _resolution.height;
					
				anchoredPosition.y += yDelta / 2f;
				sizeDelta.y -= yDelta / 2f;
			}
			
			// Check if anchored to left or right
			if (!_ignoreWidth && !CheckWidthAreaBounds(anchoredPosition))
			{
				var xDelta = (_safeArea.xMin - _resolution.xMin) * _refResolution.x / _resolution.width;
					
				anchoredPosition.x += xDelta / 2f;
				sizeDelta.x -= xDelta / 2f;
			}
				
			_rectTransform.anchoredPosition = anchoredPosition;
			_rectTransform.sizeDelta = sizeDelta;
		}
	}
	
	#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(SafeAreaHelperView))]
	public class SafeAreaHelperViewEditor : UnityEditor.Editor 
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			if (GUILayout.Button("Update Anchored Data"))
			{
				var view = (SafeAreaHelperView) target;
				
				view.OnValidate();
			}

			if (GUILayout.Button("Update Anchored View"))
			{
				var view = (SafeAreaHelperView) target;
				
				view.UpdatePositions();
			}
		}
	}
	#endif
}