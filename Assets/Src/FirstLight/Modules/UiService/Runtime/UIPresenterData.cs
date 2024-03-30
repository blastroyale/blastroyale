namespace FirstLight.UIService
{
	public abstract class UIPresenterData<T> : UIPresenter where T : class
	{
		protected new T Data => (T) base.Data;
	}
}