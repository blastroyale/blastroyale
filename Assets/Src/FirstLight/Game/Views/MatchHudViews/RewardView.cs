using System;
using System.Collections;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.AdventureHudViews
{
	/// <summary>
	/// Handles a specific reward object on the <see cref="RewardScreenPresenter"/>. A reward can be a currency or piece of equipment.
	/// </summary>
	public class RewardView : MonoBehaviour
	{
		[SerializeField] private Image _image;
		[SerializeField] private Animation _animation;
		[SerializeField] private AnimationClip _appearAnimationClip;
		[SerializeField] private AnimationClip _holdAnimationClip;
		[SerializeField] private AnimationClip _unpackAnimationClip;
		[SerializeField] private AnimationClip _summaryAnimationClip;
		[SerializeField] private TextMeshProUGUI _quantityText;
		[SerializeField] private TextMeshProUGUI _itemNameText;

		public Action OnRewardAnimationComplete;
		public Action OnSummaryAnimationComplete;

		private IGameServices _services;
		private Coroutine _rewardCoroutine;
		private Coroutine _summaryCoroutine;

		/// <summary>
		/// Requests the information if the reward view is playing
		/// </summary>
		public bool IsPlaying => _animation.isPlaying || _rewardCoroutine != null || _summaryCoroutine != null;

		/// <summary>
		/// Initializes the object with the given <paramref name="gameId"/> & <paramref name="quantity"/> data to show
		/// </summary>
		public async void Initialise(GameId gameId, uint quantity)
		{
			_services ??= MainInstaller.Resolve<IGameServices>();
			_image.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(gameId);
			_image.enabled = true;
			
			_itemNameText.SetText(gameId.GetTranslation());
			_quantityText.SetText($"x{quantity.ToString()}");
			gameObject.SetActive(true);
		}
		
		/// <summary>
		/// Rewinds this view to it's original state
		/// </summary>
		public void Rewind(RectTransform reference)
		{
			var rectTransform = GetComponent<RectTransform>();
			
			transform.SetParent(reference.transform.parent);
			gameObject.SetActive(false);

			rectTransform.anchorMin = reference.anchorMin;
			rectTransform.anchorMax = reference.anchorMax;
			transform.position = reference.position;
		}

		/// <summary>
		/// Starts the Reward Animation sequence for this object. 
		/// </summary>
		public void StartRewardSequence()
		{
			gameObject.SetActive(true);
			
			_rewardCoroutine = StartCoroutine(PlayRewardAnimation());
		}

		/// <summary>
		/// Starts the Summary Animation sequence for this object.
		/// </summary>
		public void StartSummarySequence()
		{
			gameObject.SetActive(true);
			
			_summaryCoroutine = StartCoroutine(PlaySummaryAnimation());
		}

		/// <summary>
		/// If the player skips the animation early, we want to sop it's coroutine and reset any transforms.
		/// </summary>
		public void EndAnimationEarly()
		{
			if (_rewardCoroutine != null)
			{
				StopCoroutine(_rewardCoroutine);
				FinalRewardAnimation();
			} 
			else if (_summaryCoroutine != null)
			{
				StopCoroutine(_summaryCoroutine);
				FinalSummaryAnimation();
			}
			
			_animation.Stop();
		}

		private IEnumerator PlaySummaryAnimation()
		{
			transform.rotation = new Quaternion(0, 0, 0, 0);
			_animation.clip = _summaryAnimationClip;
			_animation.Play();

			yield return new WaitForSeconds(_animation.clip.length);

			FinalSummaryAnimation();
		}

		private IEnumerator PlayRewardAnimation()
		{
			transform.rotation = new Quaternion(0, 0, 0, 0);
			_animation.clip = _appearAnimationClip;
			_animation.Play();

			yield return new WaitForSeconds(_animation.clip.length);

			_animation.clip = _holdAnimationClip;
			_animation.Play();

			yield return new WaitForSeconds(_animation.clip.length);

			_animation.clip = _unpackAnimationClip;
			_animation.Play();

			yield return new WaitForSeconds(_animation.clip.length);

			FinalRewardAnimation();
		}

		private void FinalRewardAnimation()
		{
			_rewardCoroutine = null;
			gameObject.SetActive(false);
			OnRewardAnimationComplete();
		}

		private void FinalSummaryAnimation()
		{
			_summaryCoroutine = null;
			OnSummaryAnimationComplete();
		}
	}
}

