using Animancer;
using Cysharp.Threading.Tasks;
using FirstLight.Game.MonoComponent.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.Collection
{
	/// <summary>
	/// Used for spawning a new game object and playing a animation synchronously with the character one
	/// </summary>
	public class AdditionalFlairAnimation : MonoBehaviour
	{
		[SerializeField] private AssetReferenceGameObject _assetReference;
		[SerializeField] public AnimationClip _animationClip;
		private AnimancerComponent _animancer;
		private GameObject _animationObject;

		private void Start()
		{
			GetComponent<CharacterSkinMonoComponent>().OnTriggerFlair += OnOnTriggerFlair;
			_assetReference.InstantiateAsync(this.transform, false).ToUniTask().ContinueWith((a) =>
			{
				_animationObject = a;
				_animancer = a.GetComponent<AnimancerComponent>();
				_animationObject.SetActive(false);
			});
		}

		private void OnDestroy()
		{
			if (_assetReference != null && _assetReference.IsValid())
				_assetReference.ReleaseAsset();
		}

		private void OnOnTriggerFlair()
		{
			if (_animationObject)
			{
				TriggerAnimationAndDisable();
			}
		}

		public void TriggerAnimationAndDisable()
		{
			_animationObject.SetActive(true);
			var a = _animancer.Play(_animationClip);
			a.Events(this).OnEnd += () =>
			{
				if (!this) return;
				_animationObject.SetActive(false);
			};
		}
	}
}