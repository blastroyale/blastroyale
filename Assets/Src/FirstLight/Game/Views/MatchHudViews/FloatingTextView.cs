using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// This view displays floating text and plays a legacy animation.
	/// </summary>
	public class FloatingTextView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _text;
		[SerializeField, Required] private Image _icon;

		[Header("Balancing params for the positioning/scaling during the float sequence")] [SerializeField]
		private float _minDuration = 1f;

		[SerializeField] private float _maxDuration = 1.75f;
		[SerializeField] private float _maxOffsetX = 50f;
		[SerializeField] private float _minOffsetY = 50f;
		[SerializeField] private float _maxOffsetY = 75f;
		[SerializeField, Required] private AnimationCurve _yPositionAnimCurve;
		[SerializeField, Required] private AnimationCurve _scaleCurve;

		[Header("Objects that will be affected by the float sequence")] [SerializeField]
		private RectTransform[] _objectsToFloat;


		private Tweener _tweener;

		/// <summary>
		/// Makes target object float based on positioning/scaling curves and offsets, over a duration.
		/// </summary>
		/// <returns>Duration of the sequence that will play.</returns>
		public float Play(string text, Color color, Sprite icon)
		{
			var offsetX = Random.Range(-_maxOffsetX, _maxOffsetX);
			var offsetY = Random.Range(_minOffsetY, _maxOffsetY);
			var duration = Random.Range(_minDuration, _maxDuration);

			_text.text = text;
			_text.color = color;
			_icon.sprite = icon;

			foreach (var rt in _objectsToFloat)
			{
				rt.anchoredPosition = Vector2.zero;
				rt.localScale = Vector3.one;
			}

			StartCoroutine(FloatSequence(duration, offsetX, offsetY));

			return duration;
		}

		private IEnumerator FloatSequence(float totalDuration, float offsetX, float offsetY)
		{
			var currentDuration = 0f;

			while (currentDuration < totalDuration)
			{
				var currentDurationPercent = currentDuration / totalDuration;

				var yPosProgress = _yPositionAnimCurve.Evaluate(currentDurationPercent);
				var yPos = Mathf.Lerp(0, offsetY, yPosProgress);
				var xPos = Mathf.Lerp(0, offsetX, currentDurationPercent);
				var scale = _scaleCurve.Evaluate(currentDurationPercent);

				for (int i = 0; i < _objectsToFloat.Length; i++)
				{
					_objectsToFloat[i].anchoredPosition = new Vector2(xPos, yPos);
					_objectsToFloat[i].localScale = new Vector3(scale, scale, 1f);
				}

				yield return null;

				currentDuration += Time.deltaTime;
			}
		}
	}
}