using Cysharp.Threading.Tasks;

namespace FirstLight.UIService
{
	public abstract class UIPresenterResult<T> : UIPresenter
	{
		private UniTaskCompletionSource<T> _resultTask;
		private T _cachedResult;

		internal override async UniTask OnScreenOpenedInternal(bool reload = false, UIService uiService = null)
		{
			await base.OnScreenOpenedInternal(reload, uiService);
			ResetResult();
		}

		public T GetResult()
		{
			return _cachedResult;
		}

		protected void SetResult(T result)
		{
			_cachedResult = result;
			_resultTask.TrySetResult(result);
		}

		public UniTask<T> WaitForResult()
		{
			return _resultTask.Task;
		}

		public void ResetResult()
		{
			_resultTask = new ();
			_cachedResult = default;
		}
	}
}