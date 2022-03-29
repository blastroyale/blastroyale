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
		[SerializeField] private TextMeshProUGUI _text;
		
		[Header("Balancing params for the positioning/scaling during the float sequence")]
		[SerializeField] private float _maxOffsetX = 50f;
		[SerializeField] private float _minOffsetY = 50f;
		[SerializeField] private float _maxOffsetY = 75f;
		[SerializeField] private AnimationCurve _yPositionAnimCurve;
		[SerializeField] private AnimationCurve _scaleCurve;
		
		
		[Header("Objects that will be affected by the float sequence")]
		[SerializeField] private RectTransform[] _objectsToFloat;
		
		private float _offsetX;
		private float _offsetY;
		private Tweener _tweener;

		public void Play(string text, Color color, float duration)
		{
			_offsetX = Random.Range(-_maxOffsetX, _maxOffsetX);
			_offsetY = Random.Range(_minOffsetY, _maxOffsetY);
			
			_text.text = text;
			_text.color = color;

			for (int i = 0; i < _objectsToFloat.Length; i++)
			{
				_objectsToFloat[i].anchoredPosition = Vector2.zero;
				_objectsToFloat[i].localScale = Vector3.one;
			}

			StartCoroutine(FloatSequence(duration));
		}

		/// <summary>
		/// Makes target objects float based on positioning/scaling curves and offsets, over a duration
		/// </summary>
		private IEnumerator FloatSequence(float totalDuration)
		{
			var currentDuration = 0f;

			while (currentDuration < totalDuration)
			{
				var currentDurationPercent = currentDuration / totalDuration;
				
				var yPosProgress = _yPositionAnimCurve.Evaluate(currentDurationPercent);
				var yPos = Mathf.Lerp(0, _offsetY, yPosProgress);
				var xPos = Mathf.Lerp(0, _offsetX, currentDurationPercent);
				var scale = _scaleCurve.Evaluate(currentDurationPercent);

				for (int i = 0; i < _objectsToFloat.Length; i++)
				{
					_objectsToFloat[i].anchoredPosition = new Vector2(xPos, yPos);
					_objectsToFloat[i].localScale = new Vector3(scale, scale, 1f);
				}

				yield return new WaitForEndOfFrame();

				currentDuration += Time.deltaTime;
			}
		}
	}
}