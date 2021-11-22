using System;
using System.Collections;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	/// <remarks>
	/// Allows this Presenter to have an intro and outro animation when opened and closed to provide feedback and joy for players.
	/// </remarks>
	public abstract class AnimatedUiPresenter : UiCloseActivePresenter
	{
		[SerializeField] protected Animation _animation;
		[SerializeField] protected AnimationClip _introAnimationClip;
		[SerializeField] protected AnimationClip _outroAnimationClip;

		protected IGameServices Services;
		protected bool IsOpenedComplete;
		protected bool IsClosedComplete;

		private Coroutine _coroutine; 

		private void OnValidate()
		{
			Debug.Assert(_animation != null, $"Presenter {gameObject.name} does not have a referenced Animation");
			OnEditorValidate();
		}

		protected override void OnInitialized()
		{
			Services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void OnClosed()
		{
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
			}

			_animation.clip = _outroAnimationClip;
			_animation.Play();
			IsOpenedComplete = false;
			IsClosedComplete = false;

			_coroutine = StartCoroutine(AnimationCoroutine(OnComplete));

			void OnComplete()
			{
				IsClosedComplete = true;
				
				gameObject.SetActive(false);
				OnClosedCompleted();
			}
		}

		protected override void OnOpened()
		{
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
			}

			_animation.clip = _introAnimationClip;
			_animation.Play();
			IsOpenedComplete = false;
			IsClosedComplete = false;

			_coroutine = StartCoroutine(AnimationCoroutine(OnComplete));

			void OnComplete()
			{
				IsOpenedComplete = true;
				OnOpenedCompleted();
			}
		}

		/// <summary>
		/// Called in the end of this object MonoBehaviour's OnValidate() -> <see cref="OnValidate"/>.
		/// Override this method to have your custom extra validation.
		/// </summary>
		/// <remarks>
		/// This is Editor only call.
		/// </remarks>
		protected virtual void OnEditorValidate() { }

		/// <summary>
		/// Called in the end of this object's <see cref="OnOpened"/>.
		/// Override this method to have your custom extra execution when the presenter is opened.
		/// </summary>
		protected virtual void OnOpenedCompleted() { }

		/// <summary>
		/// Called in the end of this object's <see cref="OnClosed"/>.
		/// Override this method to have your custom extra execution when the presenter is closed.
		/// </summary>
		protected virtual void OnClosedCompleted() { }

		private IEnumerator AnimationCoroutine(Action execute)
		{
			yield return new WaitForSeconds(_animation.clip.length);
			
			execute.Invoke();
		}
	}

	/// <inheritdoc />
	/// <remarks>
	/// Allows this Presenter to have an intro and outro animation when opened and closed to provide feedback and joy for players.
	/// </remarks>
	[RequireComponent(typeof(CanvasGroup))]
	public abstract class AnimatedUiPresenterData<T> : UiCloseActivePresenterData<T> where T : struct
	{
		[SerializeField] protected Animation _animation;
		[SerializeField] protected AnimationClip _introAnimationClip;
		[SerializeField] protected AnimationClip _outroAnimationClip;

		protected IGameServices Services;
		protected bool IsOpenedComplete;
		protected bool IsClosedComplete;

		private Coroutine _coroutine; 

		private void OnValidate()
		{
			Debug.Assert(_animation != null, $"Presenter {gameObject.name} does not have a referenced Animation");
			OnEditorValidate();
		}

		protected override void OnInitialized()
		{
			Services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void OnClosed()
		{
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
			}
			
			_animation.clip = _outroAnimationClip;
			_animation.Play();
			IsOpenedComplete = false;
			IsClosedComplete = false;

			_coroutine = StartCoroutine(AnimationCoroutine(OnComplete));

			void OnComplete()
			{
				IsClosedComplete = true;
				
				gameObject.SetActive(false);
				OnClosedCompleted();
			}
		}

		protected override void OnOpened()
		{
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
			}
			
			_animation.clip = _introAnimationClip;
			_animation.Play();
			IsOpenedComplete = false;
			IsClosedComplete = false;

			_coroutine = StartCoroutine(AnimationCoroutine(OnComplete));

			void OnComplete()
			{
				IsOpenedComplete = true;
				OnOpenedCompleted();
			}
		}
		
		/// <inheritdoc cref="AnimatedUiPresenter.OnEditorValidate"/>
		protected virtual void OnEditorValidate() { }

		/// <inheritdoc cref="AnimatedUiPresenter.OnOpenedCompleted"/>
		protected virtual void OnOpenedCompleted() { }

		/// <inheritdoc cref="AnimatedUiPresenter.OnClosedCompleted"/>
		protected virtual void OnClosedCompleted() { }

		private IEnumerator AnimationCoroutine(Action execute)
		{
			yield return new WaitForSeconds(_animation.clip.length);
			
			execute.Invoke();

			_coroutine = null;
		}
	}
}
	
