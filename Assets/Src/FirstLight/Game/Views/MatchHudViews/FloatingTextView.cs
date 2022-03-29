using System;
using System.Collections;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// This view displays floating text and plays a legacy animation.
	/// </summary>
	public class FloatingTextView : MonoBehaviour
	{
		[SerializeField] private RectTransform _rectTransform;
		[SerializeField] private float _maxOffsetX = 50f;
		[SerializeField] private float _minOffsetY = 50f;
		[SerializeField] private float _maxOffsetY = 75f;
		[SerializeField] private AnimationCurve _yPositionAnimCurve;
		[SerializeField] private AnimationCurve _scaleCurve;
		[SerializeField] private TextMeshProUGUI _text;
		[SerializeField] private RectTransform[] _objectsToFloat;
		private float _offsetX;
		private float _offsetY;
		
		private void Awake()
		{
			_offsetX = Random.Range(-_maxOffsetX, _maxOffsetX);
			_offsetY = Random.Range(_minOffsetY, _maxOffsetY);
			_rectTransform = GetComponent<RectTransform>();
		}

		/// <summary>
		/// Plays the floating text animation with the given <paramref name="clip"/> and information
		/// </summary>
		public void Play(string text, Color color, float duration)
		{
			_text.text = text;
			_text.color = color;

			for (int i = 0; i < _objectsToFloat.Length; i++)
			{
				_objectsToFloat[i].anchoredPosition = Vector2.zero;
				_objectsToFloat[i].localScale = Vector3.one;
			}

			Tweener meme = DOVirtual.Float(0, 1f, duration, (float progressPercent) =>
			{
				float yPosProgress = _yPositionAnimCurve.Evaluate(progressPercent);
				float yPos = Mathf.Lerp(0, _offsetY, yPosProgress);
				float xPos = Mathf.Lerp(0, _offsetX, progressPercent);
				float scale = _scaleCurve.Evaluate(progressPercent);
				
				for (int i = 0; i < _objectsToFloat.Length; i++)
				{
					Vector2 currentPos = _objectsToFloat[i].anchoredPosition;
					_objectsToFloat[i].anchoredPosition = new Vector2(xPos, yPos);
					_objectsToFloat[i].localScale = new Vector3(scale, scale, 1f);
				}
				
			});
		}
	}
}